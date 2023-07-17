namespace P3.Synology.Api.Client.Shared.Models
{
    public class ApiResponse<T> : BaseApiResponse
    {
        public T Data { get; set; }
    }
}
