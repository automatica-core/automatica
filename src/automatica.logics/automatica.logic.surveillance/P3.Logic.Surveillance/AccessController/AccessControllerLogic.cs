using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Automatica.Core.Base.IO;
using Automatica.Core.Control.Base;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.Surveillance.BurglarAlarm;
using P3.Logic.Surveillance.Security;
using F = P3.Logic.Surveillance.AccessController.AccessControllerLogicFactory;

namespace P3.Logic.Surveillance.AccessController
{
    public class AccessUser
    {
        public string Name { get; set; }
        public List<string> Groups { get; set; } = new();
        public AccessSecret Code { get; set; }
        public AccessSecret Tag { get; set; }
    }

    public class AccessControllerState : SecurityStateBase
    {
        public List<AccessUser> Users { get; set; } = new();
        public bool Locked { get; set; }
        public int Failures { get; set; }
        public DateTimeOffset? LockoutUntil { get; set; }
        public string LearnUser { get; set; }
        public DateTimeOffset? LearnUntil { get; set; }
        public string LastUser { get; set; }
        public DateTimeOffset? LastTime { get; set; }
        public string LastCause { get; set; }
    }

    /// <summary>
    /// Access controller, see <see cref="AccessControllerLogicFactory"/>. Codes and tags arrive on the inputs (keypad,
    /// NFC reader) and are compared with the users' hashes; they are never logged or stored in clear text by this logic.
    /// Users are managed with admin-only commands from the visualization.
    /// </summary>
    public class AccessControllerLogic : SecurityLogicBase<AccessControllerState>
    {
        public const string DefaultGroups = "Family: Mo-Su 00:00-24:00";

        public const int AuthTwoFactor = 0;
        public const int AuthCodeOrTag = 1;
        public const int AuthTagOnly = 2;
        public const int AuthCodeOnly = 3;

        public const int MinCodeLength = 4;
        public const int MaxCodeLength = 12;
        public static readonly TimeSpan LearnTime = TimeSpan.FromSeconds(60);

        public const string CmdOpen = "open";
        public const string CmdLock = "lock";
        public const string CmdUnlock = "unlock";
        public const string CmdUserSave = "user-save";
        public const string CmdUserDelete = "user-delete";
        public const string CmdLearnTag = "learn-tag";
        public const string CmdLearnCancel = "learn-cancel";

        private static readonly string[] Commands = { CmdOpen, CmdLock, CmdUnlock, CmdUserSave, CmdUserDelete, CmdLearnTag, CmdLearnCancel };
        private static readonly HashSet<string> AdminCommands = new() { CmdUserSave, CmdUserDelete, CmdLearnTag, CmdLearnCancel };

        private bool _offHeld;
        private DateTimeOffset? _doorUntil;
        private (AccessUser User, bool ByCode, DateTimeOffset Time)? _firstFactor;

        public AccessControllerLogic(ILogicContext context, ISecurityStateStore store) : base(context, store)
        {
        }

        public override string SecurityKind => "access";
        public override IReadOnlyCollection<string> SecurityCommands => Commands;

        private AccessSchedule Schedule => AccessSchedule.Parse(ParamString(F.ParamGroups));
        private int AuthMode => ParamInt(F.ParamAuthMode, AuthCodeOrTag);
        private bool IsLocked => State.Locked || _offHeld;

        internal IReadOnlyList<AccessUser> Users => State.Users;

        protected override void OnStarted(bool restored)
        {
            _doorUntil = null;
            _firstFactor = null;
            UpdateOutputs(true);
        }

        protected override bool OnInput(Guid template, object value, IDispatchable source)
        {
            if (template == F.InOff)
            {
                _offHeld = ToBool(value);
                UpdateOutputs();
                return false;
            }

            if (template == F.InOpen)
            {
                if (!Rising(template, source, value))
                {
                    return false;
                }
                OpenDoor(SourceName(source) ?? "O", "open-input");
                return true;
            }

            var text = value switch
            {
                null => "",
                bool => "",
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => Convert.ToString(value, CultureInfo.InvariantCulture)
            };
            text = text?.Trim() ?? "";

            // keypads and readers reset their value; values replayed after a start are no entries
            if (text.Length == 0 || text == "0" || InStartupGrace)
            {
                return false;
            }

            if (template == F.InTag)
            {
                return OnTag(text, SourceName(source));
            }
            if (template == F.InCode)
            {
                return OnCode(text, SourceName(source));
            }
            return false;
        }

