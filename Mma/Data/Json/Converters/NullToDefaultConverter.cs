using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mma.Data.Json.Converters;

public class NullToDefaultConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsValueType && Nullable.GetUnderlyingType(typeToConvert) == null;

    public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
        => (JsonConverter)Activator.CreateInstance(
            typeof(NullToDefaultConverterInner<>).MakeGenericType(type)
        )!;

    private class NullToDefaultConverterInner<T> : JsonConverter<T> where T : struct
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.TokenType == JsonTokenType.Null
                ? default
                : JsonSerializer.Deserialize<T>(ref reader, options);

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            => JsonSerializer.Serialize(writer, value, options);
    }
}