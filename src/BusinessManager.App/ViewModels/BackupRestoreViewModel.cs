using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using BusinessManager.Application.Services;
using BusinessManager.Domain.Interfaces;
using BusinessManager.Domain.Entities;
using BusinessManager.Domain.DTOs;

namespace BusinessManager.App.ViewModels;

public partial class BackupRestoreViewModel : ObservableObject
{
    private readonly IBackupService _backupService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<BackupRestoreViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<BackupInfo> _backups = new();

    [ObservableProperty]
    private BackupInfo? _selectedBackup;

    [ObservableProperty]
    private string _backupDescription = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isCreatingBackup;

    [ObservableProperty]
    private bool _isRestoring;

    [ObservableProperty]
    private string _restoreFilePath = string.Empty;

    [ObservableProperty]
    private int _backupRetentionDays = 30;

    public BackupRestoreViewModel(
        IBackupService backupService,
        INotificationService notificationService,
        ILogger<BackupRestoreViewModel> logger)
    {
        _backupService = backupService;
        _notificationService = notificationService;
        _logger = logger;

        LoadBackupsCommand = new RelayCommand(async () => await LoadBackupsAsync());
        CreateBackupCommand = new RelayCommand(async () => await CreateBackupAsync(), CanCreateBackup);
        RestoreBackupCommand = new RelayCommand<BackupInfo>(async (backup) => await RestoreBackupAsync(backup));
        RestoreFromFileCommand = new RelayCommand(async () => await RestoreFromFileAsync(), CanRestoreFromFile);
        DeleteBackupCommand = new RelayCommand<BackupInfo>(async (backup) => await DeleteBackupAsync(backup));
        DownloadBackupCommand = new RelayCommand<BackupInfo>(async (backup) => await DownloadBackupAsync(backup));
        BrowseFileCommand = new RelayCommand(BrowseFile);
        RefreshCommand = new RelayCommand(async () => await LoadBackupsAsync());
    }

    public IRelayCommand LoadBackupsCommand { get; }
    public IRelayCommand CreateBackupCommand { get; }
    public IRelayCommand<BackupInfo> RestoreBackupCommand { get; }
    public IRelayCommand RestoreFromFileCommand { get; }
    public IRelayCommand<BackupInfo> DeleteBackupCommand { get; }
    public IRelayCommand<BackupInfo> DownloadBackupCommand { get; }
    public IRelayCommand BrowseFileCommand { get; }
    public IRelayCommand RefreshCommand { get; }

    public async Task InitializeAsync()
    {
        await LoadBackupsAsync();
        await LoadSettingsAsync();
    }

    private async Task LoadBackupsAsync()
    {
        try
        {
            IsLoading = true;
            var backups = await _backupService.GetBackupsAsync();
            Backups = new ObservableCollection<BackupInfo>(backups.OrderByDescending(b => b.CreatedAt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading backups");
            _notificationService.ShowError("Error loading backups");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            // Load backup retention days from settings
            BackupRetentionDays = 30; // Default value
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading backup settings");
        }
    }

    private bool CanCreateBackup()
    {
        return !IsCreatingBackup && !string.IsNullOrWhiteSpace(BackupDescription);
    }

    private async Task CreateBackupAsync()
    {
        try
        {
            if (!CanCreateBackup()) return;

            IsCreatingBackup = true;

            var backupRequest = new CreateBackupRequest
            {
                Description = BackupDescription,
                IncludeUsers = true,
                IncludeSettings = true,
                IncludeData = true
            };

            var backup = await _backupService.CreateBackupAsync(backupRequest);
            _notificationService.ShowSuccess("Backup created successfully");
            
            BackupDescription = string.Empty;
            await LoadBackupsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating backup");
            _notificationService.ShowError("Error creating backup");
        }
        finally
        {
            IsCreatingBackup = false;
        }
    }

    private async Task RestoreBackupAsync(BackupInfo? backup)
    {
        try
        {
            if (backup == null) return;

            if (!_notificationService.ShowConfirmation(
                $"Are you sure you want to restore backup from {backup.CreatedAt:dd/MM/yyyy HH:mm}?\n\n" +
                "This will overwrite current data and cannot be undone!"))
                return;

            IsRestoring = true;

            await _backupService.RestoreBackupAsync(backup.FilePath);
            _notificationService.ShowSuccess("Backup restored successfully. Please restart the application.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring backup");
            _notificationService.ShowError("Error restoring backup");
        }
        finally
        {
            IsRestoring = false;
        }
    }

    private bool CanRestoreFromFile()
    {
        return !IsRestoring && !string.IsNullOrWhiteSpace(RestoreFilePath) && 
               System.IO.File.Exists(RestoreFilePath);
    }

    private async Task RestoreFromFileAsync()
    {
        try
        {
            if (!CanRestoreFromFile()) return;

            if (!_notificationService.ShowConfirmation(
                $"Are you sure you want to restore backup from {RestoreFilePath}?\n\n" +
                "This will overwrite current data and cannot be undone!"))
                return;

            IsRestoring = true;

            await _backupService.RestoreBackupAsync(RestoreFilePath);
            _notificationService.ShowSuccess("Backup restored successfully. Please restart the application.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring backup from file");
            _notificationService.ShowError("Error restoring backup from file");
        }
        finally
        {
            IsRestoring = false;
        }
    }

    private async Task DeleteBackupAsync(BackupInfo? backup)
    {
        try
        {
            if (backup == null) return;

            if (!_notificationService.ShowConfirmation($"Are you sure you want to delete backup from {backup.CreatedAt:dd/MM/yyyy HH:mm}?"))
                return;

            await _backupService.DeleteBackupAsync(backup.Id);
            _notificationService.ShowSuccess("Backup deleted successfully");
            await LoadBackupsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting backup");
            _notificationService.ShowError("Error deleting backup");
        }
    }

    private async Task DownloadBackupAsync(BackupInfo? backup)
    {
        try
        {
            if (backup == null) return;

            // Open the backup file location
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{backup.FilePath}\""
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening backup location");
            _notificationService.ShowError("Error opening backup location");
        }
    }

    private void BrowseFile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Backup File",
            Filter = "Backup Files (*.bak)|*.bak|All Files (*.*)|*.*",
            FilterIndex = 1
        };

        if (dialog.ShowDialog() == true)
        {
            RestoreFilePath = dialog.FileName;
        }
    }
}
