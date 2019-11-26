using System;
using System.Text;
using Automatica.Core.Base.Templates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.Knx.Exceptions;
// ReSharper disable InconsistentNaming

namespace P3.Knx.Core.Driver
{
    public enum KnxError
    {
        //STANDARD KNX ERRORS
        NoError = 0,
        NoMoreConnectionsAvailable = 36,
        DataConnectionError = 38,
        KnxConnectionError = 39,
        IpSecInvalidIndividualAddress = 45,

        //CUSTOM ERRORS
        LostSocketConnection = 1000,
        StateRequestTimeout = 1001,
        IpSecInvalidDeviceAuthCode = 1002,
        IpSecInvalidUserAuthCode = 1003,
        IpSecRespondedIndividualAddressDiffersToGiven = 1004,
        IpSecSessionTimeoutReceived = 1005
    }

    // ReSharper disable once InconsistentNaming
    public enum Dpt5Type
    {
        [EnumName("KNX.DPT5.001.NAME", "5.001")]
        Dpt5_001 = 1,
        [EnumName("KNX.DPT5.003.NAME", "5.003")]
        Dpt5_003 = 3,
        [EnumName("KNX.DPT5.004.NAME", "5.004")]
        Dpt5_004 = 4,
        [EnumName("KNX.DPT5.005.NAME", "5.005")]
        Dpt5_005 = 5,
        [EnumName("KNX.DPT5.006.NAME", "5.006")]
        Dpt5_006 = 6,
        [EnumName("KNX.DPT5.010.NAME", "5.010")]
        Dpt5_010 = 10
    }

    public enum Dpt6Type
    {
        [EnumName("KNX.DPT6.001.NAME", "6.001")]
        Dpt6_001 = 1,
        [EnumName("KNX.DPT6.010.NAME", "6.010")]
        Dpt6_010 = 10
    }

    public enum Dpt7Type
    {
        [EnumName("KNX.DPT7.001.NAME", "7.001")]
        Dpt7_001 = 1,
        [EnumName("KNX.DPT7.002.NAME", "7.002")]
        Dpt7_002 = 2,
        [EnumName("KNX.DPT7.003.NAME", "7.003")]
        Dpt7_003 = 3,
        [EnumName("KNX.DPT7.004.NAME", "7.004")]
        Dpt7_004 = 4,
        [EnumName("KNX.DPT7.005.NAME", "7.005")]
        Dpt7_005 = 5,
        [EnumName("KNX.DPT7.006.NAME", "7.006")]
        Dpt7_006 = 6,
        [EnumName("KNX.DPT7.007.NAME", "7.007")]
        Dpt7_007 = 7,

        [EnumName("KNX.DPT7.010.NAME", "7.010")]
        Dpt7_010 = 10,
        [EnumName("KNX.DPT7.011.NAME", "7.011")]
        Dpt7_011 = 11,
        [EnumName("KNX.DPT7.012.NAME", "7.012")]
        Dpt7_012 = 12,
        [EnumName("KNX.DPT7.013.NAME", "7.013")]
        Dpt7_013 = 13,
    }

    public enum Dpt8Type
    {
        [EnumName("KNX.DPT8.001.NAME", "8.001")]
        Dpt8_001 = 1,
        [EnumName("KNX.DPT8.002.NAME", "8.002")]
        Dpt8_002 = 2,
        [EnumName("KNX.DPT8.003.NAME", "8.003")]
        Dpt8_003 = 3,
        [EnumName("KNX.DPT8.004.NAME", "8.004")]
        Dpt8_004 = 4,
        [EnumName("KNX.DPT8.005.NAME", "8.005")]
        Dpt8_005 = 5,
        [EnumName("KNX.DPT8.006.NAME", "8.006")]
        Dpt8_006 = 6,
        [EnumName("KNX.DPT8.007.NAME", "8.007")]
        Dpt8_007 = 7,
        [EnumName("KNX.DPT8.010.NAME", "8.010")]
        Dpt8_010 = 10,
        [EnumName("KNX.DPT8.011.NAME", "8.011")]
        Dpt8_011 = 11
    }

