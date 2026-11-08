using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Logic;
using Microsoft.Extensions.Logging;
using P3.Logic.Surveillance.Security;
using F = P3.Logic.Surveillance.Sia.SiaLogicFactory;

namespace P3.Logic.Surveillance.Sia
{
    public class SiaQueuedEvent
    {
        /// <summary>
        /// The message as configured ("FA01^Kitchen"), without the "N"
        /// </summary>
        public string Message { get; set; }
        public int Sequence { get; set; }
        public DateTimeOffset Created { get; set; }
        public int Attempts { get; set; }
    }

    public class SiaState : SecurityStateBase
    {
        public int Sequence { get; set; }
        public List<SiaQueuedEvent> Queue { get; set; } = new();
        public DateTimeOffset? LastPrimaryContact { get; set; }
        public DateTimeOffset? LastBackupContact { get; set; }
        public bool Ok { get; set; } = true;
        public string Error { get; set; }
        public string LastEvent { get; set; }
    }

    /// <summary>
    /// SIA DC-09 sender, see <see cref="SiaLogicFactory"/>. Events are queued (persisted) and sent one by one: to the
    /// primary receiver, on failure to the backup, otherwise retried with a growing pause. Each trigger input sends its
    /// "on" message on a rising edge or an analog change and its "off" message (restore) on a falling edge;
    /// &lt;v&gt; in a message is the input value. The "Msg" input sends its text as the whole message.
    /// </summary>
    public class SiaLogic : PersistentLogicBase<SiaState>
    {
        public const int MaxQueue = 100;

        private readonly ISiaTransport _transport;
        private readonly Dictionary<Guid, object> _lastValues = new();
        private readonly Dictionary<int, DateTimeOffset> _lastSent = new();
        private readonly Dictionary<int, string> _held = new();
        private bool _locked;
        private bool _sending;
        private DateTimeOffset _nextAttempt;
        private Task _send = Task.CompletedTask;

        public SiaLogic(ILogicContext context, ISecurityStateStore store, ISiaTransport transport) : base(context, store)
        {
            _transport = transport;
        }

        /// <summary>
        /// The transmission running now (tests wait for it)
        /// </summary>
        internal Task Sending
        {
            get
            {
                lock (Sync)
                {
                    return _send;
                }
            }
        }

        protected override bool OrderedOutputs => true;

        private SiaFormat Format => ParamInt(F.ParamFormat, 0) == 1 ? SiaFormat.ContactId : SiaFormat.SiaDcs;

        private SiaAccount Account => new(ParamString(F.ParamAccount).Trim(), ParamString(F.ParamPrefix).Trim(), ParamString(F.ParamReceiver).Trim());

        protected override void OnStarted(bool restored)
        {
            _nextAttempt = Now;
            UpdateOutputs(true);
        }

