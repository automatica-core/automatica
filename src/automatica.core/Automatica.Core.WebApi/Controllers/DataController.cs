using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Trendings;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Automatica.Core.WebApi.Controllers
{
    [Route("webapi/data")]
    public class DataController : BaseController
    {
        private readonly IDispatcher _dispatcher;

        public DataController(AutomaticaContext db, IDispatcher dispatcher)
            : base(db)
        {
            _dispatcher = dispatcher;
        }

        [HttpGet]
        [Route("node/current")]
        [Authorize(Policy = Role.ViewerRole)]
        public IDictionary<Guid, object> GetCurrentNodeValues()
        {
            return _dispatcher.GetValues(DispatchableType.NodeInstance);
        }
        [HttpGet]
        [Route("node/:id")]
        [Authorize(Policy = Role.ViewerRole)]
        public object GetNodeValue(Guid id)
        {
            return _dispatcher.GetValue(DispatchableType.NodeInstance, id);
        }

        [HttpGet]
        [Route("trend/{id}/{startVisible}/{endVisible}/{startBound}/{endBound}")]
        public IList<Trending> GetTrendingValues(Guid id, DateTime startVisible, DateTime endVisible, DateTime startBound, DateTime endBound)
        {
            return DbContext.Trendings.Where(a => a.This2NodeInstance == id && a.Timestamp >= startBound && a.Timestamp <= endBound).ToList();
        }
    }
}
