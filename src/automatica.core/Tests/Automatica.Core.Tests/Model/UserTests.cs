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

        [Fact]
        public void TestVerifyLegacyHash()
        {
            const string salt = "NZsP6NnmfBuYeJrrAKNuVQ==";
            const string legacy = "/OOoOer10+tGwTRDTrQSoeCxVTFr6dtYly7d0cPxIak=";

            Assert.True(UserHelper.VerifyPassword("Xtw9NMgx", salt, legacy, out var needsRehash));
            Assert.True(needsRehash);

            Assert.False(UserHelper.VerifyPassword("wrong", salt, legacy, out needsRehash));
            Assert.False(needsRehash);
        }

        [Fact]
        public void TestVerifyCurrentHash()
        {
            var salt = UserHelper.GenerateNewSalt();
            var hash = UserHelper.CreatePasswordHash("Xtw9NMgx", salt);

            Assert.StartsWith(UserHelper.HashPrefix, hash);
            Assert.True(UserHelper.VerifyPassword("Xtw9NMgx", salt, hash, out var needsRehash));
            Assert.False(needsRehash);

            Assert.False(UserHelper.VerifyPassword("Xtw9NMgy", salt, hash, out _));
            Assert.False(UserHelper.VerifyPassword("Xtw9NMgx", UserHelper.GenerateNewSalt(), hash, out _));
            Assert.False(UserHelper.VerifyPassword("Xtw9NMgx", salt, UserHelper.HashPrefix + "abc", out _));
            Assert.False(UserHelper.VerifyPassword("Xtw9NMgx", salt, null, out _));
        }
    }
}
