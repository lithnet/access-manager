using System;
using System.Security.Cryptography.X509Certificates;

namespace Lithnet.AccessManager.Cryptography
{
    public interface IEncryptionProvider
    {
        string Decrypt(string base64Data, Func<string, X509Certificate2> certResolver);
        
        string Encrypt(X509Certificate2 cert, string data);
    }
}