using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Automatica.Core.Runtime.Exceptions;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Runtime.Core
{
    internal class NotifyDriverHandler : INotifyDriver
    {
        private readonly IDriverNodesStore _mapper;
        private readonly ILogger<NotifyDriverHandler> _logger;

        public NotifyDriverHandler(IDriverNodesStore mapper, ILogger<NotifyDriverHandler> logger)
        {
            _mapper = mapper;
            _logger = logger;
        }

        public async Task NotifyUpdate(NodeInstance node)
        {
            await ExecuteAction(node, _ => _?.OnSave(node));
        }


        public async Task NotifyAdd(NodeInstance node)
        {
            await ExecuteAction(node, _ => _?.OnSave(node));
        }

        public async Task NotifyDeleted(NodeInstance node)
        {
            await ExecuteAction(node, _ => _?.OnDelete(node));

            var driverNode = _mapper.Get(node.ObjId);

            if (driverNode == null)
            {
                return;
            }
            foreach (var child in driverNode.Children)
            {
                await NotifyDeleted(child.DriverContext.NodeInstance);
            }

            await driverNode.Stop();
            _mapper.Remove(driverNode);
        }

        public Task<IList<NodeInstance>> ScanBus(NodeInstance node)
        {
            return ExecuteAction(node, _ => _?.Scan());
        }

        public Task<IList<NodeInstance>> CustomAction(NodeInstance node, string actionName)
        {
            return ExecuteAction(node, _ => _?.CustomAction(actionName));
        }

        public Task<bool> EnableLearnMode(NodeInstance node)
        {
            return ExecuteAction(node, _ => _?.EnableLearnMode());
        }

        public Task<bool> DisableLearnMode(NodeInstance node)
        {
            return ExecuteAction(node, _ => _?.DisableLearnMode());
        }

        public Task<bool> Read(NodeInstance node)
        {
            return ExecuteAction(node, _ => _?.Read());
        }

        public Task<IList<NodeInstance>> Import(NodeInstance node, string fileName)
        {
            return ExecuteAction(node, _ => _?.Import(fileName));
        }
        public Task<IList<NodeInstance>> Import(ImportConfig config)
        {
            return ExecuteAction(config.Node, _ => _?.Import(config));
        }

        private Task<T> ExecuteAction<T>(NodeInstance node, Func<IDriverNode, Task<T>> action)
        {
            try
            {
                var driverNode = _mapper.Get(node.ObjId);
                if (driverNode != null)
                {
                    return action(driverNode);
                }
                _logger.LogError($"Could not find mapped driver instance for node {node.ObjId} {node.Name}");
                return Task.FromResult(default(T));
            }
            catch (NodeNotFoundException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError($"Could not execute action {action} {e}");
                throw;
            }
        }


        private Task ExecuteAction(NodeInstance node, Func<IDriverNode, Task> action)
        {
            try
            {
                var driverNode = _mapper.Get(node.ObjId);
                if (driverNode != null)
                {
                    return action(driverNode);
                }
            }
            catch (NodeNotFoundException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError($"Could not execute action {action} {e}");
            }

            return Task.CompletedTask;
        }
    }
}
