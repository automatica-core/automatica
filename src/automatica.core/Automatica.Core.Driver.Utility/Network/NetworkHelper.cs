using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Automatica.Core.Driver.Utility.Network
{
    /// <summary>
    /// Provides some network utilities
    /// </summary>
    public static class NetworkHelper
    {
        /// <summary>
        /// Return the local ip address
        /// If more than 1 are found, the first will be returned
        /// </summary>
        /// <returns></returns>
        public static string GetLocalIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    
                    return ip.ToString();
                }
            }
            throw new ArgumentException("No network adapters with an IPv4 address in the system!");
        }

        /// <summary>
        /// Returns a random unused tcp port
        /// </summary>
        /// <returns></returns>
        public static int GetFreeTcpPort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        /// <summary>
        /// Returns the active IP Address used on your local network
        /// </summary>
        /// <returns></returns>
        public static string GetActiveIp()
        {
            string ip = "";
            foreach (NetworkInterface f in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (f.OperationalStatus == OperationalStatus.Up)
                {
                    IPInterfaceProperties ipInterface = f.GetIPProperties();
                    if (ipInterface.GatewayAddresses.Count > 0)
                    {
                        foreach (UnicastIPAddressInformation unicastAddress in ipInterface.UnicastAddresses)
                        {
                            if ((unicastAddress.Address.AddressFamily ==
                                 System.Net.Sockets.AddressFamily.InterNetwork) &&
                                (unicastAddress.IPv4Mask.ToString() != "0.0.0.0"))
                            {
                                ip = unicastAddress.Address.ToString();
                                break;

                            }
                        }
                    }
                }
            }
            return ip;
        }

        /// <summary>
        /// Returns the active IP V6 Address used on your local network
        /// </summary>
        /// <returns></returns>
        public static string GetActiveIp6()
        {
            string ip = "";
            foreach (NetworkInterface f in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (f.OperationalStatus == OperationalStatus.Up)
                {
                    IPInterfaceProperties ipInterface = f.GetIPProperties();
                    if (ipInterface.GatewayAddresses.Count > 0)
                    {
                        foreach (UnicastIPAddressInformation unicastAddress in ipInterface.UnicastAddresses)
                        {
                            if (unicastAddress.Address.AddressFamily == AddressFamily.InterNetworkV6)
                            {
                                ip = unicastAddress.Address.ToString();
                                break;

                            }
                        }
                    }
                }
            }
            return ip;
        }

        /// <summary>
        /// Returns the active IP Address subnet mask
        /// </summary>
        /// <returns></returns>
        public static string GetActiveIpNetmask()
        {
            string ip = "";
            foreach (NetworkInterface f in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (f.OperationalStatus == OperationalStatus.Up)
                {
                    IPInterfaceProperties ipInterface = f.GetIPProperties();
                    if (ipInterface.GatewayAddresses.Count > 0)
                    {
                        foreach (UnicastIPAddressInformation unicastAddress in ipInterface.UnicastAddresses)
                        {
                            if ((unicastAddress.Address.AddressFamily ==
                                 System.Net.Sockets.AddressFamily.InterNetwork) &&
                                (unicastAddress.IPv4Mask.ToString() != "0.0.0.0"))
                            {
                                ip = unicastAddress.IPv4Mask.ToString();
                                break;

                            }
                        }
                    }
                }
            }
            return ip;
        }

        /// <summary>
        /// Returns the MAC Address of the active IP Interface
        /// </summary>
        /// <returns></returns>
        public static string GetActiveMacAddress()
        {
            string ip = "";
            foreach (NetworkInterface f in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (f.OperationalStatus == OperationalStatus.Up)
                {
                    IPInterfaceProperties ipInterface = f.GetIPProperties();
                    if (ipInterface.GatewayAddresses.Count > 0)
                    {
                        foreach (UnicastIPAddressInformation unicastAddress in ipInterface.UnicastAddresses)
                        {
                            if ((unicastAddress.Address.AddressFamily ==
                                 System.Net.Sockets.AddressFamily.InterNetwork) &&
                                (unicastAddress.IPv4Mask.ToString() != "0.0.0.0"))
                            {
                                ip = f.GetPhysicalAddress().ToString();
                                break;

                            }
                        }
                    }
                }
            }
            return ip;
        }

        /// <summary>
        /// Returns all IP Addresses
        /// </summary>
        /// <returns></returns>
        public static string[] GetIpAddresses()
        {
            var ips = new List<string>();
            foreach (NetworkInterface f in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (f.OperationalStatus == OperationalStatus.Up)
                {
                    IPInterfaceProperties ipInterface = f.GetIPProperties();
                    if (ipInterface.GatewayAddresses.Count > 0)
                    {
                        foreach (UnicastIPAddressInformation unicastAddress in ipInterface.UnicastAddresses)
                        {
                            if ((unicastAddress.Address.AddressFamily ==
                                 System.Net.Sockets.AddressFamily.InterNetwork) &&
                                (unicastAddress.IPv4Mask.ToString() != "0.0.0.0"))
                            {
                                ips.Add(unicastAddress.Address.ToString());

                            }
                        }
                    }
                }
            }
            return ips.ToArray();
        }
    }
}
