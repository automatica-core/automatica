using Automatica.Core.EF.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Internals.Core;
using Automatica.Core.Internals.Recorder;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Automatica.Core.Base.Common;
using Automatica.Core.Base.Localization;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.Cache.Logic;

namespace Automatica.Core.WebApi.Controllers
{
    public enum UpdateScope
    {
        Unknown = 0,
        GenericProperty = 1,
        SpecificProperty = 2, 
        ParentChanged = 3,
        Imported = 4
    }


    [Route("webapi/nodeInstancesV2")]
    [Authorize(Roles = Role.AdminRole)]
    public class NodeInstanceV2Controller : BaseController
    {
        private readonly INodeInstanceCache _nodeInstanceCache;
        private readonly INotifyDriver _notifyDriver;
        private readonly ICoreServer _coreServer;
        private readonly INodeTemplateCache _templateCache;
        private readonly IDriverNodesStore _driverNodeStore;
        private readonly INodeInstanceService _nodeInstanceService;
        private readonly IConfiguration _config;
        private readonly IRecorderContext _recorderContext;
        private readonly ISettingsCache _settingsCache;
        private readonly ILocalizationProvider _localizationProvider;
        private readonly ILogicCacheFacade _logicCacheFacade;
        private readonly ILogger _logger;

        public NodeInstanceV2Controller(
            AutomaticaContext dbContext, 
            INodeInstanceCache nodeInstanceCache, 
            INotifyDriver notifyDriver, 
            ICoreServer coreServer, 
            INodeTemplateCache templateCache,
            IDriverNodesStore driverNodeStore,
            INodeInstanceService nodeInstanceService,
            IConfiguration config,
            IRecorderContext recorderContext,
            ISettingsCache settingsCache,
            ILocalizationProvider localizationProvider,
            ILogicCacheFacade logicCacheFacade,
            ILogger<NodeInstanceV2Controller> logger) : base(dbContext)
        {
            _nodeInstanceCache = nodeInstanceCache;
            _notifyDriver = notifyDriver;
            _coreServer = coreServer;
            _templateCache = templateCache;
            _driverNodeStore = driverNodeStore;
            _nodeInstanceService = nodeInstanceService;
            _config = config;
            _recorderContext = recorderContext;
            _settingsCache = settingsCache;
            _localizationProvider = localizationProvider;
            _logicCacheFacade = logicCacheFacade;
            _logger = logger;
        }

        /// <summary>
        /// Only the node rows (and their properties): the posted navigations are detached, their foreign keys are kept.
        /// </summary>
        private static void DetachNavigations(NodeInstance node)
        {
            if (node.This2NodeTemplateNavigation != null && !node.This2NodeTemplate.HasValue)
            {
                node.This2NodeTemplate = node.This2NodeTemplateNavigation.ObjId;
            }
            if (node.This2AreaInstanceNavigation != null && !node.This2AreaInstance.HasValue)
            {
                node.This2AreaInstance = node.This2AreaInstanceNavigation.ObjId;
            }
            if (node.This2CategoryInstanceNavigation != null && !node.This2CategoryInstance.HasValue)
            {
                node.This2CategoryInstance = node.This2CategoryInstanceNavigation.ObjId;
            }
            if (node.This2UserGroupNavigation != null && !node.This2UserGroup.HasValue)
            {
                node.This2UserGroup = node.This2UserGroupNavigation.ObjId;
            }
            if (node.This2SlaveNavigation != null && !node.This2Slave.HasValue)
            {
                node.This2Slave = node.This2SlaveNavigation.ObjId;
            }
            if (node.This2ParentNodeInstanceNavigation != null && !node.This2ParentNodeInstance.HasValue)
            {
                node.This2ParentNodeInstance = node.This2ParentNodeInstanceNavigation.ObjId;
            }

            node.This2NodeTemplateNavigation = null;
            node.This2AreaInstanceNavigation = null;
            node.This2CategoryInstanceNavigation = null;
            node.This2UserGroupNavigation = null;
            node.This2SlaveNavigation = null;
            node.This2ParentNodeInstanceNavigation = null;
            node.NodeInstance2RulePage = null;
        }

