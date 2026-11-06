using System;
using Automatica.Core.Runtime.Exceptions;
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
        Drag = 1,
        InvertedValueUpdated = 2
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
        private readonly ILogicInstancesStore _logicInstanceStore;

        public LogicsController(ILogger<LogicsController> logger, AutomaticaContext db, ILogicDataHandler logicDataHandler, ILogicCacheFacade logicCacheFacade, IConfiguration config, 
            INotifyDriver notifyDriver, INodeInstanceCache nodeInstanceCache, ICoreServer coreServer, ILogicInstancesStore logicInstanceStore)
            : base(db)
        {
            _logger = logger;
            _logicDataHandler = logicDataHandler;
            _logicCacheFacade = logicCacheFacade;
            _config = config;
            _notifyDriver = notifyDriver;
            _nodeInstanceCache = nodeInstanceCache;
            _coreServer = coreServer;
            _logicInstanceStore = logicInstanceStore;
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
            await strategy.ExecuteAsync(async
                () =>
            {
                await using var transaction = await DbContext.Database.BeginTransactionAsync();
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
                    if (logicInstance.This2RuleTemplateNavigation != null)
                    {
                        logicInstance.This2RuleTemplate = logicInstance.This2RuleTemplateNavigation.ObjId;
                    }
                    logicInstance.This2RuleTemplateNavigation = null;

                    DbContext.RuleInstances.Add(logicInstance);
                    DbContext.Entry(logicInstance).State = EntityState.Added;

                    logicInstance.This2RulePage = pageId;
                    await DbContext.SaveChangesAsync();

                    await transaction.CommitAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Error add logicInstance {e}");
                    throw;
                }
            });

            // caches and runtime only after the commit
            _logicCacheFacade.PageCache.AddRuleInstance(logicInstance);
            _logicCacheFacade.InstanceCache.Add(logicInstance.ObjId, logicInstance);

            _logicCacheFacade.ClearInstances();
            await _coreServer.ReloadLogic(logicInstance.ObjId);

            return logicInstance;
        }

        [HttpPatch]
        [Route("item/logicInstance")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<RuleInstance> UpdateLogicInstance([FromBody] RuleInstance logicInstance, [FromQuery]LogicUpdateScope updateScope = LogicUpdateScope.Unknown)
        {
            RuleInstance existingInstance = null;
            var strategy = DbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async
                () =>
            {
                await using var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {
                    existingInstance = DbContext.RuleInstances.Single(a => a.ObjId == logicInstance.ObjId);

                    existingInstance.Name = logicInstance.Name;
                    existingInstance.VisuName = logicInstance.VisuName;
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

                    // one query for all interfaces instead of one per interface
                    var interfaceIds = logicInstance.RuleInterfaceInstance.Select(a => a.ObjId).ToList();
                    var existingInterfaces = DbContext.RuleInterfaceInstances
                        .Where(a => interfaceIds.Contains(a.ObjId))
                        .ToDictionary(a => a.ObjId);

                    foreach (var ruleInterfaceInstance in logicInstance.RuleInterfaceInstance)
                    {
                        if (!existingInterfaces.TryGetValue(ruleInterfaceInstance.ObjId, out var existingRuleInterfaceInstance))
                        {
                            throw new InvalidOperationException($"Rule interface instance {ruleInterfaceInstance.ObjId} does not exist");
                        }

                        existingRuleInterfaceInstance.ValueString = ruleInterfaceInstance.ValueString;
                        existingRuleInterfaceInstance.ValueDouble = ruleInterfaceInstance.ValueDouble;
                        existingRuleInterfaceInstance.ValueInteger = ruleInterfaceInstance.ValueInteger;
                        existingRuleInterfaceInstance.ValueBool = ruleInterfaceInstance.ValueBool;
                        existingRuleInterfaceInstance.Inverted = ruleInterfaceInstance.Inverted;

                        DbContext.Update(existingRuleInterfaceInstance);
                        DbContext.Entry(existingRuleInterfaceInstance).State = EntityState.Modified;
                    }

                    DbContext.Update(existingInstance);
                    DbContext.Entry(existingInstance).State = EntityState.Modified;

                    await DbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch(Exception e)
                {
                    // the transaction is rolled back on dispose
                    _logger.LogError(e, $"Error update logicInstance {e}");
                    throw;
                }
            });

            _logicCacheFacade.PageCache.UpdateRuleInstance(existingInstance);
            _logicCacheFacade.UpdateLogic(
                _logicCacheFacade.InstanceCache.GetSingle(DbContext, logicInstance.ObjId));

            if (updateScope != LogicUpdateScope.Drag) //do not reload if we only drag it around
            {
                await _coreServer.ReloadLogic(logicInstance.ObjId);
                _logicCacheFacade.ClearInstances();
            }

            return logicInstance;
        }

        [HttpPost]
        [Route("item/nodeInstance/{pageId}")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<NodeInstance2RulePage> AddNodeInstance([FromBody] NodeInstance2RulePage instance, Guid pageId)
        {
            var strategy = DbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async
                () =>
            {
                await using var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {
                    instance.This2RulePageNavigation = null;
                    instance.This2RulePage = pageId;
                    instance.This2NodeInstanceNavigation = null;

                    await DbContext.AddAsync(instance);
                    DbContext.Entry(instance).State = EntityState.Added;

                    await DbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch(Exception e)
                {
                    _logger.LogError(e, $"Error update nodeInstance {e}");
                    throw;
                }
            });

            _logicCacheFacade.LogicNodeInstanceCache.AddOrUpdate(instance.ObjId, instance);
            _logicCacheFacade.ClearInstances();

            return instance;
        }



        [HttpPatch]
        [Route("item/nodeInstance")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<NodeInstance2RulePage> UpdateNodeInstance([FromBody] NodeInstance2RulePage nodeInstance,
            [FromQuery] LogicUpdateScope updateScope = LogicUpdateScope.Unknown)
        {
            var strategy = DbContext.Database.CreateExecutionStrategy();
            var existingInstance = await strategy.ExecuteAsync(async
                () =>
            {
                await using var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {
                    var existing = DbContext.NodeInstance2RulePages.SingleOrDefault(a => a.ObjId == nodeInstance.ObjId);
                    if (existing == null)
                    {
                        return null;
                    }

                    if (updateScope == LogicUpdateScope.Drag)
                    {
                        existing.X = nodeInstance.X;
                        existing.Y = nodeInstance.Y;
                    }
                    else if (updateScope == LogicUpdateScope.InvertedValueUpdated)
                    {
                        existing.Inverted = nodeInstance.Inverted;
                    }


                    DbContext.Update(existing);
                    DbContext.Entry(existing).State = EntityState.Modified;

                    await DbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return existing;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Error update nodeInstance {e}");
                    throw;
                }
            });

            if (existingInstance == null)
            {
                return null;
            }

            _logicCacheFacade.PageCache.UpdateNodeInstance(existingInstance);
            _logicCacheFacade.LogicNodeInstanceCache.AddOrUpdate(existingInstance.ObjId, existingInstance);

            if (updateScope != LogicUpdateScope.Drag) //do not reload if we only drag it around
            {
                _logicCacheFacade.ClearInstances();
            }

            return nodeInstance;
        }

        [HttpDelete]
        [Route("page/{pageId}")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task RemovePage(Guid pageId)
        {
            var removedLinks = new List<RemovedLink>();
            var removedNodeInstances = new List<NodeInstance2RulePage>();
            var removedLogics = new List<RuleInstance>();

            var strategy = DbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async
                () =>
            {
                removedLinks.Clear();
                removedNodeInstances.Clear();
                removedLogics.Clear();

                await using var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {

                    var rulePage = DbContext.RulePages.Include(a => a.RuleInstance)
                        .Include(a => a.NodeInstance2RulePage).Include(a => a.Link)
                        .SingleOrDefault(a => a.ObjId == pageId);

                    if (rulePage != null)
                    {
                        foreach (var link in rulePage.Link.ToList())
                        {
                            var removed = RemoveLinkInternal(link.ObjId, DbContext);
                            if (removed != null)
                            {
                                removedLinks.Add(removed);
                            }
                        }

                        foreach (var nodeInstance in rulePage.NodeInstance2RulePage.ToList())
                        {
                            var removed = RemoveNodeInstanceInternal(nodeInstance.ObjId, DbContext);
                            if (removed != null)
                            {
                                removedNodeInstances.Add(removed);
                            }
                        }

                        foreach (var ruleInstance in rulePage.RuleInstance.ToList())
                        {
                            var removed = RemoveLogicInstanceInternal(ruleInstance.ObjId, DbContext);
                            if (removed != null)
                            {
                                removedLogics.Add(removed);
                            }
                        }


                        DbContext.RulePages.Remove(rulePage);
                        await DbContext.SaveChangesAsync();
                    }
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error remove page {ex}");
                    throw;
                }
            });

            // runtime and caches only after the commit
            foreach (var removedLink in removedLinks)
            {
                await UnlinkRemovedLink(removedLink, false);
            }
            _logicCacheFacade.LinkCache.Clear();

            foreach (var nodeInstance in removedNodeInstances)
            {
                _logicCacheFacade.PageCache.RemoveNodeInstance(nodeInstance);
                await _logicCacheFacade.RemoveNodeInstance(nodeInstance.ObjId);
            }

            foreach (var ruleInstance in removedLogics)
            {
                await StopRemovedLogic(ruleInstance);
            }
        }

        [HttpDelete]
        [Route("item/logicInstance/{instanceId}")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task RemoveLogicInstance(Guid instanceId)
        {
            RuleInstance removed = null;
            var strategy = DbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async
                () =>
            {
                await using var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {
                    removed = RemoveLogicInstanceInternal(instanceId, DbContext);
                    await DbContext.SaveChangesAsync();

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error remove logicInstance {ex}");
                    throw;
                }
            });

            if (removed != null)
            {
                await StopRemovedLogic(removed);
            }
            else
            {
                await _logicCacheFacade.RemoveLogic(instanceId);
            }

            _logicCacheFacade.ClearInstances();
        }

        /// <summary>
        /// Only marks the logic instance as removed in the context, the runtime part is done by <see cref="StopRemovedLogic"/> after the commit
        /// </summary>
        private RuleInstance RemoveLogicInstanceInternal(Guid instanceId, AutomaticaContext dbContext)
        {
            var ruleInstance = dbContext.RuleInstances.SingleOrDefault(a => a.ObjId == instanceId);

            if (ruleInstance != null)
            {
                dbContext.RuleInstances.Remove(ruleInstance);
            }

            return ruleInstance;
        }

        private async Task StopRemovedLogic(RuleInstance ruleInstance)
        {
            await _coreServer.StopLogic(ruleInstance.ObjId);

            _logicCacheFacade.PageCache.RemoveRuleInstance(ruleInstance);
            _logicInstanceStore.Remove(ruleInstance);
            await _logicCacheFacade.RemoveLogic(ruleInstance.ObjId);
        }

        [HttpDelete]
        [Route("item/nodeInstance/{instanceId}")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task RemoveNodeInstance(Guid instanceId)
        {
            NodeInstance2RulePage removed = null;
            var strategy = DbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async
                () =>
            {
                await using var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {
                    removed = RemoveNodeInstanceInternal(instanceId, DbContext);
                    await DbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error remove nodeInstance {ex}");
                    throw;
                }
            });

            if (removed != null)
            {
                _logicCacheFacade.PageCache.RemoveNodeInstance(removed);
            }
            await _logicCacheFacade.RemoveNodeInstance(instanceId);
            _logicCacheFacade.ClearInstances();
        }

        private NodeInstance2RulePage RemoveNodeInstanceInternal(Guid instanceId, AutomaticaContext dbContext)
        {
            var nodeInstance = dbContext.NodeInstance2RulePages.SingleOrDefault(a => a.ObjId == instanceId);

            if (nodeInstance != null)
            {
                dbContext.NodeInstance2RulePages.Remove(nodeInstance);
            }

            return nodeInstance;
        }

        [HttpPost]
        [Route("link")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<Link> AddOrUpdateLink([FromBody] Link link)
        {
            Guid? this2RuleOutputRuleId = null;
            Guid? this2RuleInputRuleId = null;

            var strategy = DbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async
                () =>
            {
                await using var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {
                    link.This2RuleInterfaceInstanceOutputNavigation = null;
                    link.This2RuleInterfaceInstanceInputNavigation = null;
                    link.This2NodeInstance2RulePageInputNavigation = null;
                    link.This2NodeInstance2RulePageOutputNavigation = null;

                    // the navigations are cleared above, resolve the linked logics by the foreign keys
                    this2RuleOutputRuleId = GetRuleInstanceIdOfInterface(link.This2RuleInterfaceInstanceOutput, DbContext);
                    this2RuleInputRuleId = GetRuleInstanceIdOfInterface(link.This2RuleInterfaceInstanceInput, DbContext);

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
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Error update link {e}");
                    throw;
                }
            });

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

        private static Guid? GetRuleInstanceIdOfInterface(Guid? ruleInterfaceInstanceId, AutomaticaContext dbContext)
        {
            if (!ruleInterfaceInstanceId.HasValue)
            {
                return null;
            }

            var interfaceId = ruleInterfaceInstanceId.Value;
            return dbContext.RuleInterfaceInstances.AsNoTracking()
                .Where(a => a.ObjId == interfaceId)
                .Select(a => (Guid?)a.This2RuleInstance)
                .SingleOrDefault();
        }

        [HttpDelete]
        [Route("link/{objId}")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task RemoveLink(Guid objId)
        {
            RemovedLink removed = null;
            var strategy = DbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async
                () =>
            {
                await using var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {

                    removed = RemoveLinkInternal(objId, DbContext);

                    await DbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Could not {nameof(RemoveLink)} {objId}");
                    throw;
                }
            });

            if (removed != null)
            {
                await UnlinkRemovedLink(removed, true);
            }
            _logicCacheFacade.LinkCache.Clear();

            _logicCacheFacade.ClearInstances();
            await _coreServer.ReloadLinks();
        }

        private sealed record RemovedLink(Guid LinkId, Guid? OutputRuleInstanceId, Guid? InputRuleInstanceId);

        /// <summary>
        /// Only marks the link as removed in the context, unlinking it in the runtime is done by <see cref="UnlinkRemovedLink"/> after the commit
        /// </summary>
        private RemovedLink RemoveLinkInternal(Guid objId, AutomaticaContext dbContext)
        {
            var link = dbContext.Links.SingleOrDefault(a => a.ObjId == objId);

            if (link == null)
            {
                return null;
            }

            var removed = new RemovedLink(objId,
                GetRuleInstanceIdOfInterface(link.This2RuleInterfaceInstanceOutput, dbContext),
                GetRuleInstanceIdOfInterface(link.This2RuleInterfaceInstanceInput, dbContext));

            dbContext.Links.Remove(link);
            return removed;
        }

        private async Task UnlinkRemovedLink(RemovedLink removed, bool reload)
        {
            await _coreServer.RemoveLink(removed.LinkId);
            await _logicCacheFacade.RemoveLink(removed.LinkId);

            if (removed.OutputRuleInstanceId.HasValue && reload)
            {
                await _coreServer.ReloadLogic(removed.OutputRuleInstanceId.Value);
            }
            if (removed.InputRuleInstanceId.HasValue && reload)
            {
                await _coreServer.ReloadLogic(removed.InputRuleInstanceId.Value);
            }
        }

        [HttpPost]
        [Route("reload")]
        [Authorize(Policy = Role.AdminRole)]
        public void Reload()
        {
            StopServer(ServerInfo.ExitCodeUpdateInstall);
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
            // logics of other user groups look like unknown ids
            var logic = _logicCacheFacade.InstanceCache.Get(id);
            if (logic == null || !CanAccess(logic.This2UserGroup))
            {
                throw new RuleNotFoundException();
            }

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
