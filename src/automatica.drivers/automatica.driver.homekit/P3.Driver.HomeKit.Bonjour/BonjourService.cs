using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using P3.Driver.HomeKit.Bonjour.Abstraction;
using Microsoft.Extensions.Logging;

namespace P3.Driver.HomeKit.Bonjour
{
    internal static class ListTxtHelper
    {
        internal static List<string> AddProperty(this List<string> list, string key, string value)
        {
            list.Add($"{key}={value}");
            return list;
        }
    }

    public class BonjourService
    {
        private readonly ILogger _logger;
        private readonly ushort _port;
        private readonly string _name;
        private readonly string _localIpAddress;
        private readonly MulticastService _mdns;

        internal const string HapName = "_hap";
        internal static readonly string DnsHapDomain = $"{HapName}._tcp.local";

        public BonjourService(ILogger logger, ushort port, string name, string localIpAddress)
        {
            _logger = logger;
            _port = port;
            _name = name;
            _localIpAddress = localIpAddress;
            _mdns = new MulticastService();

        }

        private Message GenerateQueryResponseMessage()
        {
            var message = new Message();

            message.Questions.Add(new Question()
                {Name = DnsHapDomain, QU = false, Class = DnsClass.IN, Type = DnsType.PTR});


            var txtList = new List<string>();
            txtList.AddProperty("sf", "1");
            txtList.AddProperty("ff", "0x00");
            txtList.AddProperty("ci", "2");
            txtList.AddProperty("id", "be:09:ea:3e:24:aa");
            txtList.AddProperty("md", _name);
            txtList.AddProperty("s#", "1");
            txtList.AddProperty("c#", "0");
            
            message.Answers.Add(new TXTRecord { Name = $"{_name}.{DnsHapDomain}", Class = DnsClass.IN, Type = DnsType.TXT,Strings = txtList});
            message.Answers.Add(new PTRRecord { Name = ServiceDiscovery.ServiceName, DomainName = DnsHapDomain, Class = DnsClass.IN, Type = DnsType.PTR });
            message.Answers.Add(new PTRRecord { Name = DnsHapDomain, DomainName = $"{_name}.{DnsHapDomain}", Class = DnsClass.IN, Type = DnsType.PTR });
            message.Answers.Add(new SRVRecord { Name = $"{_name}.{DnsHapDomain}", Port = _port, Target = $"{_name}.local",  Weight = 0, Class = DnsClass.IN, Type = DnsType.SRV });


            return message;
        }


        public Task Start()
        {
             // _mdns.QueryReceived += _mdns_QueryReceived;

             var serviceProvider = new ServiceDiscovery(_logger, _mdns);

             var serviceProfile = new ServiceProfile(_name, "_hap._tcp", _port);
             serviceProfile.AddProperty("sf", "1");
             serviceProfile.AddProperty("ff", "0x00");
             serviceProfile.AddProperty("ci", "2");
             serviceProfile.AddProperty("id", "be:09:ea:3e:24:aa");
             serviceProfile.AddProperty("md", _name);
             serviceProfile.AddProperty("s#", "1");
             serviceProfile.AddProperty("c#", "0");

             serviceProvider.Advertise(serviceProfile);

            _mdns.Start();

            Task.Run(async () => {

                serviceProvider.Announce(serviceProfile);
//                _mdns.SendQuery($"{_name}._hap._tcp");

                await Task.Delay(1000);

                serviceProvider.Announce(serviceProfile);
                //             _mdns.SendQuery($"{_name}._hap._tcp");

                await Task.Delay(2000);

                serviceProvider.Announce(serviceProfile);
                //           _mdns.SendQuery($"{_name}._hap._tcp");
            });

            return Task.CompletedTask;
        }

        private void _mdns_QueryReceived(object sender, MessageEventArgs e)
        {
            if (e.Message.IsQuery && e.Message.Questions.Any(a => a.Name.Labels.Contains(HapName)))
            {
                var localAddresses = MulticastService.GetLinkLocalInterfaces()
                    .Where(a => a.Address.AddressFamily == e.RemoteEndPoint.AddressFamily).ToArray();
                if (localAddresses.Any(a => Equals(a.Address, e.RemoteEndPoint.Address)))
                {
                    return;
                }

                var dnsMessage = GenerateQueryResponseMessage();

                foreach (var localAddress in localAddresses)
                {
                    dnsMessage.AdditionalRecords.Add(new ARecord
                    {
                        Name = $"{_name}.local",
                        Address = localAddress.Address,
                        Class = DnsClass.IN,
                        Type = DnsType.A
                    });
                }

                _mdns.SendAnswer(dnsMessage, e);
            }
        }

        public Task Stop()
        {
            _mdns.Stop();
            return Task.CompletedTask;
        }

    }
}
