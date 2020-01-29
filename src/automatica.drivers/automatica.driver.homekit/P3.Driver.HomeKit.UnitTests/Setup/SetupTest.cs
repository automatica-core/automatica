using P3.Driver.HomeKit.Hap;
using Xunit;

namespace P3.Driver.HomeKit.UnitTests.Setup
{
    public class SetupTest
    {
        [Fact]
        public void TestInvalidSetupCodes()
        {
            Assert.False(HomeKitSetup.IsSetupCodeValid("000-00-000"));
            Assert.False(HomeKitSetup.IsSetupCodeValid("111-11-111"));
            Assert.False(HomeKitSetup.IsSetupCodeValid("222-22-222"));
            Assert.False(HomeKitSetup.IsSetupCodeValid("333-33-333"));
            Assert.False(HomeKitSetup.IsSetupCodeValid("444-44-444"));
            Assert.False(HomeKitSetup.IsSetupCodeValid("555-55-555"));
            Assert.False(HomeKitSetup.IsSetupCodeValid("666-66-666"));
            Assert.False(HomeKitSetup.IsSetupCodeValid("777-77-777"));
            Assert.False(HomeKitSetup.IsSetupCodeValid("888-88-888"));
            Assert.False(HomeKitSetup.IsSetupCodeValid("999-99-999"));
            Assert.False(HomeKitSetup.IsSetupCodeValid("123-45-678"));
            Assert.False(HomeKitSetup.IsSetupCodeValid("876-54-321"));
        }
    }
}
