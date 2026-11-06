using P3.Driver.OpenWeather.Api.Models;

namespace P3.Driver.OpenWeather.Api;

/// <summary>
/// OpenWeather's transformation of any location name into geographical coordinates, and the other way around (reverse geocoding) (1.0).
/// More information in https://openweathermap.org/api/geocoding-api
/// </summary>
public static class GeoCoding
{
    /// <summary>
    /// Direct geocoding allows to get geographical coordinates (lat, lon) by using name of the location (city name or area name). If you use the limit parameter in the API call, you can cap how many locations with the same name will be seen in the API response (for instance, London in the UK and London in the US)
    /// </summary>
    /// <param name="client"> The client, that contains the API key. </param>
    /// <param name="cityName"> The city name. </param>
    /// <param name="stateCode"> The state code (only for the US). </param>
    /// <param name="countryCode"> The ISO 3166 country code. </param>
    /// <param name="limit"> Number of the locations in the API response (up to 5 results can be returned in the API response). </param>
    /// <returns> Returns up to 5 LocationCoordinates that match the query. </returns>
    public static List<GeoCodingModel> CoordinatesByLocationName(this WeatherClient client,
        string cityName, string stateCode="", string countryCode="", int limit=5) =>
        Task.Run(() => client.CoordinatesByLocationNameAsync(cityName, stateCode, countryCode, limit)).Result;

    /// <summary>
    /// Converts a zip/postal code to coordinates.
    /// </summary>
    /// <param name="client"> The client, that contains the API key. </param>
    /// <param name="zipCode"> The zip/post code. </param>
    /// <param name="countryCode"> The ISO 3166 country code. </param>
    /// <returns> Returns up to 5 LocationCoordinates that match the query. </returns>
    public static GeoCodingModel CoordinatesByZipCode(this WeatherClient client,
        string zipCode, string countryCode) =>
        Task.Run(() => client.CoordinatesByZipCodeAsync(zipCode, countryCode)).Result;

    /// <summary>
    /// Reverse geocoding allows to get name of the location (city name or area name) by using geografical coordinates (lat, lon). The limit parameter in the API call allows you to cap how many location names you will see in the API response.
    /// </summary>
    /// <param name="client"> The client, that contains the API key. </param>
    /// <param name="latitude"> Latitude. </param>
    /// <param name="longitude"> Longitude. </param>
    /// <param name="limit"> Number of the locations in the API response (up to 5 results can be returned in the API response). </param>
    /// <returns> Returns up to 5 LocationCoordinates that match the query. </returns>
    public static List<GeoCodingModel> LocationNameByCoordinates(this WeatherClient client,
        double latitude, double longitude, int limit=0) =>
        Task.Run(() => client.LocationNameByCoordinatesAsync(latitude, longitude, limit)).Result;

    /// <summary>
    /// Direct geocoding allows to get geographical coordinates (lat, lon) by using name of the location (city name or area name). If you use the limit parameter in the API call, you can cap how many locations with the same name will be seen in the API response (for instance, London in the UK and London in the US)
    /// </summary>
    /// <param name="client"> The client, that contains the API key. </param>
    /// <param name="cityName"> The city name. </param>
    /// <param name="stateCode"> The state code (only for the US). </param>
    /// <param name="countryCode"> The ISO 3166 country code. </param>
    /// <param name="limit"> Number of the locations in the API response (up to 5 results can be returned in the API response). </param>
    /// <returns> Returns up to 5 LocationCoordinates that match the query. </returns>
    public static async Task<List<GeoCodingModel>> CoordinatesByLocationNameAsync(this WeatherClient client,
        string cityName, string stateCode="", string countryCode="", int limit=5)
    {
        string query = cityName + (stateCode != "" ? "," : "") + stateCode + (countryCode != "" ? "," : "") + countryCode;
        string file = await RestApi.GetWebpageStringAsync($"https://api.openweathermap.org/geo/1.0/direct?q={query}&limit={limit}&appid={client.ApiKey}");
        return JsonConvert.DeserializeObject<List<GeoCodingModel>>(file);
    }

    /// <summary>
    /// Converts a zip/postal code to coordinates.
    /// </summary>
    /// <param name="client"> The client, that contains the API key. </param>
    /// <param name="zipCode"> The zip/post code. </param>
    /// <param name="countryCode"> The ISO 3166 country code. </param>
    /// <returns> Returns up to 5 LocationCoordinates that match the query. </returns>
    public static async Task<GeoCodingModel> CoordinatesByZipCodeAsync(this WeatherClient client,
        string zipCode, string countryCode)
    {
        string file = await RestApi.GetWebpageStringAsync($"https://api.openweathermap.org/geo/1.0/zip?zip={zipCode},{countryCode}&appid={client.ApiKey}");
        return JsonConvert.DeserializeObject<GeoCodingModel>(file);
    }

    /// <summary>
    /// Reverse geocoding allows to get name of the location (city name or area name) by using geografical coordinates (lat, lon). The limit parameter in the API call allows you to cap how many location names you will see in the API response.
    /// </summary>
    /// <param name="client"> The client, that contains the API key. </param>
    /// <param name="latitude"> Latitude. </param>
    /// <param name="longitude"> Longitude. </param>
    /// <param name="limit"> Number of the locations in the API response (up to 5 results can be returned in the API response). </param>
    /// <returns> Returns up to 5 LocationCoordinates that match the query. </returns>
    public static async Task<List<GeoCodingModel>> LocationNameByCoordinatesAsync(this WeatherClient client,
        double latitude, double longitude, int limit=0)
    {
        string file = await RestApi.GetWebpageStringAsync($"https://api.openweathermap.org/geo/1.0/reverse?lat={latitude.ToString(CultureInfo.InvariantCulture)}&lon={longitude.ToString(CultureInfo.InvariantCulture)}{(limit != 0 ? $"&limit={limit}" : "")}&appid={client.ApiKey}");
        return JsonConvert.DeserializeObject<List<GeoCodingModel>>(file);
    }

}