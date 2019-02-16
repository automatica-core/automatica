using OpenWeatherMap;
using System;

namespace P3.Driver.OpenWeather.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("Hello World!");

            var client = new OpenWeatherMapClient("SECRET");

            var cords = new Coordinates()
            {
                Latitude = 50,
                Longitude = 40
            };
            var x = client.CurrentWeather.GetByCoordinates(cords, MetricSystem.Metric, OpenWeatherMapLanguage.EN);
            
            x.ContinueWith(a =>
            {

            });
           
            System.Console.ReadLine();
        }
    }
}
