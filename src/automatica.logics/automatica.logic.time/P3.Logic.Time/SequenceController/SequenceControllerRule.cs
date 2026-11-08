using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Microsoft.Extensions.Logging;

namespace P3.Logic.Time.SequenceController
{
    /// <summary>
    /// Runs the active sequence one step per interval. A step executes one line; empty lines, comments, invalid lines,
    /// else and endif need no step. sleep and waitcondition stay on their line until the time is over / the condition is met.
    /// S1..S8 (any value other than 0) or S = n start sequence n from line 1, S = 0 stops. Off = 1 stops, sets AQ1..AQ8 to 0,
    /// clears TQ and locks the block until Off is 0. A sequence that runs past its last line ends (S and L go to 0); a
    /// sequence started with startsequence returns to the caller at its end or with return.
    /// Values written into AI1..AI8 by the program stay until the linked input sends a new value.
    /// </summary>
    public class SequenceControllerRule : Automatica.Core.Logic.Logic
    {
        /// <summary>
        /// Upper bound of the lines a single step may skip (free lines, jumps), protects against "goto" loops without a step
        /// </summary>
        private const int MaxFreeLinesPerStep = 10_000;

        private readonly object _sync = new();
        private readonly RuleInterfaceInstance[] _outputs;
        private readonly RuleInterfaceInstance _activeOutput;
        private readonly RuleInterfaceInstance _lineOutput;
        private readonly RuleInterfaceInstance _textOutput;
        private readonly RuleInterfaceInstance _errorsOutput;
        private readonly Stack<(int Sequence, int Line)> _calls = new();
        private readonly HashSet<int> _pulses = new();

        private System.Timers.Timer _timer;
        private SequenceProgram _program = SequenceProgram.Empty;
        private double[] _values = new double[SequenceProgram.Empty.SlotCount];
        private readonly double?[] _sentOutputs = new double?[SequenceProgram.IoCount];
        private int _interval = SequenceControllerLogicFactory.DefaultIntervalMs;
        private int _active = -1;
        private int _line;
        private DateTimeOffset? _sleepUntil;
        private bool _locked;
        private bool _started;
        private string _text = "";
        private int _sentActive;
        private int _sentLine;

        public SequenceControllerRule(ILogicContext context) : base(context)
        {
            RuleInterfaceInstance Find(Guid template) => context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == template);

            _outputs = SequenceControllerLogicFactory.RuleOutputs.Select(Find).ToArray();
            _activeOutput = Find(SequenceControllerLogicFactory.RuleActiveSequence);
            _lineOutput = Find(SequenceControllerLogicFactory.RuleLine);
            _textOutput = Find(SequenceControllerLogicFactory.RuleText);
            _errorsOutput = Find(SequenceControllerLogicFactory.RuleErrors);
        }

        internal SequenceProgram Program => _program;
        internal int ActiveSequence => _active + 1;
        internal int CurrentLine => _active < 0 ? 0 : _line + 1;
        internal int Interval => _interval;
        internal bool Locked => _locked;
        internal double Value(string name) => _program.Resolve(name) is { } slot ? _values[slot] : double.NaN;

        protected override Task<bool> Start(RuleInstance instance, CancellationToken token = default)
        {
            lock (_sync)
            {
                if (_timer == null)
                {
                    _timer = new System.Timers.Timer { AutoReset = true, Interval = _interval };
                    _timer.Elapsed += _timer_Elapsed;
                }
                _started = true;
            }
            return base.Start(instance, token);
        }

        protected override Task<bool> Stop(RuleInstance instance, CancellationToken token = default)
        {
            lock (_sync)
            {
                _started = false;
                _active = -1;
                if (_timer != null)
                {
                    _timer.Elapsed -= _timer_Elapsed;
                    _timer.Dispose();
                    _timer = null;
                }
            }
            return base.Stop(instance, token);
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == SequenceControllerLogicFactory.RuleParamProgram)
            {
                LoadProgram(value?.ToString());
            }
            else if (instance.This2RuleInterfaceTemplate == SequenceControllerLogicFactory.RuleParamInterval)
            {
                SetInterval(value);
            }

