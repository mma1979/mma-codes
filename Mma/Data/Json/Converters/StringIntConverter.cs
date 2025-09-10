using Mma.Extensions;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mma.Data.Json.Converters;

public class StringIntConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => 0,
            JsonTokenType.String => reader.GetString()!.Replace(",", "").ToInt(),
            _ => reader.GetInt32(),
        };
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}