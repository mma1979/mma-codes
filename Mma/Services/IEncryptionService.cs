using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mma.Services;

// Interface for encryption service
public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
    string Base64Encode(string plainText);
    string Base64Decode(string base64EncodedData);
    string ToBase64ForUrl(string input);
    string FromBase64ForUrl(string input);
}