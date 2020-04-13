using Automatica.Core.EF.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
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

        public NodeInstanceV2Controller(
            AutomaticaContext dbContext, 
            INodeInstanceCache nodeInstanceCache, 
            INotifyDriver notifyDriver, 
            ICoreServer coreServer, 
            INodeTemplateCache templateCache,
            IDriverNodesStore driverNodeStore,
            INodeInstanceService nodeInstanceService) : base(dbContext)
        {
            _nodeInstanceCache = nodeInstanceCache;
            _notifyDriver = notifyDriver;
            _coreServer = coreServer;
            _templateCache = templateCache;
            _driverNodeStore = driverNodeStore;
            _nodeInstanceService = nodeInstanceService;
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
            var instance = _nodeInstanceCache.Get(nodeInstance);

            CopyRec(instance);

            instance.This2ParentNodeInstance = targetNodeInstance;
            var childs = instance.InverseThis2ParentNodeInstanceNavigation;

            await AddNode(instance);

            instance.InverseThis2ParentNodeInstanceNavigation = childs;
            return instance;
        }

        private void CopyRec(NodeInstance nodeInstance)
        {
            nodeInstance.ObjId = Guid.NewGuid();

            foreach (var node in nodeInstance.InverseThis2ParentNodeInstanceNavigation)
            {
                CopyRec(node);
            }
        }

        [HttpPut]
        [Route("add")]
        public async Task<NodeInstance> AddNode([FromBody]NodeInstance node)
        {
            var newNode = await AddNodeInternal(node);
            await ReloadDriver(node, newNode.entityState);

            return newNode.node;
        }

        private async Task<(NodeInstance node, EntityState entityState)> AddNodeInternal(NodeInstance node)
        {
            var transaction = DbContext.Database.BeginTransaction();
            try
            {
                var entityState = await AddOrUpdateNodeInstance(node);
                await DbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                _nodeInstanceCache.Clear();
                return (_nodeInstanceCache.Get(node.ObjId), entityState);
            }
            catch (Exception e)
            {
                transaction.Rollback();
                SystemLogger.Instance.LogError(e, $"Could not {nameof(AddNode)} {nameof(NodeInstance)}", e);
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
                SystemLogger.Instance.LogError(e, "Error hot-load driver..");
            }
        }

        private async Task StopStartDriver(NodeInstance node)
        {
            var rootNode = _nodeInstanceCache.GetDriverNodeInstanceFromChild(node);
            await _notifyDriver.NotifyAdd(node);

            var driver = _driverNodeStore.GetDriver(rootNode.ObjId);
            if (driver == null)
            {
                SystemLogger.Instance.LogWarning(
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
            var transaction = DbContext.Database.BeginTransaction();
            try
            {
                var existingNode = DbContext.NodeInstances.AsNoTracking().SingleOrDefault(a => a.ObjId == node.ObjId);
                if (existingNode != null)
                {
                    DbContext.Entry(existingNode).State = EntityState.Deleted;
                }

                await DbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                try
                {
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
                    SystemLogger.Instance.LogError(e, $"Error stopping driver {node.Name}...");
                }

                _nodeInstanceCache.Clear();
            }
            catch (Exception e)
            {
                transaction.Rollback();
                SystemLogger.Instance.LogError(e, $"Could not {nameof(DeleteNode)} {nameof(NodeInstance)}", e);
                throw;
            }
        }

        [Route("reload")]
        [HttpPost]
        public async Task ReInit()
        {
            await _coreServer.ReInit();
        }

        [Route("update")]
        [HttpPost]
        public async Task<NodeInstance> UpdateNode([FromBody]NodeInstance node)
        {
            var transaction = DbContext.Database.BeginTransaction();
            try
            {
                var entityState = await AddOrUpdateNodeInstance(node);

                await DbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                _nodeInstanceCache.Clear();

                await ReloadDriver(node, entityState);

                return _nodeInstanceCache.Get(node.ObjId);
            }
            catch (Exception e)
            {
                transaction.Rollback();
                SystemLogger.Instance.LogError(e, $"Could not {nameof(DeleteNode)} {nameof(NodeInstance)}", e);
                throw;
            }
        }

        [HttpPost]
        [Route("import")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<IList<NodeInstance>> Import([FromBody] ImportData instance)
        {
            var data = await _notifyDriver.Import(instance.Node, instance.FileName);

            if (System.IO.File.Exists(instance.FileName))
            {
                System.IO.File.Delete(instance.FileName);
            }

            var savedNodes = new List<NodeInstance>();
            var savedNodesData = new List<(NodeInstance node, EntityState entityState)>();
            foreach (var node in data)
            {
                var newNode = await AddNodeInternal(node);
                savedNodes.Add(newNode.node);
                savedNodesData.Add(newNode);
            }

            if (savedNodesData.Any())
            {
                await ReloadDriver(savedNodesData.First().node, savedNodesData.First().entityState);
            }

            return savedNodes;
        }

    }
}