    public enum Dpt9Type
    {
        [EnumName("KNX.DPT9.001.NAME", "9.001")]
        Dpt9_001 = 1,
        [EnumName("KNX.DPT9.002.NAME", "9.002")]
        Dpt9_002 = 2,
        [EnumName("KNX.DPT9.003.NAME", "9.003")]
        Dpt9_003 = 3,
        [EnumName("KNX.DPT9.004.NAME", "9.004")]
        Dpt9_004 = 4,
        [EnumName("KNX.DPT9.005.NAME", "9.005")]
        Dpt9_005 = 5,
        [EnumName("KNX.DPT9.006.NAME", "9.006")]
        Dpt9_006 = 6,
        [EnumName("KNX.DPT9.007.NAME", "9.007")]
        Dpt9_007 = 7,
        [EnumName("KNX.DPT9.008.NAME", "9.008")]
        Dpt9_008 = 8,
        [EnumName("KNX.DPT9.010.NAME", "9.010")]
        Dpt9_010 = 10,
        [EnumName("KNX.DPT9.011.NAME", "9.011")]
        Dpt9_011 = 11,

        [EnumName("KNX.DPT9.020.NAME", "9.020")]
        Dpt9_020 = 20,
        [EnumName("KNX.DPT9.021.NAME", "9.021")]
        Dpt9_021 = 21,
        [EnumName("KNX.DPT9.022.NAME", "9.022")]
        Dpt9_022 = 22,
        [EnumName("KNX.DPT9.023.NAME", "9.023")]
        Dpt9_023 = 23,
        [EnumName("KNX.DPT9.024.NAME", "9.024")]
        Dpt9_024 = 24,
        [EnumName("KNX.DPT9.025.NAME", "9.025")]
        Dpt9_025 = 25,
        [EnumName("KNX.DPT9.026.NAME", "9.026")]
        Dpt9_026 = 26,
        [EnumName("KNX.DPT9.027.NAME", "9.027")]
        Dpt9_027 = 27,
        [EnumName("KNX.DPT9.028.NAME", "9.028")]
        Dpt9_028 = 28,
    }

    public enum Dpt16Type
    {
        [EnumName("KNX.DPT16.000.NAME", "16.000")]
        Dpt16_000 = 1,
        [EnumName("KNX.DPT16.001.NAME", "16.001")]
        Dpt16_001 = 2
    }

    public enum DptType
    {
        [EnumName("KNX.DPT1.NAME", "1")]
        Dpt1 = 1,
        [EnumName("KNX.DPT2.NAME", "2")]
        Dpt2 = 2,
        [EnumName("KNX.DPT3.NAME", "3")]
        Dpt3 = 3,
        [EnumName("KNX.DPT4.NAME", "4")]
        Dpt4 = 4,

        [EnumName("KNX.DPT6.020.NAME", "6.020")]
        Dpt6_020 = 6,

        [EnumName("KNX.DPT10.NAME", "10.001")]
        Dpt10 = 10,
        [EnumName("KNX.DPT11.NAME", "11.001")]
        Dpt11 = 11,
        [EnumName("KNX.DPT12.NAME", "12")]
        Dpt12 = 12,
        [EnumName("KNX.DPT13.NAME", "13")]
        Dpt13 = 13,
        [EnumName("KNX.DPT14.NAME", "14")]
        Dpt14 = 14

    }
    public class KnxHelper
    {
        public static ILogger Logger { get; set; } = NullLogger.Instance;
        public static string ByteArrayToString(byte[] data)
        {
            string hex = BitConverter.ToString(data);
            return hex.Replace("-", " ");
        }

