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
            message.QR = true;

            //message.Questions.Add(new Question()
            //    {Name = DnsHapDomain, QU = false, Class = DnsClass.IN, Type = DnsType.PTR});


            var txtList = new List<string>();
            txtList.AddProperty("sf", "1");
            txtList.AddProperty("c#", "1");
            txtList.AddProperty("s#", "1");
            txtList.AddProperty("md", _name);
            txtList.AddProperty("ff", "00");
            txtList.AddProperty("id", "be:09:ea:3e:24:aa");
            txtList.AddProperty("ci", "2");
            txtList.AddProperty("pv", "1.1");

            message.Answers.Add(new TXTRecord
            {
                Name = $"{_name}.{DnsHapDomain}", 
                Class = DnsClass.IN, 
                Type = DnsType.TXT,
                Strings = txtList, 
                TTL = TimeSpan.FromMinutes(75)
            });
            message.Answers.Add(new PTRRecord
            {
                Name = ServiceDiscovery.ServiceName, 
                DomainName = DnsHapDomain, 
                Class = DnsClass.IN, 
                Type = DnsType.PTR,
                TTL = TimeSpan.FromMinutes(75)
            });
            message.Answers.Add(new PTRRecord
            {
                Name = DnsHapDomain, 
                DomainName = $"{_name}.{DnsHapDomain}", 
                Class = DnsClass.IN, 
                Type = DnsType.PTR,
                TTL = TimeSpan.FromMinutes(75)
            });
            message.Answers.Add(new SRVRecord
            {
                Name = $"{_name}.{DnsHapDomain}", 
                Port = _port, 
                Target = $"{Dns.GetHostName()}.local", 
                Weight = 0, 
                Class = DnsClass.IN, 
                Type = DnsType.SRV,
                TTL = TimeSpan.FromMinutes(2)
            });


            return message;
        }


        public async Task Start()
        {
              _mdns.QueryReceived += _mdns_QueryReceived;

             //var serviceProvider = new ServiceDiscovery(_logger, _mdns);

             //var serviceProfile = new ServiceProfile(_name, "_hap._tcp", _port);
             //serviceProfile.AddProperty("sf", "1");
             //serviceProfile.AddProperty("ff", "0x00");
             //serviceProfile.AddProperty("ci", "2");
             //serviceProfile.AddProperty("id", "be:09:ea:3e:24:aa");
             //serviceProfile.AddProperty("md", _name);
             //serviceProfile.AddProperty("s#", "1");
             //serviceProfile.AddProperty("c#", "0");

             //serviceProvider.Advertise(serviceProfile);

             var startMessage = new Message();
             startMessage.AuthorityRecords.Add(new SRVRecord
             {
                 Name = $"{_name}.{DnsHapDomain}",
                 Port = _port
             });

             var q = new Question
             {
                 Type = DnsType.ANY,
                 Name = $"{_name}.{DnsHapDomain}",
                 QU = true,
                 Class = DnsClass.IN
             };
             startMessage.Questions.Add(q);

             _mdns.Start();

             _mdns.SendQuery(startMessage);
             await Task.Delay(1000);
             q.QU = false;
             _mdns.SendQuery(startMessage);
             await Task.Delay(1000);
             _mdns.SendQuery(startMessage);
             await Task.Delay(1000);



        }

        public async Task Stop()
        {
            var message = new Message();
            message.Answers.Add(new PTRRecord
            {
                Name = DnsHapDomain,
                DomainName = $"{_name}.{DnsHapDomain}",
                Type = DnsType.PTR,
                TTL = TimeSpan.FromSeconds(0),
                Class = DnsClass.IN
            });

            _mdns.Send(message, false);
            await Task.Delay(2000);
            _mdns.Send(message, false);
            await Task.Delay(2000);
            _mdns.Send(message, false);

            _mdns.Stop();

        }

        private void _mdns_QueryReceived(object sender, MessageEventArgs e)
        {
            if (e.Message.IsQuery && e.Message.Questions.Any(a => a.Name.Labels.Contains(HapName)))
            {
                var localAddresses = MulticastService.GetIpAddresses().ToList();
                if (localAddresses.Any(a => Equals(a, e.RemoteEndPoint.Address)))
                {
                    return;
                }

                var dnsMessage = GenerateQueryResponseMessage();

                foreach (var localAddress in localAddresses)
                {
                    dnsMessage.AdditionalRecords.Add(new ARecord
                    {
                        Name = $"{Dns.GetHostName()}.local",
                        Address = localAddress,
                        Class = DnsClass.IN,
                        Type = DnsType.A,
                        TTL = TimeSpan.FromMinutes(2)
                    });
                }

                _mdns.SendAnswer(dnsMessage, e);
            }
        }


    }
}
