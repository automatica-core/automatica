using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using P3.Driver.HomeKit.Hap.Model;
using P3.Driver.HomeKit.Http;

namespace P3.Driver.HomeKit.Hap
{
    public class HapControllerServer
    {
        public HomeKitServer HomeKitServer { get; }
        public static int ConfigVersion { get; set; } = 0;

        private readonly int _port;
        internal static string HapControllerId;
        private readonly string _pairCode;

        private readonly ILogger _logger;
        internal static string HapControllerLtsk;
        internal static string HapControllerLtpk;

        private readonly HttpServer _httpServer;

        public event EventHandler<PairSetupCompleteEventArgs> PairingCompleted;

        public HapControllerServer(ILogger logger, HomeKitServer homeKitServer, int port, string ltsk, string ltpk, string controllerId, string pairCode)
        {
            HomeKitServer = homeKitServer;
            _logger = logger;
            HapControllerLtsk = ltsk;
            HapControllerLtpk = ltpk;

            _port = port;
            HapControllerId = controllerId;
            _pairCode = pairCode;

            _httpServer = new HttpServer(logger, homeKitServer, port, controllerId, pairCode);
            HapMiddleware.PairingCompleted += PairingCompletedEvent;
        }
        public async Task<bool> Start()
        {
            await _httpServer.Start();
            return true;
        }

        private void PairingCompletedEvent(object sender, PairSetupCompleteEventArgs e)
        {
            HapControllerLtsk = e.Ltsk;
            HapControllerLtpk = e.Ltpk;

            _logger.LogInformation($"LTPK {e.Ltpk} AccessoryLtsk {e.Ltsk}");
            

            PairingCompleted?.Invoke(this, e);
        }

        public async Task<bool> Stop()
        {
            await _httpServer.Stop();
            HapMiddleware.PairingCompleted -= PairingCompletedEvent;

            return true;
        }

        internal void SendNotificiation(Characteristic characteristic, List<HapSession> eventBasedNotification)
        {
            _httpServer.SendNotification(characteristic, eventBasedNotification);
        }
    }
}
