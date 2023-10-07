﻿using Automatica.Core.EF.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

namespace Automatica.Core.WebApi.Controllers
{
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
            _logger = logger;
        }

        private async Task<EntityState> AddOrUpdateNodeInstance(NodeInstance node)
        {
            var childs = node.InverseThis2ParentNodeInstanceNavigation ?? new List<NodeInstance>();
            var props = node.PropertyInstance;

            node.InverseThis2ParentNodeInstanceNavigation = null;
            node.PropertyInstance = null;
            node.IsDeleted = false;

            if (node.Description == null)
            {
                node.Description = "";
            }

            var entityState = EntityState.Unchanged;
            var entity = DbContext.NodeInstances.AsNoTracking().SingleOrDefault(a => a.ObjId == node.ObjId);
            if (entity != null)
            {
                DbContext.Entry(entity).State = EntityState.Detached;
                DbContext.Entry(node).State = EntityState.Modified;

                DbContext.Update(node);
                entityState = EntityState.Modified;

                if (entity.Trending != node.Trending)
                {
                    if (node.Trending)
                    {
                        await _recorderContext.AddRecording(node.ObjId);
                    }
                    else
                    {
                        await _recorderContext.RemoveRecording(node.ObjId);
                    }
                }

            }
            else
            {
                DbContext.Entry(node).State = EntityState.Added;
                await DbContext.AddAsync(node);
                entityState = EntityState.Added;
            }

            foreach (var prop in props)
            {
                prop.This2NodeInstanceNavigation = node;
                prop.This2PropertyTemplateNavigation = null;
                if (entityState == EntityState.Modified)
                {
                    DbContext.Update(prop);
                }
                else
                {
                    await DbContext.AddAsync(prop);
                }
            }

            foreach (var child in childs)
            {
                await AddOrUpdateNodeInstance(child);
            }

            return entityState;

        }


        [HttpPost]
        [Authorize(Roles = Role.AdminRole)]
        [Route("create/{locale}/{parentNodeInstance}/{nodeTemplate}")]
        public async Task<NodeInstance> CreateFromTemplate(string locale, Guid parentNodeInstance, Guid nodeTemplate)
        {
            var instance = _nodeInstanceService.CreateNodeInstance(locale, _nodeInstanceCache.Get(parentNodeInstance), nodeTemplate);
            return await AddNode(instance);
        }

        [HttpPost]
        [Authorize(Roles = Role.AdminRole)]
        [Route("copy/{nodeInstance}/{targetNodeInstance}")]
        public async Task<NodeInstance> Copy(Guid nodeInstance, Guid targetNodeInstance)
        {
            await using var context = new AutomaticaContext(_config);
            var instance = _nodeInstanceCache.GetSingle(nodeInstance, context);

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

            var newNode = await AddNodeInternal(node);


            if (node.This2NodeTemplateNavigation.IsAdapterInterface.HasValue &&
                node.This2NodeTemplateNavigation.IsAdapterInterface.Value &&
                newNode.node.This2ParentNodeInstance.HasValue)
            {
                newNode.node.This2ParentNodeInstanceNavigation =
                    _nodeInstanceCache.Get(newNode.node.This2ParentNodeInstance.Value);
                return newNode.node;
            }

            async void ReloadAndForget() => await ReloadDriver(node, newNode.entityState);
            ReloadAndForget();


            _logger.LogDebug($"Add Node...done");

            await DbContext.SaveChangesAsync();

            return newNode.node;
        }

        private async Task<(NodeInstance node, EntityState entityState)> AddNodeInternal(NodeInstance node)
        {
            var transaction = await DbContext.Database.BeginTransactionAsync();
            try
            {
                var entityState = await AddOrUpdateNodeInstance(node);
                await DbContext.SaveChangesAsync();
                await transaction.CommitAsync();

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

                return (_nodeInstanceCache.Get(node.ObjId), entityState);
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                _logger.LogError(e, $"Could not {nameof(AddNode)} {nameof(NodeInstance)}", e);
                throw;
            }
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
            var rootNode = _nodeInstanceCache.GetDriverNodeInstanceFromChild(node);
            await _notifyDriver.NotifyAdd(node);
            if (rootNode == null)
            {
                return;
            }
            var driver = _driverNodeStore.GetDriver(rootNode.ObjId);
            if (driver == null)
            {
                _logger.LogWarning(
                    $"Could not hot-reload driver, seems that the driver wasn't loaded at the moment");

                return;
            }
            await _coreServer.StopDriver(driver);
            await _coreServer.InitializeAndStartDriver(rootNode,
                _templateCache.Get(rootNode.This2NodeTemplate.Value));
        }

        [Route("delete")]
        [HttpPost]
        public async Task DeleteNode([FromBody]NodeInstance node)
        {
            var transaction = await DbContext.Database.BeginTransactionAsync();
            var existingNode = DbContext.NodeInstances.AsNoTracking().Include(a => a.This2NodeTemplateNavigation)
                .SingleOrDefault(a => a.ObjId == node.ObjId);
            try
            {
                if (existingNode == null)
                {
                    return;
                }
                if (existingNode != null)
                {
                    DbContext.Entry(existingNode).State = EntityState.Deleted;
                }

                await DbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                try
                {
                    if (existingNode.This2NodeTemplateNavigation.IsAdapterInterface.HasValue &&
                        existingNode.This2NodeTemplateNavigation.IsAdapterInterface.Value)
                    {
                        return;
                    }

                    var rootNode = _nodeInstanceCache.GetDriverNodeInstanceFromChild(node);

                    if (rootNode.ObjId == node.ObjId)
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
                catch (Exception e)
                {
                    _logger.LogError(e, $"Error stopping driver {node.Name}...");
                }

            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                _logger.LogError(e, $"Could not {nameof(DeleteNode)} {nameof(NodeInstance)}", e);
                _nodeInstanceCache.Clear();
                throw;
            }
            finally
            {

                _nodeInstanceCache.Remove(existingNode);
            }
        }

        [Route("reload")]
        [HttpPost]
        public async Task ReInit()
        {
            _nodeInstanceCache.Clear();
            _templateCache.Clear();
            _driverNodeStore.Clear();

            await _coreServer.ReInit();
        }

        [Route("update")]
        [HttpPost]
        public async Task<NodeInstance> UpdateNode([FromBody]NodeInstance node)
        {
            var transaction = await DbContext.Database.BeginTransactionAsync();
            try
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
                

                var entityState = await AddOrUpdateNodeInstance(node);

                await DbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                _nodeInstanceCache.GetSingle(node.ObjId, DbContext);

                if (reloadServer)
                {
                    await _coreServer.ReInit();
                }
                else
                {
                    async void ReloadAndForget() => await ReloadDriver(node, entityState);
                    ReloadAndForget();
                }

                return _nodeInstanceCache.Get(node.ObjId);
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                _logger.LogError(e, $"Could not {nameof(UpdateNode)} {nameof(NodeInstance)}", e);
                throw;
            }
        }

        [HttpPost]
        [Route("import")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<IList<NodeInstance>> Import([FromBody] ImportConfig config)
        {
            var data = await _notifyDriver.Import(config);

            if (System.IO.File.Exists(config.FileName))
            {
                System.IO.File.Delete(config.FileName);
            }

            if (data != null)
            {
                return await SaveAndReloadNodeInstances(data);
            }
            return new List<NodeInstance>();
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
                async void ReloadAndForget() => await ReloadDriver(savedNodesData.First().node, savedNodesData.First().entityState);
                ReloadAndForget();
            }

            return savedNodes;
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

                if (scan != null)
                {
                    foreach (var s in scan)
                    {
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
    }
}
