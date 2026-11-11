using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using P3.Driver.EnOcean.Data;
using P3.Driver.EnOcean.Data.Packets;
using Automatica.Core.Driver.Utility;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Logging;
using P3.Driver.EnOcean.DriverFactory.Driver.Learned;
using P3.Driver.EnOcean.DriverFactory.Templates;
using P3.Driver.EnOcean.Serial;
using System.Threading;

[assembly:InternalsVisibleTo("P3.Driver.EnOcean.Data.Tests")]

namespace P3.Driver.EnOcean.DriverFactory.Driver
{
    public class EnOceanDriver : EnOceanBaseNode<EnOceanLearnedDevices>, ITeachInManager
    {
        private readonly EnOceanTemplateFactory _enoceanFactory;
        private P3.Driver.EnOcean.Driver _driver;
        private EnOceanLearnedDevices _learnedDevices;
        private readonly Dictionary<string, string> _teachedInDevices = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DateTime> _teachInResponses = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private readonly EnOceanLearnSession _learnSession = new EnOceanLearnSession();
        private readonly Dictionary<string, EnOceanTypeNode> _commandNodes = new Dictionary<string, EnOceanTypeNode>(StringComparer.OrdinalIgnoreCase);
        private string _port;

        public EnOceanDriver(IDriverContext driverContext, EnOceanTemplateFactory enoceanFactory) : base(driverContext, null)
        {
            _enoceanFactory = enoceanFactory;
        }

        public override Task<bool> EnableLearnMode(CancellationToken token = default)
        {
            _learnSession.Start();
            _driver?.StartTeachInMode();
            DriverContext.Logger.LogInformation("Learn mode enabled");
            return Task.FromResult(true);
        }

        public override Task<bool> DisableLearnMode(CancellationToken token = default)
        {
            _learnSession.Stop();
            _driver?.StopTeachInMode();
            DriverContext.Logger.LogInformation("Learn mode disabled");
            return Task.FromResult(true);
        }

        protected override bool CreateTelegramMonitor()
        {
            return true;
        }

        public override async Task<bool> Init(CancellationToken token = default)
        {
            if (DriverContext.IsTest)
            {
                return true;
            }
            _port = GetProperty("enocean-port").ValueString;

            if (String.IsNullOrEmpty(_port))
            {
                Error = $"{nameof(_port)} cannot be empty";
                Logger.Logger.Instance.LogError(Error);
                return false;
            }

            _driver = new P3.Driver.EnOcean.Driver(new SerialStream(_port));

            return await base.Init(token);
        }

        public override async Task<bool> Start(CancellationToken token = default)
        {
            if (DriverContext.IsTest)
            {
                return true;
            }
            if (!await _driver.Open())
            {
                Error = $"Could not open port {_port}";
                return false;
            }

            _driver.TelegramReceived += DriverOnTelegramReceived;
            _driver.AnswerReceived += _driver_AnswerReceived;
            _driver.PacketSent += _driver_PacketSent;
            _driver.TeachInReceived += _driver_TeachInReceived;

            return await base.Start(token);
        }

        private void _driver_TeachInReceived(object sender, PacketReceivedEventArgs e)
        {
            if (e.Telegram is RadioErp1Packet radio)
            {
                LearnTelegramReceived(radio);
            }
        }

        /// <summary>
        /// Learn mode: a teach-in telegram. Unknown bidirectional devices get their teach-in response right away (they
        /// only listen for about a second), every sender is reported to the learn dialog once with what the telegram
        /// tells (profile, signal strength, already known).
        /// </summary>
        internal void LearnTelegramReceived(RadioErp1Packet radio)
        {
            var teachIn = TeachIn.Parse(radio);
            if (teachIn == null || !_learnSession.Active)
            {
                return;
            }

            var serial = radio.SenderIdString.ToUpperInvariant();
            if (teachIn.IsDeletion)
            {
                DriverContext.Logger.LogInformation($"{serial} asks to be taught out, ignored");
                return;
            }

            var known = GetKnownDevice(serial);
            var paired = false;
            if (known == null && teachIn.ResponseExpected && ClaimTeachInResponse(serial))
            {
                DriverContext.Logger.LogInformation($"{serial} ({teachIn.Eep}) waits for a teach-in response, answering");
                LastLearnResponse = SendTeachInResponse(radio, teachIn, serial);
                paired = true;
            }

            var notification = _learnSession.TeachIn(serial, teachIn, radio.Rssi, known, paired);
            if (notification != null)
            {
                Notify(notification, teachIn);
            }
        }

