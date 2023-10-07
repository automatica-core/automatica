﻿using Automatica.Core.Base.Tunneling;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Logging;
using P3.Driver.Loxone.Miniserver.Driver;
using P3.Driver.Loxone.Miniserver.Driver.Data.LoxApp;
using P3.Driver.Loxone.Miniserver.Driver.Data.Message;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace P3.Driver.Loxone.Miniserver.DriverFactory
{
    public class LoxoneDriver : DriverNoneAttributeBase
    {
        private LoxoneMiniserverConnection _miniserver;
        private bool _connected = false;
        private LoxoneDriverConnectedNode _connectionNode;

        public LoxApp3Data LoxData => _miniserver.LoxData;
        private bool _tunnelingEnabled;
        private string _ip;
        private int _port;

        public IDictionary<string, List<LoxoneDriverNode>> Nodes { get; } = new ConcurrentDictionary<string, List<LoxoneDriverNode>>();

        public LoxoneDriver(IDriverContext driverContext) : base(driverContext)
        {

        }

        public override async Task<bool> Init(CancellationToken token = default)
        {
            _ip = GetPropertyValueString("ip-address");
            _port = GetPropertyValueInt("port");
            _miniserver = new LoxoneMiniserverConnection(_ip, _port, GetPropertyValueString("user"), GetPropertyValueString("password"), DriverContext.Logger);
            _miniserver.OnConnectionEstablished += _miniserver_OnConnectionEstablished;
            _miniserver.OnConnectionClosed += _miniserver_OnConnectionClosed;
            _miniserver.OnMessage += _miniserver_OnMessage;



            var useTunnel = GetProperty("loxone-use-tunnel").ValueBool;

            if (useTunnel.HasValue && useTunnel.Value)
            {
                _tunnelingEnabled = true;
                DriverContext.Logger.LogInformation($"Using remote connect...");
                await InitRemoteConnect(token).ConfigureAwait(false);
            }

            return await base.Init(token);
        }

        private async Task InitRemoteConnect(CancellationToken token = default)
        {
            try
            {
                var remoteFeatureEnabled =
                    DriverContext.LicenseContract.IsFeatureLicensed("loxone-remote-connection");
                if (remoteFeatureEnabled && _tunnelingEnabled && await DriverContext.TunnelingProvider.IsAvailableAsync(default))
                {
                    string tunnel = await DriverContext.TunnelingProvider.CreateWebTunnelAsync(TunnelingProtocol.Http,
                        DriverContext.NodeInstance.Name, "loxone", _ip, _port, null, null, token);

                    DriverContext.Logger.LogInformation($"Tunnel created {tunnel}");
                }
                else
                {
                    DriverContext.Logger.LogInformation($"Tunnel is disabled or not licensed...");
                }
            }

            catch (Exception e)
            {
                DriverContext.Logger.LogError($"Could not start tunnel {e}");
            }

        }

        public async Task<bool> WriteValue(string uuid, object value)
        {
            try
            {
                var res = await _miniserver.WriteValue(uuid, value);
                if(res.Data.Code == 200)
                {
                    return true;
                }
                return false;
            }
            catch(Exception e)
            {
                DriverContext.Logger.LogError(e, "Could not write value");
                return false;
            }
        }

        private void _miniserver_OnMessage(object sender, Driver.EventArgs.OnEventTableMessageEventArgs e)
        {
            if(e.Message is EventTableOfValueStates values)
            {
                foreach(var x in values.Values)
                {
                    if(Nodes.ContainsKey(x.Key))
                    {
                        Nodes[x.Key].ForEach(a =>
                        {
                            a.ValueChanged(x.Value);
                        });
                    }
                }
            }
        }

        public override Task<bool> Stop(CancellationToken token = default)
        {
            _miniserver.Close();
            return base.Stop(token);
        }

        public override async Task<IList<NodeInstance>> Scan(CancellationToken token = default)
        {
            var ret = new List<NodeInstance>();

            var data = await _miniserver.LoadLoxAppData();
            var roomNodeInstanceDic = new Dictionary<string, NodeInstance>();

            foreach(var control in data.Controls)
            {
                NodeTemplate nodeTemplate = null;
                string state = null;

                if (!roomNodeInstanceDic.ContainsKey(control.Value.RoomUuid))
                {
                    var roomNode = DriverContext.NodeTemplateFactory.CreateNodeInstance(LoxoneDriverFactory.FolderNode);
                    roomNode.Name = data.Rooms[control.Value.RoomUuid].Name;
                    roomNodeInstanceDic.Add(control.Value.RoomUuid, roomNode);
                    ret.Add(roomNode);
                }

                switch(control.Value.Type.ToLower())
                {
                    case "switch":
                        {
                            nodeTemplate = DriverContext.NodeTemplateFactory.GetNodeTemplateById(LoxoneDriverFactory.SwitchNodeGuid);
                            state = "active";
                            break;
                        }
                    case "dimmer":
                    case "eibdimmer":
                        {
                            nodeTemplate = DriverContext.NodeTemplateFactory.GetNodeTemplateById(LoxoneDriverFactory.DimmerNode);
                            state = "position";
                            break;
                        }
                    case "infoonlydigital":
                        {
                            nodeTemplate = DriverContext.NodeTemplateFactory.GetNodeTemplateById(LoxoneDriverFactory.InfoOnlyDigitalNode);
                            state = "active";
                            break;
                        }
                    case "infoonlyanalog":
                        {
                            nodeTemplate = DriverContext.NodeTemplateFactory.GetNodeTemplateById(LoxoneDriverFactory.InfoOnlyAnlogNode);
                            state = "value";
                            break;
                        }
                    case "pushbutton":
                        {
                            nodeTemplate = DriverContext.NodeTemplateFactory.GetNodeTemplateById(LoxoneDriverFactory.PushButtonNode);
                            state = "active";
                            break;
                        }
                    default:
                        continue;
                }

                var nodeInstance = DriverContext.NodeTemplateFactory.CreateNodeInstance(nodeTemplate);

                nodeInstance.This2ParentNodeInstance = roomNodeInstanceDic[control.Value.RoomUuid].ObjId;

                var uuidProperty = nodeInstance.GetProperty("uuid");
                uuidProperty.Value = control.Key;
                var stateProperty = nodeInstance.GetProperty("state");
                stateProperty.Value = state;

                nodeInstance.Name = control.Value.Name;

                roomNodeInstanceDic[control.Value.RoomUuid].InverseThis2ParentNodeInstanceNavigation.Add(nodeInstance);

            }
            return ret;
        }

        private void _miniserver_OnConnectionClosed(object sender, EventArgs e)
        {
            _connected = true;
            _connectionNode?.StateChanged(_connected);
        }

        private void _miniserver_OnConnectionEstablished(object sender, EventArgs e)
        {
            _connected = true;
            _connectionNode?.StateChanged(_connected);
        }

        public override async Task<bool> Start(CancellationToken token = default)
        {
            _connected = await _miniserver.Connect();
            _connectionNode?.StateChanged(_connected);

            if(_connected)
            {
                return await base.Start(token);
            }
            return false;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            if (ctx.NodeInstance.This2NodeTemplateNavigation.Key == "loxone-connected")
            {
                _connectionNode = new LoxoneDriverConnectedNode(ctx);
                return _connectionNode;
            }

            if (ctx.NodeInstance.This2NodeTemplateNavigation.Key == "loxone-folder")
            {
                return new LoxoneFolderNode(ctx, this);
            }

            return new LoxoneDriverNode(ctx, this);
        }
    }
}
