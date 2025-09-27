using Microsoft.Extensions.Configuration;

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Mma.Helpers;

public static class EncryptionHelper
{
    
    private static string Secret
    {
        get
        {
            var config = GetConfiguration();
            return config.GetValue<string>("EncryptionSecret")!;
        }
    }

    private static IConfigurationSection GetConfiguration()
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true);

        var Configuration = builder.Build();

        var config = Configuration.GetSection("Secrets");

        return config;
    }

    public static string Encrypt(string text)
    {
        try
        {
            using Aes aes = Aes.Create();
            // First we need to turn the input strings into a byte array.
            byte[] plainText = System.Text.Encoding.Unicode.GetBytes(text);
            // We are using Salt to make it harder to guess our key
            // using a dictionary attack.
            byte[] salt = Encoding.ASCII.GetBytes(Secret.Length.ToString());
            // The (Secret Key) will be generated from the specified
            // password and Salt.
            //PasswordDeriveBytes -- It Derives a key from a password
            PasswordDeriveBytes SecretKey = new PasswordDeriveBytes(Secret, salt);
            // Create a encryptor from the existing SecretKey bytes.
            // We use 32 bytes for the secret key
            // (the default Rijndael key length is 256 bit = 32 bytes) and
            // then 16 bytes for the IV (initialization vector),
            // (the default Rijndael IV length is 128 bit = 16 bytes)
            ICryptoTransform encryptor = aes.CreateEncryptor(SecretKey.GetBytes(16), SecretKey.GetBytes(16));
            // Create a MemoryStream that is going to hold the encrypted bytes
            MemoryStream memoryStream = new MemoryStream();
            // Create a CryptoStream through which we are going to be processing our data.
            // CryptoStreamMode.Write means that we are going to be writing data
            // to the stream and the output will be written in the MemoryStream
            // we have provided. (always use write mode for encryption)
            CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
            // Start the encryption process.
            cryptoStream.Write(plainText, 0, plainText.Length);
            // Finish encrypting.
            cryptoStream.FlushFinalBlock();
            // Convert our encrypted data from a memoryStream into a byte array.
            byte[] CipherBytes = memoryStream.ToArray();
            // Close both streams.
            memoryStream.Close();
            cryptoStream.Close();
            // Convert encrypted data into a base64-encoded string.
            // A common mistake would be to use an Encoding class for that.
            // It does not work, because not all byte values can be
            // represented by characters. We are going to be using Base64 encoding
            // That is designed exactly for what we are trying to do.
            string encryptedData = Convert.ToBase64String(CipherBytes);
            // Return encrypted string.
            return ToBase64ForUrl(encryptedData);
        }
        catch
        {
            throw;
        }
    }

    public static string Encrypt2(string plainText)
    {
        byte[] salt = Encoding.ASCII.GetBytes(Secret.Length.ToString());
        var secretKey = new PasswordDeriveBytes(Secret, salt);

        using Aes aes = Aes.Create();
        aes.Key = secretKey.GetBytes(16);
        aes.IV = secretKey.GetBytes(16);

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }
        var base64String = Convert.ToBase64String(ms.ToArray());
        while (base64String.Length % 4 != 0)
        {
            base64String += "=";
        }

        return base64String;
    }


    public static string Decrypt(string text)
    {
        try
        {
            text = FromBase64ForUrl(text);
            using Aes aes = Aes.Create();
            byte[] encryptedData = Convert.FromBase64String(text);
            byte[] salt = Encoding.ASCII.GetBytes(Secret.Length.ToString());
            PasswordDeriveBytes SecretKey = new PasswordDeriveBytes(Secret, salt);
            // Create a decryptor from the existing SecretKey bytes.
            ICryptoTransform Decryptor = aes.CreateDecryptor(SecretKey.GetBytes(16), SecretKey.GetBytes(16));
            MemoryStream memoryStream = new MemoryStream(encryptedData);
            // Create a CryptoStream. (always use Read mode for decryption).
            CryptoStream cryptoStream = new CryptoStream(memoryStream, Decryptor, CryptoStreamMode.Read);
            // Since at this point we don't know what the size of decrypted data
            // will be, allocate the buffer long enough to hold EncryptedData;
            // DecryptedData is never longer than EncryptedData.
            byte[] plainText = new byte[encryptedData.Length];
            // Start decrypting.
            int decryptedCount = cryptoStream.Read(plainText, 0, plainText.Length);
            memoryStream.Close();
            cryptoStream.Close();
            // Convert decrypted data into a string.
            string decryptedData = Encoding.Unicode.GetString(plainText, 0, decryptedCount);
            // Return decrypted string.
            return decryptedData;
        }
        catch //(Exception exception)
        {
            throw;
        }
    }

    public static string Decrypt2(string cipherText)
    {
        byte[] salt = Encoding.ASCII.GetBytes(Secret.Length.ToString());
        var secretKey = new PasswordDeriveBytes(Secret, salt);

        using Aes aes = Aes.Create();
        aes.Key = secretKey.GetBytes(16);
        aes.IV = secretKey.GetBytes(16);

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        return sr.ReadToEnd();
    }

    public static string? EncryptMD5(string originalPassword)
    {
        byte[]? originalBytes = null;
        byte[]? encodedBytes = null;
        MD5? md5 = null;
        try
        {
            // Conver the original password to bytes; then create the hash
            md5 = MD5.Create();
            originalBytes = ASCIIEncoding.Default.GetBytes(originalPassword);
            encodedBytes = md5.ComputeHash(originalBytes);

            // Bytes to string
            return Regex.Replace(BitConverter.ToString(encodedBytes), "-", "").ToLower();
        }
        catch
        {
            return null;
        }
        finally
        {
            originalBytes = null;
            encodedBytes = null;
            md5 = null;
        }
    }

    public static string ToBase64ForUrl(string input)
    {
        StringBuilder result = new StringBuilder(input.TrimEnd('='));
        result.Replace('+', '-');
        result.Replace('/', '_');
        return result.ToString();
    }

    public static string FromBase64ForUrl(string input)
    {
        int padChars = (input.Length % 4) == 0 ? 0 : (4 - (input.Length % 4));
        StringBuilder result = new StringBuilder(input, input.Length + padChars);
        result.Append(String.Empty.PadRight(padChars, '='));
        result.Replace('-', '+');
        result.Replace('_', '/');
        return result.ToString();// Convert.FromBase64String(result.ToString());
    }

    // By Alhassan Try More Simple Methods
    //private static byte[] key = new byte[8] { 1, 2, 3, 4, 5, 6, 7, 8 };
    //private static byte[] iv = new byte[8] { 1, 2, 3, 4, 5, 6, 7, 8 };

    //public static string CryptMe(this string text)
    //{
    //    SymmetricAlgorithm algorithm = DES.Create();
    //    ICryptoTransform transform = algorithm.CreateEncryptor(key, iv);
    //    byte[] inputbuffer = Encoding.Unicode.GetBytes(text);
    //    byte[] outputBuffer = transform.TransformFinalBlock(inputbuffer, 0, inputbuffer.Length);
    //    return Convert.ToBase64String(outputBuffer);
    //}

    //public static string DecryptMe(this string text)
    //{
    //    SymmetricAlgorithm algorithm = DES.Create();
    //    ICryptoTransform transform = algorithm.CreateDecryptor(key, iv);
    //    byte[] inputbuffer = Convert.FromBase64String(text);
    //    byte[] outputBuffer = transform.TransformFinalBlock(inputbuffer, 0, inputbuffer.Length);
    //    return Encoding.Unicode.GetString(outputBuffer);
    //}

    public static string EncryptMe(this string sData)
    {
        try
        {
            byte[] encData_byte = new byte[sData.Length];
            encData_byte = System.Text.Encoding.UTF8.GetBytes(sData);
            string encodedData = Convert.ToBase64String(encData_byte);
            return encodedData;
        }
        catch (Exception ex)
        {
            throw new
Exception("Error in base64Encode" + ex.Message);
        }
    }
    public static string DecryptMe(this string sData)
    {
        System.Text.UTF8Encoding encoder = new System.Text.UTF8Encoding();
        System.Text.Decoder utf8Decode = encoder.GetDecoder();
        byte[] todecode_byte = Convert.FromBase64String(sData);
        int charCount = utf8Decode.GetCharCount(todecode_byte, 0, todecode_byte.Length);
        char[] decoded_char = new char[charCount];
        utf8Decode.GetChars(todecode_byte, 0, todecode_byte.Length, decoded_char, 0);
        string result = new string(decoded_char);
        return result;
    }

    public static string Base64Encode(string plainText)
    {
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        return System.Convert.ToBase64String(plainTextBytes);
    }

    public static string Base64Decode(string base64EncodedData)
    {
        var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
        return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
    }



}