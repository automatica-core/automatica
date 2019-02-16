using System;
using System.Collections.Generic;
using Com.AugustCellars.CoAP;
using Newtonsoft.Json;
using P3.Driver.IkeaTradfri.Extensions;
using P3.Driver.IkeaTradfri.Models;

namespace P3.Driver.IkeaTradfri.Controllers
{
    public class SmartTaskController
    {
        private readonly CoapClient _coapClient;
        private readonly long _id;
        private TradfriSmartTask _task;
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="id">group id</param>
        /// <param name="coapClient">existing coap client</param>
        /// <param name="loadAutomatically">Load group object automatically (default: true)</param>
        public SmartTaskController(long id, CoapClient coapClient, bool loadAutomatically = true)
        {
            this._id = id;
            this._coapClient = coapClient;
            if (loadAutomatically)
                GetTradfriSmartTask();
        }

        /// <summary>
        /// Get group information from gateway
        /// </summary>
        /// <returns></returns>
        public Response Get()
        {
            return _coapClient.GetValues(new TradfriRequest { UriPath = $"/{(int)TradfriConstRoot.SmartTasks}/{_id}" });
        }
        /// <summary>
        /// Acquires TradfriGroup object
        /// </summary>
        /// <param name="refresh">If set to true, than it will ignore existing cached value and ask the gateway for the object</param>
        /// <returns></returns>
        public TradfriSmartTask GetTradfriSmartTask(bool refresh = false)
        {
            if (!refresh && _task != null)
                return _task;
            _task = JsonConvert.DeserializeObject<TradfriSmartTask>(Get().PayloadString);
            return _task;
        }

        public List<string> GetSelectedRepeatDays()
        {
            List<string> days = new List<string>();
            if (_task?.RepeatDays > 0)
            {
                int tempDaysVariable = (int)_task.RepeatDays;
                Array daysArray = Enum.GetValues(typeof(Days));
                //Array.Reverse(daysArray);
                string selectedDaysBinary = Convert.ToString(_task.RepeatDays, 2);
                int currentSign = selectedDaysBinary.Length - 1;
                if (currentSign >= 0)
                {
                    foreach (Days currentDay in daysArray)
                    {
                        if (selectedDaysBinary[currentSign].Equals('1'))
                        {
                            days.Add(currentDay.ToString());
                        }
                        currentSign--;
                        if (currentSign < 0)
                            break;
                    }
                }
            }
            return days;
        }




    }
}
