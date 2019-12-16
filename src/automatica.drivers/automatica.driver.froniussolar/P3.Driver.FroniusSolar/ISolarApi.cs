using System;
using System.Threading.Tasks;
using P3.Driver.FroniusSolar.Model.Realtime;

namespace P3.Driver.FroniusSolar
{
    public interface ISolarApi : IDisposable
    {
        Task<InverterInfo> GetInverterInfo();
    }
}
