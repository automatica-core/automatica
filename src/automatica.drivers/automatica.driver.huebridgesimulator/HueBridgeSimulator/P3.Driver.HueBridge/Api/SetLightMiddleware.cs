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
    public class SetLightMiddleware
    {
        private readonly RequestDelegate _next;
        public SetLightMiddleware(RequestDelegate next, ILogger<WebApiErrorMiddleware> logger)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path.Value.EndsWith("state") && context.Request.Method == "PUT")
            {
                using (StreamReader reader = new StreamReader(context.Request.Body, Encoding.UTF8))
                {
                    var split = context.Request.Path.Value.Split("/");
                    var data = await reader.ReadToEndAsync();

                    HueDriver.Logger.LogDebug($"Received 'state' action url {context.Request.Path.ToString()} data {data}");

                    // /api/{user}/lights/{id}/state
                    if (split.Length < 6 || split[3] != "lights" || !int.TryParse(split[4], out var lightId) || !HueDriver.Instance.Lights.ContainsKey(lightId))
                    {
                        await context.Response.WriteAsync(HueController.ResourceNotAvailable(ResourceAddress(split)));
                        return;
                    }

                    var result = new HueController().SetLightState(split[2], lightId, data);
                    var json = JsonConvert.SerializeObject(result);

                    await context.Response.WriteAsync(json);
                    return;
                }

            }
            else if (context.Request.Path.Value.EndsWith("action") && context.Request.Method == "PUT")
            {
                using (StreamReader reader = new StreamReader(context.Request.Body, Encoding.UTF8))
                {
                    var split = context.Request.Path.Value.Split("/");
                    var data = await reader.ReadToEndAsync();

                    HueDriver.Logger.LogDebug($"Received 'action' action url {context.Request.Path.ToString()} data {data}");

                    // /api/{user}/groups/{id}/action is a group action, not a light id.
                    if (split.Length < 6 || split[3] != "groups" || !int.TryParse(split[4], out var groupId))
                    {
                        await context.Response.WriteAsync(HueController.ResourceNotAvailable(ResourceAddress(split)));
                        return;
                    }

                    var groupData = JsonConvert.DeserializeObject<HueSwitchLightData>(data) ?? new HueSwitchLightData();
                    var result = new HueController().SetGroupsAction(split[2], groupId, groupData);

                    await context.Response.WriteAsync(result);
                    return;
                }

            }
            await _next.Invoke(context);
        }

        private static string ResourceAddress(string[] split)
        {
            // split = ["", "api", "{user}", resource...]
            return split.Length > 3 ? "/" + string.Join("/", split, 3, split.Length - 3) : "/";
        }
    }
}