        internal static bool ProcessCemi(KnxDatagram datagram, byte[] cemi)
        {
            try
            {
                // CEMI
                // --------+--------+--------+--------+----------------+----------------+--------+----------------+
                //   Msg   |Add.Info| Ctrl 1 | Ctrl 2 | Source Address | Dest. Address  |  Data  |      APDU      |
                //  Code   | Length |        |        |                |                | Length |                |
                // --------+--------+--------+--------+----------------+----------------+--------+----------------+
                //   1 byte   1 byte   1 byte   1 byte      2 bytes          2 bytes       1 byte      2 bytes
                //
                //  Message Code    = 0x11 - a L_Data.req primitive
                //      COMMON EMI MESSAGE CODES FOR DATA LINK LAYER PRIMITIVES
                //          FROM NETWORK LAYER TO DATA LINK LAYER
                //          +---------------------------+--------------+-------------------------+---------------------+------------------+
                //          | Data Link Layer Primitive | Message Code | Data Link Layer Service | Service Description | Common EMI Frame |
                //          +---------------------------+--------------+-------------------------+---------------------+------------------+
                //          |        L_Raw.req          |    0x10      |                         |                     |                  |
                //          +---------------------------+--------------+-------------------------+---------------------+------------------+
                //          |                           |              |                         | Primitive used for  | Sample Common    |
                //          |        L_Data.req         |    0x11      |      Data Service       | transmitting a data | EMI frame        |
                //          |                           |              |                         | frame               |                  |
                //          +---------------------------+--------------+-------------------------+---------------------+------------------+
                //          |        L_Poll_Data.req    |    0x13      |    Poll Data Service    |                     |                  |
                //          +---------------------------+--------------+-------------------------+---------------------+------------------+
                //          |        L_Raw.req          |    0x10      |                         |                     |                  |
                //          +---------------------------+--------------+-------------------------+---------------------+------------------+
                //          FROM DATA LINK LAYER TO NETWORK LAYER
                //          +---------------------------+--------------+-------------------------+---------------------+
                //          | Data Link Layer Primitive | Message Code | Data Link Layer Service | Service Description |
                //          +---------------------------+--------------+-------------------------+---------------------+
                //          |        L_Poll_Data.con    |    0x25      |    Poll Data Service    |                     |
                //          +---------------------------+--------------+-------------------------+---------------------+
                //          |                           |              |                         | Primitive used for  |
                //          |        L_Data.ind         |    0x29      |      Data Service       | receiving a data    |
                //          |                           |              |                         | frame               |
                //          +---------------------------+--------------+-------------------------+---------------------+
                //          |        L_Busmon.ind       |    0x2B      |   Bus Monitor Service   |                     |
                //          +---------------------------+--------------+-------------------------+---------------------+
                //          |        L_Raw.ind          |    0x2D      |                         |                     |
                //          +---------------------------+--------------+-------------------------+---------------------+
                //          |                           |              |                         | Primitive used for  |
                //          |                           |              |                         | local confirmation  |
                //          |        L_Data.con         |    0x2E      |      Data Service       | that a frame was    |
                //          |                           |              |                         | sent (does not mean |
                //          |                           |              |                         | successful receive) |
                //          +---------------------------+--------------+-------------------------+---------------------+
                //          |        L_Raw.con          |    0x2F      |                         |                     |
                //          +---------------------------+--------------+-------------------------+---------------------+

                //  Add.Info Length = 0x00 - no additional info
                //  Control Field 1 = see the bit structure above
                //  Control Field 2 = see the bit structure above
                //  Source Address  = 0x0000 - filled in by router/gateway with its source address which is
                //                    part of the KNX subnet
                //  Dest. Address   = KNX group or individual address (2 byte)
                //  Data Length     = Number of bytes of data in the APDU excluding the TPCI/APCI bits
                //  APDU            = Application Protocol Data Unit - the actual payload including transport
                //                    protocol control information (TPCI), application protocol control
                //                    information (APCI) and data passed as an argument from higher layers of
                //                    the KNX communication stack
                //
                datagram.MessageCode = cemi[0];
                datagram.AditionalInfoLength = cemi[1];

                if (datagram.AditionalInfoLength > 0)
                {
                    datagram.AditionalInfo = new byte[datagram.AditionalInfoLength];
                    for (var i = 0; i < datagram.AditionalInfoLength; i++)
                    {
                        datagram.AditionalInfo[i] = cemi[2 + i];
                    }
                }

                datagram.ControlField1 = cemi[2 + datagram.AditionalInfoLength];
                datagram.ControlField2 = cemi[3 + datagram.AditionalInfoLength];
                datagram.SourceAddress = GetIndividualAddress(new[] { cemi[4 + datagram.AditionalInfoLength], cemi[5 + datagram.AditionalInfoLength] });

                datagram.DestinationAddress =
                    GetKnxDestinationAddressType(datagram.ControlField2).Equals(KnxDestinationAddressType.Individual)
                        ? GetIndividualAddress(new[] { cemi[6 + datagram.AditionalInfoLength], cemi[7 + datagram.AditionalInfoLength] })
                        : GetGroupAddress(new[] { cemi[6 + datagram.AditionalInfoLength], cemi[7 + datagram.AditionalInfoLength] }, true);

                datagram.DataLength = cemi[8 + datagram.AditionalInfoLength];
                datagram.Apdu = new byte[datagram.DataLength + 1];

                for (var i = 0; i < datagram.Apdu.Length; i++)
                    datagram.Apdu[i] = cemi[9 + i + datagram.AditionalInfoLength];

                datagram.Data = new byte[datagram.Apdu.Length - 2];
                Array.Copy(datagram.Apdu, 2, datagram.Data, 0, datagram.Apdu.Length - 2);

                if (datagram.Data.Length == 0)
                {
                    datagram.Data = new byte[1];
                    datagram.Data[0] = (byte)(datagram.Apdu[1] & 0x01);
                }

                if (datagram.MessageCode != 0x29)
                    return false;

                var type = datagram.Apdu[1] >> 4;

                return true;
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Unkown error occured");
            }

            return false;
        }