        protected override bool OnInput(Guid template, object value, IDispatchable source)
        {
            if (template == F.InOff)
            {
                _locked = ToBool(value);
                return false;
            }

            if (template == F.InMessage)
            {
                var text = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim();
                var previousText = _lastValues.TryGetValue(template, out var pt) ? pt as string : null;
                _lastValues[template] = text;
                if (_locked || InStartupGrace || string.IsNullOrEmpty(text) || text == previousText)
                {
                    return false;
                }
                return Enqueue(-1, text);
            }

            var index = Array.IndexOf(F.InTriggers, template);
            if (index < 0)
            {
                return false;
            }

            var previous = _lastValues.TryGetValue(template, out var p) ? p : null;
            _lastValues[template] = value;
            if (_locked || InStartupGrace)
            {
                // right after a start the values are only the baseline (replays of the last values)
                return false;
            }

            string message;
            if (value is bool || value is string s && (s == "0" || s == "1" || bool.TryParse(s, out _)))
            {
                if (ToBool(previous) == ToBool(value))
                {
                    return false;
                }
                message = ToBool(value) ? ParamString(F.ParamOnMessages[index]) : ParamString(F.ParamOffMessages[index]);
            }
            else
            {
                // analog: every change sends the on message, 0 the off message if there is one
                if (previous != null && ToDouble(previous, double.NaN).Equals(ToDouble(value, double.NaN)))
                {
                    return false;
                }
                var off = ParamString(F.ParamOffMessages[index]);
                message = ToDouble(value, 1) == 0 && !string.IsNullOrWhiteSpace(off) ? off : ParamString(F.ParamOnMessages[index]);
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            message = message.Replace("<v>", Convert.ToString(value is bool b ? (b ? 1 : 0) : value, CultureInfo.InvariantCulture));
            return Throttle(index, message);
        }

        private bool Throttle(int index, string message)
        {
            var min = ParamSeconds(F.ParamMinInterval, 5);
            if (min > TimeSpan.Zero && _lastSent.TryGetValue(index, out var last) && Now - last < min)
            {
                // the latest state is sent when the interval has passed
                _held[index] = message;
                return false;
            }

            _lastSent[index] = Now;
            return Enqueue(index, message);
        }

        private bool Enqueue(int index, string message)
        {
            try
            {
                SiaProtocol.EventData(Format, message);
            }
            catch (FormatException e)
            {
                AddLog("sia-invalid-message", e.Message);
                SetError(e.Message);
                return true;
            }

            State.Sequence = State.Sequence % 9999 + 1;
            State.Queue.Add(new SiaQueuedEvent { Message = message, Sequence = State.Sequence, Created = Now });
            if (State.Queue.Count > MaxQueue)
            {
                AddLog("sia-queue-full", State.Queue[0].Message);
                State.Queue.RemoveAt(0);
            }
            AddLog("sia-queued", message);
            _nextAttempt = Now;
            StartNext(Now);
            UpdateOutputs();
            return true;
        }

        protected override bool OnTick(DateTimeOffset now)
        {
            var changed = false;
            foreach (var (index, message) in _held.ToList())
            {
                if (!_lastSent.TryGetValue(index, out var last) || now - last >= ParamSeconds(F.ParamMinInterval, 5))
                {
                    _held.Remove(index);
                    _lastSent[index] = now;
                    changed |= Enqueue(index, message);
                }
            }

            StartNext(now);
            return changed;
        }

        private void StartNext(DateTimeOffset now)
        {
            if (_sending || !Started)
            {
                return;
            }

            var primary = SiaProtocol.ParseEndpoint(ParamString(F.ParamServer));
            var backup = SiaProtocol.ParseEndpoint(ParamString(F.ParamBackupServer));
            var key = SiaProtocol.ParseKey(ParamString(F.ParamKey));
            var account = Account;
            if (primary == null || key == null || !Regex.IsMatch(account.Account, "^[0-9A-Fa-f]{3,16}$"))
            {
                if (State.Queue.Count > 0)
                {
                    SetError(primary == null ? "no server configured" : key == null ? "invalid encryption key" : "invalid account number");
                }
                return;
            }

            SiaQueuedEvent next = null;
            var pollBackup = false;
            if (State.Queue.Count > 0)
            {
                if (now < _nextAttempt)
                {
                    return;
                }
                next = State.Queue[0];
                next.Attempts++;
            }
            else
            {
                var polling = ParamSeconds(F.ParamPolling, 90);
                var backupPolling = ParamSeconds(F.ParamBackupPolling, 1800);
                var primaryDue = polling > TimeSpan.Zero && (State.LastPrimaryContact == null || now - State.LastPrimaryContact >= polling);
                pollBackup = !primaryDue && backup != null && backupPolling > TimeSpan.Zero &&
                             (State.LastBackupContact == null || now - State.LastBackupContact >= backupPolling);
                if (!primaryDue && !pollBackup)
                {
                    return;
                }
                if (now < _nextAttempt)
                {
                    return;
                }
            }

            int sequence;
            if (next != null)
            {
                sequence = next.Sequence;
            }
            else
            {
                State.Sequence = State.Sequence % 9999 + 1;
                sequence = State.Sequence;
            }

            var format = Format;
            var data = next == null ? null : SiaProtocol.EventData(format, next.Message);
            var frame = SiaProtocol.BuildFrame(format, account, sequence, data, now, key);
            var timeout = TimeSpan.FromSeconds(Math.Clamp(ParamDouble(F.ParamTimeout, 3), 1, 10));
            var targets = pollBackup ? new[] { backup.Value } : backup == null ? new[] { primary.Value } : new[] { primary.Value, backup.Value };

            _sending = true;
            _send = Task.Run(() => Transmit(next, frame, sequence, key, timeout, targets, pollBackup));
        }

        private async Task Transmit(SiaQueuedEvent item, string frame, int sequence, byte[] key, TimeSpan timeout, (string Host, int Port)[] targets, bool backupPoll)
        {
            string error = null;
            var acceptedBy = -1;
            for (var i = 0; i < targets.Length && acceptedBy < 0; i++)
            {
                string answer;
                try
                {
                    answer = await _transport.SendAsync(targets[i].Host, targets[i].Port, frame, timeout).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Context.Logger.LogWarning(e, $"SIA transmission of {Name} failed");
                    answer = null;
                }

                var response = SiaProtocol.ParseResponse(answer, key);
                if (response.Kind == SiaResponseKind.Ack && response.Sequence == sequence)
                {
                    acceptedBy = i;
                }
                else
                {
                    error = response.Kind switch
                    {
                        SiaResponseKind.Ack => "acknowledged with a wrong sequence",
                        SiaResponseKind.Nak => "rejected by the receiver (NAK)",
                        SiaResponseKind.Duh => "receiver does not understand the message (DUH)",
                        _ => answer == null ? "receiver not reachable" : response.Detail
                    };
                }
            }

            lock (Sync)
            {
                _sending = false;
                if (!Started)
                {
                    return;
                }

                var now = Now;
                if (acceptedBy >= 0)
                {
                    if (backupPoll || targets.Length > 1 && acceptedBy == 1)
                    {
                        State.LastBackupContact = now;
                    }
                    else
                    {
                        State.LastPrimaryContact = now;
                    }

                    if (item != null)
                    {
                        State.Queue.Remove(item);
                        State.LastEvent = item.Message;
                        AddLog(acceptedBy == 0 && !backupPoll ? "sia-sent" : "sia-sent-backup", item.Message);
                    }
                    if (!State.Ok)
                    {
                        AddLog("sia-restored");
                    }
                    State.Ok = true;
                    State.Error = null;
                    _nextAttempt = now;
                }
                else
                {
                    if (State.Ok)
                    {
                        AddLog("sia-failed", error);
                        Notify("SURVEILLANCE.NOTIFY.SIA_FAILED", error, Automatica.Core.EF.Models.NotificationSeverity.Error);
                    }
                    SetError(error);
                    var attempts = item?.Attempts ?? 1;
                    _nextAttempt = now + TimeSpan.FromSeconds(Math.Min(10 * attempts, 60));
                    if (item == null)
                    {
                        // a failed poll is retried after the pause, not at once
                        if (backupPoll)
                        {
                            State.LastBackupContact = now - ParamSeconds(F.ParamBackupPolling, 1800) + TimeSpan.FromSeconds(10);
                        }
                    }
                }

                UpdateOutputs();
                Persist();
            }
            Flush();
        }

        private void SetError(string error)
        {
            State.Ok = false;
            State.Error = error;
            UpdateOutputs();
        }

        private void UpdateOutputs(bool force = false)
        {
            SetOutput(F.OutOk, State.Ok, force);
            SetOutput(F.OutError, State.Error ?? "", force);
            SetOutput(F.OutQueue, State.Queue.Count, force);
            SetOutput(F.OutLastEvent, State.LastEvent ?? "", force);
        }

        protected override object BuildState(bool includeAdminData) => new
        {
            ok = State.Ok,
            error = State.Error ?? "",
            queue = State.Queue.Count,
            lastEvent = State.LastEvent ?? "",
            lastContact = State.LastPrimaryContact,
            log = LogForVisu()
        };
    }
}
