using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using Mma.Helpers;

namespace Mma.Data.ValueConverters;

public class SecureDataConverter : ValueConverter<string, string>
{

    public SecureDataConverter() : base(
        value => value == null ? null : EncryptionHelper.Encrypt2(value),
        value => value == null ? null : EncryptionHelper.Decrypt2(value)
        )
    { }
}