        #region Address Processing
        //           +-----------------------------------------------+
        // 16 bits   |              INDIVIDUAL ADDRESS               |
        //           +-----------------------+-----------------------+
        //           | OCTET 0 (high byte)   |  OCTET 1 (low byte)   |
        //           +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        //    bits   | 7| 6| 5| 4| 3| 2| 1| 0| 7| 6| 5| 4| 3| 2| 1| 0|
        //           +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        //           |  Subnetwork Address   |                       |
        //           +-----------+-----------+     Device Address    |
        //           |(Area Adrs)|(Line Adrs)|                       |
        //           +-----------------------+-----------------------+

        //           +-----------------------------------------------+
        // 16 bits   |             GROUP ADDRESS (3 level)           |
        //           +-----------------------+-----------------------+
        //           | OCTET 0 (high byte)   |  OCTET 1 (low byte)   |
        //           +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        //    bits   | 7| 6| 5| 4| 3| 2| 1| 0| 7| 6| 5| 4| 3| 2| 1| 0|
        //           +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        //           |  | Main Grp  | Midd G |       Sub Group       |
        //           +--+--------------------+-----------------------+

        //           +-----------------------------------------------+
        // 16 bits   |             GROUP ADDRESS (2 level)           |
        //           +-----------------------+-----------------------+
        //           | OCTET 0 (high byte)   |  OCTET 1 (low byte)   |
        //           +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        //    bits   | 7| 6| 5| 4| 3| 2| 1| 0| 7| 6| 5| 4| 3| 2| 1| 0|
        //           +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        //           |  | Main Grp  |            Sub Group           |
        //           +--+--------------------+-----------------------+
        public static bool IsAddressIndividual(string address)
        {
            return address.Contains('.');
        }

        public static string GetIndividualAddress(byte[] addr)
        {
            return GetAddress(addr, '.', false);
        }

        public static string GetGroupAddress(byte[] addr, bool threeLevelAddressing)
        {
            return GetAddress(addr, '/', threeLevelAddressing);
        }

        private static string GetAddress(byte[] addr, char separator, bool threeLevelAddressing)
        {
            var group = separator.Equals('/');
            string address;

            if (group && !threeLevelAddressing)
            {
                // 2 level group
                address = (addr[0] >> 3).ToString();
                address += separator;
                address += (((addr[0] & 0x07) << 8) + addr[1]).ToString(); // this may not work, must be checked
            }
            else
            {
                // 3 level individual or group
                address = group
                    ? ((addr[0] & 0x7F) >> 3).ToString()
                    : (addr[0] >> 4).ToString();

                address += separator;

                if (group)
                    address += (addr[0] & 0x07).ToString();
                else
                    address += (addr[0] & 0x0F).ToString();

                address += separator;
                address += addr[1].ToString();
            }

            return address;
        }

        public static byte[] GetAddress(string address)
        {
            try
            {
                var addr = new byte[2];
                var threeLevelAddressing = true;
                string[] parts;
                var group = address.Contains('/');

                if (!group)
                {
                    // individual address
                    parts = address.Split('.');
                    if (parts.Length != 3 || parts[0].Length > 2 || parts[1].Length > 2 || parts[2].Length > 3)
                        throw new InvalidKnxAddressException(address);
                }
                else
                {
                    // group address
                    parts = address.Split('/');
                    if (parts.Length != 3 || parts[0].Length > 2 || parts[1].Length > 1 || parts[2].Length > 3)
                    {
                        if (parts.Length != 2 || parts[0].Length > 2 || parts[1].Length > 4)
                            throw new InvalidKnxAddressException(address);

                        threeLevelAddressing = false;
                    }
                }

                if (!threeLevelAddressing)
                {
                    var part = int.Parse(parts[0]);
                    if (part > 15)
                        throw new InvalidKnxAddressException(address);

                    addr[0] = (byte)(part << 3);
                    part = int.Parse(parts[1]);
                    if (part > 2047)
                        throw new InvalidKnxAddressException(address);

                    var part2 = BitConverter.GetBytes(part);
                    if (part2.Length > 2)
                        throw new InvalidKnxAddressException(address);

                    addr[0] = (byte)(addr[0] | part2[0]);
                    addr[1] = part2[1];
                }
                else
                {
                    var part = int.Parse(parts[0]);
                    if (part > 15)
                        throw new InvalidKnxAddressException(address);

                    addr[0] = group
                        ? (byte)(part << 3)
                        : (byte)(part << 4);

                    part = int.Parse(parts[1]);
                    if ((group && part > 7) || (!group && part > 15))
                        throw new InvalidKnxAddressException(address);

                    addr[0] = (byte)(addr[0] | part);
                    part = int.Parse(parts[2]);
                    if (part > 255)
                        throw new InvalidKnxAddressException(address);

                    addr[1] = (byte)part;
                }

                return addr;
            }
            catch (Exception)
            {
                throw new InvalidKnxAddressException(address);
            }
        }
        #endregion

