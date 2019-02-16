using System;
using System.Collections.Generic;
using System.Linq;
using Com.AugustCellars.CoAP;
using Newtonsoft.Json;
using P3.Driver.IkeaTradfri.Extensions;
using P3.Driver.IkeaTradfri.Models;

namespace P3.Driver.IkeaTradfri.Controllers
{
    internal class GatewayController
    {
        private readonly CoapClient _coapClient;
        private GatewayInfo _gatewayInfo;
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="coapClient">existing coap client</param>
        /// <param name="loadAutomatically">Load GatewayInfo object automatically (default: true)</param>
        public GatewayController(CoapClient coapClient, bool loadAutomatically = true)
        {
            _coapClient = coapClient;
            if (loadAutomatically)
                GetGatewayInfo();
        }

        /// <summary>
        /// Acquires GatewayInfo object
        /// </summary>
        /// <param name="refresh">If set to true, than it will ignore existing cached value and ask the gateway for the object</param>
        /// <returns></returns>
        public GatewayInfo GetGatewayInfo(bool refresh = false)
        {
            if (!refresh && _gatewayInfo != null)
                return _gatewayInfo;
            _gatewayInfo = JsonConvert.DeserializeObject<GatewayInfo>(_coapClient.GetValues(new TradfriRequest { UriPath = $"/{(int)TradfriConstRoot.Gateway}/{(int)TradfriConstAttribute.GatewayInfo}" }).PayloadString);
            return _gatewayInfo;
        }
        /// <summary>
        /// Reboot the gateway
        /// </summary>
        /// <returns></returns>
        public Response Reboot()
        {
            return _coapClient.SetValues(new TradfriRequest { UriPath = $"/{(int)TradfriConstRoot.Gateway}/{(int)TradfriConstAttribute.GatewayReboot}" });
        }
        

        /// <summary>
        /// Acquire IDs of connected devices
        /// </summary>
        /// <returns></returns>
        public List<long> GetDevices()
        {
            return GetEntityCollectionIDs(TradfriConstRoot.Devices);
        }
        /// <summary>
        /// Acquire IDs of groups
        /// </summary>
        /// <returns></returns>
        public List<long> GetGroups()
        {
            return GetEntityCollectionIDs(TradfriConstRoot.Groups);
        }
        /// <summary>
        /// Acquire TradFriMoods by groups
        /// </summary>
        /// <returns></returns>
        public List<TradfriMood> GetMoods()
        {
            var moods = new List<TradfriMood>();
            var groupIDsResponse = _coapClient.GetValues(new TradfriRequest { UriPath = $"/{(int)TradfriConstRoot.Moods}" }).PayloadString;
            foreach (var groupId in JsonConvert.DeserializeObject<List<int>>(groupIDsResponse))
            {
                string moodsForGroup = _coapClient.GetValues(new TradfriRequest { UriPath = $"/{(int)TradfriConstRoot.Moods}/{groupId}" }).PayloadString;
                foreach (var moodId in JsonConvert.DeserializeObject<List<int>>(moodsForGroup))
                {
                    var moodResponse = _coapClient.GetValues(new TradfriRequest { UriPath = $"/{(int)TradfriConstRoot.Moods}/{groupId}/{moodId}" }).PayloadString;
                    var mood = JsonConvert.DeserializeObject<TradfriMood>(moodResponse);
                    mood.GroupID = groupId;
                    moods.Add(mood);
                }
            }
            return moods;
        }

        public List<long> GetSmartTasks()
        {
            return GetEntityCollectionIDs(TradfriConstRoot.SmartTasks);
        }
        
        /// <summary>
        /// Acquire All Resources
        /// </summary>
        /// <returns></returns>
        public List<WebLink> GetResources()
        {
            return _coapClient.Discover().ToList();
        }

        private List<long> GetEntityCollectionIDs(TradfriConstRoot rootConst)
        {
            return JsonConvert.DeserializeObject<List<long>>(_coapClient.GetValues(new TradfriRequest { UriPath = $"/{(int)rootConst}" }).PayloadString);
        }
    }
}
