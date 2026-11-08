using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Control.Base;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Surveillance.AccessController;
using P3.Logic.Surveillance.Security;
using P3.Logic.Surveillance.Tests.TestSupport;
using Xunit;
using F = P3.Logic.Surveillance.AccessController.AccessControllerLogicFactory;

namespace P3.Logic.Surveillance.Tests.AccessController
{
    public class AccessControllerTests : SecurityLogicTest<AccessControllerLogicFactory, AccessControllerLogic>
    {
        private readonly TestSource _keypad = new("Keypad front door");
        private readonly TestSource _reader = new("NFC front door");

        public AccessControllerTests()
        {
            FakeTimeProvider.SetTimeZone(TimeZoneInfo.Utc);
        }

        public override void Dispose()
        {
            FakeTimeProvider.SetTimeZone(TimeZoneInfo.Local);
            base.Dispose();
        }

        protected override AccessControllerLogic Create(ILogicContext context, ISecurityStateStore store) => new(context, store);

        private SecurityCommandResult SaveUser(string name, string groups = "Family", string code = null, string tag = null, bool admin = true, string previousName = null)
        {
            var args = new Dictionary<string, string> { ["name"] = name, ["groups"] = groups };
            if (code != null) args["code"] = code;
            if (tag != null) args["tag"] = tag;
            if (previousName != null) args["previousName"] = previousName;
            return Command(AccessControllerLogic.CmdUserSave, args, "admin", admin);
        }

        private void Setup(string groups = "Family: Mo-Su 00:00-24:00; Cleaner: Mo,Th 08:00-12:00", int mode = AccessControllerLogic.AuthCodeOrTag)
        {
            SetParam(F.ParamGroups, groups);
            SetParam(F.ParamAuthMode, mode);
            Start();
            Assert.Equal(SecurityCommandResult.Ok, SaveUser("Anna", code: "4711", tag: "04:A2:19:FF"));
        }

        private bool DoorOpened() => Values(F.OutDoor).Contains(true);

        [Fact]
        public void Code_OpensDoorForItsDuration()
        {
            SetParam(F.ParamDoorOn, 3);
            Setup();

            Input(F.InCode, "4711", _keypad);

            Assert.True(LastBool(F.OutDoor));
            Assert.Equal("Anna", Last(F.OutUser));
            Assert.Contains(true, Values(F.OutGranted));
            Advance(3);
            Assert.False(LastBool(F.OutDoor));
        }

        [Fact]
        public void Tag_OpensDoor_NumericCodeFromKeypadToo()
        {
            Setup();
            Input(F.InTag, "04:A2:19:FF", _reader);
            Assert.True(DoorOpened());

            Advance(5);
            ClearRecorded();
            Input(F.InCode, 4711, _keypad);
            Assert.True(DoorOpened());
        }

        [Fact]
        public void WrongCode_IsDenied_AndLoggedWithoutTheCode()
        {
            Setup();

            Input(F.InCode, "9999", _keypad);

            Assert.False(DoorOpened());
            Assert.Contains(true, Values(F.OutDenied));
            var raw = Store.Raw(Sut.Id);
            Assert.DoesNotContain("9999", raw);
            Assert.DoesNotContain("4711", raw);
            Assert.DoesNotContain("04:A2:19:FF", raw);
            Assert.Contains("denied-unknown-code", raw);
        }

        [Fact]
        public void Lockout_AfterMaxFailures()
        {
            SetParam(F.ParamMaxFailures, 3);
            SetParam(F.ParamLockout, 60);
            Setup();

            for (var i = 0; i < 3; i++)
            {
                Input(F.InCode, "000" + i, _keypad);
            }
            Assert.True(LastBool(F.OutLocked));

            Input(F.InCode, "4711", _keypad);
            Assert.False(DoorOpened());

            Advance(61);
            Assert.False(LastBool(F.OutLocked));
            Input(F.InCode, "4711", _keypad);
            Assert.True(DoorOpened());
        }

