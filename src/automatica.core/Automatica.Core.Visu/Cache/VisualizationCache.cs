using Automatica.Core.Internals.Cache.Visualization;

namespace Automatica.Core.Visu.Cache
{
    internal class VisualizationCache : IVisualizationCache
    {
        public VisualizationCache(IVisualizationPageCache pageCache, IVisualizationInstanceCache instanceCache, IVisualizationTemplateCache templateCache)
        {
            PageCache = pageCache;
            InstanceCache = instanceCache;
            TemplateCache = templateCache;
        }

        public IVisualizationPageCache PageCache { get; }
        public IVisualizationInstanceCache InstanceCache { get; }
        public IVisualizationTemplateCache TemplateCache { get; }

        public void ClearAll()
        {
            TemplateCache.Clear();
            ClearInstances();
        }

        public void ClearInstances()
        {
            PageCache.Clear();
            InstanceCache.Clear();
        }
    }
}
