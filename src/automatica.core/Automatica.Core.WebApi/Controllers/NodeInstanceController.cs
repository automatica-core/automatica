using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.LinqExtensions;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals;
using Automatica.Core.Internals.Cache.Driver;
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

    [Route("nodeInstances")]
    public class NodeInstanceController : BaseController
    {
        private readonly INotifyDriver _notifyDriver;
        private readonly INodeInstanceCache _nodeInstanceCache;

        public NodeInstanceController(AutomaticaContext db, INotifyDriver notifyDriver, INodeInstanceCache nodeInstanceCache)
            : base(db)
        {
            _notifyDriver = notifyDriver;
            _nodeInstanceCache = nodeInstanceCache;
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

        private async Task<NodeStates> SetNodeInstanceState(NodeInstance node, Guid? parent, Dictionary<Guid, NodeInstance> dbEntries)
        {
            var ret = new NodeStates();
            if (_notifyDriver != null)
            {
                try
                {
                    await _notifyDriver.NotifySave(node);
                }
                catch (Exception e)
                {
                    SystemLogger.Instance.LogError($"Could not notify save {e}");
                }
            }

            var childs = node.InverseThis2ParentNodeInstanceNavigation;
            var props = node.PropertyInstance;

            node.InverseThis2ParentNodeInstanceNavigation = null;
            node.PropertyInstance = null;
            node.IsDeleted = false;

            if (node.Description == null)
            {
                node.Description = "";
            }

            var entityState = EntityState.Added;
            if (!dbEntries.ContainsKey(node.ObjId))
            {
                if (parent.HasValue)
                {
                    node.This2ParentNodeInstance = parent;
                }
                ret.AddedNodeInstances.Add(node);
            }
            else
            {
                ret.UpdatedNodeInstances.Add(node);
                entityState = EntityState.Modified;
            }

            foreach (var prop in props)
            {
                prop.This2NodeInstanceNavigation = node;
                prop.This2PropertyTemplateNavigation = null;
                if (entityState == EntityState.Modified)
                {
                    ret.UpdatedPropertyInstances.Add(prop);
                }
                else
                {
                    ret.AddedPropertyInstances.Add(prop);
                }
            }

            foreach (var child in childs)
            {
                var state = await SetNodeInstanceState(child, node.ObjId, dbEntries);

                ret.AddedNodeInstances.AddRange(state.AddedNodeInstances);
                ret.UpdatedNodeInstances.AddRange(state.UpdatedNodeInstances);
                ret.UpdatedPropertyInstances.AddRange(state.UpdatedPropertyInstances);
                ret.AddedPropertyInstances.AddRange(state.AddedPropertyInstances);
            }
            return ret;
        }

        [HttpGet]
        [Route("linkable")]
        [Authorize(Policy = Role.AdminRole)]
        public IEnumerable<NodeInstance> GetLinkableNodes()
        {
            return DbContext.NodeInstances.AsNoTracking().Include(a => a.This2NodeTemplateNavigation).Where(a =>
                a.This2NodeTemplateNavigation.ProvidesInterface2InterfaceType == GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value)).Where(a => IsUserInGroup(a.This2UserGroup)).ToList();
        }

        [HttpPost]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<IEnumerable<NodeInstance>> Save([FromBody]List<NodeInstance> nodeInstances)
        {
            SystemLogger.Instance.LogDebug($"Begin NodeInstance save...");
            var transaction = DbContext.Database.BeginTransaction();
            try
            {
                SystemLogger.Instance.LogDebug("Start checking node instances...");
                var dbEntries = DbContext.NodeInstances.AsNoTracking();
                foreach (var node in nodeInstances)
                {
                    node.This2ParentNodeInstance = null; //set root parent always to null -> if we are a slave server the parentid could be set
                    var state = await SetNodeInstanceState(node, null, dbEntries.ToDictionary(a => a.ObjId, a => a));
                    SystemLogger.Instance.LogDebug($"NodeInstance State Added Nodes {state.AddedNodeInstances.Count}, UpdatedNodes {state.UpdatedNodeInstances.Count}. AddedProperties: {state.AddedPropertyInstances.Count}, UpdatedProperties: {state.UpdatedPropertyInstances.Count}");
                    SystemLogger.Instance.LogDebug("Start add/updating entities...done");
                    DbContext.NodeInstances.AddRange(state.AddedNodeInstances);
                    DbContext.NodeInstances.UpdateRange(state.UpdatedNodeInstances);
                    DbContext.PropertyInstances.AddRange(state.AddedPropertyInstances);
                    DbContext.PropertyInstances.UpdateRange(state.UpdatedPropertyInstances);

                    SystemLogger.Instance.LogDebug("Start add/updating entities...done");

                }
                SystemLogger.Instance.LogDebug("Start checking node instances...done");
                SystemLogger.Instance.LogDebug("Start checking deleted items...");

                var flatList = nodeInstances.Flatten(a => a.InverseThis2ParentNodeInstanceNavigation).ToList();

                var removedNodes = (from c in DbContext.NodeInstances
                    where !(from o in flatList select o.ObjId).Contains(c.ObjId)
                    select c).ToList();

                if (_notifyDriver != null)
                {
                    foreach (var removed in removedNodes)
                    {
                        try
                        {
                            await _notifyDriver.NotifyDeleted(removed);
                        }
                        catch (Exception e)
                        {
                            SystemLogger.Instance.LogError(e, "Could not call notify save");
                        }
                    }
                }

                SystemLogger.Instance.LogDebug($"Found {removedNodes.Count} items to delete...");
                DbContext.RemoveRange(removedNodes);
                SystemLogger.Instance.LogDebug("Start checking deleted items...done");

                var settings = DbContext.Settings.SingleOrDefault(a => a.ValueKey == ServerInfo.DbConfigVersionKey);
                if (settings != null)
                {
                    settings.ValueInt++;
                    ServerInfo.DbConfigVersion = settings.ValueInt.GetValueOrDefault();

                    DbContext.Settings.Update(settings);
                }
                SystemLogger.Instance.LogDebug("Save and Commit changes");
                DbContext.SaveChanges();
                transaction.Commit();
                _nodeInstanceCache.Clear();
                SystemLogger.Instance.LogDebug("Save and Commit changes...done");
            }
            catch (Exception e)
            {
                transaction.Rollback();
                SystemLogger.Instance.LogError(e, "Could not save nodeInstances", e);
            }

            SystemLogger.Instance.LogDebug($"Begin NodeInstance save...done");
            return Get();
        }

        [HttpPost]
        [Route("scan")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<IList<NodeInstance>> Scan([FromBody] NodeInstance instance)
        {
            try
            {
                SystemLogger.Instance.LogInformation($"Start scan for {instance.Name} ({instance.ObjId})");
                return await _notifyDriver.ScanBus(instance);
            }
            catch (Exception e)
            {
                SystemLogger.Instance.LogError(e, "Could not scan driver");
                throw;
            }
        }


        [HttpPost]
        [Route("import")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<IList<NodeInstance>> Import([FromBody] ImportData instance)
        {
            return await _notifyDriver.Import(instance.Node, instance.FileName);
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

    }
}
