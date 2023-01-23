using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Internals.Core;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.WebApi.Controllers
{
    public class ImportData
    {
        public NodeInstance Node { get; set; }
        public string FileName { get; set; }
    }

    public class ResultState
    {
        public bool Result { get; set; }
    }
    internal class NodeStates
    {
        public NodeStates()
        {
            AddedNodeInstances = new List<NodeInstance>();
            UpdatedNodeInstances = new List<NodeInstance>();
            AddedPropertyInstances = new List<PropertyInstance>();
            UpdatedPropertyInstances = new List<PropertyInstance>();
        }
        public List<NodeInstance> AddedNodeInstances { get; set; }
        public List<NodeInstance> UpdatedNodeInstances { get; set; }

        public List<PropertyInstance> AddedPropertyInstances { get; set; }
        public List<PropertyInstance> UpdatedPropertyInstances { get; set; }


    }

    [Route("webapi/nodeInstances")]
    public class NodeInstanceController : BaseController
    {
        private readonly INotifyDriver _notifyDriver;
        private readonly INodeInstanceCache _nodeInstanceCache;
        private readonly ICoreServer _server;

        public NodeInstanceController(AutomaticaContext db, INotifyDriver notifyDriver, INodeInstanceCache nodeInstanceCache, ICoreServer server)
            : base(db)
        {
            _notifyDriver = notifyDriver;
            _nodeInstanceCache = nodeInstanceCache;
            _server = server;
        }

        [HttpGet]
        [Route("single/{guid}")]
        [Authorize(Policy = Role.ViewerRole)]
        public NodeInstance GetSingle(Guid guid)
        {
            return _nodeInstanceCache.Get(guid);
        }

        [HttpGet]
        [Authorize(Policy = Role.AdminRole)]
        public IEnumerable<NodeInstance> Get()
        {
            SystemLogger.Instance.LogDebug($"Begin NodeInstance load...");
            var items = _nodeInstanceCache.All();

            SystemLogger.Instance.LogDebug($"Begin NodeInstance load...done");

            return items;
        }

        [HttpGet]
        [Route("linkable")]
        [Authorize(Policy = Role.AdminRole)]
        public IEnumerable<NodeInstance> GetLinkableNodes()
        {
            return DbContext.NodeInstances.AsNoTracking()
                .Include(a => a.This2NodeTemplateNavigation).ToList()
                .Where(a =>
                    a.This2NodeTemplateNavigation.ProvidesInterface2InterfaceType == GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value))
                .Where(a => IsUserInGroup(a.This2UserGroup)).ToList();
        }

        [HttpPost]
        [Route("read")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<ResultState> Read([FromBody] NodeInstance instance)
        {
            var result = await _notifyDriver.Read(instance);

            return new ResultState
            {
                Result = result
            };
        }

        [HttpPost]
        [Route("customAction/{actionName}")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<IList<NodeInstance>> CustomAction([FromBody] NodeInstance instance, string actionName)
        {
            var result = await _notifyDriver.CustomAction(instance, actionName);
            return result;
        }

    }
}
