using System;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace P3.Driver.HueBridge.Tests
{
    public class HueDriverUserTests
    {
        [Fact]
        public void IsLinkButtonPressed_NewDriver_IsFalse()
        {
            var driver = HueTestFactory.CreateDriver();

            Assert.False(driver.IsLinkButtonPressed);
        }

        [Fact]
        public void PressLinkButton_Default_OpensWindowForThreeMinutes()
        {
            var clock = new FakeClock();
            var driver = HueTestFactory.CreateDriver(clock);

            driver.PressLinkButton();
            Assert.True(driver.IsLinkButtonPressed);

            clock.Advance(TimeSpan.FromMinutes(3) - TimeSpan.FromSeconds(1));
            Assert.True(driver.IsLinkButtonPressed);

            clock.Advance(TimeSpan.FromSeconds(1));
            Assert.False(driver.IsLinkButtonPressed);
        }

        [Fact]
        public void PressLinkButton_CustomDuration_ClosesAfterThatDuration()
        {
            var clock = new FakeClock();
            var driver = HueTestFactory.CreateDriver(clock);

            driver.PressLinkButton(TimeSpan.FromSeconds(10));
            clock.Advance(TimeSpan.FromSeconds(9));
            Assert.True(driver.IsLinkButtonPressed);

            clock.Advance(TimeSpan.FromSeconds(1));
            Assert.False(driver.IsLinkButtonPressed);
        }

        [Fact]
        public void PressLinkButton_PressedAgain_ExtendsWindowFromNow()
        {
            var clock = new FakeClock();
            var driver = HueTestFactory.CreateDriver(clock);

            driver.PressLinkButton();
            clock.Advance(TimeSpan.FromMinutes(2));
            driver.PressLinkButton();
            clock.Advance(TimeSpan.FromMinutes(2));

            Assert.True(driver.IsLinkButtonPressed);
        }

        [Fact]
        public void ReleaseLinkButton_OpenWindow_ClosesIt()
        {
            var driver = HueTestFactory.CreateDriver();
            driver.PressLinkButton();

            driver.ReleaseLinkButton();

            Assert.False(driver.IsLinkButtonPressed);
        }

        [Fact]
        public void BridgeConfigLinkButton_FollowsTheWindow()
        {
            var clock = new FakeClock();
            var driver = HueTestFactory.CreateDriver(clock);
            Assert.False(driver.BridgeConfig.LinkButton);

            driver.PressLinkButton();
            Assert.True(driver.BridgeConfig.LinkButton);

            clock.Advance(HueDriver.DefaultLinkButtonWindow);
            Assert.False(driver.BridgeConfig.LinkButton);
        }

        [Fact]
        public void TryCreateUser_LinkButtonNotPressed_FailsAndAddsNoUser()
        {
            var driver = HueTestFactory.CreateDriver();

            var created = driver.TryCreateUser(out var userName);

            Assert.False(created);
            Assert.Null(userName);
            Assert.Empty(driver.GetUserNames());
        }

        [Fact]
        public void TryCreateUser_LinkButtonNotPressed_DoesNotRaiseWhiteListChanged()
        {
            var driver = HueTestFactory.CreateDriver();
            var raised = 0;
            driver.WhiteListChanged += (_, _) => raised++;

            driver.TryCreateUser(out _);

            Assert.Equal(0, raised);
        }

        [Fact]
        public void TryCreateUser_LinkButtonPressed_CreatesRandomValidAuthorizedUser()
        {
            var driver = HueTestFactory.CreateDriver();
            driver.PressLinkButton();

            var created = driver.TryCreateUser(out var userName);

            Assert.True(created);
            Assert.Matches("^[0-9a-f]{32}$", userName);
            Assert.True(HueDriver.IsValidUserName(userName));
            Assert.True(driver.IsAuthorized(userName));
            Assert.Equal(new[] { userName }, driver.GetUserNames());
        }

        [Fact]
        public void TryCreateUser_LinkButtonPressed_RaisesWhiteListChanged()
        {
            var driver = HueTestFactory.CreateDriver();
            driver.PressLinkButton();
            var raised = 0;
            driver.WhiteListChanged += (_, _) => raised++;

            driver.TryCreateUser(out _);

            Assert.Equal(1, raised);
        }

        [Fact]
        public void TryCreateUser_TwoUsersInOneWindow_AreDistinct()
        {
            var driver = HueTestFactory.CreateDriver();
            driver.PressLinkButton();

            Assert.True(driver.TryCreateUser(out var first));
            Assert.True(driver.TryCreateUser(out var second));

            Assert.NotEqual(first, second);
            Assert.Equal(2, driver.GetUserNames().Count);
        }

        [Fact]
        public void TryCreateUser_AfterWindowExpired_Fails()
        {
            var clock = new FakeClock();
            var driver = HueTestFactory.CreateDriver(clock);
            driver.PressLinkButton();
            clock.Advance(HueDriver.DefaultLinkButtonWindow + TimeSpan.FromSeconds(1));

            Assert.False(driver.TryCreateUser(out _));
            Assert.Empty(driver.GetUserNames());
        }

        [Fact]
        public void TryCreateUser_AfterRelease_Fails()
        {
            var driver = HueTestFactory.CreateDriver();
            driver.PressLinkButton();
            driver.ReleaseLinkButton();

            Assert.False(driver.TryCreateUser(out _));
        }

        [Fact]
        public void TryCreateUser_WhitelistFull_Fails()
        {
            var driver = HueTestFactory.CreateDriver();
            driver.LoadUsers(Enumerable.Range(0, HueDriver.MaxUsers).Select(i => $"persisted-user-{i:D4}"));
            driver.PressLinkButton();

            var created = driver.TryCreateUser(out var userName);

            Assert.False(created);
            Assert.Null(userName);
            Assert.Equal(HueDriver.MaxUsers, driver.GetUserNames().Count);
        }

        [Fact]
        public void TryCreateUser_OneSlotLeft_CreatesExactlyOneMore()
        {
            var driver = HueTestFactory.CreateDriver();
            driver.LoadUsers(Enumerable.Range(0, HueDriver.MaxUsers - 1).Select(i => $"persisted-user-{i:D4}"));
            driver.PressLinkButton();

            Assert.True(driver.TryCreateUser(out _));
            Assert.False(driver.TryCreateUser(out _));
            Assert.Equal(HueDriver.MaxUsers, driver.GetUserNames().Count);
        }

        [Fact]
        public void IsAuthorized_UnknownUser_False()
        {
            var driver = HueTestFactory.CreateDriver();
            driver.LoadUsers(new[] { HueTestFactory.ValidUser });

            Assert.False(driver.IsAuthorized(HueTestFactory.OtherValidUser));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void IsAuthorized_NullOrEmpty_False(string userName)
        {
            var driver = HueTestFactory.CreateDriver();
            driver.LoadUsers(new[] { HueTestFactory.ValidUser });

            Assert.False(driver.IsAuthorized(userName));
        }

        [Fact]
        public void IsAuthorized_KnownUser_True()
        {
            var driver = HueTestFactory.CreateDriver();
            driver.LoadUsers(new[] { HueTestFactory.ValidUser });

            Assert.True(driver.IsAuthorized(HueTestFactory.ValidUser));
        }

        [Fact]
        public void IsAuthorized_IsCaseSensitive()
        {
            var driver = HueTestFactory.CreateDriver();
            driver.LoadUsers(new[] { "abcdef0123456789" });

            Assert.False(driver.IsAuthorized("ABCDEF0123456789"));
        }

        [Fact]
        public void IsAuthorized_KnownUser_UpdatesLastUsed()
        {
            var driver = HueTestFactory.CreateDriver();
            driver.LoadUsers(new[] { HueTestFactory.ValidUser });
            var longAgo = new DateTime(2000, 1, 1);
            driver.WhiteList[HueTestFactory.ValidUser].LastUsed = longAgo;

            driver.IsAuthorized(HueTestFactory.ValidUser);

            Assert.True(driver.WhiteList[HueTestFactory.ValidUser].LastUsed > longAgo);
        }

        [Fact]
        public void IsAuthorized_AfterRemoveAllUsers_False()
        {
            var driver = HueTestFactory.CreateDriver();
            driver.LoadUsers(new[] { HueTestFactory.ValidUser });

            driver.RemoveAllUsers();

            Assert.False(driver.IsAuthorized(HueTestFactory.ValidUser));
        }

        [Fact]
        public void RemoveAllUsers_ClearsWhitelistAndRaisesWhiteListChanged()
        {
            var driver = HueTestFactory.CreateDriver();
            driver.LoadUsers(new[] { HueTestFactory.ValidUser, HueTestFactory.OtherValidUser });
            var raised = 0;
            driver.WhiteListChanged += (_, _) => raised++;

            driver.RemoveAllUsers();

            Assert.Empty(driver.GetUserNames());
            Assert.Equal(1, raised);
        }

        [Fact]
        public void LoadUsers_Null_DoesNothing()
        {
            var driver = HueTestFactory.CreateDriver();

            driver.LoadUsers(null);

            Assert.Empty(driver.GetUserNames());
        }

        [Fact]
        public void LoadUsers_SkipsInvalidNames()
        {
            var driver = HueTestFactory.CreateDriver();

            driver.LoadUsers(new[] { HueTestFactory.ValidUser, "short", "has space 12345", "semi;colon-12345", "", null, new string('a', 65) });

            Assert.Equal(new[] { HueTestFactory.ValidUser }, driver.GetUserNames());
        }

        [Fact]
        public void LoadUsers_Duplicates_AreStoredOnce()
        {
            var driver = HueTestFactory.CreateDriver();

            driver.LoadUsers(new[] { HueTestFactory.ValidUser, HueTestFactory.ValidUser });

            Assert.Single(driver.GetUserNames());
        }

        [Fact]
        public void LoadUsers_MoreThanMaxUsers_KeepsOnlyMaxUsers()
        {
            var driver = HueTestFactory.CreateDriver();

            driver.LoadUsers(Enumerable.Range(0, HueDriver.MaxUsers + 10).Select(i => $"persisted-user-{i:D4}"));

            Assert.Equal(HueDriver.MaxUsers, driver.GetUserNames().Count);
        }

        [Fact]
        public void LoadUsers_DoesNotRaiseWhiteListChanged()
        {
            var driver = HueTestFactory.CreateDriver();
            var raised = 0;
            driver.WhiteListChanged += (_, _) => raised++;

            driver.LoadUsers(new[] { HueTestFactory.ValidUser });

            Assert.Equal(0, raised);
        }

        [Theory]
        [InlineData("0123456789", true)]
        [InlineData("abcdefABCDEF-0123", true)]
        [InlineData("012345678", false)]
        [InlineData("has space 0123", false)]
        [InlineData("under_score_0123", false)]
        [InlineData("dot.dot.dot.dot", false)]
        [InlineData("          ", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsValidUserName_Theory(string name, bool expected)
        {
            Assert.Equal(expected, HueDriver.IsValidUserName(name));
        }

        [Fact]
        public void IsValidUserName_LengthBounds()
        {
            Assert.True(HueDriver.IsValidUserName(new string('a', 64)));
            Assert.False(HueDriver.IsValidUserName(new string('a', 65)));
        }

        [Fact]
        public void BridgeConfig_Whitelist_NeverContainsUsers()
        {
            var driver = HueTestFactory.CreateDriver();
            driver.LoadUsers(new[] { HueTestFactory.ValidUser });

            Assert.Empty(driver.BridgeConfig.Whitelist);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(65536)]
        public void Constructor_InvalidPort_FallsBackToDefaultPort(int port)
        {
            var driver = new HueDriver(NullLogger.Instance, IPAddress.Loopback, port);

            Assert.Equal(HueDriver.DefaultPort, driver.Port);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(8080)]
        [InlineData(65535)]
        public void Constructor_ValidPort_IsUsed(int port)
        {
            var driver = new HueDriver(NullLogger.Instance, IPAddress.Loopback, port);

            Assert.Equal(port, driver.Port);
        }

        [Fact]
        public void Constructor_NullBindAddress_UsesAny()
        {
            var driver = new HueDriver(NullLogger.Instance, null, 1234);

            Assert.Equal(IPAddress.Any, driver.BindAddress);
        }

        [Fact]
        public void Constructor_Default_UsesAnyAndDefaultPort()
        {
            var driver = new HueDriver(NullLogger.Instance);

            Assert.Equal(IPAddress.Any, driver.BindAddress);
            Assert.Equal(HueDriver.DefaultPort, driver.Port);
            Assert.Equal(driver.BridgeId.ToString(), driver.BridgeConfig.BridgeId);
        }

        [Fact]
        public void Constructor_NullLogger_UsesNullLogger()
        {
            _ = new HueDriver(null, IPAddress.Loopback, 1234);

            Assert.NotNull(HueDriver.Logger);
        }
    }
}
