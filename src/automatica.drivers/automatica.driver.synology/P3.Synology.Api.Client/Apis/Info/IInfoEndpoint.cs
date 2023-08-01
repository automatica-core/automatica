using System.Threading.Tasks;
using P3.Synology.Api.Client.Apis.Info.Models;

namespace P3.Synology.Api.Client.Apis.Info
{
    public interface IInfoEndpoint
    {
        Task<InfoQueryResponse> QueryAsync();
    }
}