        /// <param name="node">the node to save (recursive with its children)</param>
        /// <param name="trendingChanges">collects the nodes whose trending flag changed, the recorders are updated after the commit</param>
        private async Task<EntityState> AddOrUpdateNodeInstance(NodeInstance node, List<(Guid nodeId, bool trending)> trendingChanges)
        {
            var childs = node.InverseThis2ParentNodeInstanceNavigation ?? new List<NodeInstance>();
            var props = node.PropertyInstance;

            node.InverseThis2ParentNodeInstanceNavigation = null;
            node.PropertyInstance = null;
            try
            {
                return await AddOrUpdateNodeInstance(node, childs, props, trendingChanges);
            }
            finally
            {
                // the posted graph stays intact: the execution strategy runs this again on a transient error
                node.InverseThis2ParentNodeInstanceNavigation = childs;
                node.PropertyInstance = props;
            }
        }

        private async Task<EntityState> AddOrUpdateNodeInstance(NodeInstance node, List<NodeInstance> childs, List<PropertyInstance> props, List<(Guid nodeId, bool trending)> trendingChanges)
        {
            DetachNavigations(node);
            node.IsDeleted = false;
            node.ModifiedAt = DateTimeOffset.Now;

            if (node.Description == null)
            {
                node.Description = "";
            }

            var entityState = EntityState.Unchanged;
            var entity = DbContext.NodeInstances.AsNoTracking().SingleOrDefault(a => a.ObjId == node.ObjId);
            if (entity != null)
            {
                DbContext.Entry(entity).State = EntityState.Detached;
                // server side field, never the posted value
                node.CreatedAt = entity.CreatedAt;
                DbContext.Entry(node).State = EntityState.Modified;

                DbContext.Update(node);
                entityState = EntityState.Modified;

                if (entity.Trending != node.Trending)
                {
                    trendingChanges.Add((node.ObjId, node.Trending));
                }

            }
            else
            {
                node.CreatedAt = DateTimeOffset.Now;
                DbContext.Entry(node).State = EntityState.Added;
                await DbContext.AddAsync(node);
                entityState = EntityState.Added;
            }

            if (props != null)
            {
                foreach (var prop in props)
                {
                    prop.This2NodeInstanceNavigation = node;
                    prop.This2PropertyTemplateNavigation = null;
                    prop.ModifiedAt = DateTimeOffset.Now;
                    if (entityState == EntityState.Modified)
                    {
                        DbContext.Update(prop);
                    }
                    else
                    {
                        prop.CreatedAt = DateTimeOffset.Now;
                        await DbContext.AddAsync(prop);
                    }
                }
            }

            if (childs != null)
            {
                foreach (var child in childs)
                {
                    await AddOrUpdateNodeInstance(child, trendingChanges);
                }
            }

            return entityState;

        }

        private async Task ApplyTrendingChanges(IEnumerable<(Guid nodeId, bool trending)> trendingChanges)
        {
            foreach (var (nodeId, trending) in trendingChanges)
            {
                if (trending)
                {
                    await _recorderContext.AddRecording(nodeId);
                }
                else
                {
                    await _recorderContext.RemoveRecording(nodeId);
                }
            }
        }


        [HttpPost]
        [Authorize(Roles = Role.AdminRole)]
        [Route("create/{locale}/{parentNodeInstance}/{nodeTemplate}")]
        public async Task<NodeInstance> CreateFromTemplate(string locale, Guid parentNodeInstance, Guid nodeTemplate)
        {
            var instance = _nodeInstanceService.CreateNodeInstance(locale, _nodeInstanceCache.Get(parentNodeInstance), nodeTemplate);
            return await AddNode(instance);
        }

