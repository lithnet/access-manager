using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Unicode;
using Microsoft.Win32.SafeHandles;

namespace Lithnet.AccessManager
{
    public class EncryptionProvider : IEncryptionProvider
    {
        public X509Certificate2 CreateSelfSignedCert()
        {
            CertificateRequest request = new CertificateRequest("CN=Lithnet Access Manager", RSA.Create(4096), HashAlgorithmName.SHA384, RSASignaturePadding.Pkcs1);
            X509Certificate2 cert = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTime.UtcNow.AddYears(20));

            return cert;
        }

        public string Encrypt(X509Certificate2 cert, string data)
        {
            byte[] dataToEncrypt = Encoding.UTF8.GetBytes(data);
            return Convert.ToBase64String(this.Encrypt(cert, dataToEncrypt));
        }

        public string Decrypt(string base64Data)
        {
            return this.Decrypt(base64Data, ResolveCertificate);
        }

        public string Decrypt(string base64Data, Func<string, X509Certificate2> certResolver)
        {
            byte[] encryptedData = Convert.FromBase64String(base64Data);
            return this.Decrypt(encryptedData, certResolver);
        }

        private byte[] Encrypt(X509Certificate2 cert, byte[] dataToEncrypt)
        {
            using (AesManaged aes = new AesManaged())
            {
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform transform = aes.CreateEncryptor())
                {
                    RSAPKCS1KeyExchangeFormatter keyFormatter = new RSAPKCS1KeyExchangeFormatter(cert.PublicKey.Key);
                    byte[] encryptedKey = keyFormatter.CreateKeyExchange(aes.Key, aes.GetType());

                    using (MemoryStream outStream = new MemoryStream())
                    {
                        using (BinaryWriter writer = new BinaryWriter(outStream))
                        {
                            writer.Write(1);                                                    // Version
                            writer.Write(HexStringToBytes(cert.Thumbprint));                    // SHA1 cert thumbprint
                            writer.Write(encryptedKey.Length);                                  // Key length
                            writer.Write(aes.IV.Length);                                        // IV length
                            writer.Write(encryptedKey);                                         // Key
                            writer.Write(aes.IV);                                               // IV
                            writer.Write(transform.TransformFinalBlock(dataToEncrypt, 0, dataToEncrypt.Length)); // Payload
                        }

                        return outStream.ToArray();
                    }
                }
            }
        }

        private string Decrypt(byte[] rawData, Func<string, X509Certificate2> certResolver)
        {
            using (MemoryStream inputStream = new MemoryStream(rawData))
            {
                using (BinaryReader reader = new BinaryReader(inputStream))
                {
                    int version = reader.ReadInt32();

                    if (version != 1)
                    {
                        throw new CryptographicException($"The encrypted blob was of an unsupported version: {version}");
                    }

                    string thumbprint = ToHexString(reader.ReadBytes(20), 0, 20);

                    X509Certificate2 cert = certResolver(thumbprint);

                    int encryptedKeyLength = reader.ReadInt32();
                    int ivLength = reader.ReadInt32();

                    byte[] encryptedKey = reader.ReadBytes(encryptedKeyLength);
                    byte[] iv = reader.ReadBytes(ivLength);

                    using (AesManaged aesManaged = new AesManaged())
                    {
                        aesManaged.KeySize = 256;
                        aesManaged.BlockSize = 128;
                        aesManaged.Mode = CipherMode.CBC;
                        aesManaged.Padding = PaddingMode.PKCS7;

                        byte[] decryptedKey = ((RSACng)cert.PrivateKey).Decrypt(encryptedKey, RSAEncryptionPadding.Pkcs1);

                        using (ICryptoTransform transform = aesManaged.CreateDecryptor(decryptedKey, iv))
                        {
                            int remainingBytes = (int)(inputStream.Length - inputStream.Position);

                            var decryptedBytes = transform.TransformFinalBlock(reader.ReadBytes(remainingBytes), 0, remainingBytes);
                            return Encoding.UTF8.GetString(decryptedBytes);
                        }

                    }
                }
            }
        }

        private static X509Certificate2 ResolveCertificate(string thumbprint)
        {
            return GetCertificateFromStore(thumbprint, StoreLocation.CurrentUser) ??
                    GetCertificateFromStore(thumbprint, StoreLocation.LocalMachine) ??
                    throw new CertificateNotFoundException($"A certificate with the thumbprint {thumbprint} could not be found");
        }

        private static X509Certificate2 GetCertificateFromStore(string thumbprint, StoreLocation storeLocation)
        {
            X509Store store = new X509Store(StoreName.My, storeLocation);
            store.Open(OpenFlags.ReadOnly);

            try
            {
                foreach (var item in store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false))
                {
                    return item;
                }
            }
            finally
            {
                if (store.IsOpen)
                {
                    store.Close();
                }
            }

            return null;
        }

        private static byte[] HexStringToBytes(string hexHash)
        {
            if (hexHash == null)
            {
                throw new ArgumentNullException(nameof(hexHash));
            }

            if (hexHash.Length % 2 != 0)
            {
                throw new ArgumentException($"The value supplied must be a hexadecimal representation of the hash");
            }

            int binaryLength = hexHash.Length / 2;

            byte[] hash = new byte[binaryLength];

            for (int i = 0; i < binaryLength; i++)
            {
                hash[i] = Convert.ToByte(hexHash.Substring((i * 2), 2), 16);
            }

            return hash;
        }

        private static string ToHexString(byte[] data, int offset, int count)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data), "The binary data provided was null");
            }

            if (offset >= data.Length)
            {
                throw new ArgumentException("The value for offset cannot exceed the length of the data", nameof(offset));
            }

            if (count + offset > data.Length)
            {
                throw new ArgumentException("The combined values of offset and count cannot exceed the length of the data", nameof(offset));
            }

            StringBuilder sb = new StringBuilder(data.Length * 2);

            for (int i = offset; i < count; i++)
            {
                sb.Append(data[i].ToString("X2"));
            }

            return sb.ToString();
        }
    }
}