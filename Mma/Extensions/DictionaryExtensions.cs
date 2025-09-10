namespace Mma.Extensions;

public static class DictionaryExtensions
{
    public static TV? GetValue<TK, TV>(this IDictionary<TK, TV> dict, TK key, TV? defaultValue = default)
    {
        return dict.TryGetValue(key, out TV? value) ? value : defaultValue;
    }
}