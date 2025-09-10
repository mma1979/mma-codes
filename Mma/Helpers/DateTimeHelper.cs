using System.Globalization;

namespace Mma.Helpers;

public static class DateTimeHelper
{
    // Solution 1: Using Russian culture
    public static DateTime ParseRussianDate(string dateString)
    {
        var russianCulture = new CultureInfo("ru-RU");
        return DateTime.Parse(dateString, russianCulture);
    }

    // Solution 2: Manual month mapping (more reliable)
    private static readonly Dictionary<string, string> CyrillicMonthMap = new()
    {
        {"Янв", "Jan"}, {"янв", "Jan"},
        {"Фев", "Feb"}, {"фев", "Feb"},
        {"Мар", "Mar"}, {"мар", "Mar"},
        {"Апр", "Apr"}, {"апр", "Apr"},
        {"Май", "May"}, {"май", "May"},
        {"Июн", "Jun"}, {"июн", "Jun"},
        {"Июл", "Jul"}, {"июл", "Jul"},
        {"Авг", "Aug"}, {"авг", "Aug"}, {"Арг", "Aug"}, // Including your typo
        {"Сен", "Sep"}, {"сен", "Sep"},
        {"Окт", "Oct"}, {"окт", "Oct"},
        {"Ноя", "Nov"}, {"ноя", "Nov"},
        {"Дек", "Dec"}, {"дек", "Dec"}
    };

    public static DateTime ParseCyrillicDate(string dateString)
    {
        if (string.IsNullOrEmpty(dateString))
            throw new ArgumentNullException(nameof(dateString), "Date string cannot be null or empty.");

        var originalString = dateString;

        // Replace Cyrillic month names with English equivalents
        foreach (var mapping in CyrillicMonthMap)
        {
            dateString = dateString.Replace(mapping.Key, mapping.Value);
        }

        if (DateTime.TryParse(dateString, out var result))
        {
            return result;
        }

        throw new FormatException($"Unable to parse date string: '{originalString}'. Ensure it is in a valid format.");
    }

    // Solution 3: Try multiple approaches
    public static DateTime? ParseDateSafely(string dateString)
    {
        if (string.IsNullOrEmpty(dateString))
            return null;

        var currentYear = DateTime.Now.Year;

        // Try 1: Default parsing
        if (DateTime.TryParse(dateString, out var result))
            return AddCurrentYearIfMissing(result, dateString, currentYear);

        // Try 2: Russian culture
        var russianCulture = new CultureInfo("ru-RU");
        if (DateTime.TryParse(dateString, russianCulture, DateTimeStyles.None, out result))
            return AddCurrentYearIfMissing(result, dateString, currentYear);

        // Try 3: Manual translation
        try
        {
            result = ParseCyrillicDate(dateString);
            return AddCurrentYearIfMissing(result, dateString, currentYear);
        }
        catch
        {
            // Continue to next approach

        }

        // Try 4: Common date formats with Russian culture
        string[] formatsWithYear = {
        "dd-MMM-yyyy",
        "d-MMM-yyyy",
        "dd-MM-yyyy",
        "d-M-yyyy",
        "yyyy-MM-dd",
        "dd.MM.yyyy",
        "d.M.yyyy",
        "MMM d, h:mm tt",      // Jun 23, 12:00 AM
        "MMM dd, h:mm tt",     // Jun 23, 12:00 AM
        "MMM d, yyyy h:mm tt", // Jun 23, 2024 12:00 AM
        "MMM dd, yyyy h:mm tt",
        "MMM d, yyyy",         // Jun 23, 2024
        "MMM dd, yyyy"         // Jun 23, 2024
    };

        string[] formatsWithoutYear = {
        "MMM d",               // Jun 23
        "MMM dd",              // Jun 23
        "d MMM",               // 23 Jun
        "dd MMM",              // 23 Jun
        "dd-MMM",              // 23-Jun
        "d-MMM",               // 3-Jun
        "dd.MM",               // 23.06
        "d.M",                 // 3.6
        "MM-dd",               // 06-23
        "M-d",                  // 6-3
        "MMM d, h:mm tt",      // Jun 23, 12:00 AM
        "MMM dd, h:mm tt",     // Jun 23, 12:00 AM
    };

        // Try formats that include year first
        foreach (var format in formatsWithYear)
        {
            if (DateTime.TryParseExact(dateString, format, russianCulture, DateTimeStyles.None, out result))
                return result;
        }

        // Try formats without year and add current year
        foreach (var format in formatsWithoutYear)
        {
            if (DateTime.TryParseExact(dateString, format, russianCulture, DateTimeStyles.None, out result))
                return new DateTime(currentYear, result.Month, result.Day, result.Hour, result.Minute, result.Second);
        }

        return null; // Return null if all parsing attempts fail
    }

    private static DateTime AddCurrentYearIfMissing(DateTime parsedDate, string originalString, int currentYear)
    {
        // Check if the original string likely contains a year (4 consecutive digits)
        if (System.Text.RegularExpressions.Regex.IsMatch(originalString, @"\b\d{4}\b"))
            return parsedDate;

        // Check if parsed year is significantly different from current year
        // This handles cases where DateTime.Parse assumes a default year
        if (Math.Abs(parsedDate.Year - currentYear) > 10)
        {
            return new DateTime(currentYear, parsedDate.Month, parsedDate.Day,
                              parsedDate.Hour, parsedDate.Minute, parsedDate.Second);
        }

        return parsedDate;
    }

    private static bool ContainsYear(string dateString)
    {
        // Check for 4-digit year pattern
        return System.Text.RegularExpressions.Regex.IsMatch(dateString, @"\b\d{4}\b");
    }
}