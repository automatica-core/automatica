using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Internal;
using P3.Driver.EBus.Config;

namespace P3.Driver.EBus
{
    public abstract class EBus : IEBus
    {
        private readonly object _lock = new object();

        private readonly byte[] _buffer = new byte[1024];
        private int _pos = 0;
        private bool _inEscape = false;

        protected EBus(IEBusConfig config)
        {
            
        }

        public abstract Task<bool> Connect();

        public abstract Task<bool> Disconnect();

        protected byte[] Add(byte b)
        {
            if (_inEscape)
            {
                if (b == 0x00)
                {
                    _inEscape = false;
                    return AddByteToBuffer(Message.EscapeByte);

                }
                else if (b == 0x01)
                {
                    _inEscape = false;
                    return AddByteToBuffer(Message.SyncByte);
                }
                else
                {
                    return null;
                }
            }

            if (b == Message.EscapeByte)
            {
                _inEscape = true;
            }
            else if (b == Message.SyncByte)
            {
                return AddByteToBuffer(b);  // invalid escape sequence
            }
            else
            {
                return AddByteToBuffer(b);
            }

            return null;
        }

        private byte[] AddByteToBuffer(byte b)
        {
            lock (_lock)
            {
                if (_pos == _buffer.Length)
                {
                    _pos = 0;
                }

                _buffer[_pos] = b;
                _pos++;

                return CheckForMessages();
            }
        }

        private byte[] CheckForMessages()
        {
            int pos = 0;
            while((pos = Array.IndexOf(_buffer, Message.SyncByte))>= 0)
            {
                var localBuf = new byte[pos+1];
                Array.Copy(_buffer, 0, localBuf, 0, pos+1);
                Array.Fill(_buffer, (byte)0, 0, pos + 1);

                _pos = 0;

                return localBuf;
            }

            return null;
        }
    }
}
