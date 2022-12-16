using System.Security.Cryptography;
using System.Text;
using Xunit;
using Automatica.Core.Base.Cryptography;

namespace P3.Driver.Loxone.Miniserver.Tests
{
    public class CryptoTests
    {
        [Fact]
        public void TestSha1()
        {
            var payload = "LoxLIVEpasswordTest:31303761333133632D303263352D353133382D66666666643562633730306533363235";
            var sha1 = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(payload)).ToHex(true);


            Assert.Equal("3085B26A64CA633CBD2DD2D4A7F8401801DBD2D0", sha1);
        }

        [Fact]
        public void TestHmacSha1()
        {
            var oneTimeSalt = "38383030393837453338313236363338343639413143433634313542384532393142303230303038";
            var user = "app";
            var pwHash = "3085B26A64CA633CBD2DD2D4A7F8401801DBD2D0";

            var hash = $"{user}:{pwHash}".CalculateHmacSha1(Automatica.Core.Driver.Utility.Utils.StringToByteArray(oneTimeSalt));

            Assert.Equal("c53e99523d96ab819fbd433580bc74bc7d2dba38", hash);

        }
    }
}