            base.ParameterValueChanged(instance, source, value);
        }

        private void SetInterval(object value)
        {
            int interval;
            try
            {
                interval = value is string s
                    ? (int)double.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture)
                    : Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                interval = SequenceControllerLogicFactory.DefaultIntervalMs;
            }

            lock (_sync)
            {
                _interval = Math.Clamp(interval, SequenceControllerLogicFactory.MinIntervalMs, SequenceControllerLogicFactory.MaxIntervalMs);
                if (_timer != null)
                {
                    _timer.Interval = _interval;
                }
            }
        }

        private void LoadProgram(string text)
        {
            var program = SequenceProgram.Parse(text);
            foreach (var error in program.Errors)
            {
                Context.Logger.LogWarning($"{Context.RuleInstance.Name}: {error}");
            }

            lock (_sync)
            {
                // a new program stops the running sequence, the values of inputs, outputs and same-named variables stay
                StopSequence();
                var values = new double[program.SlotCount];
                Array.Copy(_values, values, SequenceProgram.FirstVariableSlot);
                for (var i = 0; i < program.Variables.Count; i++)
                {
                    if (_program.Resolve(program.Variables[i]) is { } old)
                    {
                        values[SequenceProgram.FirstVariableSlot + i] = _values[old];
                    }
                }
                _values = values;
                _program = program;
            }

            Send(_errorsOutput, string.Join("\n", program.Errors));
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            var template = instance.This2RuleInterfaceTemplate;

            var input = SequenceControllerLogicFactory.IndexOf(SequenceControllerLogicFactory.RuleInputs, template);
            if (input >= 0)
            {
                lock (_sync)
                {
                    _values[SequenceProgram.FirstInputSlot + input] = ToDouble(value);
                }
                return new List<ILogicOutputChanged>();
            }

            var start = SequenceControllerLogicFactory.IndexOf(SequenceControllerLogicFactory.RuleStartSequence, template);
            if (start >= 0)
            {
                if (TimerHelper.IsTruthy(value))
                {
                    StartSequence(start);
                }
                return new List<ILogicOutputChanged>();
            }

            if (template == SequenceControllerLogicFactory.RuleSelect)
            {
                var number = (int)Math.Round(ToDouble(value));
                if (number <= 0)
                {
                    lock (_sync)
                    {
                        StopSequence();
                    }
                }
                else
                {
                    StartSequence(number - 1);
                }
            }
            else if (template == SequenceControllerLogicFactory.RuleOff)
            {
                SetOff(TimerHelper.IsTruthy(value));
            }

            return new List<ILogicOutputChanged>();
        }

        private void SetOff(bool off)
        {
            lock (_sync)
            {
                _locked = off;
                if (!off)
                {
                    return;
                }

                StopSequence();
                _pulses.Clear();
                for (var i = 0; i < SequenceProgram.IoCount; i++)
                {
                    _values[SequenceProgram.FirstOutputSlot + i] = 0;
                    SendOutput(i);
                }
                SetText("");
            }
        }

        /// <summary>
        /// Starts the sequence (index 0..7) from its first line and runs the first step right away
        /// </summary>
        internal void StartSequence(int index)
        {
            lock (_sync)
            {
                if (_locked || !_started || index < 0 || index >= _program.Sequences.Count)
                {
                    return;
                }

                _calls.Clear();
                Enter(index, 0);
                _timer?.Stop();
                _timer?.Start();
                StepLocked();
            }
        }

        private void Enter(int sequence, int line)
        {
            _active = sequence;
            _line = line;
            _sleepUntil = null;
        }

        private void StopSequence()
        {
            _active = -1;
            _line = 0;
            _sleepUntil = null;
            _calls.Clear();
            _timer?.Stop();
            SendPosition();
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_sync)
            {
                if (!_started)
                {
                    return;
                }
                StepLocked();
            }
        }

        /// <summary>
        /// One step of the active sequence
        /// </summary>
        private void StepLocked()
        {
            // pulses of the previous step end now
            if (_pulses.Count > 0)
            {
                foreach (var slot in _pulses)
                {
                    _values[slot] = 0;
                    if (slot >= SequenceProgram.FirstOutputSlot && slot < SequenceProgram.FirstVariableSlot)
                    {
                        SendOutput(slot - SequenceProgram.FirstOutputSlot);
                    }
                }
                _pulses.Clear();
            }

            if (_active < 0)
            {
                _timer?.Stop();
                return;
            }

            var now = Context.TimeProvider.GetUtcNow();
            if (_sleepUntil != null)
            {
                if (now < _sleepUntil.Value)
                {
                    return;
                }
                // the sleep is over: continue with the next line in this step
                _sleepUntil = null;
                _line++;
            }

            for (var guard = 0; guard < MaxFreeLinesPerStep; guard++)
            {
                var lines = _program.Sequences[_active].Lines;
                if (_line >= lines.Count)
                {
                    if (_calls.Count > 0)
                    {
                        var (sequence, line) = _calls.Pop();
                        Enter(sequence, line);
                        continue;
                    }
                    StopSequence();
                    return;
                }

                var current = lines[_line];
                switch (current.Command)
                {
                    case SequenceCommand.None:
                    case SequenceCommand.EndIf:
                        _line++;
                        continue;
                    case SequenceCommand.Else:
                        // end of the "then" part
                        _line = current.Target + 1;
                        continue;
                }

                Execute(current, now);
                SendPosition();
                return;
            }

            Context.Logger.LogWarning($"{Context.RuleInstance.Name}: sequence {_active + 1} skipped {MaxFreeLinesPerStep} lines without a step, stopped");
            StopSequence();
        }

        private void Execute(SequenceLine line, DateTimeOffset now)
        {
            switch (line.Command)
            {
                case SequenceCommand.Set:
                    Write(line.Slot, Evaluate(line.Expression));
                    _line++;
                    break;
                case SequenceCommand.SetPulse:
                    Write(line.Slot, Evaluate(line.Expression), force: true);
                    _pulses.Add(line.Slot);
                    _line++;
                    break;
                case SequenceCommand.SetText:
                    SetText(Format(line.TextParts));
                    _line++;
                    break;
                case SequenceCommand.Sleep:
                {
                    var ms = Evaluate(line.Expression);
                    if (double.IsNaN(ms) || ms <= 0)
                    {
                        _line++;
                        break;
                    }
                    // stays on the sleep line (L shows it) until the time is over
                    _sleepUntil = now + TimeSpan.FromMilliseconds(Math.Min(ms, TimeSpan.FromDays(365).TotalMilliseconds));
                    break;
                }
                case SequenceCommand.Wait:
                    if (Evaluate(line.Expression) != 0)
                    {
                        _line++;
                    }
                    break;
                case SequenceCommand.StartSequence:
                    if (_calls.Count >= 64)
                    {
                        Context.Logger.LogWarning($"{Context.RuleInstance.Name}: more than 64 nested startsequence calls, stopped");
                        StopSequence();
                        return;
                    }
                    _calls.Push((_active, _line + 1));
                    Enter(line.Target, 0);
                    break;
                case SequenceCommand.Return:
                    if (_calls.Count > 0)
                    {
                        var (sequence, next) = _calls.Pop();
                        Enter(sequence, next);
                    }
                    else
                    {
                        _line = int.MaxValue;
                        StopSequence();
                    }
                    break;
                case SequenceCommand.Goto:
                    _line = line.Target - 1;
                    break;
                case SequenceCommand.If:
                    if (Evaluate(line.Expression) != 0)
                    {
                        _line++;
                    }
                    else
                    {
                        _line = (line.ElseIndex >= 0 ? line.ElseIndex : line.Target) + 1;
                    }
                    break;
            }
        }

        private double Evaluate(Func<double[], double> expression)
        {
            return expression(_values);
        }

        private void Write(int slot, double value, bool force = false)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                Context.Logger.LogWarning($"{Context.RuleInstance.Name}: sequence {_active + 1} line {_line + 1} is not a number, ignored");
                return;
            }

            _values[slot] = value;
            if (slot >= SequenceProgram.FirstOutputSlot && slot < SequenceProgram.FirstVariableSlot)
            {
                SendOutput(slot - SequenceProgram.FirstOutputSlot, force);
            }
        }

        private void SendOutput(int index, bool force = false)
        {
            var value = _values[SequenceProgram.FirstOutputSlot + index];
            if (!force && _sentOutputs[index] == value)
            {
                return;
            }
            _sentOutputs[index] = value;
            Send(_outputs[index], value);
        }

        private void SendPosition()
        {
            var active = _active + 1;
            var line = _active < 0 ? 0 : _line + 1;
            if (active != _sentActive)
            {
                _sentActive = active;
                Send(_activeOutput, active);
            }
            if (line != _sentLine)
            {
                _sentLine = line;
                Send(_lineOutput, line);
            }
        }

        private void SetText(string text)
        {
            if (text == _text)
            {
                return;
            }
            _text = text;
            Send(_textOutput, text);
        }

        private void Send(RuleInterfaceInstance output, object value)
        {
            if (output != null)
            {
                Context.Dispatcher.DispatchValue(new LogicOutputChanged(output, value).Instance, value);
            }
        }

        private string Format(IReadOnlyList<TextPart> parts)
        {
            var builder = new StringBuilder();
            foreach (var part in parts)
            {
                builder.Append(part.Text ?? FormatNumber(part.Value(_values)));
            }
            return builder.ToString();
        }

        internal static string FormatNumber(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return "-";
            }
            return Math.Round(value, 6).ToString("0.######", CultureInfo.InvariantCulture);
        }

        internal static double ToDouble(object value)
        {
            switch (value)
            {
                case null:
                    return 0;
                case bool b:
                    return b ? 1 : 0;
                case string s:
                    if (bool.TryParse(s, out var sb))
                    {
                        return sb ? 1 : 0;
                    }
                    return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var sd) ? sd : 0;
                case IConvertible c:
                    try
                    {
                        return c.ToDouble(CultureInfo.InvariantCulture);
                    }
                    catch (Exception)
                    {
                        return 0;
                    }
                default:
                    return 0;
            }
        }
    }
}
