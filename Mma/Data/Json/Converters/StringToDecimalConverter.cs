using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Mma.Data.Json.Converters;

public class StringToDecimalConverter : JsonConverter<decimal>
{
    private static readonly Regex CurrencyPattern = new(@"[\d,]+\.?\d*", RegexOptions.Compiled);
    private static readonly string[] CurrencySymbols = { "$", "€", "£", "¥", "₹", "R$", "₽", "₨", "₪", "₫", "₦", "₡", "₵", "₲", "₴", "₸", "₼", "₿" };

    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetDecimal(),
            JsonTokenType.String => ParseStringToDecimal(reader.GetString()),
            JsonTokenType.Null => 0m,
            _ => 0m
        };
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);

    private static decimal ParseStringToDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0m;

        // Try direct parsing first (fastest path)
        if (decimal.TryParse(value, out var directResult))
            return directResult;

        // Try parsing with invariant culture (handles basic formatting)
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariantResult))
            return invariantResult;

        // Handle currency and complex formatting
        return ParseCurrencyString(value);
    }

    private static decimal ParseCurrencyString(string value)
    {
        // Check if it contains currency symbols
        bool hasCurrencySymbol = CurrencySymbols.Any(symbol => value.Contains(symbol));

        if (hasCurrencySymbol)
        {
            // Try parsing with currency-aware cultures
            var cultures = new[]
            {
                new CultureInfo("en-US"), // $100.00
                new CultureInfo("pt-BR"), // R$100,00
                new CultureInfo("en-GB"), // £100.00
                new CultureInfo("de-DE"), // €100,00
                CultureInfo.InvariantCulture
            };

            foreach (var culture in cultures)
            {
                if (decimal.TryParse(value, NumberStyles.Currency, culture, out var result))
                    return result;
            }
        }

        // Fallback: Extract numbers using regex
        return ExtractDecimalFromString(value);
    }

    private static decimal ExtractDecimalFromString(string value)
    {
        var match = CurrencyPattern.Match(value);

        if (!match.Success)
            throw new FormatException($"Unable to parse '{value}' as decimal");

        var numberPart = match.Value;

        // Handle different decimal separators
        numberPart = NormalizeDecimalSeparators(numberPart);

        if (decimal.TryParse(numberPart, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result))
            return result;

        throw new FormatException($"Unable to parse '{value}' as decimal");
    }

    private static string NormalizeDecimalSeparators(string numberPart)
    {
        // Handle different decimal separator patterns
        if (numberPart.Contains(',') && numberPart.Contains('.'))
        {
            // Format like "1,234.56" - comma is thousands separator
            return numberPart.Replace(",", "");
        }

        if (numberPart.Contains(',') && !numberPart.Contains('.'))
        {
            // Could be either thousands separator or decimal separator
            var commaIndex = numberPart.LastIndexOf(',');
            var afterComma = numberPart.Substring(commaIndex + 1);

            // If 2 or fewer digits after comma, it's likely decimal separator
            if (afterComma.Length <= 2)
            {
                return numberPart.Replace(",", ".");
            }
            else
            {
                // More than 2 digits, likely thousands separator
                return numberPart.Replace(",", "");
            }
        }

        return numberPart;
    }
}

// Optional: Nullable version
public class StringToNullableDecimalConverter : JsonConverter<decimal?>
{
    private readonly StringToDecimalConverter _baseConverter = new();

    public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        try
        {
            return _baseConverter.Read(ref reader, typeof(decimal), options);
        }
        catch
        {
            return null; // Return null instead of throwing for nullable version
        }
    }

    public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteNumberValue(value.Value);
        else
            writer.WriteNullValue();
    }
}