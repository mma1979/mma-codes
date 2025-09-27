using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using Newtonsoft.Json;

namespace Mma.Data.ValueConverters;

public class JsonDynamicDataConverter : ValueConverter<object?, string>
{
    public JsonDynamicDataConverter() : base(
        obj => obj == null ? "{}" : JsonConvert.SerializeObject(obj),
        json => string.IsNullOrWhiteSpace(json) ? null : JsonConvert.DeserializeObject<object>(json) ?? new { }
    )
    { }
}