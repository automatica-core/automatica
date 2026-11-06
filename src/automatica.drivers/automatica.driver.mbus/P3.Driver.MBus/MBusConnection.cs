using System;
using System.Collections.Generic;
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
        public async Task SendAckWithoutRead()
        {
            await WriteFrame(new AckFrame());
            
        }

        public virtual async Task<MBusFrame> SetPrimaryAddress(int deviceId, int newDeviceId)
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
                Logger.LogDebug($"Init device {deviceId}...");
                Open();
                MBusFrame readFrame = MBusFrame.CreateShortFrame(MBus.ControlMaskSndNke, (byte) deviceId);

                Console.WriteLine(ByteArrayToString(readFrame.ToByteFrame()));
                await WriteFrame(readFrame);

                var frame = await ReadFrame();
                Logger.LogDebug($"Init device {deviceId}...done");
                return frame;
            }
            catch(Exception ex)
            {
                Logger.LogError(ex, $"Could not init device {deviceId}: {ex}");
                Close();
                throw;
            }
        }

        public virtual async Task<MBusFrame> ResetDevice(int deviceId)
        {
            try
            {
                Logger.LogDebug($"Reset device {deviceId}...");
                Open();
                // reset = SND_NKE short frame (10 40 A CS 16), SND_UD is only valid as long/control frame
                MBusFrame readFrame = MBusFrame.CreateShortFrame(MBus.ControlMaskSndNke, (byte) deviceId);

                Console.WriteLine(ByteArrayToString(readFrame.ToByteFrame()));
                await WriteFrame(readFrame);

                var frame = await ReadFrame();

                Logger.LogDebug($"Reset device {deviceId}...done");
                return frame;
            }
            catch(Exception ex)
            {
                Logger.LogError(ex, $"Could not reset device {deviceId}: {ex}");
                Close();
                throw;
            }

        }

        /// <summary>
        /// Sets the response timeout (ms) used by <see cref="ReadFrame"/>. Default: no-op.
        /// </summary>
        protected virtual void SetReadTimeout(int timeout)
        {
        }

        /// <summary>
        /// Effective response timeout for a device read: the device timeout can extend the connection timeout
        /// but never shorten it (device timeout default is 300ms, too short for a long frame over a gateway).
        /// </summary>
        internal int GetEffectiveTimeout(int deviceTimeout)
        {
            return Math.Max(deviceTimeout, _config.Timeout);
        }

        public virtual async Task<MBusFrame> ReadDevice(int deviceId, bool resetBeforeRead, int deviceTimeout)
        {
            try
            {
                Open();
                SetReadTimeout(GetEffectiveTimeout(deviceTimeout));

                Logger.LogDebug($"Read device {deviceId}...");
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

                Logger.LogDebug($"Read device {deviceId}...done");
                return res;
            }
            catch(Exception ex)
            {
                Logger.LogError(ex, $"Could not read device {deviceId}: {ex}");
                Close();
                throw;
            }
            finally
            {
                SetReadTimeout(_config.Timeout);
            }
        }
    }
}
