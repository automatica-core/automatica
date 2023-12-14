using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Internals.Cache.Logic;
using Automatica.Core.Internals.Core;
using Automatica.Core.Model;
using Automatica.Core.Model.Models.User;
using Automatica.Core.Runtime.Abstraction.Plugins.Logic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Automatica.Core.WebApi.Controllers
{
    public enum LogicUpdateScope
    {
        Unknown = 0,
        Drag = 1
    }
    public class SaveAllLogicEditor : TypedObject
    {
        [JsonProperty("logicPages")]
        public List<RulePage> LogicPages { get; set; }

        [JsonProperty("nodeInstances")]
        public List<NodeInstance> NodeInstances { get; set; }
    }

    [Route("webapi/logics")]
    public class LogicsController : BaseController
    {
        private readonly ILogger<LogicsController> _logger;
        private readonly ILogicDataHandler _logicDataHandler;
        private readonly ILogicCacheFacade _logicCacheFacade;
        private readonly IConfiguration _config;
        private readonly INotifyDriver _notifyDriver;
        private readonly INodeInstanceCache _nodeInstanceCache;
        private readonly ICoreServer _coreServer;

        public LogicsController(ILogger<LogicsController> logger, AutomaticaContext db, ILogicDataHandler logicDataHandler, ILogicCacheFacade logicCacheFacade, IConfiguration config, 
            INotifyDriver notifyDriver, INodeInstanceCache nodeInstanceCache, ICoreServer coreServer)
            : base(db)
        {
            _logger = logger;
            _logicDataHandler = logicDataHandler;
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
            page.CreatedAt = DateTimeOffset.Now;
            page.ModifiedAt = DateTimeOffset.Now;
            DbContext.RulePages.Add(page);
            await DbContext.SaveChangesAsync();
            _logicCacheFacade.ClearInstances();

            return page;
        }

        [HttpPost]
        [Route("item/logicInstance/{pageId}")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<RuleInstance> AddLogicInstance([FromBody] RuleInstance logicInstance, Guid pageId)
        {
            var strategy = DbContext.Database.CreateExecutionStrategy();
            return await strategy.Execute(async
                () =>
            {
                var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {

                    foreach (var ruleInterface in logicInstance.RuleInterfaceInstance)
                    {
                        ruleInterface.This2RuleInterfaceTemplateNavigation = null;

                        DbContext.RuleInterfaceInstances.Add(ruleInterface);
                        DbContext.Entry(ruleInterface).State = EntityState.Added;
                    }

                    logicInstance.This2RulePage = pageId;

                    logicInstance.CreatedAt = DateTimeOffset.Now;
                    logicInstance.ModifiedAt = DateTimeOffset.Now;
                    logicInstance.This2RuleTemplate = logicInstance.This2RuleTemplateNavigation.ObjId;
                    logicInstance.This2RuleTemplateNavigation = null;

                    DbContext.RuleInstances.Add(logicInstance);
                    DbContext.Entry(logicInstance).State = EntityState.Added;

                    logicInstance.This2RulePage = pageId;
                    await DbContext.SaveChangesAsync();


                    _logicCacheFacade.PageCache.AddRuleInstance(logicInstance);
                    _logicCacheFacade.InstanceCache.Add(logicInstance.ObjId, logicInstance);

                    await transaction.CommitAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Error add logicInstance {e}");
                    await transaction.RollbackAsync();
                    throw;
                }

                _logicCacheFacade.ClearInstances();
                await _coreServer.ReloadLogic(logicInstance.ObjId);

                return logicInstance;
            });
        }

        [HttpPatch]
        [Route("item/logicInstance")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<RuleInstance> UpdateLogicInstance([FromBody] RuleInstance logicInstance, [FromQuery]LogicUpdateScope updateScope = LogicUpdateScope.Unknown)
        {
            var strategy = DbContext.Database.CreateExecutionStrategy();
            return await strategy.Execute(async
                () =>
            {
                var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {
                    var existingInstance = DbContext.RuleInstances.Single(a => a.ObjId == logicInstance.ObjId);

                    existingInstance.Name = logicInstance.Name;
                    existingInstance.X = logicInstance.X;
                    existingInstance.Y = logicInstance.Y;
                    existingInstance.UseInVisu = logicInstance.UseInVisu;
                    existingInstance.Description = logicInstance.Description;
                    existingInstance.IsFavorite = logicInstance.IsFavorite;
                    existingInstance.Rating = logicInstance.Rating;
                    existingInstance.This2UserGroup = logicInstance.This2UserGroup;
                    existingInstance.This2AreaInstance = logicInstance.This2AreaInstance;
                    existingInstance.This2CategoryInstance = logicInstance.This2CategoryInstance;
                    existingInstance.ModifiedAt = DateTimeOffset.Now;

                    foreach (var ruleInterfaceInstance in logicInstance.RuleInterfaceInstance)
                    {
                        var existingRuleInterfaceInstance =
                            DbContext.RuleInterfaceInstances.Single(a => a.ObjId == ruleInterfaceInstance.ObjId);

                        existingRuleInterfaceInstance.ValueString = ruleInterfaceInstance.ValueString;
                        existingRuleInterfaceInstance.ValueDouble = ruleInterfaceInstance.ValueDouble;
                        existingRuleInterfaceInstance.ValueInteger = ruleInterfaceInstance.ValueInteger;

                        DbContext.Update(existingRuleInterfaceInstance);
                        DbContext.Entry(existingRuleInterfaceInstance).State = EntityState.Modified;
                    }

                    DbContext.Update(existingInstance);
                    DbContext.Entry(existingInstance).State = EntityState.Modified;

                    await DbContext.SaveChangesAsync();
                    
                    _logicCacheFacade.PageCache.UpdateRuleInstance(existingInstance);
                    _logicCacheFacade.InstanceCache.Update(logicInstance.ObjId,
                        _logicCacheFacade.InstanceCache.GetSingle(DbContext, logicInstance.ObjId));


                    await transaction.CommitAsync();
                }
                catch(Exception e)
                {
                    _logger.LogError(e, $"Error update logicInstance {e}");
                    await transaction.CommitAsync();
                    throw;
                }
                if (updateScope != LogicUpdateScope.Drag) //do not reload if we only drag it around
                {
                    await _coreServer.ReloadLogic(logicInstance.ObjId);
                    _logicCacheFacade.ClearInstances();
                }

                return logicInstance;
            });
        }

        [HttpPost]
        [Route("item/nodeInstance/{pageId}")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<NodeInstance2RulePage> AddNodeInstance([FromBody] NodeInstance2RulePage instance, Guid pageId)
        {
            var strategy = DbContext.Database.CreateExecutionStrategy();
            return await strategy.Execute(async
                () =>
            {
                var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {
                    instance.This2RulePage = pageId;
                    var nav = instance.This2NodeInstanceNavigation;
                    instance.This2NodeInstanceNavigation = null;
                    await DbContext.AddAsync(instance);
                    DbContext.Entry(instance).State = EntityState.Added;

                    await DbContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    instance.This2NodeInstanceNavigation = _nodeInstanceCache.GetSingle(nav.ObjId, DbContext);
         
                    _logicCacheFacade.ClearInstances();

                    return instance;
                }
                catch(Exception e)
                {
                    _logger.LogError(e, $"Error update nodeInstance {e}");
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }



        [HttpPatch]
        [Route("item/nodeInstance")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<NodeInstance2RulePage> UpdateNodeInstance([FromBody] NodeInstance2RulePage nodeInstance,
            [FromQuery] LogicUpdateScope updateScope = LogicUpdateScope.Unknown)
        {
            var strategy = DbContext.Database.CreateExecutionStrategy();
            return await strategy.Execute(async
                () =>
            {
                var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {
                    if (!DbContext.NodeInstance2RulePages.Any(a => a.ObjId == nodeInstance.ObjId))
                    {
                        return null;
                    }

                    var existingInstance = DbContext.NodeInstance2RulePages.Single(a => a.ObjId == nodeInstance.ObjId);

                    existingInstance.X = nodeInstance.X;
                    existingInstance.Y = nodeInstance.Y;

                    DbContext.Update(existingInstance);
                    DbContext.Entry(existingInstance).State = EntityState.Modified;

                    await DbContext.SaveChangesAsync();

                    _logicCacheFacade.PageCache.UpdateNodeInstance(existingInstance);
                    await transaction.CommitAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Error update nodeInstance {e}");
                    await transaction.RollbackAsync();
                    throw;
                }

                if (updateScope != LogicUpdateScope.Drag) //do not reload if we only drag it around
                {
                    _logicCacheFacade.ClearInstances();
                }

                return nodeInstance;
            });
        }

        [HttpDelete]
        [Route("page/{pageId}")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task RemovePage(Guid pageId)
        {
            var strategy = DbContext.Database.CreateExecutionStrategy();
            await strategy.Execute(async
                () =>
            {
                var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {

                    var rulePage = DbContext.RulePages.Include(a => a.RuleInstance)
                        .Include(a => a.NodeInstance2RulePage).Include(a => a.Link)
                        .SingleOrDefault(a => a.ObjId == pageId);

                    if (rulePage != null)
                    {
                        foreach (var link in rulePage.Link)
                        {
                            await RemoveLinkInternal(link.ObjId, DbContext, false);
                            await _logicCacheFacade.RemoveLink(link.ObjId);
                        }

                        foreach (var nodeInstance in rulePage.NodeInstance2RulePage)
                        {
                            await RemoveNodeInstanceInternal(nodeInstance.ObjId, DbContext);
                            await _logicCacheFacade.RemoveNodeInstance(nodeInstance.ObjId);
                        }

                        foreach (var ruleInstance in rulePage.RuleInstance)
                        {
                            await RemoveLogicInstanceInternal(ruleInstance.ObjId, DbContext);
                            await _logicCacheFacade.RemoveLogic(ruleInstance.ObjId);
                        }


                        DbContext.RulePages.Remove(rulePage);
                        await DbContext.SaveChangesAsync();
                    }
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error remove page {ex}");
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        [HttpDelete]
        [Route("item/logicInstance/{instanceId}")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task RemoveLogicInstance(Guid instanceId)
        {
            var strategy = DbContext.Database.CreateExecutionStrategy();
            await strategy.Execute(async
                () =>
            {
                var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {
                    await RemoveLogicInstanceInternal(instanceId, DbContext);
                    await DbContext.SaveChangesAsync();

                    await transaction.CommitAsync();

                    await _logicCacheFacade.RemoveLogic(instanceId);
                    _logicCacheFacade.ClearInstances();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error remove logicInstance {ex}");
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        private async Task RemoveLogicInstanceInternal(Guid instanceId, AutomaticaContext dbContext)
        {
            var ruleInstance = dbContext.RuleInstances.SingleOrDefault(a => a.ObjId == instanceId);

            if (ruleInstance != null)
            {
                await _coreServer.StopLogic(instanceId);

                _logicCacheFacade.PageCache.RemoveRuleInstance(ruleInstance);
                dbContext.RuleInstances.Remove(ruleInstance);
            }
        }

        [HttpDelete]
        [Route("item/nodeInstance/{instanceId}")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task RemoveNodeInstance(Guid instanceId)
        {
            var strategy = DbContext.Database.CreateExecutionStrategy();
            await strategy.Execute(async
                () =>
            {
                var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {
                    await RemoveNodeInstanceInternal(instanceId, DbContext);
                    await DbContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    await _logicCacheFacade.RemoveNodeInstance(instanceId);
                    _logicCacheFacade.ClearInstances();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error remove nodeInstance {ex}");
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        private async Task RemoveNodeInstanceInternal(Guid instanceId, AutomaticaContext dbContext)
        {
            await Task.CompletedTask;
            var nodeInstance = dbContext.NodeInstance2RulePages.SingleOrDefault(a => a.ObjId == instanceId);

            if (nodeInstance != null)
            {
                dbContext.NodeInstance2RulePages.Remove(nodeInstance);

                _logicCacheFacade.PageCache.RemoveNodeInstance(nodeInstance);
            }

        }

        [HttpPost]
        [Route("link")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<Link> AddOrUpdateLink([FromBody] Link link)
        {
            var strategy = DbContext.Database.CreateExecutionStrategy();
            return await strategy.Execute(async
                () =>
            {
                var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {
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

                    await DbContext.SaveChangesAsync();
                    await transaction.CommitAsync();

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

                    await _logicCacheFacade.AddOrUpdateLink(link.ObjId, DbContext);
                    _logicCacheFacade.ClearInstances();
                    await _coreServer.ReloadLinks();
                    return link;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Error update link {e}");
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        [HttpDelete]
        [Route("link/{objId}")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task RemoveLink(Guid objId)
        {
            var strategy = DbContext.Database.CreateExecutionStrategy();
            await strategy.Execute(async
                () =>
            {
                var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {

                    await RemoveLinkInternal(objId, DbContext);

                    await DbContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logicCacheFacade.ClearInstances();
                    await _coreServer.ReloadLinks();
                }
                catch (Exception e)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(e, $"Could not {nameof(RemoveLink)} {objId}", e);
                }
            });
        }

        private async Task RemoveLinkInternal(Guid objId, AutomaticaContext dbContext, bool reload = true)
        {
            var link = dbContext.Links.SingleOrDefault(a => a.ObjId == objId);

            if (link != null)
            {
                var instance = _logicCacheFacade.LinkCache.Get(objId);

                await _coreServer.RemoveLink(objId);
                await _logicCacheFacade.RemoveLink(objId);

                if (link.This2RuleInterfaceInstanceOutput.HasValue && reload)
                {
                    await _coreServer.ReloadLogic(instance.This2RuleInterfaceInstanceOutputNavigation.This2RuleInstance);
                }
                if (link.This2RuleInterfaceInstanceInput.HasValue && reload)
                {
                    await _coreServer.ReloadLogic(instance.This2RuleInterfaceInstanceInputNavigation.This2RuleInstance);
                }


                dbContext.Links.Remove(link);

            }

            _logicCacheFacade.LinkCache.Clear();
        }

        [HttpPost]
        [Route("reload")]
        [Authorize(Policy = Role.AdminRole)]
        public void Reload()
        {
            Environment.Exit(ServerInfo.ExitCodeUpdateInstall);
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
        public RulePage GetPage(Guid id)
        {
            return _logicCacheFacade.PageCache.Get(id);
        }

        [HttpGet]
        [Route("templates")]
        [Authorize(Policy = Role.AdminRole)]
        public ICollection<RuleTemplate> GetLogicTemplates()
        {
            return _logicCacheFacade.TemplateCache.All();
        }


        [HttpGet]
        [Route("data/{id}")]
        [Authorize(Policy = Role.ViewerRole)]
        public object GetInstanceData(Guid id)
        {
            return _logicDataHandler.GetDataForRuleInstance(id);
        }

        [HttpPatch]
        [Route("page")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<RulePage> UpdatePage([FromBody]RulePage page)
        {
            var rulePage = DbContext.RulePages.SingleOrDefault(a => a.ObjId == page.ObjId);

            if (rulePage != null)
            {
                rulePage.ModifiedAt = DateTimeOffset.Now;
                rulePage.Name = page.Name;
                rulePage.Description = page.Description;

                DbContext.Update(rulePage);
                await DbContext.SaveChangesAsync();
            }

            _logicCacheFacade.PageCache.Clear();
            return rulePage;
        }

    }
}
