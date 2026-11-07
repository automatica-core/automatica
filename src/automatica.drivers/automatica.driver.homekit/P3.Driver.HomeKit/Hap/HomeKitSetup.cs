using System.Linq;
using System.Security.Cryptography;

namespace P3.Driver.HomeKit.Hap
{
    internal static class HomeKitSetup
    {
        private static readonly string[] InvalidSetupCodes = new[]
        {
            "000-00-000",
            "111-11-111",
            "222-22-222",
            "333-33-333",
            "444-44-444",
            "555-55-555",
            "666-66-666",
            "777-77-777",
            "888-88-888",
            "999-99-999",
            "123-45-678",
            "876-54-321"
        };

        public static bool IsSetupCodeValid(string code)
        {
            return !InvalidSetupCodes.Contains(code);
        }

        /// <summary>
        /// Creates a random setup code in the format XXX-XX-XXX from a CSPRNG
        /// </summary>
        public static string GenerateSetupCode()
        {
            while (true)
            {
                var digits = RandomNumberGenerator.GetInt32(0, 100_000_000).ToString("D8");
                var code = $"{digits.Substring(0, 3)}-{digits.Substring(3, 2)}-{digits.Substring(5, 3)}";

                if (IsSetupCodeValid(code))
                {
                    return code;
                }
            }
        }
    }
}
