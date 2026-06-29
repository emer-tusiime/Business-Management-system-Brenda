using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using BusinessManager.Domain.DTOs;
using BusinessManager.Domain.Interfaces;

namespace BusinessManager.App.Services;

public class NotificationCenter
{
    private readonly IClientOrderService _orderService;
    private readonly DispatcherTimer _timer;
    private int _nextId = 1;

    public ObservableCollection<AppNotification> Notifications { get; } = new();
    public int UnreadCount => Notifications.Count(n => !n.IsRead);

    public event Action? CountChanged;

    public NotificationCenter(IClientOrderService orderService)
    {
        _orderService = orderService;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _timer.Tick += async (_, _) => await RefreshAsync();
        _timer.Start();
    }

    public async Task RefreshAsync()
    {
        try
        {
            var overdue = await _orderService.GetOverdueAsync();
            var dueToday = await _orderService.GetDueTodayAsync();

            var fresh = new List<AppNotification>();

            foreach (var o in overdue)
                fresh.Add(new AppNotification
                {
                    Id = _nextId++,
                    Title = "Overdue Order",
                    Message = $"{o.ClientName} — \"{o.Description}\" was due {o.PickupDate:ddd d MMM}",
                    Kind = NotificationKind.Urgent,
                    CreatedAt = DateTime.Now
                });

            foreach (var o in dueToday)
                fresh.Add(new AppNotification
                {
                    Id = _nextId++,
                    Title = "Pickup Today",
                    Message = $"{o.ClientName} — \"{o.Description}\" is due today",
                    Kind = NotificationKind.Warning,
                    CreatedAt = DateTime.Now
                });

            // Replace list, preserve IsRead on items that already exist
            var prev = Notifications.ToDictionary(n => n.Message);
            Notifications.Clear();
            foreach (var n in fresh)
            {
                if (prev.TryGetValue(n.Message, out var existing))
                    n.IsRead = existing.IsRead;
                Notifications.Add(n);
            }

            CountChanged?.Invoke();
        }
        catch { /* background — swallow silently */ }
    }

    public void MarkAllRead()
    {
        foreach (var n in Notifications) n.IsRead = true;
        CountChanged?.Invoke();
    }
}
