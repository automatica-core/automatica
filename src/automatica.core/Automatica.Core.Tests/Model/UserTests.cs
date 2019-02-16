using Automatica.Core.Internals.UserHelper;
using Xunit;

namespace Automatica.Core.Tests.Model
{
    public class UserTests
    {
        [Fact]
        public void TestHashing()
        {
            Assert.Equal("/OOoOer10+tGwTRDTrQSoeCxVTFr6dtYly7d0cPxIak=", UserHelper.HashPassword("Xtw9NMgx", "NZsP6NnmfBuYeJrrAKNuVQ=="));
        }
    }
}
