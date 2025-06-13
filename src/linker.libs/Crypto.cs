using System;
using System.Security.Cryptography;
using System.Text;

namespace linker.libs
{
    public static class CryptoFactory
    {
        /// <summary>
        /// 对称加密
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public static ISymmetricCrypto CreateSymmetric(string password, PaddingMode mode = PaddingMode.ANSIX923)
        {
            return new AesCrypto(password, mode);
        }
    }

    public interface ICrypto
    {
        public byte[] Encode(byte[] buffer);
        public byte[] Encode(byte[] buffer, int offset, int length);
        public Memory<byte> Decode(byte[] buffer);
        public Memory<byte> Decode(byte[] buffer, int offset, int length);

        public void Dispose();
    }

    public interface ISymmetricCrypto : ICrypto
    {
        public string Password { get; set; }
    }
    public sealed class AesCrypto : ISymmetricCrypto
    {
        private ICryptoTransform encryptoTransform;
        private ICryptoTransform decryptoTransform;

        public string Password { get; set; }

        public AesCrypto(string password, PaddingMode mode = PaddingMode.ANSIX923)
        {
            Password = password;
            using Aes aes = Aes.Create();
            aes.Padding = mode;
            (aes.Key, aes.IV) = GenerateKeyAndIV(password);

            encryptoTransform = aes.CreateEncryptor(aes.Key, aes.IV);
            decryptoTransform = aes.CreateDecryptor(aes.Key, aes.IV);
        }
        public byte[] Encode(byte[] buffer)
        {
            return Encode(buffer, 0, buffer.Length);
        }
        public byte[] Encode(byte[] buffer, int offset, int length)
        {
            return encryptoTransform.TransformFinalBlock(buffer, offset, length);
        }
        public Memory<byte> Decode(byte[] buffer)
        {
            return Decode(buffer, 0, buffer.Length);
        }
        public Memory<byte> Decode(byte[] buffer, int offset, int length)
        {
            return decryptoTransform.TransformFinalBlock(buffer, offset, length);
        }

        public void Dispose()
        {
            encryptoTransform.Dispose();
            decryptoTransform.Dispose();
        }
        private static (byte[] Key, byte[] IV) GenerateKeyAndIV(string password)
        {
            byte[] key = new byte[16];
            byte[] iv = new byte[16];
            byte[] hash = SHA384.HashData(Encoding.UTF8.GetBytes(password));
            Array.Copy(hash, 0, key, 0, key.Length);
            Array.Copy(hash, key.Length, iv, 0, iv.Length);
            return (Key: key, IV: iv);

        }

    }
}
