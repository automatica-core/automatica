using System;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.Core;

namespace Automatica.Core.Internals.Cache.Logic
{
    internal class LogicCacheFacade : ILogicCacheFacade
    {
        private readonly ILogicDataHandler _logicStore;

        public LogicCacheFacade(ILogicInstanceCache instanceCache, ILogicPageCache pageCache, ILogicTemplateCache templateCache, ILinkCache linkCache, ILogicNodeInstanceCache logicNodeInstanceCache)
        {
            LinkCache = linkCache;
            InstanceCache = instanceCache;
            PageCache = pageCache;
            TemplateCache = templateCache;
            LogicNodeInstanceCache = logicNodeInstanceCache;
        }

        public ILinkCache LinkCache { get; }
        public ILogicInstanceCache InstanceCache { get; }
        public ILogicPageCache PageCache { get; }
        public ILogicTemplateCache TemplateCache { get; }
        public ILogicNodeInstanceCache LogicNodeInstanceCache { get; }

        public void ClearInstances()
        {
            InstanceCache.Clear();
            PageCache.Clear();
            LinkCache.Clear();

            LogicNodeInstanceCache.Clear();
        }
        

        public Task RemoveLink(Guid linkId)
        {
            LinkCache.Remove(linkId);
            PageCache.Remove(linkId);

            return Task.CompletedTask;
        }

        public Task AddOrUpdateLink(Guid objId, AutomaticaContext dbContext)
        {
            var link = LinkCache.GetSingle(objId, dbContext);

            if (link.isNew)
            {
                PageCache.AddLink(link.link);
            }
            else
            {
                PageCache.UpdateLink(link.link);
            }

            return Task.CompletedTask;
        }

        public Task RemoveNodeInstance(Guid instanceId)
        {
            LogicNodeInstanceCache.Remove(instanceId);
            return Task.CompletedTask;
        }

        public Task RemoveLogic(Guid instanceId)
        {
            InstanceCache.Remove(instanceId);
            return Task.CompletedTask;
        }
    }
}