        private bool OnTag(string tag, string reader)
        {
            if (State.LearnUser != null && State.LearnUntil is { } until && Now < until)
            {
                var user = FindUser(State.LearnUser);
                State.LearnUser = null;
                State.LearnUntil = null;
                if (user == null)
                {
                    return true;
                }
                if (State.Users.Any(a => a != user && a.Tag != null && a.Tag.Matches(tag)))
                {
                    AddLog("tag-learn-duplicate", user.Name);
                    SetCause("SURVEILLANCE.ACCESS.CAUSE.TAG_IN_USE");
                    return true;
                }
                user.Tag = AccessSecret.Create(tag);
                AddLog("tag-learned", user.Name);
                SetCause("SURVEILLANCE.ACCESS.CAUSE.TAG_LEARNED");
                return true;
            }

            if (AuthMode == AuthCodeOnly)
            {
                return Deny(null, "tag-not-allowed", reader);
            }

            var match = State.Users.FirstOrDefault(a => a.Tag != null && a.Tag.Matches(tag));
            return Authenticate(match, false, reader);
        }

        private bool OnCode(string code, string keypad)
        {
            if (AuthMode == AuthTagOnly)
            {
                return Deny(null, "code-not-allowed", keypad);
            }

            var match = State.Users.FirstOrDefault(a => a.Code != null && a.Code.Matches(code));
            return Authenticate(match, true, keypad);
        }

        private bool Authenticate(AccessUser user, bool byCode, string device)
        {
            if (State.LockoutUntil is { } lockout && Now < lockout)
            {
                return Deny(user, "lockout", device, countFailure: false);
            }
            if (IsLocked)
            {
                return Deny(user, "locked", device, countFailure: false);
            }
            if (user == null)
            {
                return Deny(null, byCode ? "unknown-code" : "unknown-tag", device);
            }

            if (AuthMode == AuthTwoFactor)
            {
                var window = ParamSeconds(F.ParamTwoFactorWindow, 30);
                if (_firstFactor is { } first && first.ByCode != byCode && Now - first.Time <= window)
                {
                    _firstFactor = null;
                    if (first.User != user)
                    {
                        return Deny(user, "factors-mismatch", device);
                    }
                }
                else
                {
                    _firstFactor = (user, byCode, Now);
                    SetCause(byCode ? "SURVEILLANCE.ACCESS.CAUSE.WAITING_TAG" : "SURVEILLANCE.ACCESS.CAUSE.WAITING_CODE");
                    return false;
                }
            }

            var local = TimeZoneInfo.ConvertTime(Now, Context.TimeProvider.LocalTimeZone).DateTime;
            if (!Schedule.Allows(user.Groups, local))
            {
                return Deny(user, "outside-time", device, countFailure: false);
            }

            State.Failures = 0;
            State.LastUser = user.Name;
            State.LastTime = Now;
            State.LastCause = byCode ? "SURVEILLANCE.ACCESS.CAUSE.CODE" : "SURVEILLANCE.ACCESS.CAUSE.TAG";
            AddLog(byCode ? "granted-code" : "granted-tag", Describe(user.Name, device));
            PulseOutput(F.OutGranted);
            OpenDoor(null, null);
            return true;
        }

        private static string Describe(string user, string device) =>
            string.IsNullOrEmpty(device) ? user : string.IsNullOrEmpty(user) ? device : $"{user} ({device})";

        private bool Deny(AccessUser user, string reason, string device, bool countFailure = true)
        {
            _firstFactor = null;
            State.LastUser = user?.Name ?? "";
            State.LastTime = Now;
            State.LastCause = "SURVEILLANCE.ACCESS.CAUSE." + reason.ToUpperInvariant().Replace('-', '_');
            AddLog("denied-" + reason, Describe(user?.Name, device));
            PulseOutput(F.OutDenied);

            if (countFailure)
            {
                State.Failures++;
                var max = ParamInt(F.ParamMaxFailures, 5);
                if (max > 0 && State.Failures >= max)
                {
                    State.Failures = 0;
                    State.LockoutUntil = Now + ParamSeconds(F.ParamLockout, 300);
                    AddLog("lockout", device);
                    Notify("SURVEILLANCE.NOTIFY.ACCESS_LOCKOUT", device, NotificationSeverity.Warning);
                }
            }
            UpdateOutputs();
            return true;
        }

        private void SetCause(string cause)
        {
            State.LastCause = cause;
            UpdateOutputs();
        }

        private void OpenDoor(string who, string logEvent)
        {
            if (logEvent != null)
            {
                State.LastUser = who ?? "";
                State.LastTime = Now;
                State.LastCause = "SURVEILLANCE.ACCESS.CAUSE.OPEN";
                AddLog(logEvent, who);
            }
            _doorUntil = Now + ParamSeconds(F.ParamDoorOn, 3);
            UpdateOutputs();
        }

