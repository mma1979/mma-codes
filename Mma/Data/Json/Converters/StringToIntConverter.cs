using Mma.Extensions;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mma.Data.Json.Converters;

public class StringToIntConverter : JsonConverter<int?>
{
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetInt32(),
            JsonTokenType.String => reader.GetString().Replace(",", "").ToInt(),
            JsonTokenType.Null => 0,
            _ => 0
        };
    }

    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
         => writer.WriteNumberValue(value ?? 0);
}