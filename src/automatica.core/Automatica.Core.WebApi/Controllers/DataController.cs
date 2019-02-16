using System;
using System.Collections.Generic;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Automatica.Core.WebApi.Controllers
{
    [Route("data")]
    public class DataController : BaseController
    {
        private readonly IDispatcher _disptacher;

        public DataController(AutomaticaContext db, IDispatcher disptacher)
            : base(db)
        {
            _disptacher = disptacher;
        }

        [HttpGet]
        [Route("node/current")]
        [Authorize(Policy = Role.ViewerRole)]
        public IDictionary<Guid, object> GetCurrentNodeValues()
        {
            return _disptacher.GetValues(DispatchableType.NodeInstance);
        }
        [HttpGet]
        [Route("node/:id")]
        [Authorize(Policy = Role.ViewerRole)]
        public object GetNodeValue(Guid id)
        {
            return _disptacher.GetValue(DispatchableType.NodeInstance, id);
        }
    }
}
