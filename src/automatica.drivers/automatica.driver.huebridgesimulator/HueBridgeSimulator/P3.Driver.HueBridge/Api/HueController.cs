using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using P3.Driver.HueBridge.Data;
using Automatica.Core.Driver.Utility.Network;
using System;
using System.Collections.Generic;

namespace P3.Driver.HueBridge.Api
{
    [Route("api")]
    [AllowAnonymous]
    public class HueController : Controller
    {
        public HueController()
        {

        }

        private void LoginUser(string g)
        {
            var hueUser = new HueUser()
            {
                Id = g,
                Created = DateTime.Now,
                LastUsed = DateTime.Now
            };
            HueDriver.Instance.AddUser(hueUser);
        }

        [HttpPost]
        public string Login(string data)
        {
            var newUser = Guid.NewGuid();
            LoginUser(newUser.ToString());
            return $@"[{{""success"":{{""username"":""{newUser}""}}}}]";
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
        public BridgeConfig Config(string apiKey)
        {
            LoginUser(apiKey);
            return HueDriver.Instance.BridgeConfig;
        }

        [HttpGet]
        [HttpPost]
        [Route("{apiKey}/lights")]
        public IDictionary<int, HueLight> GetLights(string apiKey)
        {
            LoginUser(apiKey);
            return HueDriver.Instance.Lights;
        }

        [HttpGet]
        [HttpPost]
        [Route("{apiKey}/lights/{id}")]
        public HueLight GetLight(string apiKey, int id)
        {
            LoginUser(apiKey);
            return HueDriver.Instance.Lights[id];
        }

       // [HttpPut]
     //   [Route("{apiKey}/lights/{id}/state")]
        public HueLight SetLightState([FromRoute]string apiKey, [FromRoute]int id, string data)
        {
            LoginUser(apiKey);

            var setLightData = JsonConvert.DeserializeObject<HueSwitchLightData>(data);

            return HueDriver.Instance.SetLightState(id, setLightData, true);
        }

        [HttpGet]
        [Route("{apiKey}/groups/{id}")]
        public HueGroupData GetGroupData([FromRoute]string apiKey, [FromRoute]int id)
        {
            LoginUser(apiKey);
            if (id == 0)
            {
                var groupData = new HueGroupData();
                groupData.Lights = new List<string>();
                foreach(var n in HueDriver.Instance.Lights.Keys)
                {
                    groupData.Lights.Add(n.ToString());
                }

                groupData.Name = "Group 0";
                groupData.State = new HueGroupState();
                groupData.State.AllOn = false; //TODO
                groupData.State.AnyOn = true; //TODO

                groupData.Action = new HueGroupAction();
                return groupData;
            }
            throw new NotImplementedException();
        }

       // [HttpPut]
      //  [Route("{apiKey}/groups/{id}/action")]
        public string SetGroupsAction([FromRoute]string apiKey, [FromRoute]int id, [FromBody]HueSwitchLightData data)
        {
            LoginUser(apiKey);
            if (id == 0)
            {
                HueDriver.Instance.SetAll(data);
                return $@"[{{
                            ""success"": {{
                                ""address"": ""/groups/0/action/on"",
                                ""value"": {data.On.ToString().ToLower()}
                            }}}}]";
            }
            throw new NotImplementedException();
        }

        [HttpGet]
        [HttpPost]
        [Route("{apiKey}")]
        public HueData GetData(string apiKey)
        {
            LoginUser(apiKey);
            return new HueData();
        }

        [HttpGet]
        [Route("config")]
        public string Config()
        {
            return $@"{{
                ""name"": ""{HueDriver.Instance.BridgeConfig.Name}"",
                ""apiversion"": ""{HueDriver.Instance.BridgeConfig.ApiVersion}"",
                ""swversion"": ""{HueDriver.Instance.BridgeConfig.SwVersion}"",
                ""mac"": ""{NetworkHelper.GetActiveMacAddress()}"",
                ""bridgeid"": ""{HueDriver.Instance.BridgeId}"",
                ""factorynew"": {HueDriver.Instance.BridgeConfig.FactoryNew.ToString().ToLower()},
                ""modelid"": ""{HueDriver.Instance.BridgeConfig.ModelId}""
            }}";
        }
    }
}
