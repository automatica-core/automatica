using Com.AugustCellars.CoAP;
using Newtonsoft.Json;
using P3.Driver.IkeaTradfri.Extensions;
using P3.Driver.IkeaTradfri.Models;

namespace P3.Driver.IkeaTradfri.Controllers
{
    public class GroupController
    {
        private readonly CoapClient _coapClent;
        private readonly long _id;
        private TradfriGroup _group;
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="id">group id</param>
        /// <param name="coapClent">existing coap client</param>
        /// <param name="loadAutomatically">Load group object automatically (default: true)</param>
        public GroupController(long id, CoapClient coapClent, bool loadAutomatically = true)
        {
            this._id = id;
            _coapClent = coapClent;
            if (loadAutomatically)
                GetTradfriGroup();
        }

        /// <summary>
        /// Get group information from gateway
        /// </summary>
        /// <returns></returns>
        public Response Get()
        {
            return _coapClent.GetValues(new TradfriRequest { UriPath = $"/{(int)TradfriConstRoot.Groups}/{_id}" });
        }
        /// <summary>
        /// Acquires TradfriGroup object
        /// </summary>
        /// <param name="refresh">If set to true, than it will ignore existing cached value and ask the gateway for the object</param>
        /// <returns></returns>
        public TradfriGroup GetTradfriGroup(bool refresh = false)
        {
            if (!refresh && _group != null)
                return _group;
            _group = JsonConvert.DeserializeObject<TradfriGroup>(Get().PayloadString);
            return _group;
        }

        /// <summary>
        /// Turn Off Devices in Group
        /// </summary>
        /// <returns></returns>
        public Response TurnOff()
        {
            Response state = _coapClent.UpdateValues(SwitchState(0));
            if (_group != null && state.CodeString.Equals("2.04 Changed"))
                _group.LightState = 0;
            return state;
        }
      
        /// <summary>
        /// Turn On Devices in Group
        /// </summary>
        /// <returns></returns>
        public Response TurnOn()
        {
            Response state = _coapClent.UpdateValues(SwitchState(1));
            if (_group != null && state.CodeString.Equals("2.04 Changed"))
                _group.LightState = 1;
            return state;
        }

        /// <summary>
        /// Sets a mood for the group
        /// </summary>
        /// <param name="mood">TradfriMood object which needs to be set</param>
        /// <returns></returns>
        public Response SetMood(TradfriMood mood)
        {
            Response request = _coapClent.UpdateValues(new TradfriRequest
            {
                UriPath = $"/{(int)TradfriConstRoot.Groups}/{_id}",
                Payload = JsonConvert.SerializeObject(mood.MoodProperties[0])
            });
            if (_group != null && request.CodeString.Equals("2.04 Changed"))
                _group.ActiveMood = mood.ID;
            return _coapClent.UpdateValues(new TradfriRequest
            {
                UriPath = $"/{(int)TradfriConstRoot.Groups}/{_id}",
                Payload = string.Format(@"{{""{0}"":{1}}}", (int)TradfriConstAttribute.Mood, mood.ID)
            });
        }

        /// <summary>
        /// Set Dimmer for Light Devices in Group
        /// </summary>
        /// <param name="value">Dimmer intensity (0-255)</param>
        /// <returns></returns>
        public Response SetDimmer(int value)
        {
            Response dimmer = _coapClent.UpdateValues(new TradfriRequest
            {
                UriPath = $"/{(int)TradfriConstRoot.Groups}/{_id}",
                Payload = string.Format(@"{{""{0}"":{1}}}", (int)TradfriConstAttribute.LightDimmer, value)
            });
            if (_group != null && dimmer.CodeString.Equals("2.04 Changed"))
                _group.LightDimmer = value;
            return dimmer;
        }

        private TradfriRequest SwitchState(int turnOn = 1)
        {
            return new TradfriRequest
            {
                UriPath = $"/{(int)TradfriConstRoot.Groups}/{_id}",
                Payload = string.Format(@"{{""{0}"":{1}}}", (int)TradfriConstAttribute.LightState, turnOn)
            };
        }
    }
}
