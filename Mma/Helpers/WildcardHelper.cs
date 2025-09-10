using System.Text.RegularExpressions;

namespace Mma.Helpers;

public static class WildcardHelper
{
    public static bool IsMatch(string pattern, string input)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrWhiteSpace(pattern))
        {
            return false;
        }

        string regexPattern = "^" + Regex.Escape(pattern)
                                  .Replace("\\*", ".*")
                                  .Replace("\\?", ".") + "$";
        return Regex.IsMatch(input, regexPattern);
    }
}