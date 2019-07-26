using System.Threading.Tasks;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Driver.Loader
{
    public interface IDriverFactoryLoader
    {
        Task LoadDriverFactory(NodeInstance nodeInstance, IDriverFactory factory, IDriverContext context);
    }
}
