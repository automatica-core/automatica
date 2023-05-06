// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.Pixoo64.Screens;
using P3.PixooSharp;

Console.WriteLine("Hello, World!");


var pixoo = new Pixoo64("192.168.8.118", 64, true);
var pixoo2 = new Pixoo64("192.168.8.112", 64, true);

var pixooList = new List<Pixoo64> { pixoo2, pixoo };

var batteryScreen = new BatteryScreen(pixooList, NullLogger.Instance);
var cryptoScreen = new CryptoPriceScreen(pixooList, NullLogger.Instance);
var meterScreen = new MeterScreen(pixooList, NullLogger.Instance);
var infoScreen = new InfoScreen(pixooList, NullLogger.Instance);
var screens = new List<BaseScreen>();

screens.Add(batteryScreen);
screens.Add(cryptoScreen);
screens.Add(meterScreen);
screens.Add(infoScreen);


await infoScreen.Paint();
Console.ReadLine();

int i = 0;
while (true)
{
    var screen = screens[i];
    screen.Start();

    do
    {
        await screen.Paint();
        Thread.Sleep(1000);
    } while (screen.TimeForNextScreen > 0);


    i++;

    if (i >= screens.Count)
    {
        i = 0;
    }
}



Console.ReadLine();