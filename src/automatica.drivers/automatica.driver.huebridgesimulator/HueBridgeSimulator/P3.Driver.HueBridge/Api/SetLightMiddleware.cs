using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
                    var result = new HueController().SetLightState(split[2], Convert.ToInt32(split[4]), data);
                    var json = JsonConvert.SerializeObject(result);

                    HueDriver.Logger.LogDebug($"Received 'state' action url {context.Request.Path.ToString()} data {data}");

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
                    var result = new HueController().SetLightState(split[2], Convert.ToInt32(split[4]), data);
                    var json = JsonConvert.SerializeObject(result);

                    HueDriver.Logger.LogDebug($"Received 'action' action url {context.Request.Path.ToString()} data {data}");

                    await context.Response.WriteAsync(json);
                    return;
                }

            }
            await _next.Invoke(context);
        }
    }
}
