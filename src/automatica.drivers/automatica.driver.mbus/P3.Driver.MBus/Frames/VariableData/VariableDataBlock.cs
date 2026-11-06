using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("P3.Driver.MBus.Tests")]

namespace P3.Driver.MBus.Frames.VariableData
{
    public class VariableDataBlock
    {
        public DataInformationField DataInformationField { get; private set; }
        private readonly IList<DataInformationFieldExtension> _difes = new List<DataInformationFieldExtension>();

        public ValueInformationField ValueInformationField { get; private set; }
        private readonly IList<ValueInformationField> _vifes = new List<ValueInformationField>();

        private byte[] _data;

        public byte[] Data => _data;

        public int StorageNumber { get; private set; }

        /// <summary>
        /// Tariff number per EN 13757-3 (DIFE bits 4-5, 2 bits per DIFE).
        /// </summary>
        public int Tariff { get; private set; }

        /// <summary>
        /// Tariff value as it was computed before the DIFE tariff bits were shifted (= Tariff &lt;&lt; 4).
        /// Attribute addresses and the stored "mbus-tariff" property use this value, keep it stable so
        /// already configured attributes keep matching.
        /// </summary>
        public int TariffKey => Tariff << 4;

        public object Value { get; set; }

        private VariableDataBlock()
        {
            
        }

        public static VariableDataBlock Parse(ReadOnlySpan<byte> data, int offset, Dictionary<Unit, int> usedStorageMap, out int bytesUsed, ILogger logger)
        {
            var vdb = new VariableDataBlock();
            bytesUsed = vdb.ParseData(data, offset, usedStorageMap, logger);
            return vdb;
        }

        private int ParseData(ReadOnlySpan<byte> data, int offset, Dictionary<Unit, int> usedStorageMap, ILogger logger)
        {
            ReadOnlySpan<byte> dataSpan = data;

            DataInformationField = DataInformationField.Parse(in data[offset]);
            bool hasExtension = DataInformationField.HasExtension;
            int index = offset + 1;

            int storageBitIndex = 1;
            int tariffBitIndex = 0;
            StorageNumber = DataInformationField.LsbStorageNumber ? 1 : 0;

            while (hasExtension)
            {
                var dife = DataInformationFieldExtension.Parse(in data[index++]);

                hasExtension = dife.HasExtension;

                StorageNumber |= dife.StorageNumber << storageBitIndex;
                storageBitIndex += 4;

                Tariff |= dife.Tariff << tariffBitIndex;
                tariffBitIndex += 2;

                _difes.Add(dife);
            }


            if (data.Length > index)
            {
                ValueInformationField = ValueInformationField.Parse(in data[index++]);

                hasExtension = ValueInformationField.HasExtension;

                while (hasExtension)
                {
                    var vife = ValueInformationField.Parse(in data[index++]);

                    hasExtension = vife.HasExtension;

                    _vifes.Add(vife);
                }
            }
            else
            {
                ValueInformationField = ValueInformationField.Default();
            }

            // only the 0xFD extension table is implemented, 0xFB codes stay Unit.Unknown
            if (ValueInformationField.RawData == 0xFD && _vifes.Count > 0)
            {
                ValueInformationField.ParseExtensionUnit(_vifes[0]);
            }

            if (DataInformationField.DataFieldType == DataFieldType.SpecialFunction)
            {
                int valueLength = data.Length - index;
                _data = dataSpan.Slice(index, valueLength).ToArray();

                index += valueLength;

            }
            else if (DataInformationField.DataFieldType == DataFieldType.VariableLength)
            {
                byte lvar = data[index++];
                int valueLength = GetVariableDataLength(lvar, data.Length - index);
                _data = dataSpan.Slice(index, valueLength).ToArray();

                index += valueLength;

                var value = ConvertVariableLengthValue(lvar);
                Value = value is string ? value : ApplyMultiplier(value);
            }
            else
            {
                int valueLength = DataInformationField.DataFieldLength;
                _data = dataSpan.Slice(index, valueLength).ToArray();

                index += valueLength;
                var value = ApplyMultiplier(ConvertValue());
                if (value != null)
                {
                    Value = value;
                }
            }

            if (!usedStorageMap.ContainsKey(ValueInformationField.Unit))
            {
                usedStorageMap.Add(ValueInformationField.Unit, 0);
            }
            else
            {
                var storageIndex = usedStorageMap[ValueInformationField.Unit];
                if (StorageNumber == storageIndex)
                {
                    StorageNumber += 1;
                    usedStorageMap[ValueInformationField.Unit] = storageIndex + 1;
                }

            }

            logger.LogTrace($"{ValueInformationField.Unit} {Value} storage {StorageNumber} tariff {Tariff}");


            return index - offset;
        }

