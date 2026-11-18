using System;
using System.Globalization;

public static class TrainingDaySchedule
{
    public static string GetDayKey(DateTime localNow)
    {
        return GetDay(localNow).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public static bool IsAvailable(string completedDay, DateTime localNow)
    {
        if (!DateTime.TryParseExact(completedDay, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var savedDay))
        {
            return true;
        }

        return GetDay(localNow) > savedDay;
    }

    private static DateTime GetDay(DateTime localNow) =>
        localNow.Hour < 5 ? localNow.Date.AddDays(-1) : localNow.Date;
}
