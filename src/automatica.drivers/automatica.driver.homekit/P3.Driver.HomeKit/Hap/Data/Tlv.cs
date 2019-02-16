﻿using System.Collections.Generic;
using System.Linq;

namespace P3.Driver.HomeKit.Hap.Data
{
    public class Tlv
    {
        public Dictionary<Constants, byte[]> Values = new Dictionary<Constants, byte[]>();

        public byte[] GetType(Constants type)
        {
            return Values[type];
        }

        public int GetTypeAsInt(Constants type)
        {
            if (!Values.ContainsKey(type))
            {
                return -1;
            }
            return Values[type][0];
        }

        public void AddType(Constants type, byte[] value)
        {
            if (Values.ContainsKey(type))
            {
                Values[type] = Combine(Values[type], value);
            }
            else
            {
                Values.Add(type, value);
            }
        }

        public void AddType(Constants type, ErrorCodes value)
        {
            AddType(type, (int)value);
        }

        public void AddType(Constants type, int value)
        {
            Values.Add(type, new byte[1] { (byte)value });
        }

        private byte[] Combine(params byte[][] arrays)
        {
            byte[] rv = new byte[arrays.Sum(a => a.Length)];

            int offset = 0;

            foreach (byte[] array in arrays)
            {
                System.Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }

            return rv;
        }

        public bool HasType(Constants state)
        {
            return Values.ContainsKey(state);
        }
    }
}
