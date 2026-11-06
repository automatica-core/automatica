using P3.Driver.OpenWeather.Api.Models;

namespace P3.Driver.OpenWeather.Api;

/// <summary>
/// Provides a description of the current, future and historical air of a specific location.
/// OpenWeatherMap docs: https://openweathermap.org/api/air-pollution
/// </summary>
public static class AirPollution
{
    /// <summary>
    /// Gets the current air pollution of a given location.
    /// </summary>
    /// <param name="client"> The WeatherClient of the user. </param>
    /// <param name="latitude"> The latitude of the location. </param>
    /// <param name="longitude"> The longitude of the location. </param>
    /// <returns> The current air pollution. </returns>
    public static PollutionModel GetCurrentAirPollution(this WeatherClient client, double latitude, double longitude) => Task.Run(() => client.GetCurrentAirPollutionAsync(latitude, longitude)).Result;

    /// <summary>
    /// Gets a forecast of the air pollution of a given location.
    /// </summary>
    /// <param name="client"> The WeatherClient of the user. </param>
    /// <param name="latitude"> The latitude of the location. </param>
    /// <param name="longitude"> The longitude of the location. </param>
    /// <returns> The forecast of the air pollution. </returns>
    public static PollutionModel GetForecastAirPollution(this WeatherClient client, double latitude, double longitude) => Task.Run(() => client.GetForecastAirPollutionAsync(latitude, longitude)).Result;

    /// <summary>
    /// Gets a historical record of the air pollution of a given location.
    /// </summary>
    /// <param name="client"> The WeatherClient of the user. </param>
    /// <param name="latitude"> The latitude of the location. </param>
    /// <param name="longitude"> The longitude of the location. </param>
    /// <param name="startUnix"> The start of the historical record, in unix seconds. </param>
    /// <param name="endUnix"> The end of the historical record, in unix seconds. </param>
    /// <returns> The historical record of the air pollution. </returns>
    public static PollutionModel GetHistoricalAirPollution(this WeatherClient client, double latitude, double longitude, long startUnix, long endUnix) => client.GetHistoricalAirPollutionAsync(latitude, longitude, startUnix, endUnix).Result;

    /// <summary>
    /// Gets the current air pollution of a given location asynchronously.
    /// </summary>
    /// <param name="client"> The WeatherClient of the user. </param>
    /// <param name="latitude"> The latitude of the location. </param>
    /// <param name="longitude"> The longitude of the location. </param>
    /// <returns> The current air pollution. </returns>
    public static async Task<PollutionModel> GetCurrentAirPollutionAsync(this WeatherClient client, double latitude, double longitude)
    {
        string file = await RestApi.GetWebpageStringAsync($"https://api.openweathermap.org/data/2.5/air_pollution?lat={latitude.ToString(CultureInfo.InvariantCulture)}&lon={longitude.ToString(CultureInfo.InvariantCulture)}&appid={client.ApiKey}");
        return JsonConvert.DeserializeObject<PollutionModel>(file);
    }

    /// <summary>
    /// Gets a forecast of the air pollution of a given location asynchronously.
    /// </summary>
    /// <param name="client"> The WeatherClient of the user. </param>
    /// <param name="latitude"> The latitude of the location. </param>
    /// <param name="longitude"> The longitude of the location. </param>
    /// <returns> The forecast of the air pollution. </returns>
    public static async Task<PollutionModel> GetForecastAirPollutionAsync(this WeatherClient client, double latitude, double longitude)
    {
        string file = await RestApi.GetWebpageStringAsync($"https://api.openweathermap.org/data/2.5/air_pollution/forecast?lat={latitude.ToString(CultureInfo.InvariantCulture)}&lon={longitude.ToString(CultureInfo.InvariantCulture)}&appid={client.ApiKey}");
        return JsonConvert.DeserializeObject<PollutionModel>(file);
    }

    /// <summary>
    /// Gets a historical record of the air pollution of a given location asynchronously.
    /// </summary>
    /// <param name="client"> The WeatherClient of the user. </param>
    /// <param name="latitude"> The latitude of the location. </param>
    /// <param name="longitude"> The longitude of the location. </param>
    /// <param name="startUnix"> The start of the historical record, in unix seconds. </param>
    /// <param name="endUnix"> The end of the historical record, in unix seconds. </param>
    /// <returns> The historical record of the air pollution. </returns>
    public static async Task<PollutionModel> GetHistoricalAirPollutionAsync(this WeatherClient client, double latitude, double longitude, long startUnix, long endUnix)
    {
        string file = await RestApi.GetWebpageStringAsync($"https://api.openweathermap.org/data/2.5/air_pollution/history?lat={latitude.ToString(CultureInfo.InvariantCulture)}&lon={longitude.ToString(CultureInfo.InvariantCulture)}&start={startUnix}&end={endUnix}&appid={client.ApiKey}");
        return JsonConvert.DeserializeObject<PollutionModel>(file);
    }
}