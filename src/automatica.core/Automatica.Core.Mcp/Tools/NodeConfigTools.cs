using System.ComponentModel;
using System.Globalization;
using System.Net;
using System.Text.Json;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Internals.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Automatica.Core.Mcp.Tools
{
    /// <summary>
    /// Changes the configuration properties of a node (driver, device or data point: IP address, port, poll interval, register, ...)
    /// like the property editor of the configuration page, and restarts the driver the node belongs to, as the web UI does for a
    /// changed property. Administrators, or the API key with <c>mcpAllowWrite</c>.
    /// </summary>
    [McpServerToolType]
    public class NodeConfigTools
    {
        /// <summary>Property types that hold a plain value; the others (scan, import, controls, users, ...) only make sense in the web UI.</summary>
        private static readonly HashSet<PropertyTemplateType> EditableTypes = new()
        {
            PropertyTemplateType.Text, PropertyTemplateType.Integer, PropertyTemplateType.Numeric, PropertyTemplateType.DropDown,
            PropertyTemplateType.Bool, PropertyTemplateType.Ip, PropertyTemplateType.Baudrate, PropertyTemplateType.Parity,
            PropertyTemplateType.Databits, PropertyTemplateType.Stopbits, PropertyTemplateType.Enum, PropertyTemplateType.Range,
            PropertyTemplateType.Color, PropertyTemplateType.UsbPort, PropertyTemplateType.Long, PropertyTemplateType.Password,
            PropertyTemplateType.Time
        };

        private readonly INodeInstanceCache _nodeInstanceCache;
        private readonly ICoreServer _coreServer;
        private readonly IConfiguration _configuration;
        private readonly McpAccess _access;
        private readonly ILogger<NodeConfigTools> _logger;

        public NodeConfigTools(INodeInstanceCache nodeInstanceCache, ICoreServer coreServer, IConfiguration configuration, McpAccess access,
            ILogger<NodeConfigTools> logger)
        {
            _nodeInstanceCache = nodeInstanceCache;
            _coreServer = coreServer;
            _configuration = configuration;
            _access = access;
            _logger = logger;
        }

        /// <summary>The driver restart runs in the background like in the web UI; tests wait for it.</summary>
        public Task LastDriverRestart { get; private set; } = Task.CompletedTask;

        [McpServerTool(Name = "update_node_settings", Title = "Change node settings", Destructive = false, Idempotent = true, OpenWorld = true)]
        [Description("Changes configuration properties of a node (driver, device or data point), e.g. the IP address, port or poll interval of a " +
                     "driver or the register of a data point. Properties are set by key as get_datapoint lists them. The driver the node " +
                     "belongs to restarts afterwards (it is offline for a moment); check get_current_values or read_log afterwards.")]
        public async Task<NodeSettingsChanged> UpdateNodeSettings(
            [Description("Node id (driver, device or data point), see browse_nodes or get_datapoint.")] Guid nodeId,
            [Description("Property values by key, e.g. {\"modbus-master-ip\": \"192.168.8.247\", \"modbus-master-port\": 502}.")] Dictionary<string, JsonElement> properties = null,
            [Description("New name of the node, empty to keep it.")] string name = null)
        {
            if (!_access.CanEditLogics)
            {
                throw new McpException("Changing the configuration is disabled for the MCP API key. Enable the setting 'mcpAllowWrite' or log in as an administrator.");
            }
            if ((properties == null || properties.Count == 0) && string.IsNullOrWhiteSpace(name))
            {
                throw new McpException("Nothing to change: pass properties and/or name");
            }

            var cached = _nodeInstanceCache.Get(nodeId);
            if (cached == null || cached.IsDeleted || !_access.CanSee(cached.This2UserGroup))
            {
                throw new McpException($"Node {nodeId} not found");
            }

            var changes = new List<PropertyChange>();
            await using (var db = new AutomaticaContext(_configuration))
            {
                var node = await db.NodeInstances
                               .Include(a => a.PropertyInstance).ThenInclude(a => a.This2PropertyTemplateNavigation).ThenInclude(a => a.This2PropertyTypeNavigation)
                               .SingleOrDefaultAsync(a => a.ObjId == nodeId && !a.IsDeleted)
                           ?? throw new McpException($"Node {nodeId} not found");

                foreach (var (key, json) in properties ?? new Dictionary<string, JsonElement>())
                {
                    var property = node.PropertyInstance.FirstOrDefault(a => !a.IsDeleted && a.This2PropertyTemplateNavigation?.Key == key);
                    var editable = node.PropertyInstance.Where(a => !a.IsDeleted && IsEditable(a)).Select(a => a.This2PropertyTemplateNavigation.Key).OrderBy(a => a);
                    if (property == null)
                    {
                        throw new McpException($"'{key}' is not a property of '{node.Name}'. Properties: {string.Join(", ", editable)}");
                    }
                    if (!IsEditable(property))
                    {
                        throw new McpException($"'{key}' of '{node.Name}' can only be changed in the web UI");
                    }

                    var oldValue = Display(property);
                    property.Value = Convert(property, key, json);
                    property.ModifiedAt = DateTimeOffset.Now;
                    changes.Add(new PropertyChange(key, oldValue, Display(property)));
                }

                if (!string.IsNullOrWhiteSpace(name))
                {
                    node.Name = name.Trim();
                }
                node.ModifiedAt = DateTimeOffset.Now;
                await db.SaveChangesAsync();

                // caches after the commit, like NodeInstanceV2Controller.UpdateNode
                _nodeInstanceCache.GetSingle(nodeId, db);
            }

            _logger.LogInformation("{Caller} changed node {Node} ({Id}): {Changes}", _access.CallerName, cached.Name, nodeId,
                string.Join(", ", changes.Select(a => $"{a.Key} {a.OldValue} -> {a.NewValue}")));

            var restart = changes.Count > 0;
            if (restart)
            {
                var updated = _nodeInstanceCache.Get(nodeId);
                LastDriverRestart = Task.Run(async () =>
                {
                    try
                    {
                        await _coreServer.StartStopDriver(updated);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Could not restart the driver of {Node}", updated?.Name);
                    }
                });
            }

            return new NodeSettingsChanged(nodeId, _nodeInstanceCache.Get(nodeId)?.Name ?? cached.Name, changes, restart);
        }

        private static bool IsEditable(PropertyInstance property)
        {
            var template = property.This2PropertyTemplateNavigation;
            return template != null && template.IsVisible && !template.IsReadonly && EditableTypes.Contains(property.PropertyType);
        }

        private static object Display(PropertyInstance property)
        {
            if (property.PropertyType == PropertyTemplateType.Password)
            {
                return string.IsNullOrEmpty(property.ValueString) ? null : "***";
            }
            return McpValues.ToJsonValue(property.Value);
        }

        /// <summary>Strict conversion: the value setter of <see cref="PropertyInstance"/> silently stores 0 for a value it cannot parse.</summary>
        private static object Convert(PropertyInstance property, string key, JsonElement json)
        {
            var type = property.PropertyType;
            var text = json.ValueKind switch
            {
                JsonValueKind.String => json.GetString(),
                JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => json.GetRawText(),
                _ => throw new McpException($"'{key}' needs a single value, got {json.ValueKind}")
            };

            McpException Invalid() => new($"'{text}' is not a valid value for '{key}' ({type})");

            switch (type)
            {
                case PropertyTemplateType.Integer:
                case PropertyTemplateType.DropDown:
                case PropertyTemplateType.Baudrate:
                case PropertyTemplateType.Databits:
                case PropertyTemplateType.Enum:
                case PropertyTemplateType.Range:
                    return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : throw Invalid();
                case PropertyTemplateType.Long:
                    return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l) ? l : throw Invalid();
                case PropertyTemplateType.Numeric:
                case PropertyTemplateType.Stopbits:
                    return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : throw Invalid();
                case PropertyTemplateType.Bool:
                    return bool.TryParse(text, out var b) ? b : throw Invalid();
                case PropertyTemplateType.Ip:
                    var host = text?.Trim();
                    return !string.IsNullOrEmpty(host) && (IPAddress.TryParse(host, out _) || Uri.CheckHostName(host) == UriHostNameType.Dns) ? host : throw Invalid();
                default:
                    return text ?? "";
            }
        }
    }

    public record PropertyChange(string Key, object OldValue, object NewValue);

    public record NodeSettingsChanged(Guid Id, string Name, IList<PropertyChange> Changes, bool DriverRestarted);
}
