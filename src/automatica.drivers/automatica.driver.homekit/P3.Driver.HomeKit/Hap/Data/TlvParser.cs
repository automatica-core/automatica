using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace P3.Driver.HomeKit.Hap.Data
{
    public class TlvParser
    {
        public static Tlv Parse(byte[] data)
        {
            int currentIndex = 0;
            var result = new Tlv();

            if (data == null)
            {
                return result;
            }

            while (currentIndex < data.Length)
            {
                var tag = (Constants)data[currentIndex];
                currentIndex++;
                var length = data[currentIndex];
                currentIndex++;

                byte[] value = data.Skip(currentIndex).Take(length).ToArray();

                result.AddType(tag, value);

                currentIndex += length;
            }

            return result;
        }

        public static byte[] Serialise(Tlv item)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter br = new BinaryWriter(ms))
                {
                    foreach (var i in item.Values)
                    {
                        // If the item is longer than 255, we need to create multiple entries!
                        //
                        if (i.Value.Length > 255)
                        {
                            foreach (byte[] copySlice in i.Value.Slices(255))
                            {
                                br.Write((byte)i.Key);
                                br.Write((byte)copySlice.Length);
                                br.Write(copySlice);
                            }
                        }
                        else
                        {
                            br.Write((byte)i.Key);
                            br.Write((byte)i.Value.Length);
                            br.Write(i.Value);
                        }
                    }
                }

                return ms.ToArray();
            }
        }
    }

    public static class TlvExtensions
    {
        public static T[] CopySlice<T>(this T[] source, int index, int length, bool padToLength = false)
        {
            int n = length;
            T[] slice = null;

            if (source.Length < index + length)
            {
                n = source.Length - index;
                if (padToLength)
                {
                    slice = new T[length];
                }
            }

            if (slice == null) slice = new T[n];
            Array.Copy(source, index, slice, 0, n);
            return slice;
        }

        public static IEnumerable<T[]> Slices<T>(this T[] source, int count, bool padToLength = false)
        {
            for (var i = 0; i < source.Length; i += count)
            {
                yield return source.CopySlice(i, count, padToLength);
            }
        }
    }
}
