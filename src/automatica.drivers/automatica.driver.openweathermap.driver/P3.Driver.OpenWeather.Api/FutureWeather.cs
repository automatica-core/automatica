using P3.Driver.OpenWeather.Api.Enums;
using P3.Driver.OpenWeather.Api.Extensions;
using P3.Driver.OpenWeather.Api.Models;

namespace P3.Driver.OpenWeather.Api;

/// <summary>
/// Provides a 5 day / 3 hour weather forecast of a specific location.
/// OpenWeatherMap docs: https://openweathermap.org/forecast5
/// </summary>
public static class FutureWeather
{    
    /// <summary>
    /// Gets a list of weather forecasts with a 5 day limit and a 3 hour interval.
    /// More information in https://openweathermap.org/forecast5#name5
    /// </summary>
    /// <param name="client"> The Weather client of the user. </param>
    /// <param name="cityName"> The name of the city. </param>
    /// <param name="timestampCount"> The number of forecasts in the output. </param>
    /// <param name="measurement"> The measurement system used in the output. </param>
    /// <param name="language"> The language of the output. </param>
    /// <returns> A list of weather forecasts, with their exact moment. </returns>
    public static List<WeatherModel> GetForecast(this WeatherClient client, string cityName, int timestampCount = 96, Measurement measurement = Measurement.Standard, Language language = Language.English) =>
        Task.Run(() => client.GetForecastAsync(cityName, timestampCount, measurement, language)).Result;

    /// <summary>
    /// Gets a list of weather forecasts with a 5 day limit and a 3 hour interval.
    /// More information in https://openweathermap.org/forecast5#cityid5
    /// </summary>
    /// <param name="client"> The Weather client of the user. </param>
    /// <param name="cityId"> The OpenWeatherMap City ID. </param>
    /// <param name="timestampCount"> The number of forecasts in the output. </param>
    /// <param name="measurement"> The measurement system used in the output. </param>
    /// <param name="language"> The language of the output. </param>
    /// <returns> A list of weather forecasts, with their exact moment. </returns>
    public static List<WeatherModel> GetForecast(this WeatherClient client, long cityId, int timestampCount = 96, Measurement measurement = Measurement.Standard, Language language = Language.English) =>
        Task.Run(() => client.GetForecastAsync(cityId, timestampCount, measurement, language)).Result;

    /// <summary>
    /// Gets a list of weather forecasts with a 5 day limit and a 3 hour interval.
    /// More information in https://openweathermap.org/forecast5#geo5
    /// </summary>
    /// <param name="client"> The Weather client of the user. </param>
    /// <param name="latitude"> The latitude of the location. </param>
    /// <param name="longitude"> The longitude of the location. </param>
    /// <param name="timestampCount"> The number of forecasts in the output. </param>
    /// <param name="measurement"> The measurement system used in the output. </param>
    /// <param name="language"> The language of the output. </param>
    /// <returns> A list of weather forecasts, with their exact moment. </returns>
    public static List<WeatherModel> GetForecast(this WeatherClient client, double latitude, double longitude, int timestampCount = 96, Measurement measurement = Measurement.Standard, Language language = Language.English) =>
        Task.Run(() => client.GetForecastAsync(latitude, longitude, timestampCount, measurement, language)).Result;

    /// <summary>
    /// Gets a list of weather forecasts with a 5 day limit and a 3 hour interval.
    /// More information in https://openweathermap.org/forecast5#zip
    /// </summary>
    /// <param name="client"> The Weather client of the user. </param>
    /// <param name="zipCode"> The ZIP Code of the location. </param>
    /// <param name="countryCode"> The ISO 3166 country code of the location, defaults to the United States. </param>
    /// <param name="timestampCount"> The number of forecasts in the output. </param>
    /// <param name="measurement"> The measurement system used in the output. </param>
    /// <param name="language"> The language of the output. </param>
    /// <returns> A list of weather forecasts, with their exact moment. </returns>
    public static List<WeatherModel> GetForecast(this WeatherClient client, string zipCode, string countryCode, int timestampCount = 96, Measurement measurement = Measurement.Standard, Language language = Language.English) =>
        Task.Run(() => client.GetForecastAsync(zipCode, countryCode, timestampCount, measurement, language)).Result;

    /// <summary>
    /// Gets a list of weather forecasts with a 5 day limit and a 3 hour interval asynchronously.
    /// More information in https://openweathermap.org/forecast5#name5
    /// </summary>
    /// <param name="client"> The Weather client of the user. </param>
    /// <param name="cityName"> The name of the city. </param>
    /// <param name="timestampCount"> The number of forecasts in the output. </param>
    /// <param name="measurement"> The measurement system used in the output. </param>
    /// <param name="language"> The language of the output. </param>
    /// <returns> A list of weather forecasts, with their exact moment. </returns>
    public static async Task<List<WeatherModel>> GetForecastAsync(this WeatherClient client, string cityName, int timestampCount = 96, Measurement measurement = Measurement.Standard, Language language = Language.English)
    {
        string file = await RestApi.GetWebpageStringAsync($"https://api.openweathermap.org/data/2.5/forecast?q={cityName}&appid={client.ApiKey}&cnt={timestampCount}&units={measurement.Convert()}&lang={language.Convert()}");
        return New(JsonConvert.DeserializeObject<dynamic>(file));
    }

