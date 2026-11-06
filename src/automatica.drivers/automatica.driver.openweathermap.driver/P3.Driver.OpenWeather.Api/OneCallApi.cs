using P3.Driver.OpenWeather.Api.Enums;
using P3.Driver.OpenWeather.Api.Extensions;
using P3.Driver.OpenWeather.Api.Models.OneCallModel;

namespace P3.Driver.OpenWeather.Api;

/// <summary>
/// Provides easy calls to OpenWeather's One Call API.
/// More information in https://openweathermap.org/api/one-call-api
/// </summary>
public static class OneCallApi
{
    /// <summary>
    /// Gets the One Call API that contains the current weather, minutely, hourly, daily forecasts and national alerts.
    /// More information in https://openweathermap.org/api/one-call-api#current
    /// </summary>
    /// <param name="client"> The WeatherClient of the user. </param>
    /// <param name="latitude"> The latitude of the location. </param>
    /// <param name="longitude"> The longitude of the location. </param>
    /// <param name="exclude"> The parameters to exclude in the API response. </param>
    /// <param name="measurementSystem"> The measurement system of the output. </param>
    /// <param name="language"> The language of the output. </param>
    /// <returns> The OneCall API response, excluding the said parameters. </returns>
    public static OneCallModel GetOneCall(this WeatherClient client, double latitude, double longitude, ExcludeOneCall[] exclude, Measurement measurementSystem = Measurement.Standard, Language language = Language.English) =>
        Task.Run(() => client.GetOneCallAsync(latitude, longitude, exclude, measurementSystem, language)).Result;

    /// <summary>
    /// Gets the One Call API that contains the current weather, minutely, hourly, daily forecasts and national alerts asynchronously.
    /// More information in https://openweathermap.org/api/one-call-api#current
    /// </summary>
    /// <param name="client"> The WeatherClient of the user. </param>
    /// <param name="latitude"> The latitude of the location. </param>
    /// <param name="longitude"> The longitude of the location. </param>
    /// <param name="exclude"> The parameters to exclude in the API response. </param>
    /// <param name="measurementSystem"> The measurement system of the output. </param>
    /// <param name="language"> The language of the output. </param>
    /// <returns> The OneCall API response, excluding the said parameters. </returns>
    public static async Task<OneCallModel> GetOneCallAsync(this WeatherClient client, double latitude, double longitude, ExcludeOneCall[] exclude, Measurement measurementSystem = Measurement.Standard, Language language = Language.English)
    {
        string file;
        if (exclude.Length == 0)
            file = await RestApi.GetWebpageStringAsync($"https://api.openweathermap.org/data/3.0/onecall?lat={latitude.ToString(CultureInfo.InvariantCulture)}&lon={longitude.ToString(CultureInfo.InvariantCulture)}&appid={client.ApiKey}&units={measurementSystem.Convert()}&lang={language.Convert()}");
        else
            file = await RestApi.GetWebpageStringAsync($"https://api.openweathermap.org/data/3.0/onecall?lat={latitude.ToString(CultureInfo.InvariantCulture)}&lon={longitude.ToString(CultureInfo.InvariantCulture)}&exclude={string.Join(',', exclude.Select(x => x.Convert()))}&appid={client.ApiKey}&units={measurementSystem.Convert()}&lang={language.Convert()}");
        return JsonConvert.DeserializeObject<OneCallModel>(file);
    }

    /// <summary>
    /// Gets a historical One Call API call.
    /// There is no MinutelyForecast, DailyForecast and NationalAlerts,
    /// CurrentWeather refers to the requested date
    /// and HourlyForecast refers to an hourly analysis of the requested day.
    /// More information in https://openweathermap.org/api/one-call-api#hist_parameter
    /// </summary>
    /// <param name="client"> The WeatherClient of the user. </param>
    /// <param name="latitude"> The latitude of the location. </param>
    /// <param name="longitude"> The longitude of the location. </param>
    /// <param name="dateUnix"> The requested UTC date in unix time. </param>
    /// <param name="measurementSystem"> The measurement system of the output. </param>
    /// <param name="language"> The language of the output. </param>
    /// <returns> The OneCallModel response, acting as a "time machine" as a normal OneCall API call but for the requested day. </returns>
    public static OneCallModel GetHistoricalOneCall(this WeatherClient client, double latitude, double longitude, long dateUnix, Measurement measurementSystem = Measurement.Standard, Language language = Language.English) =>
        Task.Run(() => client.GetHistoricalOneCallAsync(latitude, longitude, dateUnix, measurementSystem, language)).Result;

    /// <summary>
    /// Gets a historical One Call API call to goes up to 5 days in the past.
    /// There is no MinutelyForecast, DailyForecast and NationalAlerts,
    /// CurrentWeather refers to the requested date
    /// and HourlyForecast refers to an hourly analysis of the requested day.
    /// More information in https://openweathermap.org/api/one-call-api#hist_parameter
    /// </summary>
    /// <param name="client"> The WeatherClient of the user. </param>
    /// <param name="latitude"> The latitude of the location. </param>
    /// <param name="longitude"> The longitude of the location. </param>
    /// <param name="dateUnix"> The requested UTC date in unix time. </param>
    /// <param name="measurementSystem"> The measurement system of the output. </param>
    /// <param name="language"> The language of the output. </param>
    /// <returns> The OneCallModel response, acting as a "time machine" as a normal OneCall API call but for the requested day. </returns>
    public static async Task<OneCallModel> GetHistoricalOneCallAsync(this WeatherClient client, double latitude, double longitude, long dateUnix, Measurement measurementSystem = Measurement.Standard, Language language = Language.English)
    {
        string file = await RestApi.GetWebpageStringAsync($"https://api.openweathermap.org/data/2.5/onecall/timemachine?lat={latitude.ToString(CultureInfo.InvariantCulture)}&lon={longitude.ToString(CultureInfo.InvariantCulture)}&dt={dateUnix}&appid={client.ApiKey}&units={measurementSystem.Convert()}&lang={language.Convert()}");
        return JsonConvert.DeserializeObject<OneCallModel>(file);
    }
}