        /// <summary>
        /// Learn mode: a data telegram of a device that is set up, the learn dialog shows it as paired.
        /// </summary>
        internal void LearnDataReceived(RadioErp1Packet radio)
        {
            if (!_learnSession.Active || RadioErp1Packet.IsTechIn(radio))
            {
                return;
            }

            var serial = radio.SenderIdString.ToUpperInvariant();
            var known = GetKnownDevice(serial);
            if (known == null)
            {
                return;
            }

            var notification = _learnSession.Receiving(serial, radio.Rssi, known);
            if (notification != null)
            {
                Notify(notification, null);
            }
        }

        /// <summary>
        /// Tests: the last teach-in response sent in learn mode.
        /// </summary>
        internal Task<bool> LastLearnResponse { get; private set; } = Task.FromResult(false);

        /// <summary>
        /// Tests: the learn dialog notifications with the offered templates.
        /// </summary>
        internal event Action<EnOceanLearnNotification, IList<Guid>> LearnNotified;

        private Task<bool> SendTeachInResponse(RadioErp1Packet query, TeachIn teachIn, string serial)
        {
            var ute = teachIn.Kind == TeachInKind.Ute;
            var data = ute ? TeachIn.UteResponse(query.Data.Span) : EnOceanTypeNode.FourBsTeachInResponse(query.Data.Span);
            var telegram = new RadioErp1Packet(ute ? Rorg.Ute : Rorg.FourBs, data, 0x00)
            {
                // the node created for the device later derives the same offset, so the device keeps working
                SenderOffset = EnOceanTypeNode.DerivedSenderOffset(serial),
                DestinationId = query.SenderId.ToArray()
            };
            return Send(telegram);
        }

        private void Notify(EnOceanLearnNotification notification, TeachIn teachIn)
        {
            var templates = teachIn == null ? new List<Guid>() : LearnTemplates(teachIn);
            LearnNotified?.Invoke(notification, templates);

            if (DriverContext.LearnMode == null || _learnedDevices == null)
            {
                return;
            }

            IList<NodeTemplate> nodeTemplates = new List<NodeTemplate>();
            if (templates.Count > 0)
            {
                nodeTemplates = DriverContext.NodeTemplateFactory.GetNodeTemplates(templates.ToArray())
                    .OrderBy(a => templates.IndexOf(a.ObjId)).ToList();
            }

            var _ = NotifyLearnNode(notification, nodeTemplates);
        }

        private async Task NotifyLearnNode(EnOceanLearnNotification notification, IList<NodeTemplate> templates)
        {
            try
            {
                await DriverContext.LearnMode.NotifyLearnNode(notification.Serial, notification.Description,
                    _learnedDevices.DriverContext.NodeInstance, templates, notification.Properties);
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError(e, $"Could not report {notification.Serial} to the learn dialog");
            }
        }

        /// <summary>
        /// The templates the learn dialog offers: the detected profile, otherwise every device template of the RORG with
        /// the common profiles first. The cases of a two-way device are left out, the two-way template replaces them.
        /// </summary>
        internal IList<Guid> LearnTemplates(TeachIn teachIn)
        {
            IList<Guid> templates = new List<Guid>();
            if (teachIn.HasProfile)
            {
                templates = _enoceanFactory.GetDeviceTemplates(teachIn.Rorg, teachIn.Func.Value, teachIn.Type.Value);
            }
            if (templates.Count == 0)
            {
                templates = _enoceanFactory.GetDeviceTemplates(teachIn.Rorg);
            }

            return templates
                .Where(a => !EnOceanEepMetadata.IsReplyCommand(a) && !EnOceanEepMetadata.GetTwoWayOfStatus(a).HasValue)
                .OrderBy(a => EnOceanEepMetadata.IsTwoWay(a) ? 0 : 1)
                .ThenBy(EnOceanLearnSession.CommonRank)
                .ToList();
        }

        /// <returns>true once per device within a few seconds: a device with several case nodes gets one response</returns>
        public bool ClaimTeachInResponse(string serial)
        {
            lock (_teachInResponses)
            {
                var now = DateTime.UtcNow;
                if (_teachInResponses.TryGetValue(serial, out var last) && now - last < TimeSpan.FromSeconds(2))
                {
                    return false;
                }

                _teachInResponses[serial] = now;
                return true;
            }
        }

