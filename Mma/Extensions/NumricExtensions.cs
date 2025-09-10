using System.Collections.Specialized;

namespace Mma.Extensions;

public static class NumricExtensions
{
    public static int? ToNullIfZero(this int? value)
    {
        if (value.HasValue && value > 0)
        {
            return value;
        }

        return null;
    }

    public static int ToInt(this bool value)
    {
        return value ? 1 : 0;
    }
    public static IDictionary<string, string> ToDictionary(this NameValueCollection col)
    {
        IDictionary<string, string> dict = new Dictionary<string, string>();
        foreach (var k in col.AllKeys)
        {
            dict.Add(k, col[k]);
        }
        return dict;
    }

    public static DateTime GetDateFromNumber(this double num)
    {
        return DateTime.FromOADate(num);
    }
    public static DateTime GetDateFromNumber(this int num)
    {
        return DateTime.FromOADate(num);
    }

    public static int GetDayFromNumber(this double num)
    {
        return DateTime.FromOADate(num).Day;
    }
    public static int GetMonthFromNumber(this double num)
    {
        return DateTime.FromOADate(num).Month;
    }
    public static int GetYearFromNumber(this double num)
    {
        return DateTime.FromOADate(num).Year;
    }

    public static int? ToNullableInt(this string s)
    {
        if (int.TryParse(s, out int i)) return i;
        return null;
    }
}