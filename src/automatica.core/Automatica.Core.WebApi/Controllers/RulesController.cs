using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Internals.Cache.Logic;
using Automatica.Core.Internals.Core;
using Automatica.Core.Model;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Automatica.Core.WebApi.Controllers
{
    public class SaveAllLogicEditor : TypedObject
    {
        [JsonProperty("logicPages")]
        public List<RulePage> LogicPages { get; set; }

        [JsonProperty("nodeInstances")]
        public List<NodeInstance> NodeInstances { get; set; }
    }

    [Route("webapi/rules")]
    public class RulesController : BaseController
    {
        private readonly IRuleDataHandler _ruleDataHandler;
        private readonly ILogicCacheFacade _logicCacheFacade;
        private readonly IConfiguration _config;
        private readonly INotifyDriver _notifyDriver;
        private readonly INodeInstanceCache _nodeInstanceCache;
        private readonly ICoreServer _coreServer;

        public RulesController(AutomaticaContext db, IRuleDataHandler ruleDataHandler, ILogicCacheFacade logicCacheFacade, IConfiguration config, 
            INotifyDriver notifyDriver, INodeInstanceCache nodeInstanceCache, ICoreServer coreServer)
            : base(db)
        {
            _ruleDataHandler = ruleDataHandler;
            _logicCacheFacade = logicCacheFacade;
            _config = config;
            _notifyDriver = notifyDriver;
            _nodeInstanceCache = nodeInstanceCache;
            _coreServer = coreServer;
        }

        [HttpPost]
        [Route("page/add")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<RulePage> AddPage([FromBody]RulePage page)
        {
            await using var dbContext = new AutomaticaContext(_config);

            dbContext.RulePages.Add(page);
            await dbContext.SaveChangesAsync();
            _logicCacheFacade.ClearInstances();

            return page;
        }

        [HttpPost]
        [Route("item/ruleInstance/{pageId}")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<RuleInstance> AddRuleInstance([FromBody] RuleInstance ruleInstance, Guid pageId)
        {
            await using var dbContext = new AutomaticaContext(_config);

            foreach (var ruleInterface in ruleInstance.RuleInterfaceInstance)
            {
                ruleInterface.This2RuleInterfaceTemplateNavigation = null;

                dbContext.RuleInterfaceInstances.Add(ruleInterface);
                dbContext.Entry(ruleInterface).State = EntityState.Added;
            }

            ruleInstance.This2RulePage = pageId;

            ruleInstance.This2RuleTemplate = ruleInstance.This2RuleTemplateNavigation.ObjId;
            ruleInstance.This2RuleTemplateNavigation = null;

            dbContext.RuleInstances.Add(ruleInstance);
            dbContext.Entry(ruleInstance).State = EntityState.Added;

            ruleInstance.This2RulePage = pageId;
            await dbContext.SaveChangesAsync();


            _logicCacheFacade.PageCache.AddRuleInstance(ruleInstance);
            _logicCacheFacade.InstanceCache.Add(ruleInstance.ObjId, ruleInstance);
            _logicCacheFacade.ClearInstances();

            await _coreServer.ReloadLogic(ruleInstance.ObjId);

            return ruleInstance;
        }

        [HttpPatch]
        [Route("item/ruleInstance")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<RuleInstance> UpdateRuleInstance([FromBody] RuleInstance ruleInstance)
        {
            await using var dbContext = new AutomaticaContext(_config);

            var existingInstance = dbContext.RuleInstances.Single(a => a.ObjId == ruleInstance.ObjId);

            existingInstance.Name = ruleInstance.Name;
            existingInstance.X = ruleInstance.X;
            existingInstance.Y= ruleInstance.Y;
            existingInstance.UseInVisu = ruleInstance.UseInVisu;
            existingInstance.Description = ruleInstance.Description;
            existingInstance.IsFavorite = ruleInstance.IsFavorite;
            existingInstance.Rating = ruleInstance.Rating;
            existingInstance.This2UserGroup = ruleInstance.This2UserGroup;
            existingInstance.This2AreaInstance = ruleInstance.This2AreaInstance;
            existingInstance.This2CategoryInstance = ruleInstance.This2CategoryInstance;
            
            foreach(var ruleInterfaceInstance in ruleInstance.RuleInterfaceInstance)
            {
                var existingRuleInterfaceInstance = dbContext.RuleInterfaceInstances.Single(a => a.ObjId == ruleInterfaceInstance.ObjId);

                existingRuleInterfaceInstance.ValueString = ruleInterfaceInstance.ValueString;
                existingRuleInterfaceInstance.ValueDouble = ruleInterfaceInstance.ValueDouble;
                existingRuleInterfaceInstance.ValueInteger = ruleInterfaceInstance.ValueInteger;
                
                dbContext.Update(existingRuleInterfaceInstance);
                dbContext.Entry(existingRuleInterfaceInstance).State = EntityState.Modified;
            }

            dbContext.Update(existingInstance);
            dbContext.Entry(existingInstance).State = EntityState.Modified;

            await dbContext.SaveChangesAsync();

            _logicCacheFacade.PageCache.UpdateRuleInstance(existingInstance);
            _logicCacheFacade.InstanceCache.Update(ruleInstance.ObjId, ruleInstance);
            await _coreServer.ReloadLogic(ruleInstance.ObjId);
            _logicCacheFacade.ClearInstances();
            return ruleInstance;
        }

        [HttpPost]
        [Route("item/nodeInstance/{pageId}")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<NodeInstance2RulePage> AddNodeInstance([FromBody]NodeInstance2RulePage instance, Guid pageId)
        {
            await using var dbContext = new AutomaticaContext(_config);

            instance.This2RulePage = pageId;
            var nav = instance.This2NodeInstanceNavigation;
            
            instance.This2NodeInstanceNavigation = null;
            await dbContext.AddAsync(instance);
            dbContext.Entry(instance).State = EntityState.Added;

            await dbContext.SaveChangesAsync();

            instance.This2NodeInstanceNavigation = nav;
            _logicCacheFacade.PageCache.AddNodeInstance(instance);


            _logicCacheFacade.ClearInstances();
            return instance;
        }



        [HttpPatch]
        [Route("item/nodeInstance")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<NodeInstance2RulePage> UpdateNodeInstance([FromBody] NodeInstance2RulePage nodeInstance)
        {
            await using var dbContext = new AutomaticaContext(_config);

            if (!dbContext.NodeInstance2RulePages.Any(a => a.ObjId == nodeInstance.ObjId))
            {
                return null;
            }

            var existingInstance = dbContext.NodeInstance2RulePages.Single(a => a.ObjId == nodeInstance.ObjId);

            existingInstance.X = nodeInstance.X;
            existingInstance.Y = nodeInstance.Y;

            dbContext.Update(existingInstance);
            dbContext.Entry(existingInstance).State = EntityState.Modified;

            await dbContext.SaveChangesAsync();

            _logicCacheFacade.PageCache.UpdateNodeInstance(existingInstance);
            _logicCacheFacade.ClearInstances();
            return nodeInstance;
        }

        [HttpDelete]
        [Route("page/{pageId}")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task RemovePage(Guid pageId)
        {
            await using var dbContext = new AutomaticaContext(_config);

            var rulePage = dbContext.RulePages.SingleOrDefault(a => a.ObjId == pageId);

            if (rulePage != null)
            {
                dbContext.RulePages.Remove(rulePage);
                await dbContext.SaveChangesAsync();
            }
            
            _logicCacheFacade.ClearInstances();
        }

        [HttpDelete]
        [Route("item/ruleInstance/{instanceId}")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task RemoveRuleInstance(Guid instanceId)
        {
            await using var dbContext = new AutomaticaContext(_config);

            var ruleInstance = dbContext.RuleInstances.SingleOrDefault(a => a.ObjId == instanceId);

            if (ruleInstance != null)
            {
                await _coreServer.StopLogic(instanceId);

                _logicCacheFacade.PageCache.RemoveRuleInstance(ruleInstance);
                dbContext.RuleInstances.Remove(ruleInstance);
                await dbContext.SaveChangesAsync();
            }

            _logicCacheFacade.ClearInstances();
        }

        [HttpDelete]
        [Route("item/nodeInstance/{instanceId}")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task RemoveNodeInstance(Guid instanceId)
        {
            await using var dbContext = new AutomaticaContext(_config);

            var nodeInstance = dbContext.NodeInstance2RulePages.SingleOrDefault(a => a.ObjId == instanceId);

            if (nodeInstance != null)
            {
                dbContext.NodeInstance2RulePages.Remove(nodeInstance);

                _logicCacheFacade.PageCache.RemoveNodeInstance(nodeInstance);
                await dbContext.SaveChangesAsync();
            }

            _logicCacheFacade.ClearInstances();
        }

        [HttpPost]
        [Route("link")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<Link> AddOrUpdateLink([FromBody] Link link)
        {
            await using var dbContext = new AutomaticaContext(_config);

          
            link.This2RuleInterfaceInstanceOutputNavigation = null;
            link.This2RuleInterfaceInstanceInputNavigation = null;
            link.This2NodeInstance2RulePageInputNavigation = null;
            link.This2NodeInstance2RulePageOutputNavigation = null;

            if (dbContext.Links.AsNoTracking().SingleOrDefault(a => a.ObjId == link.ObjId) == null)
            {
                await dbContext.AddAsync(link);
                dbContext.Entry(link).State = EntityState.Added;

                await dbContext.SaveChangesAsync();
                _logicCacheFacade.PageCache.AddLink(link);
            }
            else
            {
                dbContext.Update(link);
                dbContext.Entry(link).State = EntityState.Modified;

                await dbContext.SaveChangesAsync();
                _logicCacheFacade.PageCache.UpdateLink(link);
            }
            var this2RuleOutputRuleId = link.This2RuleInterfaceInstanceOutputNavigation?.This2RuleInstance;
            var this2RuleInputRuleId = link.This2RuleInterfaceInstanceInputNavigation?.This2RuleInstance;

            if (this2RuleOutputRuleId.HasValue)
            {
                await _coreServer.ReloadLogic(this2RuleOutputRuleId.Value);
            }
            if (this2RuleInputRuleId.HasValue)
            {
                await _coreServer.ReloadLogic(this2RuleInputRuleId.Value);
            }
            
            await _logicCacheFacade.AddOrUpdateLink(link.ObjId, dbContext);
            await _coreServer.ReloadLinks();
            return link;
        }

        [HttpDelete]
        [Route("link/{objId}")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task RemoveLink(Guid objId)
        {
            await using var dbContext = new AutomaticaContext(_config);
            var link = dbContext.Links.SingleOrDefault(a => a.ObjId == objId);

            if (link != null)
            {
                var instance = _logicCacheFacade.LinkCache.Get(objId);
                if (link.This2RuleInterfaceInstanceOutput.HasValue)
                {
                    await _coreServer.ReloadLogic(instance.This2RuleInterfaceInstanceOutputNavigation.This2RuleInstance);
                }
                if (link.This2RuleInterfaceInstanceInput.HasValue)
                {
                    await _coreServer.ReloadLogic(instance.This2RuleInterfaceInstanceInputNavigation.This2RuleInstance);
                }

                await _coreServer.RemoveLink(objId);

                dbContext.Remove(link);
                await dbContext.SaveChangesAsync();
                await _logicCacheFacade.RemoveLink(objId);
            }
            
        }

        [HttpPost]
        [Route("reload")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task Reload()
        {
            await _coreServer.ReloadLogicServices();
        }


        [HttpGet]
        [Route("pages")]
        [Authorize(Policy = Role.AdminRole)]
        public IEnumerable<RulePage> GetPages()
        {
            return _logicCacheFacade.PageCache.All();
        }

        [HttpGet]
        [Route("page/{id}")]
        [Authorize(Policy = Role.AdminRole)]
        public RulePage GetRulePage(Guid id)
        {
            return _logicCacheFacade.PageCache.Get(id);
        }

        [HttpGet]
        [Route("templates")]
        [Authorize(Policy = Role.AdminRole)]
        public ICollection<RuleTemplate> GetRuleTemplates()
        {
            return _logicCacheFacade.TemplateCache.All();
        }


        [HttpGet]
        [Route("data/{id}")]
        [Authorize(Policy = Role.VisuRole)]
        public object GetInstanceData(Guid id)
        {
            return _ruleDataHandler.GetDataForRuleInstance(id);
        }

        [HttpPatch]
        [Route("page")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<RulePage> UpdateRulePage([FromBody]RulePage page)
        {
            await using var dbContext = new AutomaticaContext(_config);

            var rulePage = dbContext.RulePages.SingleOrDefault(a => a.ObjId == page.ObjId);

            if (rulePage != null)
            {
                rulePage.Name = page.Name;
                rulePage.Description = page.Description;

                dbContext.Update(rulePage);
                await dbContext.SaveChangesAsync();
            }

            _logicCacheFacade.PageCache.Clear();
            return rulePage;
        }

    }
}
