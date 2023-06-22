using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace P3.Knx.Core.Driver.IpSecure
{
    internal static class CryptoHelper
    {
        public static byte[] Xor(byte[] buffer1, byte[] buffer2)
        {
            for (int i = 0; i < buffer1.Length; i++)
                buffer1[i] ^= buffer2[i];
            return buffer1;
        }

        public static byte[] Pad(byte[] buffer, int padLen=16)
        {
            byte[] b = new byte[padLen];

            for (int i = 0; i < buffer.Length; i++)
            {
                b[i] = buffer[i];
            }

            return b;
        }

        public static byte[] GetSessionKey(byte[] sharedSecret)
        {
            // ReSharper disable once PossibleNullReferenceException
            byte[] s = SHA256.Create().ComputeHash(sharedSecret);
            return GetMsb(16, s);
        }

        public static byte[] EcnryptAes128WithXor(byte[] key, byte[] b1, byte[] b2)
        {
            return EncryptAes128(key, Xor(b1, b2));
        }

        public static byte[] EncryptAes128(byte[] key, byte[] b1)
        {
            return Encrypt(b1, key);
        }

        public static byte[] DecryptAes128(byte[] key, byte[] b1)
        {
            var aesAlg = new AesManaged
            {
                KeySize = 128,
                Key = key,
                BlockSize = 128,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.Zeros,
                IV = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }
            };

            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
            return decryptor.TransformFinalBlock(b1, 0, b1.Length);
        }

        public static byte[] Encrypt(byte[] input, byte[] key)
        {
            var aesAlg = new AesManaged
            {
                KeySize = 128,
                Key = key,
                BlockSize = 128,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.Zeros,
                IV = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }
            };

            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
            return encryptor.TransformFinalBlock(input, 0, input.Length);
        }

        public static byte[] DecryptPayload(byte[] key, byte[] cipher, byte[] ctr, byte[] mac)
        {
            short bLen = Convert.ToInt16(Math.Ceiling((double)cipher.Length / 16));

            List<byte> list = new List<byte>();

            for (int i = 0; i < bLen; i++)
            {
                ctr[15] = Convert.ToByte(i+1);
              
                byte[] sn = EncryptAes128(key, ctr);
              
                if (i == 1)
                {
                    byte[] lsbS = new byte[12];
                    Array.Copy(sn, sn.Length - 12, lsbS, 0, 12);
                    list.AddRange(GetLsb(12, sn));
                    GetMsb(4, sn);
                }
                else
                {
                    list.AddRange(sn);
                }

            }
            byte[] array = list.ToArray();
            byte[] realP = Xor(GetMsb(cipher.Length, array), GetMsb(cipher.Length, cipher));

            return realP;
        }

        public static byte[] EncryptPayload2(byte[] key, byte[] payload, byte[] ctr)
        {
            short bLen = Convert.ToInt16(Math.Ceiling((double)payload.Length / 16));
            List<byte> s = new List<byte>();
            
            for (int i = 0; i < bLen; i++)
            {
                ctr[15] = Convert.ToByte(i+1);

                s.AddRange(Encrypt(ctr, key));

            }

            return Xor(GetMsb(payload.Length, s.ToArray()), payload);
        }

        public static byte[] DecryptPayload2(byte[] key, byte[] cipher, byte[] ctr)
        {
            return EncryptPayload2(key, cipher, ctr);
        }
        public static byte[] EncryptPayload(byte[] hashedPassword, byte[] payload, byte[] ctr, byte[] mac, byte[] yn)
        {
            short bLen = Convert.ToInt16(Math.Ceiling((double) payload.Length/16));
            List<byte> streamS = new List<byte>();
            byte[] s0 = null;
            for (int i = 0; i < bLen; i++)
            {
                ctr[15] = Convert.ToByte(i*1);
                byte[] s = EncryptAes128(hashedPassword, ctr);

                if (i == 0)
                {
                    s0 = GetLsb(12, s);
                    streamS.AddRange(s0);
                }
                else
                {
                    streamS.AddRange(s);
                }
            }

            List<byte> c = new List<byte>();
            c.AddRange(Xor(GetMsb(payload.Length, payload), GetMsb(payload.Length, streamS.ToArray())));


            c.AddRange(Xor(GetMsb(4, yn), GetMsb(4, s0)));


            return c.ToArray();
        }

        public static byte[] CalculateMac(byte[] key, byte[] yn, byte[] ctr)
        {
            ctr[15] = Convert.ToByte(0);

            byte[] s0 = Encrypt(ctr, key);
            byte[] mac = Xor(s0, yn);
            
            return mac;
        }

        public static byte[] CalculateMac(byte[] hashedPassword, byte[] p, byte[] a, byte[] ctr, byte[] b0)
        {
            byte[] y = CalculateY(hashedPassword, p, a, b0);
            return CalculateMac(hashedPassword, y, ctr);
        }

        public static byte[] CalculateY(byte[] hashedPassword, byte[] p, byte[] a, byte[] b0)
        {
            List<byte> bParam = new List<byte>();
            bParam.AddRange(a);
            bParam.AddRange(p);
            byte[][] bs = CreateBParams(bParam.ToArray(), Convert.ToUInt16(a.Length));
            bs[0] = b0;
            byte[] y = CalculateYParam(hashedPassword, bs);
            return y;
        }

        public static byte[][] CreateBParams(byte[] datagram, ushort a)
        {
            short bLen = Convert.ToInt16(Math.Ceiling((double)(datagram.Length+2) / 16) + 1);

            byte[][] bs = new byte[bLen][];

            for (int i = 0; i < bLen; i++)
            {
                byte[] data = new byte[16];
                bs[i] = data;
                if (i == 0)
                    continue;

                if (i == 1)
                {
                    byte[] len = BitConverter.GetBytes(a);
                    data[0] = len[1];
                    data[1] = len[0];
                    Array.Copy(datagram, 0, data, 2, datagram.Length <  14 ? datagram.Length : 14);
                 
                    continue;
                }
                int index = ((i - 2) * 16) + 14;

                if (index + 16 > datagram.Length)
                {
                    byte[] lastBuffer = new byte[datagram.Length - index];
                    Array.Copy(datagram, index, lastBuffer, 0, datagram.Length - index);
                    data = Pad(lastBuffer);
                }
                else
                {
                    Array.Copy(datagram, index, data, 0, 16);
                }
                bs[i] = data;
            }

            return bs;
        }

        public static byte[] CreateDefaultB(byte[] seqNr, byte[] serialNr, byte[] tagNr, byte[] q)
        {
            byte[] ctr = new byte[16];

            Array.Copy(seqNr, 0, ctr, 0, 6);
            Array.Copy(serialNr, 0, ctr, 6, 6);
            Array.Copy(tagNr, 0, ctr, 12, 2);
            Array.Copy(q, 0, ctr, 14, 2);
            return ctr;
        }

        public static byte[] CreateDefaultCtr(byte[] seqNr, byte[] serialNr, byte[] tagNr)
        {
            return CreateDefaultCtr(CreateDefaultB(seqNr, serialNr, tagNr, new byte[2]));
        }

        public static byte[] CreateDefaultCtr(byte[] ctr)
        {
            byte[] cpy = ctr.Clone() as byte[];
            if (cpy != null)
            {
                cpy[14] = 0xff;
                cpy[15] = 0;
                return cpy;
            }
            throw new ArgumentException(nameof(ctr));
        }

        public static byte[] GetLsb(int byteCount, byte[] array) //get n right bytes
        {
            byte[] lsb = new byte[byteCount];

            Array.Copy(array, array.Length - byteCount, lsb, 0, byteCount);

            return lsb;
        }

        public static byte[] GetMsb(int byteCount, byte[] array) //get n left bytes
        {
            byte[] msb = new byte[byteCount];
            Array.Copy(array, 0, msb, 0, byteCount);

            return msb;
        }
        public static byte[] CalculateYParam(byte[] hashedPassword, byte[][] bParam)
        {
            byte[] yi = new byte[16];
            foreach (byte[] b in bParam)
            {
                byte[] xored = Xor(b, yi);
                yi = Encrypt(xored, hashedPassword);
            }

            return yi;
        }

        //http://stackoverflow.com/questions/18648084/rfc2898-pbkdf2-with-sha256-as-digest-in-c-sharp
        public static byte[] Pbkdf2Sha256GetBytes(int dklen, byte[] password, byte[] salt, int iterationCount)
        {
            using (var hmac = new HMACSHA256(password))
            {
                int hashLength = hmac.HashSize/8;
                if ((hmac.HashSize & 7) != 0)
                    hashLength++;
                int keyLength = dklen/hashLength;
                if (dklen > (0xFFFFFFFFL*hashLength) || dklen < 0)
                    throw new ArgumentOutOfRangeException(nameof(dklen));
                if (dklen%hashLength != 0)
                    keyLength++;
                byte[] extendedkey = new byte[salt.Length + 4];
                Buffer.BlockCopy(salt, 0, extendedkey, 0, salt.Length);
                using (var ms = new System.IO.MemoryStream())
                {
                    for (int i = 0; i < keyLength; i++)
                    {
                        extendedkey[salt.Length] = (byte) (((i + 1) >> 24) & 0xFF);
                        extendedkey[salt.Length + 1] = (byte) (((i + 1) >> 16) & 0xFF);
                        extendedkey[salt.Length + 2] = (byte) (((i + 1) >> 8) & 0xFF);
                        extendedkey[salt.Length + 3] = (byte) (i + 1 & 0xFF);
                        byte[] u = hmac.ComputeHash(extendedkey);
                        Array.Clear(extendedkey, salt.Length, 4);
                        byte[] f = u;
                        for (int j = 1; j < iterationCount; j++)
                        {
                            u = hmac.ComputeHash(u);
                            for (int k = 0; k < f.Length; k++)
                            {
                                f[k] ^= u[k];
                            }
                        }
                        ms.Write(f, 0, f.Length);
                        Array.Clear(u, 0, u.Length);
                        Array.Clear(f, 0, f.Length);
                    }
                    byte[] dk = new byte[dklen];
                    ms.Position = 0;
                    ms.Read(dk, 0, dklen);
                    ms.Position = 0;
                    for (long i = 0; i < ms.Length; i++)
                    {
                        ms.WriteByte(0);
                    }
                    Array.Clear(extendedkey, 0, extendedkey.Length);
                    return dk;
                }
            }
        }

    }
}