        protected override bool OnTick(DateTimeOffset now)
        {
            var changed = false;
            if (_doorUntil is { } door && now >= door)
            {
                _doorUntil = null;
            }
            if (State.LockoutUntil is { } lockout && now >= lockout)
            {
                State.LockoutUntil = null;
                changed = true;
            }
            if (State.LearnUntil is { } learn && now >= learn)
            {
                AddLog("tag-learn-timeout", State.LearnUser);
                State.LearnUser = null;
                State.LearnUntil = null;
                changed = true;
            }
            if (_firstFactor is { } first && now - first.Time > ParamSeconds(F.ParamTwoFactorWindow, 30))
            {
                _firstFactor = null;
                State.LastCause = "SURVEILLANCE.ACCESS.CAUSE.TIMEOUT";
            }
            UpdateOutputs();
            return changed;
        }

        private void UpdateOutputs(bool force = false)
        {
            SetOutput(F.OutDoor, _doorUntil.HasValue, force);
            SetOutput(F.OutLocked, IsLocked || State.LockoutUntil.HasValue, force);
            SetOutput(F.OutUser, State.LastUser ?? "", force);
            SetOutput(F.OutTime, State.LastTime?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "", force);
            SetOutput(F.OutCause, Translate(State.LastCause), force);
            if (force)
            {
                SetOutput(F.OutGranted, false, true);
                SetOutput(F.OutDenied, false, true);
            }
        }

        /// <summary>
        /// The cause output is text for displays: translated in the server language (the key if there is none)
        /// </summary>
        private string Translate(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return "";
            }
            try
            {
                var text = Context.LocalizationProvider?.GetTranslation(key);
                return string.IsNullOrEmpty(text) ? key : text;
            }
            catch (Exception)
            {
                return key;
            }
        }

        private AccessUser FindUser(string name) =>
            State.Users.FirstOrDefault(a => string.Equals(a.Name, name?.Trim(), StringComparison.OrdinalIgnoreCase));

        protected override SecurityCommandResult OnCommand(string command, IReadOnlyDictionary<string, string> arguments, SecurityCommandContext context)
        {
            if (AdminCommands.Contains(command) && !context.IsAdmin)
            {
                return SecurityCommandResult.Denied;
            }

            string Arg(string key) => arguments != null && arguments.TryGetValue(key, out var v) ? v : null;
            var result = SecurityCommandResult.Ok;

            switch (command)
            {
                case CmdOpen:
                    if (IsLocked)
                    {
                        result = SecurityCommandResult.NotPossible;
                        break;
                    }
                    OpenDoor(context.UserName, "open-visu");
                    break;
                case CmdLock:
                case CmdUnlock:
                    var locked = command == CmdLock;
                    if (State.Locked == locked && (locked || !State.LockoutUntil.HasValue))
                    {
                        result = SecurityCommandResult.NotPossible;
                        break;
                    }
                    State.Locked = locked;
                    if (!locked)
                    {
                        State.LockoutUntil = null;
                        State.Failures = 0;
                    }
                    AddLog(locked ? "locked" : "unlocked", context.UserName);
                    break;
                case CmdUserSave:
                    result = SaveUser(Arg("name"), Arg("groups"), Arg("code"), Arg("tag"), Arg("clearCode") == "true", Arg("clearTag") == "true", Arg("previousName"), context.UserName);
                    break;
                case CmdUserDelete:
                    var user = FindUser(Arg("name"));
                    if (user == null)
                    {
                        result = SecurityCommandResult.NotPossible;
                        break;
                    }
                    State.Users.Remove(user);
                    AddLog("user-deleted", $"{user.Name} ({context.UserName})");
                    break;
                case CmdLearnTag:
                    var learner = FindUser(Arg("name"));
                    if (learner == null)
                    {
                        result = SecurityCommandResult.NotPossible;
                        break;
                    }
                    State.LearnUser = learner.Name;
                    State.LearnUntil = Now + LearnTime;
                    AddLog("tag-learn-start", learner.Name);
                    break;
                case CmdLearnCancel:
                    if (State.LearnUser == null)
                    {
                        result = SecurityCommandResult.NotPossible;
                        break;
                    }
                    State.LearnUser = null;
                    State.LearnUntil = null;
                    break;
                default:
                    result = SecurityCommandResult.UnknownCommand;
                    break;
            }

            UpdateOutputs();
            return result;
        }

