using Mma.Extensions;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mma.Data.Json.Converters;

public class StringDecimalConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => 0m,
            JsonTokenType.String => reader.GetString()!.Replace(",", "").ToDecimal(),
            _ => reader.GetDecimal(),
        };
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}