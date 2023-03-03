// See https://aka.ms/new-console-template for more information

using Automatica.Core.Base.TelegramMonitor;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.VkingBms.Driver;

Console.WriteLine("Hello, World!");

var driver = new VkingDriver("COM6", new EmptyTelegramMonitorInstance(), NullLogger.Instance);
driver.Open();

var cts = new CancellationTokenSource();

var data = await driver.ReadAnalogValues(1, cts.Token);
var data2 = await driver.ReadAnalogValues(2, cts.Token);

var bms = await driver.ReadBmsInfo(1, cts.Token);
var bms2 = await driver.ReadBmsInfo(2, cts.Token);

var ver = await driver.ReadVersionInfo(1, cts.Token);
var ver2 = await driver.ReadVersionInfo(2, cts.Token);


Console.ReadLine();
