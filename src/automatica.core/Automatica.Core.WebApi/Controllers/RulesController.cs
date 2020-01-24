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
        [Route("saveAll")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<SaveAllLogicEditor> SaveAll([FromBody] SaveAllLogicEditor data)
        {
            IEnumerable<NodeInstance> nodeInstancesSaved = null;
            await using (var dbContextNodeInstances = new AutomaticaContext(_config))
            {
                var nodeInstanceController = new NodeInstanceController(dbContextNodeInstances, _notifyDriver, _nodeInstanceCache, _coreServer);
                nodeInstancesSaved = await nodeInstanceController.Save(data.NodeInstances,false);
            }

            var pages = await Save(data.LogicPages);

            await _coreServer.ReInit();
            return new SaveAllLogicEditor
            {
                LogicPages = pages.ToList(),
                NodeInstances = nodeInstancesSaved.ToList()
            };
        }

        internal async Task<IEnumerable<RulePage>> Save(List<RulePage> data)
        {
            await using var dbContext = new AutomaticaContext(_config);
            {
                var transaction = await dbContext.Database.BeginTransactionAsync();
                try
                {
                    foreach (var page in data)
                    {
                        var isNewPage = false;
                        var dbPage = dbContext.RulePages.SingleOrDefault(a => a.ObjId == page.ObjId);

                        if (dbPage == null)
                        {
                            dbPage = page;
                            isNewPage = true;
                        }
                        else
                        {
                            dbContext.Entry(dbPage).State = EntityState.Detached;
                        }

                        if (page.Description == null)
                        {
                            page.Description = "";
                        }

                        foreach (var ruleInstance in page.RuleInstance)
                        {
                            if (ruleInstance.Description == null)
                            {
                                ruleInstance.Description = "";
                            }

                            foreach (var ruleInterface in ruleInstance.RuleInterfaceInstance)
                            {
                                ruleInterface.This2RuleInterfaceTemplateNavigation = null;

                                if (dbContext.RuleInterfaceInstances.AsNoTracking()
                                        .SingleOrDefault(a => a.ObjId == ruleInterface.ObjId) != null)
                                {
                                    dbContext.Entry(ruleInterface).State = EntityState.Modified;
                                    dbContext.RuleInterfaceInstances.Update(ruleInterface);
                                }
                                else
                                {
                                    dbContext.RuleInterfaceInstances.Add(ruleInterface);
                                    dbContext.Entry(ruleInterface).State = EntityState.Added;
                                }
                            }

                            ruleInstance.This2RulePage = page.ObjId;

                            ruleInstance.This2RuleTemplate = ruleInstance.This2RuleTemplateNavigation.ObjId;
                            ruleInstance.This2RuleTemplateNavigation = null;

                            if (dbContext.RuleInstances.AsNoTracking()
                                    .SingleOrDefault(a => a.ObjId == ruleInstance.ObjId) == null)
                            {
                                dbContext.RuleInstances.Add(ruleInstance);
                                dbContext.Entry(ruleInstance).State = EntityState.Added;
                            }
                            else
                            {
                                dbContext.Entry(ruleInstance).State = EntityState.Modified;
                                dbContext.RuleInstances.Update(ruleInstance);
                            }
                        }

                        foreach (var node in page.NodeInstance2RulePage)
                        {
                            if (dbContext.NodeInstance2RulePages.AsNoTracking()
                                    .SingleOrDefault(a => a.ObjId == node.ObjId) == null)
                            {
                                node.This2NodeInstanceNavigation = null;
                                await dbContext.AddAsync(node);
                                dbContext.Entry(node).State = EntityState.Added;
                            }
                            else
                            {
                                dbContext.Entry(node).State = EntityState.Modified;
                                dbContext.NodeInstance2RulePages.Update(node);
                            }
                        }

                        foreach (var link in page.Link)
                        {
                            link.This2RulePage = dbPage.ObjId;


                            link.This2RuleInterfaceInstanceOutputNavigation = null;
                            link.This2RuleInterfaceInstanceInputNavigation = null;
                            link.This2NodeInstance2RulePageInputNavigation = null;
                            link.This2NodeInstance2RulePageOutputNavigation = null;

                            if (dbContext.Links.AsNoTracking().SingleOrDefault(a => a.ObjId == link.ObjId) == null)
                            {
                                await dbContext.AddAsync(link);
                                dbContext.Entry(link).State = EntityState.Added;
                            }
                            else
                            {
                                dbContext.Update(link);
                                dbContext.Entry(link).State = EntityState.Modified;
                            }
                        }

                        if (isNewPage)
                        {
                            dbContext.Entry(page).State = EntityState.Added;
                            await dbContext.RulePages.AddAsync(page);
                        }
                        else
                        {
                            dbContext.Entry(page).State = EntityState.Modified;
                            dbContext.RulePages.Update(page);
                        }
                    }

                    await dbContext.SaveChangesAsync(true);

                    foreach (var page in data)
                    {
                        var removedRules = from c in dbContext.RuleInstances
                                           where !(from o in page.RuleInstance select o.ObjId).Contains(c.ObjId) &&
                                                 c.This2RulePage == page.ObjId
                                           select c;

                        var removedRulesList = removedRules.ToList();
                        dbContext.RuleInstances.RemoveRange(removedRulesList);


                        var removedLinks = from c in dbContext.Links
                                           where !(from o in page.Link select o.ObjId).Contains(c.ObjId) &&
                                                 c.This2RulePage == page.ObjId
                                           select c;

                        dbContext.Links.RemoveRange(removedLinks);



                        var removedNodes = (from c in dbContext.NodeInstance2RulePages
                                            where !(from o in page.NodeInstance2RulePage select o.ObjId).Contains(c.ObjId) &&
                                                  c.This2RulePage == page.ObjId
                                            select c).ToList();

                        dbContext.NodeInstance2RulePages.RemoveRange(removedNodes);
                    }

                    var removedPages = (from c in dbContext.RulePages
                                        where !(from o in data select o.ObjId).Contains(c.ObjId)
                                        select c).ToList();

                    foreach (var removedPage in removedPages)
                    {
                        var removedRules = from c in dbContext.RuleInstances
                                           where c.This2RulePage == removedPage.ObjId
                                           select c;
                        dbContext.RuleInstances.RemoveRange(removedRules);

                        var removedLinks = from c in dbContext.Links
                                           where c.This2RulePage == removedPage.ObjId
                                           select c;

                        dbContext.Links.RemoveRange(removedLinks);

                        var removedNodes = (from c in dbContext.NodeInstance2RulePages
                                            where c.This2RulePage == removedPage.ObjId
                                            select c).ToList();

                        dbContext.NodeInstance2RulePages.RemoveRange(removedNodes);

                    }

                    dbContext.RemoveRange(removedPages);

                    await dbContext.SaveChangesAsync(true);
                    await transaction.CommitAsync();
                    _logicCacheFacade.ClearInstances();
                }
                catch (Exception e)
                {
                    SystemLogger.Instance.LogError(e, "Could not save data");
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            return GetPages();
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
    }
}
