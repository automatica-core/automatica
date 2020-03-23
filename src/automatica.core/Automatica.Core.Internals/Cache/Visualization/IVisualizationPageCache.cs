using System;
using Automatica.Core.Base.Cache;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Internals.Cache.Visualization
{
    public interface IVisualizationPageCache : IStore<VisuPage>
    {
        VisualizationDataFacade AllByCategory(Guid categoryId);
        VisualizationDataFacade AllByArea(Guid areaInstance);

        VisualizationDataFacade ByFavorites();

        object ByPage(Guid id);
    }
}