        /// <summary>
        /// Adds a device found in learn mode in one step (name, area and address properties set, the driver reloads
        /// once), or lets it take over an existing node. 409 if another node below the learn node has the same
        /// <see cref="LearnedNodeRequest.UniqueProperty"/> value.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = Role.AdminRole)]
        [Route("learned/{locale}")]
        public async Task<ActionResult<NodeInstance>> AddLearnedNode(string locale, [FromBody] LearnedNodeRequest request)
        {
            var parent = request == null ? null : _nodeInstanceCache.Get(request.Parent);
            if (parent == null)
            {
                return NotFound();
            }

            var properties = request.Properties ?? new Dictionary<string, string>();
            if (!String.IsNullOrEmpty(request.UniqueProperty) && properties.TryGetValue(request.UniqueProperty, out var unique) &&
                !String.IsNullOrEmpty(unique))
            {
                var duplicate = FindByProperty(parent, request.UniqueProperty, unique, request.Replace);
                if (duplicate != null)
                {
                    return Conflict(new LearnedNodeConflict { NodeId = duplicate.ObjId, Name = duplicate.Name });
                }
            }

            if (request.Replace.HasValue)
            {
                if (!IsBelow(request.Replace.Value, parent.ObjId))
                {
                    return BadRequest($"{request.Replace} is not below {parent.ObjId}");
                }

                var existing = _nodeInstanceCache.GetSingle(request.Replace.Value, DbContext);
                SetProperties(existing, properties);
                _logger.LogInformation($"Learned device takes over {existing.ObjId} {existing.Name}");
                return await UpdateNode(existing, UpdateScope.SpecificProperty);
            }

            var node = _nodeInstanceService.CreateNodeInstance(locale, parent, request.NodeTemplate);
            if (!String.IsNullOrWhiteSpace(request.Name))
            {
                node.Name = request.Name.Trim();
            }
            if (request.Description != null)
            {
                node.Description = request.Description;
            }
            node.This2AreaInstance = request.Area;
            node.This2CategoryInstance = request.Category;
            SetProperties(node, properties);

            _logger.LogInformation($"Add learned device {node.Name} below {parent.ObjId} {parent.Name}");
            return await AddNode(node);
        }

        private static void SetProperties(NodeInstance node, IDictionary<string, string> properties)
        {
            foreach (var property in properties)
            {
                node.SetProperty(property.Key, property.Value);
                foreach (var child in node.InverseThis2ParentNodeInstanceNavigation ?? new List<NodeInstance>())
                {
                    child.SetProperty(property.Key, property.Value);
                }
            }
        }

        /// <returns>the first node below <paramref name="root"/> (except <paramref name="except"/>) with the property value</returns>
        private NodeInstance FindByProperty(NodeInstance root, string key, string value, Guid? except)
        {
            var queue = new Queue<NodeInstance>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                var item = queue.Dequeue();
                foreach (var child in item.InverseThis2ParentNodeInstanceNavigation ?? new List<NodeInstance>())
                {
                    var node = _nodeInstanceCache.Get(child.ObjId) ?? child;
                    var property = node.PropertyInstance?.FirstOrDefault(p => p.This2PropertyTemplateNavigation?.Key == key);
                    if (node.ObjId != except && property?.Value != null &&
                        String.Equals(Convert.ToString(property.Value, System.Globalization.CultureInfo.InvariantCulture)?.Replace(" ", ""),
                            value.Replace(" ", ""), StringComparison.OrdinalIgnoreCase))
                    {
                        return node;
                    }
                    queue.Enqueue(node);
                }
            }

            return null;
        }

        private bool IsBelow(Guid node, Guid ancestor)
        {
            var current = _nodeInstanceCache.Get(node);
            for (var depth = 0; current?.This2ParentNodeInstance != null && depth < 64; depth++)
            {
                if (current.This2ParentNodeInstance == ancestor)
                {
                    return true;
                }
                current = _nodeInstanceCache.Get(current.This2ParentNodeInstance.Value);
            }

            return false;
        }