        private object ApplyMultiplier(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (ValueInformationField.Multiplier != 0 && double.TryParse(value.ToString(), out var dblValue))
            {
                return dblValue * Math.Pow(10, ValueInformationField.Multiplier);
            }

            return value;
        }

        /// <summary>
        /// Length of the data following the LVAR byte (EN 13757-3, DIF data field 0x0D).
        /// Codes with an unknown/reserved length consume the remaining bytes so parsing stops safely.
        /// </summary>
        internal static int GetVariableDataLength(byte lvar, int remaining)
        {
            int length;
            if (lvar <= 0xBF)
            {
                length = lvar; // ASCII string with LVAR characters
            }
            else if (lvar >= 0xC0 && lvar <= 0xCF)
            {
                length = lvar - 0xC0; // positive BCD, (LVAR-C0h)*2 digits
            }
            else if (lvar >= 0xD0 && lvar <= 0xDF)
            {
                length = lvar - 0xD0; // negative BCD, (LVAR-D0h)*2 digits
            }
            else if (lvar >= 0xE0 && lvar <= 0xEF)
            {
                length = lvar - 0xE0; // binary number with (LVAR-E0h) bytes
            }
            else
            {
                return remaining; // floating point / reserved: length not reliably defined, skip the rest
            }

            return length;
        }

        private object ConvertVariableLengthValue(byte lvar)
        {
            if (lvar <= 0xBF)
            {
                // mode 1: the last character is transmitted first
                var chars = new char[_data.Length];
                for (int i = 0; i < _data.Length; i++)
                {
                    chars[i] = (char)_data[_data.Length - 1 - i];
                }
                return new string(chars);
            }
            if (lvar >= 0xC0 && lvar <= 0xCF)
            {
                return DecodeBcd(_data, false);
            }
            if (lvar >= 0xD0 && lvar <= 0xDF)
            {
                var bcd = DecodeBcd(_data, false);
                return bcd.HasValue ? -bcd.Value : (double?)null;
            }
            if (lvar >= 0xE0 && lvar <= 0xEF && _data.Length > 0 && _data.Length <= 8)
            {
                long value = 0;
                for (int i = _data.Length - 1; i >= 0; i--)
                {
                    value = (value << 8) | _data[i];
                }
                return value;
            }

            return null;
        }

        private static DateTime? CreateDateTime(int year, int month, int day, int hour = 0, int minute = 0, int second = 0)
        {
            if (month < 1 || month > 12 || day < 1 || day > DateTime.DaysInMonth(year, month) || hour > 23 || minute > 59 || second > 59)
            {
                return null;
            }
            return new DateTime(year, month, day, hour, minute, second);
        }

