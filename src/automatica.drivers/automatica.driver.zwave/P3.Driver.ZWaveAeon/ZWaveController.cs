﻿using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using P3.Driver.ZWaveAeon.Channel;

namespace P3.Driver.ZWaveAeon
{
    public class ZWaveController
    {
        private Task<NodeCollection> _getNodes;
        private string _version;
        private uint? _homeID;
        private byte? _nodeID;
        public readonly ZWaveChannel Channel;
        public event EventHandler<ErrorEventArgs> Error;
        public event EventHandler ChannelClosed;
        private bool _isOpened;

        private ZWaveController(ZWaveChannel channel)
        {
            Channel = channel;
        }

        public ZWaveController(ISerialPort port)
            : this(new ZWaveChannel(port))
        {
        }

#if NET || WINDOWS_UWP || NETCOREAPP2_0 || NETSTANDARD2_0
        public ZWaveController(string portName)
            : this(new ZWaveChannel(portName))
        {
        }
#endif

#if WINDOWS_UWP
        public ZWaveController(ushort vendorId, ushort productId)
             : this(new ZWaveChannel(vendorId, productId))
        {
        }
#endif

        protected virtual void OnError(ErrorEventArgs e)
        {
            Error?.Invoke(this, e);
        }

        protected virtual void OnChannelClosed(EventArgs e)
        {
            ChannelClosed?.Invoke(this, e);
        }

        public void Open()
        {
            Channel.NodeEventReceived += Channel_NodeEventReceived;
            Channel.NodeUpdateReceived += Channel_NodeUpdateReceived;
            Channel.Error += Channel_Error;
            Channel.Closed += Channel_Closed;
            Channel.Open();
            _isOpened = true;
        }

        public async Task<bool> SetLearnMode(bool on)
        {
            if (on)
            {
                var responseOn = await Channel.Send(Function.SetLearnMode, new byte[] {0xFF});
                if (responseOn != null)
                {
                    return true;
                }
            }
            else
            {
                var responseOff = await Channel.Send(Function.SetLearnMode, new byte[] { 0x00 });
                if (responseOff != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void Channel_Error(object sender, ErrorEventArgs e)
        {
            OnError(e);
        }

        private void Channel_Closed(object sender, EventArgs e)
        {
            OnChannelClosed(e);
        }

        private async void Channel_NodeEventReceived(object sender, NodeEventArgs e)
        {
            try
            {
                var nodes = await GetNodes();
                var target = nodes[e.NodeID];
                if (target != null)
                {
                    target.HandleEvent(e.Command);
                }
            }
            catch(Exception ex)
            {
                OnError(new ErrorEventArgs(ex));
            }
        }

        private async void Channel_NodeUpdateReceived(object sender, NodeUpdateEventArgs e)
        {
            try
            {
                var nodes = await GetNodes();
                var target = nodes[e.NodeID];
                if (target != null)
                {
                    target.HandleUpdate();
                }
            }
            catch (Exception ex)
            {
                OnError(new ErrorEventArgs(ex));
            }
        }

        public void Close()
        {
            Channel.Error -= Channel_Error;
            Channel.NodeEventReceived -= Channel_NodeEventReceived;
            Channel.NodeUpdateReceived -= Channel_NodeUpdateReceived;
            Channel.Close();
            _isOpened = false;
        }

        public Task<string> GetVersion()
        {
            return GetVersion(CancellationToken.None);
        }

        private void CheckIfOpen()
        {
            if (!_isOpened)
            {
                throw new DeviceNotOpenedException();
            }
        }

        public async Task<string> GetVersion(CancellationToken cancellationToken)
        {
            CheckIfOpen();
            if (_version == null)
            {
                var response = await Channel.Send(Function.GetVersion, cancellationToken);
                var data = response.TakeWhile(element => element != 0).ToArray();
                _version = Encoding.UTF8.GetString(data, 0, data.Length);
            }
            return _version;
        }

        public Task<uint> GetHomeId()
        {
            return GetHomeId(CancellationToken.None);
        }

        public async Task<uint> GetHomeId(CancellationToken cancellationToken)
        {
            CheckIfOpen();
            if (_homeID == null)
            {
                var response = await Channel.Send(Function.MemoryGetId, cancellationToken);
                _homeID = PayloadConverter.ToUInt32(response);
            }
            return _homeID.Value;
        }

        public Task<byte> GetNodeId()
        {
            return GetNodeId(CancellationToken.None);
        }

        public async Task<byte> GetNodeId(CancellationToken cancellationToken)
        {
            CheckIfOpen();
            if (_nodeID == null)
            {
                var response = await Channel.Send(Function.MemoryGetId, cancellationToken);
                _nodeID = response[4];
            }
            return _nodeID.Value;
        }

        public Task<NodeCollection> DiscoverNodes()
        {
            return DiscoverNodes(CancellationToken.None);
        }

        public Task<NodeCollection> DiscoverNodes(CancellationToken cancellationToken)
        {
            CheckIfOpen();
            return _getNodes = Task.Run(async () =>
            {
                var response = await Channel.Send(Function.DiscoveryNodes, cancellationToken);
                var values = response.Skip(3).Take(29).ToArray();

                var nodes = new NodeCollection();
                var bits = new BitArray(values);
                for (byte i = 0; i < bits.Length; i++)
                {
                    if (bits[i])
                    {
                        var node = new Node((byte)(i + 1), this);
                        nodes.Add(node);
                    }
                }
                return nodes;
            });
        }

        public Task<NodeCollection> GetNodes()
        {
            return GetNodes(CancellationToken.None);
        }

        public async Task<NodeCollection> GetNodes(CancellationToken cancellationToken)
        {
            CheckIfOpen();
            return await (_getNodes ?? (_getNodes = DiscoverNodes(cancellationToken)));
        }
    }
}
