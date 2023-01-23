using System;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Common;

namespace Automatica.Core.Internals.Cache.Logic
{
    public interface ILogicCacheFacade
    {
        ILinkCache LinkCache { get; }
        ILogicInstanceCache InstanceCache { get; }
        ILogicPageCache PageCache { get; }
        ILogicTemplateCache TemplateCache{ get; }
        ILogicNodeInstanceCache LogicNodeInstanceCache { get; }


        void ClearInstances();

        Task RemoveLink(Guid linkId);
        Task AddOrUpdateLink(Guid objId, AutomaticaContext dbContext);
    }
}
