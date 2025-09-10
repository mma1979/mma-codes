

using NodaTime;

using System.Security.Cryptography;

namespace Mma.Extensions;

public static class GuidExtensions
{
    public static string ExtractPin(this Guid guid)
    {
        var hash = guid.GetHashCode().ToString();
        return hash.Substring(hash.Length - 6, 6);
    }

    public static string ExtractPin(this Guid guid, int pinLength)
    {
        var hash = guid.GetHashCode().ToString();
        return hash.Substring(hash.Length - pinLength, pinLength);
    }

    public static bool VirifyPin(this Guid guid, string pin)
    {
        var hash = guid.GetHashCode().ToString();
        var computedPin = hash.Substring(hash.Length - 6, 6);
        return pin == computedPin;
    }

    public static Guid V7(this Guid _)
    {
        byte[] randomBytes = new byte[10];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        var timestamp = Duration.FromTicks(SystemClock.Instance.GetCurrentInstant().ToUnixTimeTicks()).TotalMilliseconds;

        byte[] guidBytes = new byte[16];
        BitConverter.GetBytes(timestamp).CopyTo(guidBytes, 0);
        randomBytes.CopyTo(guidBytes, 6);

        // Set version to 7
        guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x70);
        // Set variant to RFC4122
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);

        return new Guid(guidBytes);
    }

}