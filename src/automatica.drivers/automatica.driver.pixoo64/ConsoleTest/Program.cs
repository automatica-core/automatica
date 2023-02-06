// See https://aka.ms/new-console-template for more information

using P3.Driver.Pixoo64.Screens;
using P3.PixooSharp;

Console.WriteLine("Hello, World!");


var pixoo = new Pixoo64("192.168.8.118", 64, true);

var pixooList = new List<Pixoo64> { pixoo };

var batteryScreen = new BatteryScreen(pixooList);
var cryptoScreen = new CryptoPriceScreen(pixooList);
var meterScreen = new MeterScreen(pixooList);
var infoScreen = new InfoScreen(pixooList);
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