        private object ConvertValue()
        {
            if (_data.Length == 0)
            {
                return null;
            }
            switch (ValueInformationField.Unit)
            {
                case Unit.TimePoint:
                {
                    DateTime? value = null;
                    if (_data.Length == 6 && (_data[1] & 0x80) == 0)
                    {
                        //var isDaylightSaving = (_data[0] & 0x40) > 0;
                        value = CreateDateTime(2000 + (((_data[3] & 0xE0) >> 5) | ((_data[4] & 0xF0) >> 1)), (_data[4] & 0x0F) , _data[3] & 0x1F,
                            _data[2] & 0x1F, _data[1] & 0x3F, _data[0] & 0x3F);

                    }
                    // type F: IV is bit 7 of the minute byte (SU is bit 7 of the hour byte)
                    if (_data.Length == 4 && (_data[0] & 0x80) == 0)
                    {
                        value = CreateDateTime(2000 + (((_data[2] & 0xE0) >> 5) |
                                                    ((_data[3] & 0xF0) >> 1)), (_data[3] & 0x0F), _data[2] & 0x1F,
                            _data[1] & 0x1F, _data[0] & 0x3F, 0);
                    }
                    // type G: an all zero / out of range date is invalid -> null
                    if (_data.Length == 2)
                    {
                        value = CreateDateTime(2000 + (((_data[0] & 0xE0) >> 5) | ((_data[1] & 0xF0) >> 1)), (_data[1] & 0x0F), _data[0] & 0x1F);
                    }

                    return value;

                }
            }
            switch (DataInformationField.DataFieldType)
            {
                case DataFieldType.Integer8Bit:
                    return _data[0];
                case DataFieldType.Integer16Bit:
                    return BitConverter.ToInt16(_data, 0);
                case DataFieldType.Integer24Bit:
                    return Bit24ToInt32(_data);
                case DataFieldType.Integer32Bit:
                    return BitConverter.ToInt32(_data, 0);
                case DataFieldType.Real32Bit:
                    return BinaryPrimitives.ReadSingleLittleEndian(_data);
                case DataFieldType.Integer48Bit:
                    return Bit48ToInt64(_data);
                case DataFieldType.Integer64Bit:
                    return BitConverter.ToInt64(_data, 0);
                case DataFieldType.Digit2Bcd:
                case DataFieldType.Digit4Bcd:
                case DataFieldType.Digit6Bcd:
                case DataFieldType.Digit8Bcd:
                case DataFieldType.Digit12Bcd:
                    return DecodeBcd(_data, true);

                case DataFieldType.NoData:
                    return 0;
            }

            return null;
        }
        private static int Bit24ToInt32(byte[] byteArray)
        {
            var data = new byte[4];
            data[0] = byteArray[0];
            data[1] = byteArray[1];
            data[2] = byteArray[2];

            var test = BitConverter.ToInt32(data, 0);
            
            return test;
        }
        /// <summary>
        /// 48 bit signed integer (type B), little endian.
        /// </summary>
        internal static long Bit48ToInt64(byte[] byteArray)
        {
            long value = 0;
            for (int i = 5; i >= 0; i--)
            {
                value = (value << 8) | byteArray[i];
            }

            if ((byteArray[5] & 0x80) != 0)
            {
                value |= unchecked((long)0xFFFF000000000000);
            }

            return value;
        }

        /// <summary>
        /// Decodes a little endian packed BCD value (type A). If <paramref name="allowSign"/> is set, a high nibble
        /// of 0xF in the most significant byte marks a negative value (EN 13757-3).
        /// Returns null if a digit is not a valid BCD digit.
        /// </summary>
        internal static double? DecodeBcd(ReadOnlySpan<byte> data, bool allowSign)
        {
            if (data.Length == 0)
            {
                return null;
            }

            bool negative = false;
            double val = 0;

            for (int i = data.Length - 1; i >= 0; i--)
            {
                int high = (data[i] >> 4) & 0x0F;
                int low = data[i] & 0x0F;

                if (allowSign && i == data.Length - 1 && high == 0x0F)
                {
                    negative = true;
                    high = 0;
                }

                if (high > 9 || low > 9)
                {
                    return null;
                }

                val = (val * 100) + (high * 10) + low;
            }

            return negative ? -val : val;
        }
    }
}