        [Fact]
        public void TimeWindow_IsEnforced()
        {
            Setup();
            Assert.Equal(SecurityCommandResult.Ok, SaveUser("Cleaner Eva", "Cleaner", code: "5555"));

            // Sunday 2026-11-08 10:00 UTC: no access
            Input(F.InCode, "5555", _keypad);
            Assert.False(DoorOpened());
            Assert.Equal("SURVEILLANCE.ACCESS.CAUSE.OUTSIDE_TIME", Last(F.OutCause));

            // Monday 09:00
            FakeTimeProvider.SetDateTime(new DateTime(2026, 11, 9, 9, 0, 0, DateTimeKind.Utc));
            Input(F.InCode, "5555", _keypad);
            Assert.True(DoorOpened());
        }

        [Fact]
        public void TwoFactor_NeedsCodeAndTagOfSameUser()
        {
            Setup(mode: AccessControllerLogic.AuthTwoFactor);
            Assert.Equal(SecurityCommandResult.Ok, SaveUser("Ben", code: "1234", tag: "BEN-TAG"));

            Input(F.InCode, "4711", _keypad);
            Assert.False(DoorOpened());
            Input(F.InTag, "BEN-TAG", _reader);
            Assert.False(DoorOpened());
            Assert.Equal("SURVEILLANCE.ACCESS.CAUSE.FACTORS_MISMATCH", Last(F.OutCause));

            Input(F.InTag, "04:A2:19:FF", _reader);
            Assert.False(DoorOpened());
            Advance(5);
            Input(F.InCode, "4711", _keypad);
            Assert.True(DoorOpened());
        }

        [Fact]
        public void TwoFactor_SecondFactorTooLate()
        {
            SetParam(F.ParamTwoFactorWindow, 30);
            Setup(mode: AccessControllerLogic.AuthTwoFactor);

            Input(F.InCode, "4711", _keypad);
            Advance(31);
            Input(F.InTag, "04:A2:19:FF", _reader);

            Assert.False(DoorOpened());
        }

        [Fact]
        public void TagOnlyMode_RejectsCodes()
        {
            Setup(mode: AccessControllerLogic.AuthTagOnly);
            Input(F.InCode, "4711", _keypad);
            Assert.False(DoorOpened());
            Input(F.InTag, "04:A2:19:FF", _reader);
            Assert.True(DoorOpened());
        }

        [Fact]
        public void LearnTag_AssignsNextTag()
        {
            Setup();
            Assert.Equal(SecurityCommandResult.Ok, SaveUser("Ben", code: "1234"));
            Assert.Equal(SecurityCommandResult.Ok, Command(AccessControllerLogic.CmdLearnTag, new() { ["name"] = "Ben" }, "admin", true));

            Input(F.InTag, "NEW-TAG", _reader);
            Assert.False(DoorOpened());

            Input(F.InTag, "NEW-TAG", _reader);
            Assert.True(DoorOpened());
            Assert.Equal("Ben", Last(F.OutUser));
        }

        [Fact]
        public void LearnTag_TimesOut()
        {
            Setup();
            Command(AccessControllerLogic.CmdLearnTag, new() { ["name"] = "Anna" }, "admin", true);
            Advance(61);

            Input(F.InTag, "OTHER", _reader);

            Assert.False(DoorOpened());
            Assert.Contains(true, Values(F.OutDenied));
        }

        [Fact]
        public void UserManagement_IsAdminOnly()
        {
            Setup();
            Assert.Equal(SecurityCommandResult.Denied, SaveUser("Mallory", code: "1111", admin: false));
            Assert.Equal(SecurityCommandResult.Denied, Command(AccessControllerLogic.CmdUserDelete, new() { ["name"] = "Anna" }));
            Assert.Equal(SecurityCommandResult.Denied, Command(AccessControllerLogic.CmdLearnTag, new() { ["name"] = "Anna" }));
            Assert.Single(Sut.Users);
        }

        [Fact]
        public void Codes_AreValidatedAndUnique()
        {
            Setup();
            Assert.Equal(SecurityCommandResult.NotPossible, SaveUser("Ben", code: "123"));
            Assert.Equal(SecurityCommandResult.NotPossible, SaveUser("Ben", code: "12ab"));
            Assert.Equal(SecurityCommandResult.NotPossible, SaveUser("Ben", code: "4711"));
            Assert.Equal(SecurityCommandResult.NotPossible, SaveUser("Ben", tag: "04:A2:19:FF"));
            Assert.Equal(SecurityCommandResult.NotPossible, SaveUser("anna", code: "2222", previousName: "Nobody"));
            Assert.Equal(SecurityCommandResult.Ok, SaveUser("Ben", code: "8888"));
        }

