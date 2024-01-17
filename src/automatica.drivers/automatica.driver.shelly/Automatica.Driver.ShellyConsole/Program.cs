// See https://aka.ms/new-console-template for more information

using Automatica.Core.Base.TelegramMonitor;
using Automatica.Driver.Shelly.Gen1.Options;
using Automatica.Driver.Shelly.Gen2;
using Automatica.Driver.ShellyConsole;
using Automatica.Driver.ShellyFactory.Discovery;
using Microsoft.Extensions.Logging.Abstractions;

Console.WriteLine("Hello, World!");

var discovery = new ShellyDiscoveryService(new ZeroconfDiscovery(), NullLogger.Instance);

var shelly = await discovery.SearchShellys();

var shellyGen2 = new ShellyGen2Client(new EmptyTelegramMonitorInstance(), new ShellyOptions
{
    IpAddress = "192.168.8.165",
    ShellyId = "shellyplus1pm-b0b21c1ae944",
    UserName = "admin",
    ServerUri = new Uri("http://192.168.8.165"),
    Password = null
}, NullLogger.Instance);

await shellyGen2.Connect();


shellyGen2.OnNotifyEvent += (sender, eventMessage) =>
{
    Console.WriteLine($"Notify event: {eventMessage}");
};
await shellyGen2.GetRelayState(0);

Console.ReadLine();