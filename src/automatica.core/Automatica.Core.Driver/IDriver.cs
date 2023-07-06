
using System.Threading;
using System.Threading.Tasks;

namespace Automatica.Core.Driver
{
    /// <summary>
    /// Base root <see cref="IDriverNode"/> interface
    /// </summary>
    public interface IDriver : IDriverNode
    {
        /// <summary>
        /// Will be called before Run
        /// </summary>
        /// <returns>
        /// When return value is false the <see cref="IDriver"/> will not be started
        /// </returns>
        Task<bool> BeforeInit(CancellationToken token = default);
    }
}
