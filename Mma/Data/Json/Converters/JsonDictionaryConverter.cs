using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mma.Data.Json.Converters;

public class JsonDictionaryConverter : JsonConverter<Dictionary<string, object>>
{
    public override Dictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Handle the case where the value is already a JSON object
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            var jsonDocument = JsonDocument.ParseValue(ref reader);
            return ConvertJsonElement(jsonDocument.RootElement);
        }

        // Handle the case where the value is a JSON string
        if (reader.TokenType == JsonTokenType.String)
        {
            var jsonString = reader.GetString();
            return DeserializeToObjectDictionary(jsonString);
        }

        // Handle null values
        if (reader.TokenType == JsonTokenType.Null)
        {
            return new Dictionary<string, object>();
        }

        throw new JsonException($"Unexpected token type: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<string, object> value, JsonSerializerOptions options)
        => writer.WriteStringValue(JsonSerializer.Serialize(value, (JsonSerializerOptions)null));

    private static Dictionary<string, object> DeserializeToObjectDictionary(string json)
    {
        if (string.IsNullOrEmpty(json))
            return new Dictionary<string, object>();

        var jsonDocument = JsonDocument.Parse(json);
        return ConvertJsonElement(jsonDocument.RootElement);
    }

    private static Dictionary<string, object> ConvertJsonElement(JsonElement element)
    {
        var result = new Dictionary<string, object>();

        foreach (var property in element.EnumerateObject())
        {
            result[property.Name] = ConvertValue(property.Value);
        }

        return result;
    }

    private static object ConvertValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt32(out var intValue) ? intValue : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Object => ConvertJsonElement(element),
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertValue).ToArray(),
            _ => element.ToString()
        };
    }
}