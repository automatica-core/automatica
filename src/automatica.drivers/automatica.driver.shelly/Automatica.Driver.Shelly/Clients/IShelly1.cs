using System.Threading;
using System.Threading.Tasks;
using Automatica.Driver.Shelly.Dtos;
using Automatica.Driver.Shelly.Dtos.Shelly1PM;

namespace Automatica.Driver.Shelly.Clients
{
    public interface IShelly1 : IShelly<Shelly1StatusDto>
    {
    }
}