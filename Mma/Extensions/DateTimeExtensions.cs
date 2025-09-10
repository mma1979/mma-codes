namespace Mma.Extensions;

public static class DateTimeExtensions
{
    public static long ToLinuxTime(this DateTime dateTime)
    {
        return (long)(dateTime.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
    }

    public static int GetNumberFromDate(this System.DateTime date)
    {
        return Convert.ToInt32(date.ToOADate());
    }
}