using P3.Driver.VkingBms.Driver.Interfaces;

namespace P3.Driver.VkingBms.Driver.Data
{
    internal abstract class BaseResponse : IDataResponse
    {
        protected BaseResponse(byte[] response)
        {
            if (response.Length >= 15)
            {
                this.Version = Util.GetByte(response, 1);
                this.PackAddress= Util.GetByte(response, 3);
                this.Rtn = Util.GetByte(response, 7);
            }
        }
        public byte Version { get; private set; }
        public byte PackAddress { get; private set; }
        public byte Rtn { get; private set; }
    }
}
