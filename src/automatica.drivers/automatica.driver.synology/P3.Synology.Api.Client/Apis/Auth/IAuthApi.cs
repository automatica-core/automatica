using System.Threading.Tasks;
using P3.Synology.Api.Client.Apis.Auth.Models;
using P3.Synology.Api.Client.Shared.Models;

namespace P3.Synology.Api.Client.Apis.Auth
{
    public interface IAuthApi
    {
        Task<LoginResponse> LoginAsync(string username, string password, string otpCode = "");

        Task<BaseApiResponse> LogoutAsync(string sid);
    }
}
