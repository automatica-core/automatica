using P3.Driver.VkingBms.Driver.Interfaces;

namespace P3.Driver.VkingBms.Driver.Data
{
    internal class BmsInfoResponse : BaseResponse, IBmsInfoResponse
    {
        public string CompanyName { get; } = "";
        public string ManufacturedTime { get; } = "";
        public string ModuleName { get; } = "";
        public string VersionNumber { get; } = "";
        public string BatteryType { get; } = "";
        public string SerialNumber1 { get; } = "";
        public string SerialNumber2 { get; } = "";

        public BmsInfoResponse(byte[] response) : base(response)
        {
            var index = 13;
            index += 2;
            for (var index3 = 0; index3 < 30; ++index3)
            {
                CompanyName += Util.GetChar(response, index);
                index += 2;
            }
            for (var index4 = 0; index4 < 30; ++index4)
            {
                ManufacturedTime += Util.GetChar(response, index);
                index += 2;
            }
            for (var index5 = 0; index5 < 30; ++index5)
            {
                ModuleName += Util.GetChar(response, index);
                index += 2;
            }
            for (var index6 = 0; index6 < 30; ++index6)
            {
                VersionNumber += Util.GetChar(response, index);
                index += 2;
            }
            for (var index7 = 0; index7 < 30; ++index7)
            {
                BatteryType += Util.GetChar(response, index);
                index += 2;
            }
            for (var index8 = 0; index8 < 30; ++index8)
            {
                SerialNumber1 += Util.GetChar(response, index);
                index += 2;
            }
            for (var index9 = 0; index9 < 30; ++index9)
            {
                SerialNumber2 += Util.GetChar(response, index);
                index += 2;
            }
        }
    }
}
