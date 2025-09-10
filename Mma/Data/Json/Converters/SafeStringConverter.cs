using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mma.Data.Json.Converters;

public class SafeStringConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => GetNumberAsString(ref reader),
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.StartObject or JsonTokenType.StartArray => GetJsonString(ref reader),
            JsonTokenType.True => "true",
            JsonTokenType.False => "false",
            JsonTokenType.Null => null,

            _ => reader.GetString()
        };
    }

    private static string GetNumberAsString(ref Utf8JsonReader reader)
    {
        if (reader.TryGetInt64(out long intValue))
            return intValue.ToString();

        return reader.GetDouble().ToString(CultureInfo.InvariantCulture);
    }

    private static string GetJsonString(ref Utf8JsonReader reader)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        return doc.RootElement.GetRawText();
    }




    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        => writer.WriteStringValue(value);
}