        [HttpPost]
        [Authorize(Roles = Role.AdminRole)]
        [Route("copy/{nodeInstance}/{targetNodeInstance}")]
        public async Task<NodeInstance> Copy(Guid nodeInstance, Guid targetNodeInstance)
        {
            var instance = _nodeInstanceCache.GetSingle(nodeInstance, DbContext);

            var copyInstance = JsonConvert.DeserializeObject<NodeInstance>(JsonConvert.SerializeObject(instance));

            CopyRec(copyInstance);

            copyInstance.This2ParentNodeInstance = targetNodeInstance;
            var childs = copyInstance.InverseThis2ParentNodeInstanceNavigation;

            await AddNode(copyInstance);


            copyInstance.InverseThis2ParentNodeInstanceNavigation = childs;
            return copyInstance;
        }

        private void CopyRec(NodeInstance nodeInstance)
        {
            nodeInstance.ObjId = Guid.NewGuid();

            foreach (var property in nodeInstance.PropertyInstance)
            {
                property.ObjId = Guid.NewGuid();
            }

            foreach (var node in nodeInstance.InverseThis2ParentNodeInstanceNavigation)
            {
                CopyRec(node);
                node.This2ParentNodeInstance = nodeInstance.ObjId;
            }
        }

        [HttpPut]
        [Route("add")]
        public async Task<NodeInstance> AddNode([FromBody] NodeInstance node)
        {
            _logger.LogDebug($"Add Node...");

            // read before saving, the navigations are detached when the node is saved
            var nodeTemplate = node.This2NodeTemplateNavigation ??
                               (node.This2NodeTemplate.HasValue ? _templateCache.Get(node.This2NodeTemplate.Value) : null);
            var newNode = await AddNodeInternal(node);


            if (nodeTemplate != null &&
                nodeTemplate.IsAdapterInterface.HasValue &&
                nodeTemplate.IsAdapterInterface.Value &&
                newNode.node.This2ParentNodeInstance.HasValue)
            {
                newNode.node.This2ParentNodeInstanceNavigation =
                    _nodeInstanceCache.Get(newNode.node.This2ParentNodeInstance.Value);
                return newNode.node;
            }

            _ = Task.Factory.StartNew(() => ReloadDriver(node, newNode.entityState).ConfigureAwait(false), CancellationToken.None);

            _logger.LogDebug($"Add Node...done");

            await DbContext.SaveChangesAsync();

            return newNode.node;
        }

        private async Task<(NodeInstance node, EntityState entityState)> AddNodeInternal(NodeInstance node)
        {
            var entityState = EntityState.Unchanged;
            var trendingChanges = new List<(Guid nodeId, bool trending)>();
            var children = new List<NodeInstance>();

            var strategy = DbContext.Database.CreateExecutionStrategy();
            var attempt = 0;
            await strategy.ExecuteAsync(async
                () =>
            {
                trendingChanges.Clear();
                if (attempt++ > 0)
                {
                    // retry: forget the entities tracked by the failed attempt
                    DbContext.ChangeTracker.Clear();
                }
                await using var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {
                    entityState = await AddOrUpdateNodeInstance(node, trendingChanges);
                    await DbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Could not {nameof(AddNode)} {nameof(NodeInstance)} {e}");
                    throw;
                }
            });

            // caches and recorders only after the commit
            _nodeInstanceCache.GetSingle(node.ObjId, DbContext);

            var queue = new Queue<NodeInstance>();
            queue.Enqueue(node);

            while (queue.Count > 0)
            {
                var item = queue.Dequeue();
                if (item.InverseThis2ParentNodeInstanceNavigation != null)
                {
                    foreach (var child in item.InverseThis2ParentNodeInstanceNavigation)
                    {
                        queue.Enqueue(child);
                        _nodeInstanceCache.GetSingle(child.ObjId, DbContext);
                    }
                }

            }

            await ApplyTrendingChanges(trendingChanges);

            return (_nodeInstanceCache.Get(node.ObjId), entityState);
        }

        private async Task ReloadDriver(NodeInstance node, EntityState entityState)
        {
            if (!node.This2ParentNodeInstance.HasValue)
            {
                return;
            }
            try
            {
                var nodeInstance = _nodeInstanceCache.Get(node.ObjId);
                if (entityState == EntityState.Added)
                {
                    var nodeTemplate = _templateCache.Get(nodeInstance.This2NodeTemplate.Value);
                    if (nodeTemplate.ProvidesInterface2InterfaceTypeNavigation.IsDriverInterface)
                    {
                        await _coreServer.InitializeAndStartDriver(nodeInstance, nodeTemplate);
                    }
                    else
                    {
                        await StopStartDriver(nodeInstance);
                    }
                }
                else if (entityState == EntityState.Modified)
                {
                    await StopStartDriver(nodeInstance);
                }

            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error hot-load driver..");
            }
        }

