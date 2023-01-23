// See https://aka.ms/new-console-template for more information

using P3.Driver.Pixoo64.Screens;
using P3.PixooSharp;

Console.WriteLine("Hello, World!");


var pixoo = new Pixoo64("192.168.8.118", 64, true);

var batteryScreen = new BatteryScreen(pixoo);
var cryptoScreen = new CryptoPriceScreen(pixoo);
var meterScreen = new MeterScreen(pixoo);
var infoScreen = new InfoScreen(pixoo);
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