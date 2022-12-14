// See https://aka.ms/new-console-template for more information

using Automatica.Core.Base.TelegramMonitor;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.VkingBms.Driver;

Console.WriteLine("Hello, World!");

var driver = new VkingDriver("COM10", new EmptyTelegramMonitorInstance(), NullLogger.Instance);
driver.Open();

var data = await driver.ReadAnalogValues(1);
var data2 = await driver.ReadAnalogValues(2);

var bms = await driver.ReadBmsInfo(1);
var bms2 = await driver.ReadBmsInfo(2);

var ver = await driver.ReadVersionInfo(1);
var ver2 = await driver.ReadVersionInfo(2);


Console.ReadLine();
