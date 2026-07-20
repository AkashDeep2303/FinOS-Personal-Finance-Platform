namespace FinOS.Common.Helpers;

/// <summary>
/// Date and time utility methods for financial calculations,
/// supporting both standard calendar days and business-day conventions.
/// </summary>
public static class DateTimeHelper
{
    /// <summary>
    /// Returns the current UTC timestamp.
    /// </summary>
    public static DateTime UtcNow => DateTime.UtcNow;

    /// <summary>
    /// Converts a <see cref="DateTime"/> to an ISO-8601 string (yyyy-MM-ddTHH:mm:ss.fffZ).
    /// </summary>
    public static string ToIso8601(DateTime dateTime)
    {
        return dateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
    }

    /// <summary>
    /// Returns the date portion only (yyyy-MM-dd).
    /// </summary>
    public static string ToDateString(DateTime dateTime)
    {
        return dateTime.ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// Calculates the number of calendar days between two dates (inclusive of start, exclusive of end).
    /// </summary>
    public static int DaysBetween(DateTime start, DateTime end)
    {
        return (end.Date - start.Date).Days;
    }

    /// <summary>
    /// Calculates the number of business (working) days between two dates,
    /// excluding weekends (Saturday and Sunday).
    /// </summary>
    public static int BusinessDaysBetween(DateTime start, DateTime end)
    {
        if (start.Date > end.Date)
        {
            return -BusinessDaysBetween(end, start);
        }

        int totalDays = DaysBetween(start, end);
        int fullWeeks = totalDays / 7;
        int remainingDays = totalDays % 7;

        int businessDays = fullWeeks * 5;

        // Process remaining days
        for (int i = 0; i < remainingDays; i++)
        {
            var day = start.Date.AddDays(i).DayOfWeek;
            if (day != DayOfWeek.Saturday && day != DayOfWeek.Sunday)
            {
                businessDays++;
            }
        }

        return businessDays;
    }

    /// <summary>
    /// Adds the specified number of business days to a date, skipping weekends.
    /// </summary>
    public static DateTime AddBusinessDays(DateTime date, int businessDays)
    {
        if (businessDays == 0) return date;

        int direction = businessDays > 0 ? 1 : -1;
        int remaining = Math.Abs(businessDays);
        var current = date.Date;

        while (remaining > 0)
        {
            current = current.AddDays(direction);
            if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
            {
                remaining--;
            }
        }

        return current;
    }

    /// <summary>
    /// Returns the next business day on or after the given date.
    /// If the date falls on a weekend, Monday is returned.
    /// </summary>
    public static DateTime NextBusinessDay(DateTime date)
    {
        var current = date.Date;
        while (current.DayOfWeek == DayOfWeek.Saturday || current.DayOfWeek == DayOfWeek.Sunday)
        {
            current = current.AddDays(1);
        }
        return current;
    }

    /// <summary>
    /// Returns whether the given date falls on a weekend.
    /// </summary>
    public static bool IsWeekend(DateTime date)
    {
        return date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    }

    /// <summary>
    /// Returns whether the given date is a valid financial settlement date
    /// (not a weekend, not in the past).
    /// </summary>
    public static bool IsValidSettlementDate(DateTime date)
    {
        return !IsWeekend(date) && date.Date >= DateTime.UtcNow.Date;
    }

    /// <summary>
    /// Calculates the day-count fraction between two dates using the Actual/365 Fixed convention.
    /// Commonly used in interest rate calculations.
    /// </summary>
    public static decimal Actual365Fixed(DateTime start, DateTime end)
    {
        return (decimal)DaysBetween(start, end) / 365m;
    }

    /// <summary>
    /// Calculates the day-count fraction using the 30/360 convention.
    /// Each month is treated as 30 days and each year as 360 days.
    /// </summary>
    public static decimal ThirtyOver360(DateTime start, DateTime end)
    {
        int d1 = Math.Min(start.Day, 30);
        int d2 = end.Day;

        // If d1 is 30, cap d2 at 30
        if (d1 == 30)
        {
            d2 = Math.Min(d2, 30);
        }

        int days = (360 * (end.Year - start.Year)) + (30 * (end.Month - start.Month)) + (d2 - d1);
        return days / 360m;
    }

    /// <summary>
    /// Returns the start of the financial year for a given date.
    /// Defaults to January 1st; adjust <paramref name="fyStartMonth"/> for other start months.
    /// </summary>
    public static DateTime StartOfFinancialYear(DateTime date, int fyStartMonth = 1)
    {
        int year = date.Month >= fyStartMonth ? date.Year : date.Year - 1;
        return new DateTime(year, fyStartMonth, 1);
    }

    /// <summary>
    /// Returns the end of the financial year for a given date.
    /// </summary>
    public static DateTime EndOfFinancialYear(DateTime date, int fyStartMonth = 1)
    {
        return StartOfFinancialYear(date, fyStartMonth).AddYears(1).AddDays(-1);
    }
}
