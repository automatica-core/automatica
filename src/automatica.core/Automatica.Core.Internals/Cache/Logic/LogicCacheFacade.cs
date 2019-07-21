namespace Automatica.Core.Internals.Cache.Logic
{
    internal class LogicCacheFacade : ILogicCacheFacade
    {
        public LogicCacheFacade(ILogicInstanceCache instanceCache, ILogicPageCache pageCache, ILogicTemplateCache templateCache)
        {
            InstanceCache = instanceCache;
            PageCache = pageCache;
            TemplateCache = templateCache;
        }

        public ILogicInstanceCache InstanceCache { get; }
        public ILogicPageCache PageCache { get; }
        public ILogicTemplateCache TemplateCache { get; }

        public void ClearInstances()
        {
            InstanceCache.Clear();
            PageCache.Clear();
        }
    }
}
