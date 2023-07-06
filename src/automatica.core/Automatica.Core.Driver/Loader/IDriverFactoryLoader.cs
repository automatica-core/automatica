using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Driver.Loader
{
    public interface IDriverFactoryLoader
    {
        Task<IDriver> LoadDriverFactory(NodeInstance nodeInstance, IDriverFactory factory, IDriverContext context, CancellationToken token = default);
    }
}
