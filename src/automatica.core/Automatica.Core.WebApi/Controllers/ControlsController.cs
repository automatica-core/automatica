using System;
using Automatica.Core.Control.Cache;
using Automatica.Core.EF.Models;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Automatica.Core.Control.Base;

namespace Automatica.Core.WebApi.Controllers
{
    public class ControlInfo : IControl
    {
        public ControlInfo()
        {
            
        }

        public ControlInfo(IControl control)
        {
            Id = control.Id;
            Name = control.Name;

            switch (control)
            {
                case IDimmer:
                    Type = "dimmer";
                    break;
                case ISwitch: 
                    Type = "switch";
                    break;
                case IBlind:
                    Type = "blind";
                    break;
                default:
                    Type = control.GetType().Name;
                    break;
            }
        }
        
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public void RegisterValueChanged(Action<IControl> callback)
        {
            //not needed here
        }

        public string TypeInfo => "Control";
    }
    
    [Route("webapi/controls")]
    public class ControlsController : BaseController
    {
        private readonly IControlCache _controlCache;

        public ControlsController(AutomaticaContext dbContext, IControlCache controlCache) : base(dbContext)
        {
            _controlCache = controlCache;
        }

        [HttpGet]
        [Route("all")]
        [Authorize(Policy = Role.AdminRole)]
        public IEnumerable<ControlInfo> GetControls()
        {
            var ret = new List<ControlInfo>();
            var controls = _controlCache.All();

            foreach (var control in controls)
            {
                ret.Add(new ControlInfo(control));
            }

            return ret;
        }
    }
}
