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
        public int Encode(byte[] buffer, byte[] outputBuffer, int outputOffset);
        public int Encode(byte[] buffer, int offset, int length, byte[] outputBuffer, int outputOffset);
        public Memory<byte> Decode(byte[] buffer);
        public Memory<byte> Decode(byte[] buffer, int offset, int length);
        public int Decode(byte[] buffer, byte[] outputBuffer, int outputOffset);
        public int Decode(byte[] buffer, int offset, int length, byte[] outputBuffer, int outputOffset);

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
        public int Encode(byte[] buffer, byte[] outputBuffer, int outputOffset)
        {
            return Encode(buffer, 0, buffer.Length, outputBuffer, outputOffset);
        }
        public int Encode(byte[] buffer, int offset, int length, byte[] outputBuffer, int outputOffset)
        {
            int blockSize = encryptoTransform.InputBlockSize;
            int blocks = length / blockSize;
            int remainingBytes = length % blockSize;

            int written = 0;
            for (int i = 0; i < blocks; i++)
            {
                written += encryptoTransform.TransformBlock(buffer, offset+ written, blockSize, outputBuffer, outputOffset + written);
            }
            byte[] finalBlock = encryptoTransform.TransformFinalBlock(buffer, offset + written, remainingBytes);
            finalBlock.CopyTo(outputBuffer, outputOffset + written);
            written += finalBlock.Length;
            return written;
        }

        public Memory<byte> Decode(byte[] buffer)
        {
            return Decode(buffer, 0, buffer.Length);
        }
        public Memory<byte> Decode(byte[] buffer, int offset, int length)
        {
            return decryptoTransform.TransformFinalBlock(buffer, offset, length);
        }
        public int Decode(byte[] buffer, byte[] outputBuffer, int outputOffset)
        {
            return Decode(buffer, 0, buffer.Length, outputBuffer, outputOffset);
        }
        public int Decode(byte[] buffer, int offset, int length, byte[] outputBuffer, int outputOffset)
        {
            int blockSize = decryptoTransform.InputBlockSize;
            int blocks = length / blockSize;
            int remainingBytes = length % blockSize;

            int written = 0;
            for (int i = 0; i < blocks; i++)
            {
                written += decryptoTransform.TransformBlock(buffer, offset + written, blockSize, outputBuffer, outputOffset + written);
            }
            byte[] finalBlock = decryptoTransform.TransformFinalBlock(buffer, offset + written, remainingBytes);
            finalBlock.CopyTo(outputBuffer, outputOffset + written);
            written += finalBlock.Length;
            return written;
        }


        public void Dispose()
        {
            encryptoTransform.Dispose();
            decryptoTransform.Dispose();
        }
        private (byte[] Key, byte[] IV) GenerateKeyAndIV(string password)
        {
            byte[] key = new byte[16];
            byte[] iv = new byte[16];

            using SHA384 sha = SHA384.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(password));

            Array.Copy(hash, 0, key, 0, key.Length);
            Array.Copy(hash, key.Length, iv, 0, iv.Length);
            return (Key: key, IV: iv);
        }

    }
}
