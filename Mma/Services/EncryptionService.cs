using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Mma.Services;

// Main encryption service implementation
public class EncryptionService : IEncryptionService
{
    private readonly EncryptionOptions _options;
    private readonly byte[] _salt;

    public EncryptionService(IOptions<EncryptionOptions> options)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrEmpty(_options.Secret))
            throw new ArgumentException("Encryption secret cannot be null or empty.");

        // Use a consistent salt derived from the secret
        _salt = SHA256.HashData(Encoding.UTF8.GetBytes(_options.Secret + "salt"));
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            throw new ArgumentException("Plain text cannot be null or empty.", nameof(plainText));

        try
        {
            using var aes = Aes.Create();

            // Derive key and IV using PBKDF2 (more secure than PasswordDeriveBytes)
            using var keyDerivation = new Rfc2898DeriveBytes(_options.Secret, _salt, 10000, HashAlgorithmName.SHA256);
            aes.Key = keyDerivation.GetBytes(_options.KeySize / 8);
            aes.IV = keyDerivation.GetBytes(_options.IVSize / 8);

            using var encryptor = aes.CreateEncryptor();
            using var memoryStream = new MemoryStream();
            using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
            using (var writer = new StreamWriter(cryptoStream))
            {
                writer.Write(plainText);
            }

            var encryptedBytes = memoryStream.ToArray();
            var base64String = Convert.ToBase64String(encryptedBytes);

            return ToBase64ForUrl(base64String);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Encryption failed.", ex);
        }
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            throw new ArgumentException("Cipher text cannot be null or empty.", nameof(cipherText));

        try
        {
            var base64String = FromBase64ForUrl(cipherText);
            var encryptedBytes = Convert.FromBase64String(base64String);

            using var aes = Aes.Create();

            // Use the same key derivation as encryption
            using var keyDerivation = new Rfc2898DeriveBytes(_options.Secret, _salt, 10000, HashAlgorithmName.SHA256);
            aes.Key = keyDerivation.GetBytes(_options.KeySize / 8);
            aes.IV = keyDerivation.GetBytes(_options.IVSize / 8);

            using var decryptor = aes.CreateDecryptor();
            using var memoryStream = new MemoryStream(encryptedBytes);
            using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
            using var reader = new StreamReader(cryptoStream);

            return reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Decryption failed.", ex);
        }
    }

    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty.", nameof(password));

        // Use BCrypt or similar for password hashing instead of MD5
        // For demonstration, using SHA256 with salt (still not ideal for passwords)
        using var sha256 = SHA256.Create();
        var saltedPassword = Encoding.UTF8.GetBytes(password + _options.Secret);
        var hashedBytes = sha256.ComputeHash(saltedPassword);
        return Convert.ToHexString(hashedBytes).ToLowerInvariant();
    }

    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
            return false;

        var computedHash = HashPassword(password);
        return string.Equals(computedHash, hash, StringComparison.OrdinalIgnoreCase);
    }

    public string Base64Encode(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            throw new ArgumentException("Plain text cannot be null or empty.", nameof(plainText));

        var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(plainTextBytes);
    }

    public string Base64Decode(string base64EncodedData)
    {
        if (string.IsNullOrEmpty(base64EncodedData))
            throw new ArgumentException("Base64 encoded data cannot be null or empty.", nameof(base64EncodedData));

        var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
        return Encoding.UTF8.GetString(base64EncodedBytes);
    }

    public string ToBase64ForUrl(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return input.TrimEnd('=')
                   .Replace('+', '-')
                   .Replace('/', '_');
    }

    public string FromBase64ForUrl(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var padChars = (input.Length % 4) == 0 ? 0 : (4 - (input.Length % 4));
        var result = new StringBuilder(input, input.Length + padChars);
        result.Append(new string('=', padChars));
        result.Replace('-', '+');
        result.Replace('_', '/');

        return result.ToString();
    }
}

// Configuration class for encryption settings
public class EncryptionOptions
{
    public const string SectionName = "Encryption";
    public string Secret { get; set; } = string.Empty;
    public int KeySize { get; set; } = 256; // AES key size in bits
    public int IVSize { get; set; } = 128; // IV size in bits
}

// Extension methods for service registration
public static class EncryptionServiceExtensions
{
    /// <summary>
    /// Adds encryption services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddEncryptionServices(this IServiceCollection services, IConfigurationSection configurationSection)
    {
        // Configure options from appsettings
        services.Configure<EncryptionOptions>(options => configurationSection.Bind(options));

        // Register the encryption service
        services.AddSingleton<IEncryptionService, EncryptionService>();

        return services;
    }

    /// <summary>
    /// Adds encryption services with custom configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Action to configure encryption options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddEncryptionServices(this IServiceCollection services, Action<EncryptionOptions> configureOptions)
    {
        services.Configure(configureOptions);
        services.AddSingleton<IEncryptionService, EncryptionService>();

        return services;
    }

    /// <summary>
    /// Adds encryption services with options validation
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddEncryptionServicesWithValidation(this IServiceCollection services, IConfigurationSection configurationSection)
    {
        services.Configure<EncryptionOptions>(options => configurationSection.Bind(options));

        // Add options validation
        services.AddOptions<EncryptionOptions>()
            .Configure(options => configurationSection.Bind(options))
            .Validate(options => !string.IsNullOrEmpty(options.Secret), "Encryption secret is required")
            .Validate(options => options.Secret.Length >= 32, "Encryption secret must be at least 32 characters long")
            .ValidateOnStart();

        services.AddSingleton<IEncryptionService, EncryptionService>();

        return services;
    }
}
