namespace Automatica.Core.Internals.Cache.Logic
{
    public interface ILogicCacheFacade
    {
        ILogicInstanceCache InstanceCache { get; }
        ILogicPageCache PageCache { get; }
        ILogicTemplateCache TemplateCache{ get; }

        void ClearInstances();
    }
}
