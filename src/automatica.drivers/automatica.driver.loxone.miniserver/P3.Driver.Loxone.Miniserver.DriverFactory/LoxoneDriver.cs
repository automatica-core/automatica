using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Logging;
using P3.Driver.Loxone.Miniserver.Driver;
using P3.Driver.Loxone.Miniserver.Driver.Data.LoxApp;
using P3.Driver.Loxone.Miniserver.Driver.Data.Message;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace P3.Driver.Loxone.Miniserver.DriverFactory
{
    public class LoxoneDriver : DriverBase
    {
        private LoxoneMiniserverConnection _miniserver;
        private bool _connected = false;
        private LoxoneDriverConnectedNode _connectionNode;

        public LoxApp3Data LoxData => _miniserver.LoxData;

        public Dictionary<string, List<LoxoneDriverNode>> Nodes { get; } = new Dictionary<string, List<LoxoneDriverNode>>();

        public LoxoneDriver(IDriverContext driverContext) : base(driverContext)
        {

        }

        public override bool Init()
        {
            _miniserver = new LoxoneMiniserverConnection(GetPropertyValueString("ip-address"), GetPropertyValueInt("port"), GetPropertyValueString("user"), GetPropertyValueString("password"), DriverContext.Logger);
            _miniserver.OnConnectionEstablished += _miniserver_OnConnectionEstablished;
            _miniserver.OnConnectionClosed += _miniserver_OnConnectionClosed;
            _miniserver.OnMessage += _miniserver_OnMessage;
            return base.Init();
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

        public override Task<bool> Stop()
        {
            _miniserver.Close();
            return base.Stop();
        }

        public override async Task<IList<NodeInstance>> Scan()
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

        public override async Task<bool> Start()
        {
            _connected = await _miniserver.Connect();
            _connectionNode?.StateChanged(_connected);

            if(_connected)
            {
                return await base.Start();
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
