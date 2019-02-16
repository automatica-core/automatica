using System;
using Com.AugustCellars.CoAP;
using Newtonsoft.Json;
using P3.Driver.IkeaTradfri.Extensions;
using P3.Driver.IkeaTradfri.Models;

namespace P3.Driver.IkeaTradfri.Controllers
{
    public class DeviceController
    {
        private readonly CoapClient _coapClient;
        private readonly long _id;
        private TradfriDevice _device;
        public bool HasLight
        {
            get { return _device?.LightControl != null; }
        }

        public DeviceController(long id, IkeaTradfriDriver driver, bool loadAutomatically = true)
        {
            this._id = id;
            _coapClient = driver.Client;
            if (loadAutomatically)
                GetTradFriDevice();

            _coapClient.Observe(driver.HandleObserveResponse);
        }

        /// <summary>
        /// Get device information from gateway
        /// </summary>
        /// <returns></returns>
        public Response Get()
        {
            return _coapClient.GetValues(new TradfriRequest { UriPath = $"/{(int)TradfriConstRoot.Devices}/{_id}" });
        }
        /// <summary>
        /// Acquires TradFriDevice object
        /// </summary>
        /// <param name="refresh">If set to true, than it will ignore existing cached value and ask the gateway for the object</param>
        /// <returns></returns>
        public TradfriDevice GetTradFriDevice(bool refresh = false)
        {
            if (!refresh && _device != null)
                return _device;
            var data = Get();
            _device = JsonConvert.DeserializeObject<TradfriDevice>(data.PayloadString);
            return _device;
        }
        /// <summary>
        /// Turn Off Device
        /// </summary>
        /// <returns></returns>
        public Response TurnOff()
        {
            Response deviceResponse = _coapClient.UpdateValues(SwitchState(0));
            if (HasLight && deviceResponse.CodeString.Equals("2.04 Changed"))
                _device.LightControl[0].State = Bool.False;
            return deviceResponse;
        }
        /// <summary>
        /// Turn On Device
        /// </summary>
        /// <returns></returns>
        public Response TurnOn()
        {
            Response deviceResponse = _coapClient.UpdateValues(SwitchState(1));
            if (HasLight && deviceResponse.CodeString.Equals("2.04 Changed"))
                _device.LightControl[0].State = Bool.True;
            return deviceResponse;
        }


        /// <summary>
        /// Changes the color of the light device
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public Response SetColor(string value)
        {
            Response deviceColor = _coapClient.UpdateValues(new TradfriRequest
            {
                UriPath = $"/{(int)TradfriConstRoot.Devices}/{_id}",
                Payload =
                    $@"{{""{(int) TradfriConstAttribute.LightControl}"":[{{ ""{(int) TradfriConstAttribute.LightColorHex}"":""{value}""}}]}}"
            });
            if (HasLight && deviceColor.CodeString.Equals("2.04 Changed"))
                _device.LightControl[0].ColorHex = value;
            return deviceColor;
        }
        /// <summary>
        /// Set Dimmer for Light Device
        /// </summary>
        /// <param name="value">Dimmer intensity (0-255)</param>
        /// <returns></returns>
        public Response SetDimmer(int value)
        {
            Response deviceDimmer = _coapClient.UpdateValues(new TradfriRequest
            {
                UriPath = $"/{(int)TradfriConstRoot.Devices}/{_id}",
                Payload =
                    $@"{{""{(int) TradfriConstAttribute.LightControl}"":[{{ ""{(int) TradfriConstAttribute.LightDimmer}"":{value}}}]}}"
            });
            if (HasLight && deviceDimmer.CodeString.Equals("2.04 Changed"))
                _device.LightControl[0].Dimmer = value;
            return deviceDimmer;
        }

        private TradfriRequest SwitchState(int turnOn = 1)
        {
            if (HasLight)
            {
                _device.LightControl[0].State = (Bool)turnOn;
            }
            return new TradfriRequest
            {
                UriPath = $"/{(int)TradfriConstRoot.Devices}/{_id}",
                Payload =
                    $@"{{""{(int) TradfriConstAttribute.LightControl}"":[{{ ""{(int) TradfriConstAttribute.LightState}"":{turnOn}}}]}}" //@"{ ""3311"":[{ ""5850"":" + turnOn + "}]}"
            };
        }
    }
}