    /// <summary>
    /// Gets a list of weather forecasts with a 5 day limit and a 3 hour interval asynchronously.
    /// More information in https://openweathermap.org/forecast5#cityid5
    /// </summary>
    /// <param name="client"> The Weather client of the user. </param>
    /// <param name="cityId"> The OpenWeatherMap City ID. </param>
    /// <param name="timestampCount"> The number of forecasts in the output. </param>
    /// <param name="measurement"> The measurement system used in the output. </param>
    /// <param name="language"> The language of the output. </param>
    /// <returns> A list of weather forecasts, with their exact moment. </returns>
    public static async Task<List<WeatherModel>> GetForecastAsync(this WeatherClient client, long cityId, int timestampCount = 96, Measurement measurement = Measurement.Standard, Language language = Language.English)
    {
        string file = await RestApi.GetWebpageStringAsync($"https://api.openweathermap.org/data/2.5/forecast?id={cityId}&appid={client.ApiKey}&cnt={timestampCount}&units={measurement.Convert()}&lang={language.Convert()}");
        return New(JsonConvert.DeserializeObject<dynamic>(file));
    }

    /// <summary>
    /// Gets a list of weather forecasts with a 5 day limit and a 3 hour interval asynchronously.
    /// More information in https://openweathermap.org/forecast5#geo5
    /// </summary>
    /// <param name="client"> The Weather client of the user. </param>
    /// <param name="latitude"> The latitude of the location. </param>
    /// <param name="longitude"> The longitude of the location. </param>
    /// <param name="timestampCount"> The number of forecasts in the output. </param>
    /// <param name="measurement"> The measurement system used in the output. </param>
    /// <param name="language"> The language of the output. </param>
    /// <returns> A list of weather forecasts, with their exact moment. </returns>
    public static async Task<List<WeatherModel>> GetForecastAsync(this WeatherClient client, double latitude, double longitude, int timestampCount = 96, Measurement measurement = Measurement.Standard, Language language = Language.English)
    {
        string file = await RestApi.GetWebpageStringAsync($"https://api.openweathermap.org/data/2.5/forecast?lat={latitude.ToString(CultureInfo.InvariantCulture)}&lon={longitude.ToString(CultureInfo.InvariantCulture)}&appid={client.ApiKey}&cnt={timestampCount}&units={measurement.Convert()}&lang={language.Convert()}");
        return New(JsonConvert.DeserializeObject<dynamic>(file));
    }

    /// <summary>
    /// Gets a list of weather forecasts with a 5 day limit and a 3 hour interval asynchronously.
    /// More information in https://openweathermap.org/forecast5#zip
    /// </summary>
    /// <param name="client"> The Weather client of the user. </param>
    /// <param name="zipCode"> The ZIP Code of the location. </param>
    /// <param name="countryCode"> The ISO 3166 country code of the location, defaults to the United States. </param>
    /// <param name="timestampCount"> The number of forecasts in the output. </param>
    /// <param name="measurement"> The measurement system used in the output. </param>
    /// <param name="language"> The language of the output. </param>
    /// <returns> A list of weather forecasts, with their exact moment. </returns>
    public static async Task<List<WeatherModel>> GetForecastAsync(this WeatherClient client, string zipCode, string countryCode, int timestampCount = 96, Measurement measurement = Measurement.Standard, Language language = Language.English)
    {
        string file = await RestApi.GetWebpageStringAsync($"https://api.openweathermap.org/data/2.5/forecast?zip={zipCode},{countryCode}&appid={client.ApiKey}&cnt={timestampCount}&units={measurement.Convert()}&lang={language.Convert()}");
        return New(JsonConvert.DeserializeObject<dynamic>(file));
    }

    private static List<WeatherModel> New(dynamic json)
    {
        // Because of the nature of the forecast JSON response,
        // the WeatherModel needs to be initialized in this way.
        
        var output = new List<WeatherModel>();
        foreach (var item in json.list)
        {
            output.Add(new WeatherModel
            {
                Location = new Location { Longitude = json.city.coord.lon, Latitude = json.city.coord.lat },

                Weather = new List<Weather> {
                new Weather {
                    Id = item.weather[0].id,
                    Title = item.weather[0].main,
                    Description = item.weather[0].description,
                    IconId = item.weather[0].icon,
                }},

                Main = new Main
                {
                    Temperature = item.main.temp,
                    TemperaturePerception = item.main.feels_like,
                    TemperatureMin = item.main.temp_min,
                    TemperatureMax = item.main.temp_max,
                    AtmosphericPressure = item.main.pressure,
                    HumidityPercentage = item.main.humidity
                },

                Visibility = item.visibility,
                Wind = new Wind { Speed = item.wind.speed, Direction = item.wind.deg, Gust = item.wind.gust },
                Clouds = new Clouds { Percentage = item.clouds.all },
                AnalysisDateUnix = item.dt,

                Internal = new Internal
                {
                    Message = json.message,
                    Country = json.city.country,
                    SunriseUnix = json.city.sunrise,
                    SunsetUnix = json.city.sunset
                },
                
                Timezone = json.city.timezone,
                CityId = json.city.id,
                CityName = json.city.name,
                StatusCode = json.cod
            });
        }

        return output;
    }
}