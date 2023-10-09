// See https://aka.ms/new-console-template for more information

using Automatica.Driver.ShellyConsole;
using Automatica.Driver.ShellyFactory.Discovery;
using Microsoft.Extensions.Logging.Abstractions;

Console.WriteLine("Hello, World!");

var discovery = new ShellyDiscoveryService(new ZeroconfDiscovery(), NullLogger.Instance);

var shelly = await discovery.SearchShellys();

Console.ReadLine();