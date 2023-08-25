using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using Automatica.Core.Driver.Utility;

[assembly: InternalsVisibleTo("P3.Driver.WakeOnLan.Tests")]

namespace P3.Driver.WakeOnLan
{
    public class WakeOnLan : DriverNoneAttributeBase
    {
        public WakeOnLan(IDriverContext ctx) : base(ctx)
        {
        }

        public override Task WriteValue(IDispatchable source, object value)
        {
            var mac = GetPropertyValueString("mac");
            if (!String.IsNullOrEmpty(mac))
            {
                DriverContext.Logger.LogDebug($"Try to wake up {mac}");
                if (!WakeUp(DriverContext.Logger, mac, DriverContext))
                {
                    DriverContext.Logger.LogError($"Could not wake up {mac}");
                }

                DispatchRead(mac);
            }
            return Task.CompletedTask;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }

        internal static byte[] BuildWakeUpPacket(string macAddress)
        {
            byte[] mac;
            
            if (macAddress.Contains("-"))
            {
                mac = macAddress.Split('-').Select(s => Convert.ToByte(s, 16)).ToArray();
            }
            else if(macAddress.Contains(":"))
            {
                mac = macAddress.Split(':').Select(s => Convert.ToByte(s, 16)).ToArray();
            }
            else
            {
                mac = Utils.StringToByteArray(macAddress);

            }

            if (mac != null)
            {
                // WOL packet contains a 6-bytes trailer and 16 times a 6-bytes sequence containing the MAC address.
                byte[] packet = new byte[17 * 6];

                // Trailer of 6 times 0xFF.
                for (int i = 0; i < 6; i++)
                    packet[i] = 0xFF;
                // Body of magic packet contains 16 times the MAC address.
                for (int i = 1; i <= 16; i++)
                for (int j = 0; j < 6; j++)
                    packet[i * 6 + j] = mac[j];

                return packet;
            }

            return new byte[0];
        }

        public static bool WakeUp(ILogger logger, string macAddress, IDriverContext ctx)
        {
            if(ctx.IsTest)
            {
                return true;
            }
            var retVal = false;

            var packet = BuildWakeUpPacket(macAddress);
            if (packet.Length > 0)
            {
                // WOL packet is sent over UDP 255.255.255.0:40000.
                using (UdpClient client = new UdpClient())
                {
                    client.Client.Connect(IPAddress.Broadcast, 40000);
                    client.EnableBroadcast = true;
                    // Submit
                    int result = client.Client.Send(packet);
                    logger.LogHexOut(packet);
                    if (result > 0)
                    {
                        retVal = true;
                    }
                }
            }

            return retVal;
        }

    }
}