        #region Control Fields
        // Bit order
        // +---+---+---+---+---+---+---+---+
        // | 7 | 6 | 5 | 4 | 3 | 2 | 1 | 0 |
        // +---+---+---+---+---+---+---+---+

        //  Control Field 1

        //   Bit  |
        //  ------+---------------------------------------------------------------
        //    7   | Frame Type  - 0x0 for extended frame
        //        |               0x1 for standard frame
        //  ------+---------------------------------------------------------------
        //    6   | Reserved
        //        |
        //  ------+---------------------------------------------------------------
        //    5   | Repeat Flag - 0x0 repeat frame on medium in case of an error
        //        |               0x1 do not repeat
        //  ------+---------------------------------------------------------------
        //    4   | System Broadcast - 0x0 system broadcast
        //        |                    0x1 broadcast
        //  ------+---------------------------------------------------------------
        //    3   | Priority    - 0x0 system
        //        |               0x1 normal (also called alarm priority)
        //  ------+               0x2 urgent (also called high priority)
        //    2   |               0x3 low
        //        |
        //  ------+---------------------------------------------------------------
        //    1   | Acknowledge Request - 0x0 no ACK requested
        //        | (L_Data.req)          0x1 ACK requested
        //  ------+---------------------------------------------------------------
        //    0   | Confirm      - 0x0 no error
        //        | (L_Data.con) - 0x1 error
        //  ------+---------------------------------------------------------------


        //  Control Field 2

        //   Bit  |
        //  ------+---------------------------------------------------------------
        //    7   | Destination Address Type - 0x0 individual address
        //        |                          - 0x1 group address
        //  ------+---------------------------------------------------------------
        //   6-4  | Hop Count (0-7)
        //  ------+---------------------------------------------------------------
        //   3-0  | Extended Frame Format - 0x0 standard frame
        //  ------+---------------------------------------------------------------
        public enum KnxDestinationAddressType
        {
            Individual = 0,
            Group = 1
        }

        public static KnxDestinationAddressType GetKnxDestinationAddressType(byte controlField2)
        {
            return (0x80 & controlField2) != 0
                ? KnxDestinationAddressType.Group
                : KnxDestinationAddressType.Individual;
        }

        #endregion

        #region DataString Processing
        // In the Common EMI frame, the APDU payload is defined as follows:

        // +--------+--------+--------+--------+--------+
        // | TPCI + | APCI + |  DataString  |  DataString  |  DataString  |
        // |  APCI  |  DataString  |        |        |        |
        // +--------+--------+--------+--------+--------+
        //   byte 1   byte 2  byte 3     ...     byte 16

        // For data that is 6 bits or less in length, only the first two bytes are used in a Common EMI
        // frame. Common EMI frame also carries the information of the expected length of the Protocol
        // DataString Unit (PDU). DataString payload can be at most 14 bytes long.  <p>

        // The first byte is a combination of transport layer control information (TPCI) and application
        // layer control information (APCI). First 6 bits are dedicated for TPCI while the two least
        // significant bits of first byte hold the two most significant bits of APCI field, as follows:

