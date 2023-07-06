using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using P3.Driver.IkeaTradfri;
using P3.Driver.IkeaTradfriDriverFactory.Devices;

[assembly: InternalsVisibleTo("P3.Driver.IkeaTradfri.Tests")]

namespace P3.Driver.IkeaTradfriDriverFactory
{
    public interface IIkeaTradfriGateway
    {
        IIkeaTradfriDriver Driver { get; set; }

        Task<bool> Start(CancellationToken token = default);
        Task<bool> Stop(CancellationToken token = default);

        List<IkeaTradfriAttribute> Devices { get; }
    }
}
