namespace Automatica.Core.Internals.Cache.Visualization
{
    public interface IVisualizationCache
    {
        IVisualizationPageCache PageCache { get; }
        IVisualizationInstanceCache InstanceCache { get; }
        IVisualizationTemplateCache TemplateCache { get; }

        void ClearAll();
        void ClearInstances();
    }
}
