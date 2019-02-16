﻿using System;
using System.Threading.Tasks;
using P3.Driver.EnOcean.Data;
using P3.Driver.EnOcean.Data.Packets;
using P3.Driver.EnOcean.Serial;

namespace P3.Driver.EnOcean
{
    public class Driver
    {
        private readonly string _port;
        private SerialStream _stream;

        private ReadOnlyMemory<byte> _idBase;
        private bool _learnModeActive;

        public event EventHandler<PacketReceivedEventArgs> TelegramReceived;
        public event EventHandler<PacketReceivedEventArgs> TeachInReceived;
        public event EventHandler<AnswerReceviedEventArgs> AnswerReceived;
        public event EventHandler<PacketSentEventArgs> PacketSent;

        public Driver(string port)
        {
            _port = port;
        }

        public ReadOnlyMemory<byte> IdBase => _idBase;

        public void StartTeachInMode()
        {
            _learnModeActive = true;
        }
        public void StopTeachInMode()
        {
            _learnModeActive = false;
        }

        public async Task<bool> Open()
        {
            _stream = new SerialStream(_port);

            if (_stream.Open())
            {
                _stream.TelegramReceived += TelegramReceivedEventHandler;

                var readIdBase = await SendTelegram(new CommonCommandPacket(CommonCommands.CO_RD_IDBASE));

                if(readIdBase != null)
                {
                    _idBase = readIdBase.Data.Slice(1, 4);
                    
                }


                return true;
            }

            return false;
        }
        
        public async Task<EnOceanPacket> SendTelegram(EnOceanTelegram telegram)
        {
            _stream.Pause();
            telegram.SetIdBase(_idBase.Span);

            var packet = telegram.ToPacket();

            PacketSent?.Invoke(this, new PacketSentEventArgs(packet, telegram));

            _stream.WriteFrame(packet);
            
            var p =  await _stream.ReadFrame();

            if(p != null)
            {
                AnswerReceived?.Invoke(this, new AnswerReceviedEventArgs(p));
            }

            _stream.Continue();

            return p;
        }

        public void Close()
        {
            if (_stream != null)
            {
                _stream.TelegramReceived -= TelegramReceived;
                _stream.Close();
            }
        }

        private void TelegramReceivedEventHandler(object sender, PacketReceivedEventArgs packetReceivedEventArgs)
        {
            if (packetReceivedEventArgs.Telegram is RadioErp1Packet radio1 && _learnModeActive && RadioErp1Packet.IsTechIn(radio1))
            {
                TeachInReceived?.Invoke(sender, packetReceivedEventArgs);
            }
            TelegramReceived?.Invoke(sender, packetReceivedEventArgs);
        }
    }
}