        private async Task StopStartDriver(NodeInstance node)
        {
            await _coreServer.StartStopDriver(node);
        }

        [Route("delete")]
        [HttpPost]
        public async Task<IActionResult> DeleteNode([FromBody] NodeInstance node)
        {
            NodeInstance existingNode = null;
            var strategy = DbContext.Database.CreateExecutionStrategy();
            try
            {
                await strategy.ExecuteAsync(async
                    () =>
                {
                    await using var transaction = await DbContext.Database.BeginTransactionAsync();
                    existingNode = DbContext.NodeInstances.AsNoTracking().Include(a => a.This2NodeTemplateNavigation)
                        .SingleOrDefault(a => a.ObjId == node.ObjId);

                    if (existingNode == null)
                    {
                        // nothing to delete, the transaction is disposed (rolled back)
                        return;
                    }

                    DbContext.Entry(existingNode).State = EntityState.Deleted;

                    await DbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not {nameof(DeleteNode)} {nameof(NodeInstance)} {e}");
                _nodeInstanceCache.Clear();
                throw;
            }

            if (existingNode == null)
            {
                return NotFound();
            }

            try
            {
                // runtime only after the commit (and before the node is removed from the cache, the driver lookup needs it)
                if (!(existingNode.This2NodeTemplateNavigation?.IsAdapterInterface ?? false))
                {
                    var rootNode = _nodeInstanceCache.GetDriverNodeInstanceFromChild(node);

                    if (rootNode == null)
                    {
                        _logger.LogWarning($"Could not find the driver node of {node.Name}");
                    }
                    else if (rootNode.ObjId == node.ObjId)
                    {
                        var driver = _driverNodeStore.GetDriver(rootNode.ObjId);
                        if (driver != null)
                        {
                            await _coreServer.StopDriver(driver);
                        }
                    }
                    else
                    {
                        await _notifyDriver.NotifyDeleted(node);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error stopping driver {node.Name}...");
            }
            finally
            {
                _nodeInstanceCache.Remove(existingNode);
            }

            return Ok();
        }

        [Route("reload")]
        [HttpPost]
        public void ReInit()
        {
            StopServer(ServerInfo.ExitCodeUpdateInstall);
        }

        [Route("update")]
        [HttpPost]
        public async Task<NodeInstance> UpdateNode([FromBody]NodeInstance node,
            [FromQuery] UpdateScope updateScope = UpdateScope.Unknown)
        {
            var existingNode = _nodeInstanceCache.Get(node.ObjId);
            var reloadServer = false;

            if (existingNode != null)
            {
                if (existingNode.This2Slave != node.This2Slave)
                {
                    //slave changed, we need to reload all drivers!
                    reloadServer = true;
                }
            }

            var entityState = EntityState.Unchanged;
            var trendingChanges = new List<(Guid nodeId, bool trending)>();

            var strategy = DbContext.Database.CreateExecutionStrategy();
            var attempt = 0;
            await strategy.ExecuteAsync(async
                () =>
            {
                trendingChanges.Clear();
                if (attempt++ > 0)
                {
                    // retry: forget the entities tracked by the failed attempt
                    DbContext.ChangeTracker.Clear();
                }
                await using var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {
                    entityState = await AddOrUpdateNodeInstance(node, trendingChanges);

                    await DbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Could not {nameof(UpdateNode)} {nameof(NodeInstance)} {e}");
                    throw;
                }
            });

            // caches, recorders and drivers only after the commit
            _nodeInstanceCache.GetSingle(node.ObjId, DbContext);
            await ApplyTrendingChanges(trendingChanges);

            if (reloadServer)
            {
                await _coreServer.ReInit();
            }
            else
            {
                if (updateScope == UpdateScope.SpecificProperty)
                {
                    _logger.LogInformation($"Reload driver {node.ObjId} {node.FullName}");
                    _ = Task.Factory.StartNew(() => ReloadDriver(node, entityState).ConfigureAwait(false),
                        CancellationToken.None);
                }
                else
                {
                    _logger.LogInformation($"Ignore reload for driver update scope is {updateScope} {node.ObjId} {node.FullName}");
                }
            }

            return _nodeInstanceCache.Get(node.ObjId);
        }

        [HttpPost]
        [Route("import")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<ActionResult<IList<NodeInstance>>> Import([FromBody] ImportConfig config)
        {
            if (config == null)
            {
                return BadRequest();
            }

            // only a file uploaded to the private upload directory (FileUploadController returns its random name), never a path from the request
            string uploadedFile;
            try
            {
                uploadedFile = UploadPathHelper.GetSafeFilePath(ServerInfo.GetUploadDirectory(), config.FileName);
            }
            catch (ArgumentException)
            {
                return BadRequest("Invalid file name");
            }
            config.FileName = uploadedFile;

            IList<NodeInstance> data;
            try
            {
                data = await _notifyDriver.Import(config);
            }
            finally
            {
                if (System.IO.File.Exists(uploadedFile))
                {
                    System.IO.File.Delete(uploadedFile);
                }
            }

            if (data != null)
            {
                return Ok(await SaveAndReloadNodeInstances(data));
            }
            return Ok(new List<NodeInstance>());
        }

        private string ConvertLanguageEnum(Language language)
        {
            switch (language)
            {
                case Language.German:
                    return "de";
                case Language.English:
                    return "en";

            }

            return "en";
        }

        private async Task<IList<NodeInstance>> SaveAndReloadNodeInstances(IList<NodeInstance> nodeInstances)
        {
            var savedNodes = new List<NodeInstance>();
            var savedNodesData = new List<(NodeInstance node, EntityState entityState)>();
            foreach (var node in nodeInstances)
            {
                var newNode = await AddNodeInternal(node);
                savedNodes.Add(newNode.node);
                savedNodesData.Add(newNode);
            }

            if (savedNodesData.Any())
            {
                _ = Task.Factory.StartNew(() => ReloadDriver(savedNodesData.First().node, savedNodesData.First().entityState).ConfigureAwait(false), CancellationToken.None);
            }

            return savedNodes;
        }

        private void TranslateNodesRecursive(NodeInstance node, string curLanguage)
        {
            node.Name = _localizationProvider.GetTranslation(curLanguage, node.Name);
            node.Description = _localizationProvider.GetTranslation(curLanguage, node.Description);

            if (node.InverseThis2ParentNodeInstanceNavigation != null)
            {
                foreach (var child in node.InverseThis2ParentNodeInstanceNavigation)
                {
                    TranslateNodesRecursive(child, curLanguage);
                }
            }
        }

        [HttpPost]
        [Route("scan")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<IList<NodeInstance>> Scan([FromBody] NodeInstance instance)
        {
            try
            {
                _logger.LogInformation($"Start scan for {instance.Name} ({instance.ObjId})");
                var scan = await _notifyDriver.ScanBus(instance);

                var curLanguage = (Language)_settingsCache.GetByKey("language").ValueInt;
                var languageCode = ConvertLanguageEnum(curLanguage);

                if (scan != null)
                {
                    foreach (var s in scan)
                    {
                        TranslateNodesRecursive(s, languageCode);
                        s.This2ParentNodeInstance = instance.ObjId;
                    }

                    return await SaveAndReloadNodeInstances(scan);
                }
                
                return new List<NodeInstance>();
                
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not scan driver");
                throw;
            }
        }


        [HttpGet]
        [Route("logicNodeInstances")]
        [Authorize(Policy = Role.AdminRole)]
        public ICollection<NodeInstance2RulePage> GetNodeInstances()
        {
            var ret = _logicCacheFacade.LogicNodeInstanceCache.All();

            return ret;
        }
    }
}
