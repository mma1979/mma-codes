using System.Web;

namespace Mma.Extensions;

public static class ObjectExtensions
{
    public static int ToInt(this object value)
    {
        return Convert.ToInt32(value);
    }
    public static int? ToNullableInt(this object value)
    {
        if (Convert.IsDBNull(value) || value == null)
            return null;
        return value.ToInt();
    }
    public static string? ToNullableString(this object value)
    {
        if (IsNull(value))
            return null;
        return Convert.ToString(value);
    }
    public static string? ToStringIfNull(this object value)
    {
        if (IsNull(value))
            return "";
        return Convert.ToString(value);
    }
    public static bool ToBoolean(this object value)
    {
        return Convert.ToBoolean(value);
    }
    public static bool? ToNullableBoolean(this object value)
    {
        if (IsNull(value))
            return null;
        return value.ToBoolean();
    }
    public static bool ToBooleanIfNull(this bool? value)
    {
        if (value is null)
            return false;
        if (!value.HasValue)
            return false;
        return value.ToBoolean();
    }



    public static DateTime ToDateTime(this object value)
    {
        return Convert.ToDateTime(value);
    }
    public static DateTime? ToNullableDateTime(this object value)
    {
        if (IsNull(value))
            return null;
        return value.ToDateTime();
    }

    public static decimal? ToNullableDecimal(this object value)
    {
        if (IsNull(value) || string.IsNullOrEmpty(value.ToNullableString()))
            return null;
        return value.ToDecimal();
    }

    public static decimal ToDecimal(this object value)
    {
        return Convert.ToDecimal(value);
    }

    public static double ToDouble(this object value)
    {
        return Convert.ToDouble(value);
    }

    public static byte ToByte(this object value)
    {
        return Convert.ToByte(value);
    }
    public static byte? ToNullableByte(this object value)
    {
        if (IsNull(value))
            return null;
        return value.ToByte();
    }
    public static sbyte ToSByte(this object value)
    {
        return Convert.ToSByte(value);
    }
    public static sbyte? ToNullableSByte(this object value)
    {
        if (IsNull(value))
            return null;
        return value.ToSByte();
    }
    public static short ToShort(this object value)
    {
        return Convert.ToInt16(value);
    }
    public static short? ToNullableShort(this object value)
    {
        if (IsNull(value))
            return null;
        return value.ToShort();
    }
    public static long ToLong(this object value)
    {
        return Convert.ToInt64(value);
    }
    public static long? ToNullableLong(this object value)
    {
        if (IsNull(value))
            return null;
        return value.ToLong();
    }
    public static char ToChar(this object value)
    {
        return Convert.ToChar(value);
    }
    public static char? ToNullableChar(this object value)
    {
        if (IsNull(value))
            return null;
        return value.ToChar();
    }

    public static string ToQueryString(this object obj)
    {
        var properties = from p in obj.GetType().GetProperties()
                         where p.GetValue(obj, null) != null
                         select p.Name + "=" + HttpUtility.UrlEncode(p.GetValue(obj, null)?.ToString());
        return string.Join("&", properties.ToArray());
    }

    public static bool IsNull(this object obj)
    {
        return (Convert.IsDBNull(obj) || obj == null);
    }

    public static void Dump(this object value)
    {
        Console.WriteLine(value);
    }

    public static T With<T>(this T obj, Action<T> action)
    {
        action(obj);
        return obj;
    }
}