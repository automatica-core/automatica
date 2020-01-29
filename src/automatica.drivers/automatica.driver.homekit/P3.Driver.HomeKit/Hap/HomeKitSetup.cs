using System.Linq;

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
    }
}
