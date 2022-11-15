using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
using Microsoft.Extensions.Logging;
using P3.Driver.MBus.Config;
using P3.Driver.MBus.Frames;
using P3.Driver.MBus.Scan;

namespace P3.Driver.MBus
{ 

    public abstract class MBusConnection
    {
        private readonly MBusConfig _config;
        protected ILogger Logger { get; }

        protected ITelegramMonitorInstance TelegramMonitor { get; }

        protected MBusConnection(MBusConfig config, ITelegramMonitorInstance telegramMonitor, ILogger logger)
        {
            _config = config;
            Logger = logger;
            TelegramMonitor = telegramMonitor;
        }
        public abstract bool Open();

        public abstract bool Close();

        public abstract bool IsConnected();

        public virtual async Task<IList<ScanResult>> Scan(Action<int, int> onProgress)
        {
            try
            {
                Open();
                var result = new List<ScanResult>();

                for (int i = 1; i < 250; i++)
                {
                   
                    var dataFrame = await ReadDevice(i, false, 1000);
                    if (dataFrame != null)
                    {
                        result.Add(new ScanResult(i, dataFrame));
                    }

                    try
                    {
                        onProgress(i, result.Count);
                    }
                    catch (OperationCanceledException)
                    {
                        return result;
                    }
                }

                return result;
            }
            catch
            {
                Close();
                throw;
            }
        }

        protected abstract Task WriteFrame(MBusFrame frame);
        public abstract Task WriteRaw(ReadOnlyMemory<byte> data);
        protected abstract Task<MBusFrame> ReadFrame();

        public Task<MBusFrame> TryReadFrame()
        {
            return ReadFrame();
        }

        public async Task<MBusFrame> SendAck()
        {
            await WriteFrame(new AckFrame());

            return await ReadFrame();
        }

        public virtual async Task<MBusFrame> SetPrimaryAddres(int deviceId, int newDeviceId)
        {
            try
            {
                Open();

                var frame = MBusFrame.CreateChangePrimaryAddressFrame((byte) deviceId, (byte) newDeviceId);
                Console.WriteLine(ByteArrayToString(frame.ToByteFrame()));
               await  WriteFrame(frame);

                return await ReadFrame();
            }
            catch
            {
                Close();
                throw;
            }
        }

        public static string ByteArrayToString(ReadOnlyMemory<byte> data)
        {
            string hex = BitConverter.ToString(data.ToArray());
            return hex.Replace("-", " ");
        }

        public virtual async Task<MBusFrame> InitDevice(int deviceId)
        {
            try
            {
                Open();
                MBusFrame readFrame = MBusFrame.CreateShortFrame(MBus.ControlMaskSndNke, (byte) deviceId);

                Console.WriteLine(ByteArrayToString(readFrame.ToByteFrame()));
                await WriteFrame(readFrame);

                return await ReadFrame();
            }
            catch
            {
                Close();
                throw;
            }
        }

        public virtual async Task<MBusFrame> ResetDevice(int deviceId)
        {
            try
            {
                Open();
                MBusFrame readFrame = MBusFrame.CreateShortFrame(MBus.ControlMaskSndUd, (byte) deviceId);

                Console.WriteLine(ByteArrayToString(readFrame.ToByteFrame()));
                await WriteFrame(readFrame);

                return await ReadFrame();
            }
            catch
            {
                Close();
                throw;
            }

        }

        public virtual async Task<MBusFrame> ReadDevice(int deviceId, bool resetBeforeRead, int deviceTimeout)
        {
            try
            {
                Open();
                if (_config.ResetBeforeRead || resetBeforeRead)
                {
                    var init = await ResetDevice(deviceId);

                    if (init == null)
                    {
                        return null;
                    }
                }
                MBusFrame readFrame = MBusFrame.CreateShortFrame(MBus.ControlMaskReqUd2, (byte) deviceId);
                await TelegramMonitor.NotifyTelegram(TelegramDirection.Output, "SELF", deviceId.ToString(), readFrame.ToString(), "");
                await WriteFrame(readFrame);

                var res = await ReadFrame();

                if(res != null)
                {
                    await TelegramMonitor.NotifyTelegram(TelegramDirection.Output, res.AddressField.ToString(), "SELF", res.ToString(), "");
                }

                return res;
            }
            catch
            {
                Close();
                throw;
            }
        }
    }
}