        /// <summary>
        /// Creates or updates a user. The code and tag are only replaced when given; the result says nothing about why a
        /// code was rejected beyond "not possible" (the visualization validates the format before).
        /// </summary>
        private SecurityCommandResult SaveUser(string name, string groups, string code, string tag, bool clearCode, bool clearTag, string previousName, string admin)
        {
            name = name?.Trim() ?? "";
            if (name.Length is 0 or > 40)
            {
                return SecurityCommandResult.NotPossible;
            }

            var user = FindUser(string.IsNullOrWhiteSpace(previousName) ? name : previousName);
            var other = FindUser(name);
            if (other != null && other != user)
            {
                return SecurityCommandResult.NotPossible;
            }

            if (!string.IsNullOrEmpty(code))
            {
                if (!Regex.IsMatch(code, $"^[0-9]{{{MinCodeLength},{MaxCodeLength}}}$") ||
                    State.Users.Any(a => a != user && a.Code != null && a.Code.Matches(code)))
                {
                    return SecurityCommandResult.NotPossible;
                }
            }
            if (!string.IsNullOrEmpty(tag) && State.Users.Any(a => a != user && a.Tag != null && a.Tag.Matches(tag)))
            {
                return SecurityCommandResult.NotPossible;
            }

            var created = user == null;
            user ??= new AccessUser();
            user.Name = name;
            user.Groups = (groups ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            if (clearCode) user.Code = null;
            if (clearTag) user.Tag = null;
            if (!string.IsNullOrEmpty(code)) user.Code = AccessSecret.Create(code);
            if (!string.IsNullOrEmpty(tag)) user.Tag = AccessSecret.Create(tag.Trim());
            if (created)
            {
                State.Users.Add(user);
            }
            AddLog(created ? "user-created" : "user-changed", $"{name} ({admin})");
            return SecurityCommandResult.Ok;
        }

        protected override object BuildState(bool includeAdminData)
        {
            var lockout = State.LockoutUntil is { } until && Now < until;
            var actions = new List<object>();
            if (!IsLocked)
            {
                actions.Add(BurglarAlarmLogic.Action(CmdOpen, "SURVEILLANCE.VISU.OPEN_DOOR", "primary", true));
                actions.Add(BurglarAlarmLogic.Action(CmdLock, "SURVEILLANCE.VISU.LOCK_ACCESS", "default", true));
            }
            if (State.Locked || lockout)
            {
                actions.Add(BurglarAlarmLogic.Action(CmdUnlock, "SURVEILLANCE.VISU.UNLOCK_ACCESS", "default", true));
            }

            var details = new List<object>();
            if (!string.IsNullOrEmpty(State.LastCause))
            {
                details.Add(new { label = "SURVEILLANCE.VISU.DETAIL.LAST_ACCESS", value = State.LastCause, translate = true });
            }
            if (!string.IsNullOrEmpty(State.LastUser))
            {
                details.Add(new { label = "SURVEILLANCE.VISU.DETAIL.USER", value = State.LastUser, translate = false });
            }

            var schedule = Schedule;
            return new
            {
                status = IsLocked ? "locked" : lockout ? "lockout" : _doorUntil.HasValue ? "open" : "ready",
                statusText = IsLocked ? "SURVEILLANCE.VISU.STATUS.ACCESS_LOCKED" : lockout ? "SURVEILLANCE.VISU.STATUS.LOCKOUT"
                    : _doorUntil.HasValue ? "SURVEILLANCE.VISU.STATUS.DOOR_OPEN" : "SURVEILLANCE.VISU.STATUS.READY",
                severity = IsLocked || lockout ? "warning" : "ok",
                icon = IsLocked || lockout ? "lock" : _doorUntil.HasValue ? "door-open" : "door-closed",
                countdownUntil = lockout ? State.LockoutUntil : null,
                details,
                actions,
                log = LogForVisu(),
                // users only for administrators, never the hashes
                users = includeAdminData
                    ? State.Users.OrderBy(a => a.Name).Select(a => new { name = a.Name, groups = a.Groups, hasCode = a.Code != null, hasTag = a.Tag != null }).ToList()
                    : null,
                groups = includeAdminData ? schedule.Groups.Keys.OrderBy(a => a).ToList() : null,
                scheduleErrors = includeAdminData ? schedule.Errors : null,
                learn = includeAdminData && State.LearnUser != null ? new { name = State.LearnUser, until = State.LearnUntil } : null,
                authMode = AuthMode,
                minCodeLength = MinCodeLength
            };
        }
    }
}