        /// <returns>the name of the node of the device, null if it is not set up</returns>
        internal string GetKnownDevice(string serial)
        {
            lock (_teachedInDevices)
            {
                return _teachedInDevices.TryGetValue(serial, out var name) ? name : null;
            }
        }

        private void _driver_PacketSent(object sender, PacketSentEventArgs e)
        {
            var idBase = _driver.IdBase;

            if (e.Telegram is RadioErp1Packet radio)
            {
                TelegramMonitor.NotifyTelegram(Automatica.Core.Base.TelegramMonitor.TelegramDirection.Output, Utils.ByteArrayToString(idBase), "FF FF FF FF", e.Packet.ToString(), Utils.ByteArrayToString(radio.Data));
            }
            else
            {
                TelegramMonitor.NotifyTelegram(Automatica.Core.Base.TelegramMonitor.TelegramDirection.Output, Utils.ByteArrayToString(idBase), "FF FF FF FF", e.Packet.ToString(), "");
            }
        }

        private void _driver_AnswerReceived(object sender, AnswerReceviedEventArgs e)
        {
            TelegramMonitor.NotifyTelegram(Automatica.Core.Base.TelegramMonitor.TelegramDirection.Input, "EnOcean Dongle", "Automatica.Core.Server", e.Packet.ToString(), Utils.ByteArrayToString(e.Packet.Data));
        }

        private void DriverOnTelegramReceived(object sender, PacketReceivedEventArgs packetReceivedEventArgs)
        {
            DriverContext.Logger.LogInformation($"Received telegram {packetReceivedEventArgs.Telegram.GetType()}");

            if (packetReceivedEventArgs.Telegram is RadioErp1Packet radio)
            {
                TelegramMonitor.NotifyTelegram(Automatica.Core.Base.TelegramMonitor.TelegramDirection.Input, radio.SenderIdString, radio.Packet.DestinationIdString, radio.Packet.ToString(), Utils.ByteArrayToString(radio.Data));
                TelegramReceived(radio);
                LearnDataReceived(radio);
            }
        }

        public override Task<bool> Stop(CancellationToken token = default)
        {
            if(_driver != null) 
            {
                _driver.Close();
                _driver.TelegramReceived -= DriverOnTelegramReceived;
                _driver.AnswerReceived -= _driver_AnswerReceived;
                _driver.PacketSent -= _driver_PacketSent;
                _driver.TeachInReceived -= _driver_TeachInReceived;
            }
            return base.Stop(token);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            if(ctx.NodeInstance.This2NodeTemplateNavigation.Key == "enocean-simulated")
            {
                return new EnOceanSimulatedDevices(ctx, _driver);
            }
            _learnedDevices = new EnOceanLearnedDevices(ctx, this);
            return _learnedDevices;
        }

        /// <summary>
        /// Tests: the stick connection to send over (Init/Start do not open one in test mode).
        /// </summary>
        internal void SetTransport(P3.Driver.EnOcean.Driver driver)
        {
            _driver = driver;
        }

        public async Task<bool> Send(RadioErp1Packet telegram)
        {
            if (_driver == null)
            {
                DriverContext.Logger.LogWarning("EnOcean stick is not open, telegram not sent");
                return false;
            }

            if (_driver.IdBase.Length != 4)
            {
                DriverContext.Logger.LogError("The id base of the EnOcean stick is unknown, telegram not sent");
                return false;
            }

            var answer = await _driver.SendTelegram(telegram);
            if (answer != null && answer.PacketType == EnOcean.PacketType.Response && answer.Data.Length > 0 && answer.Data.Span[0] != 0)
            {
                DriverContext.Logger.LogError($"EnOcean stick rejected the telegram, return code {answer.Data.Span[0]}");
                return false;
            }

            return true;
        }

        public void SetCommandNode(string serialnumber, EnOceanTypeNode node)
        {
            if (String.IsNullOrEmpty(serialnumber))
            {
                return;
            }

            lock (_commandNodes)
            {
                _commandNodes[serialnumber] = node;
            }
        }

        public EnOceanTypeNode GetCommandNode(string serialnumber)
        {
            if (String.IsNullOrEmpty(serialnumber))
            {
                return null;
            }

            lock (_commandNodes)
            {
                return _commandNodes.TryGetValue(serialnumber, out var node) ? node : null;
            }
        }

        public void SetTeachedIn(string serialnumber, string name)
        {
            if (String.IsNullOrEmpty(serialnumber))
            {
                return;
            }

            lock (_teachedInDevices)
            {
                _teachedInDevices[serialnumber] = name ?? serialnumber;
            }
        }
    }
}
