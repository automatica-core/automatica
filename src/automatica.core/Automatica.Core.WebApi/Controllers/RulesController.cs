using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals;
using Automatica.Core.Internals.Cache.Logic;
using Automatica.Core.Internals.Core;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.WebApi.Controllers
{
    [Route("rules")]
    public class RulesController : BaseController
    {
        private readonly IRuleDataHandler _ruleDataHandler;
        private readonly ILogicCacheFacade _logicCacheFacade;

        public RulesController(AutomaticaContext db, IRuleDataHandler ruleDataHandler, ILogicCacheFacade logicCacheFacade)
            : base(db)
        {
            _ruleDataHandler = ruleDataHandler;
            _logicCacheFacade = logicCacheFacade;
        }

        [HttpPost]
        [Route("saveAll")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<IEnumerable<RulePage>> SaveAll([FromBody]List<RulePage> pages)
        {
            var transaction = await DbContext.Database.BeginTransactionAsync();
            try
            {
                var newRuleInterfaceInstanceMap = new Dictionary<Guid, RuleInterfaceInstance>();
                var newNodeInterfaceInstanceMap = new Dictionary<Guid, NodeInstance2RulePage>();

                foreach (var page in pages)
                {
                    bool pageIsNew = false;
                    var dbPage = DbContext.RulePages.AsNoTracking().SingleOrDefault(a => a.ObjId == page.ObjId);

                    if (dbPage == null)
                    {
                        dbPage = page;
                    }

                    if (dbPage.Description == null)
                    {
                        dbPage.Description = "";
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
                            newRuleInterfaceInstanceMap.Add(ruleInterface.ObjId, ruleInterface);
                            ruleInterface.This2RuleInterfaceTemplateNavigation = null;

                            if (DbContext.RuleInterfaceInstances.AsNoTracking()
                                    .SingleOrDefault(a => a.ObjId == ruleInterface.ObjId) != null)
                            {
                                DbContext.Entry(ruleInterface).State = EntityState.Modified;
                                DbContext.RuleInterfaceInstances.Update(ruleInterface);
                            }
                            else
                            {
                                DbContext.RuleInterfaceInstances.Add(ruleInterface);
                                DbContext.Entry(ruleInterface).State = EntityState.Added;
                            }
                        }

                        ruleInstance.This2RulePage = page.ObjId;

                        ruleInstance.This2RuleTemplate = ruleInstance.This2RuleTemplateNavigation.ObjId;
                        ruleInstance.This2RuleTemplateNavigation = null;

                        if (DbContext.RuleInstances.AsNoTracking().SingleOrDefault(a => a.ObjId == ruleInstance.ObjId) == null)
                        {
                            DbContext.RuleInstances.Add(ruleInstance);
                            DbContext.Entry(ruleInstance).State = EntityState.Added;
                        }
                        else
                        {
                            DbContext.Entry(ruleInstance).State = EntityState.Modified;
                            DbContext.RuleInstances.Update(ruleInstance);
                        }
                    }

                    foreach (var node in page.NodeInstance2RulePage)
                    {
                        if (DbContext.NodeInstance2RulePages.AsNoTracking().SingleOrDefault(a => a.ObjId == node.ObjId) == null)
                        {
                            newNodeInterfaceInstanceMap.Add(node.ObjId, node);
                            node.This2NodeInstanceNavigation = null;
                            await DbContext.AddAsync(node);
                            DbContext.Entry(node).State = EntityState.Added;
                        }
                        else
                        {
                            DbContext.Entry(node).State = EntityState.Modified;
                            DbContext.NodeInstance2RulePages.Update(node);
                        }
                    }

                    foreach (var link in page.Link)
                    {
                        link.This2RulePage = dbPage.ObjId;


                        link.This2RuleInterfaceInstanceOutputNavigation = null;
                        link.This2RuleInterfaceInstanceInputNavigation = null;
                        link.This2NodeInstance2RulePageInputNavigation = null;
                        link.This2NodeInstance2RulePageOutputNavigation = null;

                        if (DbContext.Links.AsNoTracking().SingleOrDefault(a => a.ObjId == link.ObjId) == null)
                        {
                            await DbContext.AddAsync(link);
                            DbContext.Entry(link).State = EntityState.Added;
                        }
                        else
                        {
                            DbContext.Update(link);
                            DbContext.Entry(link).State = EntityState.Modified;
                        }
                    }

                    if (DbContext.RulePages.AsNoTracking().SingleOrDefault(a => a.ObjId == page.ObjId) == null)
                    {
                        await DbContext.RulePages.AddAsync(page);
                        DbContext.Entry(page).State = EntityState.Added;
                        pageIsNew = true;
                    }

                    if (!pageIsNew)
                    {
                        DbContext.Entry(page).State = EntityState.Modified;
                        DbContext.RulePages.Update(page);
                    }
                }
                await DbContext.SaveChangesAsync(true);

                foreach (var page in pages)
                {
                    var removedRules = from c in DbContext.RuleInstances
                                       where !(from o in page.RuleInstance select o.ObjId).Contains(c.ObjId) && c.This2RulePage == page.ObjId
                                       select c;

                    var removedRulesList = removedRules.ToList();
                    DbContext.RuleInstances.RemoveRange(removedRulesList);


                    var removedLinks = from c in DbContext.Links
                                       where !(from o in page.Link select o.ObjId).Contains(c.ObjId) && c.This2RulePage == page.ObjId
                                       select c;

                    DbContext.Links.RemoveRange(removedLinks);



                    var removedNodes = (from c in DbContext.NodeInstance2RulePages
                                       where !(from o in page.NodeInstance2RulePage select o.ObjId).Contains(c.ObjId) && c.This2RulePage == page.ObjId
                                       select c).ToList();

                    DbContext.NodeInstance2RulePages.RemoveRange(removedNodes);
                }

                var removedPages = (from c in DbContext.RulePages
                    where !(from o in pages select o.ObjId).Contains(c.ObjId) select c).ToList();

                foreach (var removedPage in removedPages)
                {
                    var removedRules = from c in DbContext.RuleInstances
                        where c.This2RulePage == removedPage.ObjId
                        select c;
                    DbContext.RuleInstances.RemoveRange(removedRules);

                    var removedLinks = from c in DbContext.Links
                        where c.This2RulePage == removedPage.ObjId
                        select c;

                    DbContext.Links.RemoveRange(removedLinks);

                    var removedNodes = (from c in DbContext.NodeInstance2RulePages
                        where c.This2RulePage == removedPage.ObjId
                        select c).ToList();

                    DbContext.NodeInstance2RulePages.RemoveRange(removedNodes);

                }

                DbContext.RemoveRange(removedPages);

                await DbContext.SaveChangesAsync(true);
                transaction.Commit();
                _logicCacheFacade.ClearInstances();
            }
            catch (Exception e)
            {
                SystemLogger.Instance.LogError(e, "Could not save data");
                transaction.Rollback();
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
