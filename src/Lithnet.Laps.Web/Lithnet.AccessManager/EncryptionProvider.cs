using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Lithnet.AccessManager
{
    public class EncryptionProvider : IEncryptionProvider
    {
      
        public string Encrypt(X509Certificate2 cert, string data)
        {
            return this.Encrypt(cert, data, 2);
        }

        internal string Encrypt(X509Certificate2 cert, string data, int version)
        {
            byte[] dataToEncrypt = Encoding.UTF8.GetBytes(data);

            if (version == 1)
            {
                return Convert.ToBase64String(this.Encryptv1(cert, dataToEncrypt));
            }
            else if (version == 2)
            {
                return Convert.ToBase64String(this.Encryptv2(cert, dataToEncrypt));
            }
            else
            {
                throw new CryptographicException($"The requested encryption version is not supported: {version}");
            }
        }

        public string Decrypt(string base64Data, Func<string, X509Certificate2> certResolver)
        {
            byte[] encryptedData = Convert.FromBase64String(base64Data);
            return this.Decrypt(encryptedData, certResolver);
        }

        private byte[] Encryptv1(X509Certificate2 cert, byte[] dataToEncrypt)
        {
            using (AesManaged aes = new AesManaged())
            {
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform transform = aes.CreateEncryptor())
                {
                    RSA publicKey = cert.GetRSAPublicKey();
                    byte[] encryptedKey = publicKey.Encrypt(aes.Key, RSAEncryptionPadding.OaepSHA512);

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

        private byte[] Encryptv2(X509Certificate2 cert, byte[] dataToEncrypt)
        {
            int version = 2;
            byte[] key = new byte[32];
            RandomNumberGenerator.Fill(key);

            using (AesGcm aes = new AesGcm(key))
            {
                byte[] nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
                RandomNumberGenerator.Fill(nonce);

                byte[] encryptedData = new byte[dataToEncrypt.Length];
                byte[] tag = new byte[AesGcm.TagByteSizes.MaxSize];
                byte[] additionalData = new byte[] { Convert.ToByte(version) };

                aes.Encrypt(nonce, dataToEncrypt, encryptedData, tag, additionalData);

                RSA publicKey = cert.GetRSAPublicKey();
                byte[] encryptedKey = publicKey.Encrypt(key, RSAEncryptionPadding.OaepSHA512);

                using (MemoryStream outStream = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(outStream))
                    {
                        writer.Write(version);                                              // Version
                        writer.Write(HexStringToBytes(cert.Thumbprint));                    // SHA1 cert thumbprint
                        writer.Write(encryptedKey.Length);                                  // Key length
                        writer.Write(nonce.Length);                                         // IV length
                        writer.Write(tag.Length);                                           // Tag length
                        writer.Write(encryptedKey);                                         // Key
                        writer.Write(nonce);                                                // IV
                        writer.Write(tag);                                                  // Authentication tag
                        writer.Write(encryptedData);                                        // data
                    }

                    return outStream.ToArray();
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
                    if (version == 1)
                    {
                        return Decryptv1(certResolver, inputStream, reader);
                    }
                    else if (version == 2)
                    {
                        return Decryptv2(certResolver, inputStream, reader);
                    }
                    else
                    {
                        throw new CryptographicException($"The encrypted blob was of an unsupported version: {version}");
                    }
                }
            }
        }

        private static string Decryptv1(Func<string, X509Certificate2> certResolver, MemoryStream inputStream, BinaryReader reader)
        {
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

                RSA privateKey = cert.GetRSAPrivateKey();
                byte[] decryptedKey = privateKey.Decrypt(encryptedKey, RSAEncryptionPadding.OaepSHA512);

                using (ICryptoTransform transform = aesManaged.CreateDecryptor(decryptedKey, iv))
                {
                    int remainingBytes = (int)(inputStream.Length - inputStream.Position);

                    var decryptedBytes = transform.TransformFinalBlock(reader.ReadBytes(remainingBytes), 0, remainingBytes);
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
        }

        private static string Decryptv2(Func<string, X509Certificate2> certResolver, MemoryStream inputStream, BinaryReader reader)
        {
            int version = 2;
            string thumbprint = ToHexString(reader.ReadBytes(20), 0, 20);

            X509Certificate2 cert = certResolver(thumbprint);

            int encryptedKeyLength = reader.ReadInt32();
            int nonceLength = reader.ReadInt32();
            int tagLength = reader.ReadInt32();

            byte[] encryptedKey = reader.ReadBytes(encryptedKeyLength);
            byte[] nonce = reader.ReadBytes(nonceLength);
            byte[] tag = reader.ReadBytes(tagLength);

            RSA privateKey = cert.GetRSAPrivateKey();
            byte[] decryptedKey = privateKey.Decrypt(encryptedKey, RSAEncryptionPadding.OaepSHA512);

            int remainingBytes = (int)(inputStream.Length - inputStream.Position);

            byte[] encryptedData = reader.ReadBytes(remainingBytes);
            byte[] decryptedData = new byte[remainingBytes];
            byte[] additionalData = new byte[] { Convert.ToByte(version) };

            using (AesGcm aes = new AesGcm(decryptedKey))
            {
                aes.Decrypt(nonce, encryptedData, tag, decryptedData, additionalData);
            }

            return Encoding.UTF8.GetString(decryptedData);
        }

        private static byte[] HexStringToBytes(string h)
        {
            if (h == null)
            {
                throw new ArgumentNullException(nameof(h));
            }

            if (h.Length % 2 != 0)
            {
                throw new ArgumentException($"The value supplied must be a hexadecimal representation of the hash");
            }

            int binaryLength = h.Length / 2;

            byte[] hash = new byte[binaryLength];

            for (int i = 0; i < binaryLength; i++)
            {
                hash[i] = Convert.ToByte(h.Substring((i * 2), 2), 16);
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