using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using P3.Driver.HueBridge.Data;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace P3.Driver.HueBridge.Api
{
    /// <summary>
    /// Handles PUT /api/{user}/lights/{id}/state and PUT /api/{user}/groups/{id}/action
    /// </summary>
    public class SetLightMiddleware
    {
        internal const int MaxBodyLength = 4096;

        private readonly RequestDelegate _next;
        private readonly HueDriver _driver;

        public SetLightMiddleware(RequestDelegate next, HueDriver driver)
        {
            _next = next;
            _driver = driver;
        }

        public async Task Invoke(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            if (context.Request.Method == "PUT" && (path.EndsWith("/state") || path.EndsWith("/action")))
            {
                var response = await Handle(path, context.Request.Body);
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(response);
                return;
            }

            await _next.Invoke(context);
        }

        private static async Task<string> ReadBody(Stream body)
        {
            var buffer = new char[MaxBodyLength + 1];
            using (var reader = new StreamReader(body, Encoding.UTF8))
            {
                var read = 0;
                while (read < buffer.Length)
                {
                    var count = await reader.ReadAsync(buffer, read, buffer.Length - read);
                    if (count == 0)
                    {
                        break;
                    }
                    read += count;
                }

                return read > MaxBodyLength ? null : new string(buffer, 0, read);
            }
        }

        internal async Task<string> Handle(string path, Stream body)
        {
            // "", "api", "{user}", "lights"|"groups", "{id}", "state"|"action"
            var split = path.Split('/');
            if (split.Length != 6 || split[1] != "api")
            {
                return HueErrors.Error(HueErrors.ResourceNotAvailable, path, $"resource, {path}, not available");
            }

            var user = split[2];
            var resource = split[3];
            var address = path.Substring("/api/".Length + user.Length);

            if (!_driver.IsAuthorized(user))
            {
                HueDriver.Logger.LogWarning("Rejected Hue light request from an unknown user");
                return HueErrors.Error(HueErrors.UnauthorizedUser, address, "unauthorized user");
            }

            if (!int.TryParse(split[4], out var id))
            {
                return HueErrors.Error(HueErrors.ResourceNotAvailable, address, $"resource, {address}, not available");
            }

            var data = await ReadBody(body);
            HueSwitchLightData setLightData;
            try
            {
                setLightData = data == null ? null : JsonConvert.DeserializeObject<HueSwitchLightData>(data);
            }
            catch (JsonException)
            {
                setLightData = null;
            }

            if (setLightData == null)
            {
                return HueErrors.Error(HueErrors.InvalidJson, address, "body contains invalid json");
            }

            HueDriver.Logger.LogDebug($"Received '{split[5]}' for {resource} {id}");

            if (resource == "lights" && split[5] == "state")
            {
                var result = _driver.SetLightState(id, setLightData, true);
                if (result == null)
                {
                    return HueErrors.Error(HueErrors.ResourceNotAvailable, address, $"resource, {address}, not available");
                }

                // unchanged response format of the previous versions
                return JsonConvert.SerializeObject(result);
            }

            if (resource == "groups" && split[5] == "action" && id == 0)
            {
                _driver.SetAll(setLightData);
                return JsonConvert.SerializeObject(new[]
                {
                    new { success = new { address = "/groups/0/action/on", value = setLightData.On } }
                });
            }

            return HueErrors.Error(HueErrors.ResourceNotAvailable, address, $"resource, {address}, not available");
        }
    }
}
