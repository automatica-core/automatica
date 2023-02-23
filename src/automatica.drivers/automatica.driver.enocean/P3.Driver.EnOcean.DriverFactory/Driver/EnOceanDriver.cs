using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using P3.Driver.EnOcean.Data;
using P3.Driver.EnOcean.Data.Packets;
using Automatica.Core.Driver.Utility;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Logging;
using P3.Driver.EnOcean.DriverFactory.Driver.Learned;
using P3.Driver.EnOcean.DriverFactory.Templates;

namespace P3.Driver.EnOcean.DriverFactory.Driver
{
    public class EnOceanDriver : EnOceanBaseNode<EnOceanLearnedDevices>, ITeachInManager
    {
        private readonly EnOceanTemplateFactory _enoceanFactory;
        private P3.Driver.EnOcean.Driver _driver;
        private EnOceanLearnedDevices _learnedDevices;
        private readonly IList<string> _teachedInDevices = new List<string>();

        public EnOceanDriver(IDriverContext driverContext, EnOceanTemplateFactory enoceanFactory) : base(driverContext, null)
        {
            _enoceanFactory = enoceanFactory;
        }

        public override Task<bool> EnableLearnMode()
        {
            _driver.StartTeachInMode();
            return Task.FromResult(true);
        }

        public override Task<bool> DisableLearnMode()
        {
            _driver.StopTeachInMode();
            return Task.FromResult(true);
        }

        protected override bool CreateTelegramMonitor()
        {
            return true;
        }

        public override bool Init()
        {
            if (DriverContext.IsTest)
            {
                return true;
            }
            var port = GetProperty("enocean-port").ValueString;

            if (String.IsNullOrEmpty(port))
            {
                Logger.Logger.Instance.LogError($"{nameof(port)} cannot be empty");
                return false;
            }

            _driver = new P3.Driver.EnOcean.Driver(port);

            return base.Init();
        }

        public override async Task<bool> Start()
        {
            if (DriverContext.IsTest)
            {
                return true;
            }
            if (!await _driver.Open())
            {
                return false;
            }

            _driver.TelegramReceived += DriverOnTelegramReceived;
            _driver.AnswerReceived += _driver_AnswerReceived;
            _driver.PacketSent += _driver_PacketSent;
            _driver.TeachInReceived += _driver_TeachInReceived;

            return await base.Start();
        }

        private void _driver_TeachInReceived(object sender, PacketReceivedEventArgs e)
        {
            if (e.Telegram is RadioErp1Packet radio)
            {
                var serial = Utils.ByteArrayToString(radio.SenderId).Replace(" ", "");
                if (!_teachedInDevices.Contains(serial))
                {
                    var properties = new List<PropertyInstance>
                    {
                        new PropertyInstance
                        {
                            This2PropertyTemplateNavigation = new PropertyTemplate
                            {
                                Key = "enocean-serialnumber",
                                This2PropertyType = (long) PropertyTemplateType.Text,
                                This2PropertyTypeNavigation = new PropertyType
                                {
                                    Type = (long) PropertyTemplateType.Text
                                }
                            },
                            Value = serial
                        }
                    };
                    DriverContext.LearnMode.NotifyLearnNode(Utils.ByteArrayToString(radio.SenderId), "",
                        _learnedDevices.DriverContext.NodeInstance,
                        DriverContext.NodeTemplateFactory
                            .GetNodeTemplates(_enoceanFactory.GetTemplates(radio.Rorg).ToArray()).ToList(), properties);
                }
                else
                {
                    DriverContext.Logger.LogInformation($"Received teach-in form already known device {serial}");
                }
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
            if (packetReceivedEventArgs.Telegram is RadioErp1Packet radio)
            {
                TelegramMonitor.NotifyTelegram(Automatica.Core.Base.TelegramMonitor.TelegramDirection.Input, radio.SenderIdString, radio.Packet.DestinationIdString, radio.Packet.ToString(), Utils.ByteArrayToString(radio.Data));
                TelegramReceived(radio);
            }
        }

        public override Task<bool> Stop()
        {
            if(_driver != null) 
            {
                _driver.Close();
                _driver.TelegramReceived -= DriverOnTelegramReceived;
                _driver.AnswerReceived -= _driver_AnswerReceived;
                _driver.PacketSent -= _driver_PacketSent;
            }
            return base.Stop();
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

        public void SetTeachedIn(string serialnumber)
        {
            if (!_teachedInDevices.Contains(serialnumber))
            {
                _teachedInDevices.Add(serialnumber);
            }
        }
    }
}