        //   Bit 1    Bit 2    Bit 3    Bit 4    Bit 5    Bit 6    Bit 7    Bit 8      Bit 1   Bit 2
        // ...+--------+--------+--------+--------+--------+--------+--------+--------++--------+----
        // |        |        |        |        |        |        |        |        ||        |
        // |  TPCI  |  TPCI  |  TPCI  |  TPCI  |  TPCI  |  TPCI  | APCI   |  APCI  ||  APCI  |
        // |        |        |        |        |        |        |(bit 1) |(bit 2) ||(bit 3) |
        // ...+--------+--------+--------+--------+--------+--------+--------+--------++--------+----....
        // ...+                            B  Y  T  E    1                            ||       B Y T E  2
        // ..+-----------------------------------------------------------------------++-------------....

        //Total number of APCI control bits can be either 4 or 10. The second byte bit structure is as follows:

        //   Bit 1    Bit 2    Bit 3    Bit 4    Bit 5    Bit 6    Bit 7    Bit 8      Bit 1   Bit 2
        // ßß--------+--------+--------+--------+--------+--------+--------+--------++--------+----....
        // |        |        |        |        |        |        |        |        ||        |
        // |  APCI  |  APCI  | APCI/  |  APCI/ |  APCI/ |  APCI/ | APCI/  |  APCI/ ||  DataString  |  DataString
        // |(bit 3) |(bit 4) | DataString   |  DataString  |  DataString  |  DataString  | DataString   |  DataString  ||        |
        // ??+--------+--------+--------+--------+--------+--------+--------+--------++--------+----....
        // ??+                            B  Y  T  E    2                            ||       B Y T E  3
        // ??+-----------------------------------------------------------------------++-------------....
        public static string GetData(int dataLength, byte[] apdu)
        {

            switch (dataLength)
            {
                case 0:
                    return string.Empty;
                case 1:
                    return Convert.ToChar(0x3F & apdu[1]).ToString();
                case 2:
                    return Convert.ToChar(apdu[2]).ToString();
                default:
                    var data = new StringBuilder();
                    for (var i = 2; i < apdu.Length; i++)
                        data.Append(Convert.ToChar(apdu[i]));

                    return data.ToString();
            }
        }

        public static int GetDataLength(byte[] data)
        {
            if (data.Length <= 0)
                return 0;

            if (data.Length == 1 && data[0] < 0x3F)
                return 1;

            if (data[0] < 0x3F)
                return data.Length;

            return data.Length + 1;
        }

        public static void WriteData(byte[] datagram, byte[] data, int dataStart)
        {
            if (data.Length == 1)
            {
                if (data[0] < 0x3F)
                {
                    datagram[dataStart] = (byte)(datagram[dataStart] | data[0]);
                }
                else
                {
                    datagram[dataStart + 1] = data[0];
                }
            }
            else if (data.Length > 1)
            {
                if (data[0] < 0x3F)
                {
                    datagram[dataStart] = (byte)(datagram[dataStart] | data[0]);

                    for (var i = 1; i < data.Length; i++)
                    {
                        datagram[dataStart + i] = data[i];
                    }
                }
                else
                {
                    for (var i = 0; i < data.Length; i++)
                    {
                        datagram[dataStart + 1 + i] = data[i];
                    }
                }
            }
        }
        #endregion

        #region Service Type

        public enum ServiceType
        {
            SearchRequest = 0x0201,
            SearchResponse = 0x0202,
            DescriptionRequest = 0x203,
            DescriptionResponse = 0x0204,
            ConnectRequest = 0x0205,
            ConnectResponse = 0x0206,
            ConnectionstateRequest = 0x0207,
            ConnectionstateResponse = 0x0208,
            DisconnectRequest = 0x0209,
            DisconnectResponse = 0x020A,
            DeviceConfigurationRequest = 0x0310,
            DeviceConfigurationAck = 0x0311,
            TunnellingRequest = 0x0420,
            TunnellingAck = 0x0421,
            RoutingIndication = 0x0530,
            RoutingLostMessage = 0x0531,
            SecureWrapper = 0x0950,
            SessionRequest = 0x0951,
            SessionResponse = 0x0952,
            SessionAuthenticate = 0x0953,
            SessionStatus = 0x0954,
            TimerNotify = 0x0955,

            Unknown = 0xFFFF
        }

        public static ServiceType GetServiceType(byte[] datagram)
        {
            int serviceType = (datagram[2] << 8) + datagram[3];

            if (Enum.IsDefined(typeof(ServiceType), serviceType))
            {
                ServiceType eServiceType = (ServiceType)serviceType;
                return eServiceType;
            }

            return ServiceType.Unknown;
        }

        public static int GetChannelId(byte[] datagram)
        {
            if (datagram.Length > 6)
                return datagram[6];

            return -1;
        }

        #endregion
    }
}
