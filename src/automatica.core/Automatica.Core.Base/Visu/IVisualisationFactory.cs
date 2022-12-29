using Automatica.Core.EF.Models;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Base.Visu
{
    /// <summary>
    /// Interface for visualisation factory
    /// </summary>
    public interface IVisualisationFactory
    {
        void Initialize(AutomaticaContext database, IConfiguration config);
    }
}
