using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using P3.Driver.HueBridge.Data;
using Automatica.Core.Driver.Utility.Network;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace P3.Driver.HueBridge.Api
{
    internal static class HueErrors
    {
        public const int UnauthorizedUser = 1;
        public const int InvalidJson = 2;
        public const int ResourceNotAvailable = 3;
        public const int LinkButtonNotPressed = 101;

        public static string Error(int type, string address, string description)
        {
            return JsonConvert.SerializeObject(new[]
            {
                new { error = new { type, address, description } }
            });
        }

        public static ContentResult Result(int type, string address, string description)
        {
            return new ContentResult
            {
                ContentType = "application/json",
                Content = Error(type, address, description),
                StatusCode = 200
            };
        }

        public static ContentResult Unauthorized(string address)
        {
            return Result(UnauthorizedUser, address, "unauthorized user");
        }
    }

    [Route("api")]
    [AllowAnonymous]
    public class HueController : Controller
    {
        private readonly HueDriver _driver;

        public HueController(HueDriver driver)
        {
            _driver = driver;
        }

        /// <summary>
        /// Creates a user. Only works while the link button window is open (Hue API behaviour).
        /// </summary>
        [HttpPost]
        public ContentResult Login()
        {
            if (!_driver.TryCreateUser(out var userName))
            {
                return HueErrors.Result(HueErrors.LinkButtonNotPressed, "", "link button not pressed");
            }

            return new ContentResult
            {
                ContentType = "application/json",
                Content = JsonConvert.SerializeObject(new[] { new { success = new { username = userName } } }),
                StatusCode = 200
            };
        }

        [HttpGet]
        [Route("discovery")]
        [Produces("application/xml")]
        public ContentResult GetDeviceDescription()
        {
            string xml = HueDriver.HueDiscoveryService.Device.ToDescriptionDocument();
            return new ContentResult
            {
                ContentType = "application/xml",
                Content = xml,
                StatusCode = 200
            };
        }

        [HttpGet]
        [Route("{apiKey}/config")]
        public IActionResult Config(string apiKey)
        {
            if (!_driver.IsAuthorized(apiKey))
            {
                return HueErrors.Unauthorized("/config");
            }
            return Ok(_driver.BridgeConfig);
        }

        [HttpGet]
        [HttpPost]
        [Route("{apiKey}/lights")]
        public IActionResult GetLights(string apiKey)
        {
            if (!_driver.IsAuthorized(apiKey))
            {
                return HueErrors.Unauthorized("/lights");
            }
            return Ok(_driver.Lights);
        }

        [HttpGet]
        [HttpPost]
        [Route("{apiKey}/lights/{id}")]
        public IActionResult GetLight(string apiKey, int id)
        {
            if (!_driver.IsAuthorized(apiKey))
            {
                return HueErrors.Unauthorized($"/lights/{id}");
            }

            if (!_driver.Lights.TryGetValue(id, out var light))
            {
                return HueErrors.Result(HueErrors.ResourceNotAvailable, $"/lights/{id}", $"resource, /lights/{id}, not available");
            }
            return Ok(light);
        }

        [HttpGet]
        [Route("{apiKey}/groups/{id}")]
        public IActionResult GetGroupData([FromRoute]string apiKey, [FromRoute]int id)
        {
            if (!_driver.IsAuthorized(apiKey))
            {
                return HueErrors.Unauthorized($"/groups/{id}");
            }

            if (id != 0)
            {
                return HueErrors.Result(HueErrors.ResourceNotAvailable, $"/groups/{id}", $"resource, /groups/{id}, not available");
            }

            var groupData = new HueGroupData();
            groupData.Lights = new List<string>();
            foreach (var n in _driver.Lights.Keys)
            {
                groupData.Lights.Add(n.ToString());
            }

            groupData.Name = "Group 0";
            groupData.State = new HueGroupState();
            groupData.State.AllOn = false; //TODO
            groupData.State.AnyOn = true; //TODO

            groupData.Action = new HueGroupAction();
            return Ok(groupData);
        }

        [HttpGet]
        [HttpPost]
        [Route("{apiKey}")]
        public IActionResult GetData(string apiKey)
        {
            if (!_driver.IsAuthorized(apiKey))
            {
                return HueErrors.Unauthorized("/");
            }
            return Ok(new HueData());
        }

        [HttpGet]
        [Route("config")]
        public ContentResult Config()
        {
            var config = _driver.BridgeConfig;
            return new ContentResult
            {
                ContentType = "application/json",
                StatusCode = 200,
                Content = JsonConvert.SerializeObject(new
                {
                    name = config.Name,
                    apiversion = config.ApiVersion,
                    swversion = config.SwVersion,
                    mac = NetworkHelper.GetActiveMacAddress(),
                    bridgeid = _driver.BridgeId.ToString(),
                    factorynew = config.FactoryNew,
                    modelid = config.ModelId
                })
            };
        }
    }
}
