using P3.Driver.VkingBms.Driver.Interfaces;

namespace P3.Driver.VkingBms.Driver.Data
{
    internal class VersionIdResponse : BaseResponse, IVersionIdResponse
    {
        public VersionIdResponse(byte[] response) : base(response)
        {
            var index = 13;
            
            for (int index3 = 0; index3 < 30; ++index3)
            {
                this.VersionId += Util.GetChar(response, index);
                index += 2;
            }
        }

        public string VersionId { get; } = "";
    }
}
