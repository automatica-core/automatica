using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace P3.Driver.VkingBms.Driver
{
    internal class Command
    {
        private const byte PreHead = 126;
        private const byte Version = 82;
        private const byte Cid1 = 70;
        private const byte Tail = 13;

        private byte[] _body = {};
        private readonly byte _cid;

        public byte Address { get; }
        public Command(byte address, byte cid)
        {
            Address = address;
            _cid = cid;
        }

        public void SetBody(byte[] data) => _body = data;

        public byte[] ToByteArray()
        {
            var package = new List<byte>();


            ushort crc = _body.Length == 0 ? Util.CalLCheckSum(0) : Util.CalLCheckSum((ushort)(_body.Length * 2));

            var preHeader = Encoding.ASCII.GetString(new byte[] { 126  });
            var datagram = "";

            var headerL = new List<byte>();
            headerL.Add(Version);
            headerL.Add(Address);
            headerL.Add(Cid1);
            headerL.Add(_cid);
            headerL.Add(BitConverter.GetBytes(crc).Last());
            headerL.Add(BitConverter.GetBytes(crc).First());


            var header = BitConverter.ToString(headerL.ToArray()).Replace("-", "");
            string? body = null;
            if (_body.Length > 0)
            {
                body = BitConverter.ToString(this._body).Replace("-", "");
                datagram = header + body;
            }
            else
            {
                datagram = header;
            }
            var crc2 = Util.CalFrameCheckSum(Encoding.Default.GetBytes(datagram));

            string crc2Str = BitConverter.ToString(new byte[2]
            {
                BitConverter.GetBytes(crc2).Last<byte>(),
                BitConverter.GetBytes(crc2).First<byte>()
            }).Replace("-", "");

            string end = Encoding.ASCII.GetString(new byte[] { 13 });
            var encoded2 = Encoding.ASCII.GetBytes("~52014642E00201FD30\r");

            if (body != null)
            {
                datagram = preHeader + header + body + crc2Str + end;
            }
            else
            {
                datagram = preHeader + header + crc2Str + end;
            }

            var ret = Encoding.ASCII.GetBytes(datagram);

            return ret;
        }
    }
}
