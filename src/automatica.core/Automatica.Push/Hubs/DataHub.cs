using System;
using System.Text.Json;
using System.Threading.Tasks;
using Automatica.Core.Base.Calendar;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Templates;
using Automatica.Core.Control.Base;
using Automatica.Core.Control.Cache;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Internals.Cache.Logic;
using Automatica.Core.Model.Models.User;
using Automatica.Push.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Automatica.Push.Hubs
{
    [Authorize]
    public class DataHub : Hub
    {
        private readonly IDispatcher _dispatcher;
        private readonly INotifyDriver _notify;
        private readonly INodeInstanceCache _nodeInstanceCache;
        private readonly ILogicInstanceCache _logicInstanceCache;
        private readonly ILogicInterfaceInstanceCache _logicInterfaceInstanceCache;
        private readonly IControlCache _controlCache;
        private readonly ILogger<DataHub> _logger;

        public DataHub(IDispatcher dispatcher, INotifyDriver notify, INodeInstanceCache nodeInstanceCache, ILogicInstanceCache logicInstanceCache, ILogicInterfaceInstanceCache logicInterfaceInstanceCache, IControlCache controlCache, ILogger<DataHub> logger)
        {
            _controlCache = controlCache;
            _logger = logger;
            _dispatcher = dispatcher;
            _notify = notify;
            _nodeInstanceCache = nodeInstanceCache;
            _logicInstanceCache = logicInstanceCache;
            _logicInterfaceInstanceCache = logicInterfaceInstanceCache;
        }

        /// <summary>
        /// Every value of every node (config and logic editor), admin only.
        /// </summary>
        [Authorize(Policy = Role.AdminRole)]
        public async Task SubscribeAll()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "All");
        }
        public async Task UnsubscribeAll()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "All");
        }

        /// <summary>
        /// Values of one node or logic interface, only if the user may see it (user group).
        /// </summary>
        public async Task Subscribe(string name)
        {
            if (!CanSubscribe(name))
            {
                throw new HubException($"Not allowed to subscribe to {name}");
            }
            await Groups.AddToGroupAsync(Context.ConnectionId, name);
        }

        private bool CanSubscribe(string name)
        {
            if (HubAccess.IsAdmin(Context.User))
            {
                return true;
            }

            // only node or logic interface ids, never one of the special groups (e.g. "All")
            if (!Guid.TryParse(name, out var id))
            {
                return false;
            }

            var node = _nodeInstanceCache.Get(id);
            if (node != null)
            {
                return HubAccess.CanSee(Context.User, node.This2UserGroup);
            }

            var logicInterface = _logicInterfaceInstanceCache.Get(id);
            if (logicInterface != null)
            {
                return HubAccess.CanSee(Context.User, _logicInstanceCache.Get(logicInterface.This2RuleInstance)?.This2UserGroup);
            }

            // nothing is sent to unknown ids
            return true;
        }

        private static string SanitizeForLog(string value)
        {
            if (value == null)
            {
                return "<null>";
            }

            var cleaned = new string(Array.ConvertAll(value.ToCharArray(), c => char.IsControl(c) ? '_' : c));
            return cleaned.Length > 64 ? cleaned.Substring(0, 64) + "..." : cleaned;
        }

        [Authorize(Policy = Role.AdminRole)]
        public async Task EnableLearnMode(Guid nodeInstanceId)
        {
            var nodeInstance = _nodeInstanceCache.Get(nodeInstanceId) ?? throw new HubException($"Node {nodeInstanceId} not found");
            await _notify.EnableLearnMode(nodeInstance);
            await Subscribe(nodeInstanceId.ToString());
        }
        [Authorize(Policy = Role.AdminRole)]
        public async Task DisableLearnMode(Guid nodeInstanceId)
        {

            var nodeInstance = _nodeInstanceCache.Get(nodeInstanceId) ?? throw new HubException($"Node {nodeInstanceId} not found");
            await _notify.DisableLearnMode(nodeInstance);
            await Unsubscribe(nodeInstanceId.ToString());
        }

        public async Task Unsubscribe(string name)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, name);
        }


        /// <summary>
        /// Writes a data point or logic input. Needs a role (viewer, visu or admin); non-admins only write data points
        /// marked as writeable in their user groups.
        /// </summary>
        [Authorize(Policy = Role.ViewerRole)]
        public void SetValue(Guid instanceId, JsonElement value)
        {
            object convertedValue = null;

            switch (value.ValueKind)
            {
                case JsonValueKind.Undefined:
                    break;
                case JsonValueKind.Object:
                    try
                    {
                        var dispatchValue = JsonConvert.DeserializeObject<DispatchValue>(value.GetRawText());
                        convertedValue = dispatchValue.Value;
                        break;
                    }
                    catch
                    {
                        //ignore
                    }
                    throw new NotImplementedException();
                case JsonValueKind.Array:
                    throw new NotImplementedException();
                case JsonValueKind.String:
                    convertedValue = value.GetString();
                    break;
                case JsonValueKind.Number:
                    convertedValue = value.GetDouble();
                    break;
                case JsonValueKind.True:
                    convertedValue = true;
                    break;
                case JsonValueKind.False:
                    convertedValue = false;
                    break;
                case JsonValueKind.Null:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var nodeInstanceValue = _nodeInstanceCache.Get(instanceId);

            // same checks as the MCP write_value tool: user group and writeable flag (admins may write everything they can configure)
            if (nodeInstanceValue != null)
            {
                if (nodeInstanceValue.IsDeleted || !HubAccess.CanSee(Context.User, nodeInstanceValue.This2UserGroup))
                {
                    throw new HubException($"Data point {instanceId} not found");
                }

                if (!nodeInstanceValue.IsWriteable && !HubAccess.IsAdmin(Context.User))
                {
                    _logger.LogWarning($"Refused write of user {SanitizeForLog(Context.User?.Identity?.Name)} to data point {instanceId}, which is not writeable");
                    throw new HubException($"Data point {instanceId} is not writeable");
                }

                var dispatchable = new DispatchableInstance(DispatchableType.Visualization, $"Web", instanceId,
                    DispatchableSource.Visualization, nodeInstanceValue.IsRemanent);
                _dispatcher.DispatchValue(dispatchable,
                    new DispatchValue(instanceId, DispatchableType.Visualization, convertedValue, DateTimeHelper.ProviderInstance.GetLocalNow().DateTime, DispatchValueSource.User));
            }
            else
            {
                var logicInterface = _logicInterfaceInstanceCache.Get(instanceId);
                if (logicInterface == null ||
                    !HubAccess.CanSee(Context.User, _logicInstanceCache.Get(logicInterface.This2RuleInstance)?.This2UserGroup))
                {
                    throw new HubException($"Data point or logic interface {instanceId} not found");
                }

                // inputs (and parameters) of a logic, never its outputs
                if (logicInterface.This2RuleInterfaceTemplateNavigation?.This2RuleInterfaceDirection == (long)LogicInterfaceDirection.Output &&
                    !HubAccess.IsAdmin(Context.User))
                {
                    throw new HubException("Logic outputs cannot be written");
                }

                // security logics (alarms, access controller) are armed and acknowledged through the authorised and audited
                // webapi/security-visu, a visualization user must not disarm them by writing an input
                if (_controlCache.Get(logicInterface.This2RuleInstance) is ISecurityControl && !HubAccess.IsAdmin(Context.User))
                {
                    _logger.LogWarning($"Refused write of user {SanitizeForLog(Context.User?.Identity?.Name)} to an input of security logic {logicInterface.This2RuleInstance}");
                    throw new HubException("Inputs of security logics cannot be written, use the security commands");
                }

                var dispatchable = new DispatchableInstance(DispatchableType.Visualization, $"Web", instanceId,
                    DispatchableSource.Visualization, false);
                _dispatcher.DispatchValue(dispatchable,
                    new DispatchValue(instanceId, DispatchableType.Visualization, convertedValue, DateTimeHelper.ProviderInstance.GetLocalNow().DateTime, DispatchValueSource.User));
            }
        }
    }
}
