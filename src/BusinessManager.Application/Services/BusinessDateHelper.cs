using System;

namespace BusinessManager.Application.Services;

public static class BusinessDateHelper
{
    public static (DateTime Start, DateTime End) GetLocalDayRange(DateTime date)
    {
        var start = date.Date;
        var end = start.AddDays(1).AddTicks(-1);
        return (start, end);
    }

    public static (DateTime Start, DateTime End) GetLocalWeekRange(DateTime date)
    {
        var dayOfWeek = (int)date.DayOfWeek;
        var start = date.Date.AddDays(-dayOfWeek);
        var end = start.AddDays(7).AddTicks(-1);
        return (start, end);
    }
}
