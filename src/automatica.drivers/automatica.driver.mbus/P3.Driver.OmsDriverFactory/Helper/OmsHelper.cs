using System;
using System.IO;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using P3.Driver.MBus.Frames;

namespace P3.Driver.OmsDriverFactory.Helper
{
    public static class OmsHelper
    {
        private const int OffsetUserData = 7;
        public static byte[] GetIvFromMBusFrame(MBusFrame frame)
        {
            var userData = frame.RawData.Span;
            var iv = new byte[16];
            iv[0] = userData[OffsetUserData + 4];
            iv[1] = userData[OffsetUserData + 5];
            iv[2] = userData[OffsetUserData + 0];
            iv[3] = userData[OffsetUserData + 1];
            iv[4] = userData[OffsetUserData + 2];
            iv[5] = userData[OffsetUserData + 3];
            iv[6] = userData[OffsetUserData + 6];
            iv[7] = userData[OffsetUserData + 7];

            var ivByte = userData[OffsetUserData + 8];
            for (int i = 0; i < 8; i++)
            {
                iv[8 + i] = ivByte;
            }
            return iv;
        }

        public static byte[] GetCipherFromMBusFrame(MBusFrame frame)
        {
            var cipher = new byte[frame.RawData.Length - 21];
            Array.Copy(frame.RawData.Span.ToArray(), OffsetUserData + 12, cipher, 0, cipher.Length);
           
            return cipher;
        }

        public static byte[] AesDecryptFrame(byte[] aesKey, MBusFrame frame)
        {
            var iv = GetIvFromMBusFrame(frame);
            var cipher = GetCipherFromMBusFrame(frame);

            var encrypted = DecryptByteArray(cipher, cipher.Length, aesKey, iv);


            if (encrypted[0] != 0x2f || encrypted[1] != 0x2f || encrypted[encrypted.Length-1] != 0x2f || encrypted[encrypted.Length - 2] != 0x2f)
            {
                return new byte[0];
            }
            return encrypted;
        }

        public static MBusFrame AesDecrypt(byte[] aesKey, MBusFrame frame, ILogger logger)
        {
            var encrypted = AesDecryptFrame(aesKey, frame);

            if (encrypted == null || encrypted.Length == 0)
            {
                return null;
            }

            var vdaFrame = new byte[encrypted.Length - 4];
            Array.Copy(encrypted, 2, vdaFrame, 0, encrypted.Length - 4);

            var varDataFrame = new VariableDataFrame();
            varDataFrame.ParseVariableDataFrame(vdaFrame, 0, logger);

            return varDataFrame;

        }

        public static byte[] DecryptByteArray(byte[] cipher, int encryptLen, byte[] key, byte[] iv)
        {
            byte[] result;
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                using (var resultStream = new MemoryStream())
                {
                    var aesStream = new CryptoStream(resultStream, decryptor, CryptoStreamMode.Write);

                    aesStream.Write(cipher, 0, encryptLen);
                    aesStream.FlushFinalBlock();

                    aesStream.Close();
                    result = resultStream.ToArray();
                }
            }
            return result;
        }
    }
}
