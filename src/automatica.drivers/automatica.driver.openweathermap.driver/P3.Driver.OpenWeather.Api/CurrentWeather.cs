using P3.Driver.OpenWeather.Api.Enums;
using P3.Driver.OpenWeather.Api.Extensions;
using P3.Driver.OpenWeather.Api.Models;

namespace P3.Driver.OpenWeather.Api;

/// <summary>
/// Provides the current weather of a specific location.
/// OpenWeatherMap docs: https://openweathermap.org/current
/// </summary>
public static class CurrentWeather
{
    /// <summary>
    /// Gets the current weather of a given city.
    /// More information in https://openweathermap.org/current#name
    /// </summary>
    /// <param name="client"> The Weather client of the user. </param>
    /// <param name="cityName"> The name of the city. </param>
    /// <param name="measurement"> The measurement system used in the output. </param>
    /// <param name="language"> The language of the output. </param>
    /// <returns> The current weather. </returns>
    public static WeatherModel GetCurrentWeather(this WeatherClient client, string cityName, Measurement measurement = Measurement.Standard, Language language = Language.English) =>
        Task.Run(() => client.GetCurrentWeatherAsync(cityName, measurement, language)).Result;

    /// <summary>
    /// Gets the current weather of a given city.
    /// More information in https://openweathermap.org/current#cityid
    /// </summary>
    /// <param name="client"> The Weather client of the user. </param>
    /// <param name="cityId"> The OpenWeatherMap City ID. </param>
    /// <param name="measurement"> The measurement system used in the output. </param>
    /// <param name="language"> The language of the output. </param>
    /// <returns> The current weather. </returns>
    public static WeatherModel GetCurrentWeather(this WeatherClient client, long cityId, Measurement measurement = Measurement.Standard, Language language = Language.English) =>
        Task.Run(() => client.GetCurrentWeatherAsync(cityId, measurement, language)).Result;
    
    /// <summary>
    /// Gets the current weather of a given location.
    /// More information in https://openweathermap.org/current#geo
    /// </summary>
    /// <param name="client"> The Weather client of the user. </param>
    /// <param name="latitude"> The latitude of the location. </param>
    /// <param name="longitude"> The longitude of the location. </param>
    /// <param name="measurement"> The measurement system used in the output. </param>
    /// <param name="language"> The language of the output. </param>
    /// <returns> The current weather. </returns>
    public static WeatherModel GetCurrentWeather(this WeatherClient client, double latitude, double longitude, Measurement measurement = Measurement.Standard, Language language = Language.English) =>
        Task.Run(() => client.GetCurrentWeatherAsync(latitude, longitude, measurement, language)).Result;

    /// <summary>
    /// Gets the current weather of a given location.
    /// More information in https://openweathermap.org/current#zip
    /// </summary>
    /// <param name="client"> The Weather client of the user. </param>
    /// <param name="zipCode"> The ZIP Code of the location. </param>
    /// <param name="countryCode"> The ISO 3166 country code of the location, defaults to the United States. </param>
    /// <param name="measurement"> The measurement system used in the output. </param>
    /// <param name="language"> The language of the output. </param>
    /// <returns> The current weather. </returns>
    public static WeatherModel GetCurrentWeather(this WeatherClient client, string zipCode, string countryCode, Measurement measurement = Measurement.Standard, Language language = Language.English) =>
        Task.Run(() => client.GetCurrentWeatherAsync(zipCode, countryCode, measurement, language)).Result;

    /// <summary>
    /// Gets the current weather of a given city asynchronously.
    /// More information in https://openweathermap.org/current#name
    /// </summary>
    /// <param name="client"> The Weather client of the user. </param>
    /// <param name="cityName"> The name of the city. </param>
    /// <param name="measurement"> The measurement system used in the output. </param>
    /// <param name="language"> The language of the output. </param>
    /// <returns> The current weather. </returns>
    public static async Task<WeatherModel> GetCurrentWeatherAsync(this WeatherClient client, string cityName, Measurement measurement = Measurement.Standard, Language language = Language.English)
    {
        string file = await RestApi.GetWebpageStringAsync($"https://api.openweathermap.org/data/2.5/weather?q={cityName}&appid={client.ApiKey}&units={measurement.Convert()}&lang={language.Convert()}");
        return JsonConvert.DeserializeObject<WeatherModel>(file);
    }

    /// <summary>
    /// Gets the current weather of a given city asynchronously.
    /// More information in https://openweathermap.org/current#cityid
    /// </summary>
    /// <param name="client"> The Weather client of the user. </param>
    /// <param name="cityId"> The OpenWeatherMap City ID. </param>
    /// <param name="measurement"> The measurement system used in the output. </param>
    /// <param name="language"> The language of the output. </param>
    /// <returns> The current weather. </returns>
    public static async Task<WeatherModel> GetCurrentWeatherAsync(this WeatherClient client, long cityId, Measurement measurement = Measurement.Standard, Language language = Language.English)
    {
        string file = await RestApi.GetWebpageStringAsync($"https://api.openweathermap.org/data/2.5/weather?id={cityId}&appid={client.ApiKey}&units={measurement.Convert()}&lang={language.Convert()}");
        return JsonConvert.DeserializeObject<WeatherModel>(file);
    }

    /// <summary>
    /// Gets the current weather of a given location asynchronously.
    /// More information in https://openweathermap.org/current#geo
    /// </summary>
    /// <param name="client"> The Weather client of the user. </param>
    /// <param name="latitude"> The latitude of the location. </param>
    /// <param name="longitude"> The longitude of the location. </param>
    /// <param name="measurement"> The measurement system used in the output. </param>
    /// <param name="language"> The language of the output. </param>
    /// <returns> The current weather. </returns>
    public static async Task<WeatherModel> GetCurrentWeatherAsync(this WeatherClient client, double latitude, double longitude, Measurement measurement = Measurement.Standard, Language language = Language.English)
    {
        string file = await RestApi.GetWebpageStringAsync($"https://api.openweathermap.org/data/2.5/weather?lat={latitude.ToString(CultureInfo.InvariantCulture)}&lon={longitude.ToString(CultureInfo.InvariantCulture)}&appid={client.ApiKey}&units={measurement.Convert()}&lang={language.Convert()}");
        return JsonConvert.DeserializeObject<WeatherModel>(file);
    }

    /// <summary>
    /// Gets the current weather of a given location asynchronously.
    /// More information in https://openweathermap.org/current#zip
    /// </summary>
    /// <param name="client"> The Weather client of the user. </param>
    /// <param name="zipCode"> The ZIP Code of the location. </param>
    /// <param name="countryCode"> The ISO 3166 country code of the location, defaults to the United States. </param>
    /// <param name="measurement"> The measurement system used in the output. </param>
    /// <param name="language"> The language of the output. </param>
    /// <returns> The current weather. </returns>
    public static async Task<WeatherModel> GetCurrentWeatherAsync(this WeatherClient client, string zipCode, string countryCode, Measurement measurement = Measurement.Standard, Language language = Language.English)
    {
        string file = await RestApi.GetWebpageStringAsync($"https://api.openweathermap.org/data/2.5/weather?zip={zipCode},{countryCode}&appid={client.ApiKey}&units={measurement.Convert()}&lang={language.Convert()}");
        return JsonConvert.DeserializeObject<WeatherModel>(file);
    }
}