        [Fact]
        public void UserUpdate_KeepsCodeUnlessGiven_RenameWorks()
        {
            Setup();
            Assert.Equal(SecurityCommandResult.Ok, SaveUser("Anna Maria", "Family", previousName: "Anna"));

            Input(F.InCode, "4711", _keypad);
            Assert.Equal("Anna Maria", Last(F.OutUser));
        }

        [Fact]
        public void Delete_RemovesAccess()
        {
            Setup();
            Assert.Equal(SecurityCommandResult.Ok, Command(AccessControllerLogic.CmdUserDelete, new() { ["name"] = "anna" }, "admin", true));
            Input(F.InCode, "4711", _keypad);
            Assert.False(DoorOpened());
        }

        [Fact]
        public void Lock_FromVisu_And_OffInput()
        {
            Setup();
            Assert.Equal(SecurityCommandResult.Ok, Command(AccessControllerLogic.CmdLock));
            Input(F.InCode, "4711", _keypad);
            Assert.False(DoorOpened());
            Assert.Equal(SecurityCommandResult.NotPossible, Command(AccessControllerLogic.CmdOpen));

            Assert.Equal(SecurityCommandResult.Ok, Command(AccessControllerLogic.CmdUnlock));
            Input(F.InOff, true);
            Input(F.InCode, "4711", _keypad);
            Assert.False(DoorOpened());
            Input(F.InOff, false);
            Input(F.InCode, "4711", _keypad);
            Assert.True(DoorOpened());
        }

        [Fact]
        public void OpenFromVisu_And_OpenInput()
        {
            Setup();
            Assert.Equal(SecurityCommandResult.Ok, Command(AccessControllerLogic.CmdOpen, user: "maria"));
            Assert.True(DoorOpened());
            Assert.Equal("maria", Last(F.OutUser));

            Advance(5);
            ClearRecorded();
            Pulse(F.InOpen, new TestSource("Inside button"));
            Assert.True(DoorOpened());
        }

        [Fact]
        public void ResetValues_AreIgnored_AndReplayAfterRestartDoesNotOpen()
        {
            Setup();
            Input(F.InCode, "", _keypad);
            Input(F.InCode, 0, _keypad);
            Assert.DoesNotContain(true, Values(F.OutDenied));

            Restart(settle: false);
            Input(F.InCode, "4711", _keypad);
            Assert.False(DoorOpened());
        }

        [Fact]
        public void Users_SurviveRestart_AndAreAdminDataOnly()
        {
            Setup();
            Restart();

            Input(F.InCode, "4711", _keypad);
            Assert.True(DoorOpened());

            Assert.Equal(Newtonsoft.Json.Linq.JTokenType.Null, VisuState()["users"]!.Type);
            var users = VisuState(admin: true)["users"]!;
            var anna = Assert.Single(users);
            Assert.True((bool)anna["hasCode"]);
            Assert.True((bool)anna["hasTag"]);
            Assert.DoesNotContain("hash", users.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("Mo-Fr 08:00-17:00", 2026, 11, 9, 8, 0, true)]
        [InlineData("Mo-Fr 08:00-17:00", 2026, 11, 9, 17, 0, false)]
        [InlineData("Mo-Fr 08:00-17:00", 2026, 11, 8, 10, 0, false)]
        [InlineData("Sa-Mo 00:00-24:00", 2026, 11, 8, 23, 59, true)]
        [InlineData("Fr 22:00-06:00", 2026, 11, 14, 5, 59, true)]
        [InlineData("Fr 22:00-06:00", 2026, 11, 14, 6, 0, false)]
        [InlineData("Di,Do 10:00-11:00", 2026, 11, 12, 10, 30, true)]
        public void Schedule_Windows(string window, int y, int mo, int d, int h, int mi, bool expected)
        {
            var schedule = AccessSchedule.Parse("G: " + window);
            Assert.Empty(schedule.Errors);
            Assert.Equal(expected, schedule.Allows(new[] { "G" }, new DateTime(y, mo, d, h, mi, 0)));
        }

        [Fact]
        public void Schedule_ReportsErrors()
        {
            var schedule = AccessSchedule.Parse("Family: Xx 08:00-09:00; broken");
            Assert.Equal(2, schedule.Errors.Count);
        }
    }
}
