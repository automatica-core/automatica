using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace P3.Logic.Time.SequenceController
{
    /// <summary>
    /// A parsed sequence controller program (Loxone "Sequence Controller"). One text holds up to
    /// <see cref="MaxSequences"/> sequences, each starts with a header line <c>sequence &lt;name&gt;</c>; lines before the
    /// first header are sequence 1. Line numbers (goto, output L) count the lines of a sequence from 1, the header excluded.
    /// <code>
    /// sequence Heat
    /// set AQ1 = 1                     // set an output, input (AI) or variable to an expression
    /// waitcondition AI1 >= AI2 - 0.5  // wait until the condition is met
    /// setpulse AQ2                    // 1 now, 0 with the next step (setpulse AQ2 = 4 pulses 4)
    /// sleep 10 m                      // wait: ms, s (default), m, h
    /// if AI3 > 60
    ///   set TQ = "Tank hot: " AI3     // text output, quoted text and values
    /// else
    ///   startsequence Alarm           // call another sequence (by number or name), it returns with "return"
    /// endif
    /// goto 1                          // jump to line 1 of this sequence
    /// </code>
    /// Variables are created by assigning them (set level = 0); value1..value5 always exist. Invalid lines are reported
    /// in <see cref="Errors"/> and skipped when the sequence runs.
    /// </summary>
    internal sealed class SequenceProgram
    {
        public const int MaxSequences = 8;
        public const int IoCount = 8;
        public const int MaxLines = 1000;

        /// <summary>
        /// Value slots: AI1..AI8, AQ1..AQ8, then the variables
        /// </summary>
        public const int FirstInputSlot = 0;
        public const int FirstOutputSlot = IoCount;
        public const int FirstVariableSlot = 2 * IoCount;

        private static readonly string[] DefaultVariables = { "value1", "value2", "value3", "value4", "value5" };
        private static readonly Regex NameRegex = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);
        // AI/AQ with any number are IO names, never variables (AQ9 is an error, not a new variable)
        private static readonly Regex IoNameRegex = new("^(ai|aq)[0-9]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
        {
            "tq", "pi", "e", "true", "false", "and", "or", "not", "sequence", "set", "setpulse", "sleep", "waitcondition",
            "startsequence", "return", "goto", "if", "else", "endif"
        };

        private SequenceProgram(IReadOnlyList<Sequence> sequences, IReadOnlyList<string> variables, IReadOnlyList<string> errors)
        {
            Sequences = sequences;
            Variables = variables;
            Errors = errors;
        }

        public IReadOnlyList<Sequence> Sequences { get; }

        /// <summary>
        /// Variable names (lower case), variable i has the slot <see cref="FirstVariableSlot"/> + i
        /// </summary>
        public IReadOnlyList<string> Variables { get; }

        public IReadOnlyList<string> Errors { get; }

        public int SlotCount => FirstVariableSlot + Variables.Count;

        public int? FindSequence(string nameOrNumber)
        {
            if (int.TryParse(nameOrNumber, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
            {
                return number >= 1 && number <= Sequences.Count ? number - 1 : null;
            }
            for (var i = 0; i < Sequences.Count; i++)
            {
                if (string.Equals(Sequences[i].Name, nameOrNumber, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return null;
        }

        public static SequenceProgram Empty { get; } = new(Array.Empty<Sequence>(), DefaultVariables, Array.Empty<string>());

        public static SequenceProgram Parse(string text)
        {
            var errors = new List<string>();
            var rawLines = (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            if (rawLines.Length > MaxLines)
            {
                errors.Add($"The program has more than {MaxLines} lines, the rest is ignored");
                rawLines = rawLines.Take(MaxLines).ToArray();
            }

            // pass 1: split into sequences, collect the variables (every assignment target that is not an IO)
            var blocks = new List<(string Name, List<(int SourceLine, string Text)> Lines)>();
            var variables = new List<string>(DefaultVariables);
            for (var i = 0; i < rawLines.Length; i++)
            {
                var code = StripComment(rawLines[i]).Trim();
                var (keyword, rest) = SplitKeyword(code);

                if (keyword == "sequence")
                {
                    var name = rest.Trim();
                    if (blocks.Count >= MaxSequences)
                    {
                        errors.Add($"Line {i + 1}: at most {MaxSequences} sequences, \"{name}\" is ignored");
                        // the lines stay in the previous sequence's numbering but are never reached
                        blocks.Add((null, new List<(int, string)>()));
                        continue;
                    }
                    if (name.Length > 0 && !NameRegex.IsMatch(name))
                    {
                        errors.Add($"Line {i + 1}: sequence name \"{name}\" may only contain letters, digits and _");
                        name = "";
                    }
                    blocks.Add((name.Length == 0 ? (blocks.Count + 1).ToString(CultureInfo.InvariantCulture) : name, new List<(int, string)>()));
                    continue;
                }

                if (blocks.Count == 0)
                {
                    if (code.Length == 0)
                    {
                        // empty lines and comments before the first header do not create sequence 1
                        continue;
                    }
                    blocks.Add(("1", new List<(int, string)>()));
                }
                blocks[^1].Lines.Add((i + 1, rawLines[i]));

                if ((keyword == "set" || keyword == "setpulse"))
                {
                    var target = rest.Split('=')[0].Trim();
                    if (NameRegex.IsMatch(target) && !IoNameRegex.IsMatch(target) && !Reserved.Contains(target) &&
                        !variables.Contains(target.ToLowerInvariant()))
                    {
                        variables.Add(target.ToLowerInvariant());
                    }
                }
            }

            var program = new SequenceProgram(new List<Sequence>(), variables, errors);
            var sequences = (List<Sequence>)program.Sequences;
            foreach (var block in blocks.Where(b => b.Name != null))
            {
                sequences.Add(new Sequence(block.Name, sequences.Count + 1));
            }

            // pass 2: parse the lines
            var sequenceIndex = 0;
            foreach (var block in blocks)
            {
                if (block.Name == null)
                {
                    continue;
                }
                var sequence = sequences[sequenceIndex++];
                var open = new Stack<int>();
                foreach (var (sourceLine, raw) in block.Lines)
                {
                    SequenceLine line;
                    try
                    {
                        line = program.ParseLine(StripComment(raw).Trim(), sequence.Lines.Count, open, sequence);
                    }
                    catch (FormatException ex)
                    {
                        errors.Add($"Sequence {sequence.Name}, line {sequence.Lines.Count + 1} (\"{raw.Trim()}\"): {ex.Message}");
                        line = new SequenceLine(SequenceCommand.None, raw.Trim());
                    }
                    sequence.Lines.Add(line);
                }

                while (open.Count > 0)
                {
                    var index = open.Pop();
                    errors.Add($"Sequence {sequence.Name}, line {index + 1}: \"if\" without \"endif\"");
                    sequence.Lines[index] = new SequenceLine(SequenceCommand.None, sequence.Lines[index].Source);
                }

                for (var i = 0; i < sequence.Lines.Count; i++)
                {
                    var line = sequence.Lines[i];
                    if (line.Command == SequenceCommand.Goto && (line.Target < 1 || line.Target > sequence.Lines.Count))
                    {
                        errors.Add($"Sequence {sequence.Name}, line {i + 1}: goto {line.Target} is outside the sequence (1..{sequence.Lines.Count})");
                        sequence.Lines[i] = new SequenceLine(SequenceCommand.None, line.Source);
                    }
                }
            }

            return program;
        }

        private static string StripComment(string line)
        {
            // "//" outside of a quoted text starts a comment
            var inText = false;
            for (var i = 0; i < line.Length; i++)
            {
                if (line[i] == '"')
                {
                    inText = !inText;
                }
                else if (!inText && line[i] == '/' && i + 1 < line.Length && line[i + 1] == '/')
                {
                    return line.Substring(0, i);
                }
            }
            return line;
        }

        private static (string Keyword, string Args) SplitKeyword(string code)
        {
            var end = 0;
            while (end < code.Length && (char.IsLetterOrDigit(code[end]) || code[end] == '_'))
            {
                end++;
            }
            return (code.Substring(0, end).ToLowerInvariant(), code.Substring(end));
        }

        /// <summary>
        /// Slot of AI1..AI8 / AQ1..AQ8, null for other names
        /// </summary>
        public static int? IoSlot(string name)
        {
            if (name.Length is < 3 or > 3 || !char.IsDigit(name[2]))
            {
                return null;
            }
            var n = name[2] - '0';
            if (n < 1 || n > IoCount)
            {
                return null;
            }
            if (name.StartsWith("ai", StringComparison.OrdinalIgnoreCase))
            {
                return FirstInputSlot + n - 1;
            }
            if (name.StartsWith("aq", StringComparison.OrdinalIgnoreCase))
            {
                return FirstOutputSlot + n - 1;
            }
            return null;
        }

        public int? Resolve(string name)
        {
            var io = IoSlot(name);
            if (io != null)
            {
                return io;
            }
            var lower = name.ToLowerInvariant();
            for (var i = 0; i < Variables.Count; i++)
            {
                if (Variables[i] == lower)
                {
                    return FirstVariableSlot + i;
                }
            }
            return null;
        }

        private SequenceLine ParseLine(string code, int index, Stack<int> open, Sequence sequence)
        {
            if (code.Length == 0)
            {
                return new SequenceLine(SequenceCommand.None, code);
            }

            var (keyword, rest) = SplitKeyword(code);
            rest = rest.Trim();
            switch (keyword)
            {
                case "set":
                case "setpulse":
                    return ParseSet(keyword == "setpulse", rest, code);
                case "sleep":
                    return ParseSleep(rest, code);
                case "waitcondition":
                    return new SequenceLine(SequenceCommand.Wait, code) { Expression = SequenceExpressionParser.ParseComplete(rest, Resolve) };
                case "startsequence":
                {
                    var target = FindSequence(rest);
                    if (target == null)
                    {
                        throw new FormatException($"sequence \"{rest}\" does not exist");
                    }
                    return new SequenceLine(SequenceCommand.StartSequence, code) { Target = target.Value };
                }
                case "return":
                    ExpectNothing(rest, keyword);
                    return new SequenceLine(SequenceCommand.Return, code);
                case "goto":
                    if (!int.TryParse(rest, NumberStyles.Integer, CultureInfo.InvariantCulture, out var lineNumber))
                    {
                        throw new FormatException("goto expects a line number");
                    }
                    return new SequenceLine(SequenceCommand.Goto, code) { Target = lineNumber };
                case "if":
                    open.Push(index);
                    return new SequenceLine(SequenceCommand.If, code) { Expression = SequenceExpressionParser.ParseComplete(rest, Resolve), Target = -1 };
                case "else":
                {
                    ExpectNothing(rest, keyword);
                    if (open.Count == 0 || sequence.Lines[open.Peek()].ElseIndex >= 0)
                    {
                        throw new FormatException("\"else\" without \"if\"");
                    }
                    var ifLine = sequence.Lines[open.Peek()];
                    ifLine.ElseIndex = index;
                    return new SequenceLine(SequenceCommand.Else, code) { Target = -1 };
                }
                case "endif":
                {
                    ExpectNothing(rest, keyword);
                    if (open.Count == 0)
                    {
                        throw new FormatException("\"endif\" without \"if\"");
                    }
                    var ifLine = sequence.Lines[open.Pop()];
                    ifLine.Target = index;
                    if (ifLine.ElseIndex >= 0)
                    {
                        sequence.Lines[ifLine.ElseIndex].Target = index;
                    }
                    return new SequenceLine(SequenceCommand.EndIf, code);
                }
                default:
                    throw new FormatException($"unknown command \"{keyword}\"");
            }
        }

        private static void ExpectNothing(string rest, string keyword)
        {
            if (rest.Length > 0)
            {
                throw new FormatException($"\"{keyword}\" takes no arguments");
            }
        }

        private SequenceLine ParseSet(bool pulse, string rest, string code)
        {
            var eq = rest.IndexOf('=');
            var target = (eq < 0 ? rest : rest.Substring(0, eq)).Trim();
            var valueText = eq < 0 ? null : rest.Substring(eq + 1);

            if (string.Equals(target, "tq", StringComparison.OrdinalIgnoreCase))
            {
                if (pulse)
                {
                    throw new FormatException("setpulse does not work with TQ");
                }
                if (valueText == null)
                {
                    throw new FormatException("set TQ = ... expects a text");
                }
                return new SequenceLine(SequenceCommand.SetText, code) { TextParts = ParseText(valueText) };
            }

            var slot = Resolve(target);
            if (slot == null)
            {
                throw new FormatException($"\"{target}\" is not an input, output or variable");
            }

            if (valueText == null)
            {
                if (!pulse)
                {
                    throw new FormatException("set expects \"= value\"");
                }
                return new SequenceLine(SequenceCommand.SetPulse, code) { Slot = slot.Value, Expression = _ => 1 };
            }

            return new SequenceLine(pulse ? SequenceCommand.SetPulse : SequenceCommand.Set, code)
            {
                Slot = slot.Value,
                Expression = SequenceExpressionParser.ParseComplete(valueText, Resolve)
            };
        }

        /// <summary>
        /// "Value of AQ2 is " AQ2 " °C": quoted texts and expressions, one after the other
        /// </summary>
        private IReadOnlyList<TextPart> ParseText(string text)
        {
            var parts = new List<TextPart>();
            var parser = new SequenceExpressionParser(text, 0, Resolve);
            while (!parser.AtEnd)
            {
                if (parser.Peek() == '"')
                {
                    var start = parser.Position + 1;
                    var end = text.IndexOf('"', start);
                    if (end < 0)
                    {
                        throw new FormatException("missing closing \"");
                    }
                    parts.Add(new TextPart(text.Substring(start, end - start), null));
                    parser.Advance(end + 1 - parser.Position);
                }
                else
                {
                    parts.Add(new TextPart(null, parser.ParseExpression()));
                }
            }
            if (parts.Count == 0)
            {
                throw new FormatException("set TQ = ... expects a text");
            }
            return parts;
        }

        private SequenceLine ParseSleep(string rest, string code)
        {
            var factor = 1000.0;
            var expression = rest;
            var match = Regex.Match(rest, @"^(?<value>.*?)\s*(?<unit>ms|s|m|h)$", RegexOptions.IgnoreCase);
            if (match.Success && match.Groups["value"].Value.Trim().Length > 0)
            {
                expression = match.Groups["value"].Value;
                factor = match.Groups["unit"].Value.ToLowerInvariant() switch
                {
                    "ms" => 1,
                    "m" => 60_000,
                    "h" => 3_600_000,
                    _ => 1000
                };
            }

            var value = SequenceExpressionParser.ParseComplete(expression, Resolve);
            return new SequenceLine(SequenceCommand.Sleep, code) { Expression = v => value(v) * factor };
        }
    }

    internal enum SequenceCommand
    {
        /// <summary>Empty line, comment or invalid line: skipped without a step</summary>
        None,
        Set,
        SetPulse,
        SetText,
        /// <summary>Expression = milliseconds</summary>
        Sleep,
        Wait,
        StartSequence,
        Return,
        Goto,
        /// <summary>Target = index of the endif, ElseIndex = index of the else (or -1)</summary>
        If,
        /// <summary>Target = index of the endif</summary>
        Else,
        EndIf
    }

    internal sealed record TextPart(string Text, Func<double[], double> Value);

    internal sealed class SequenceLine
    {
        public SequenceLine(SequenceCommand command, string source)
        {
            Command = command;
            Source = source;
        }

        public SequenceCommand Command { get; }
        public string Source { get; }
        public int Slot { get; init; }
        public Func<double[], double> Expression { get; init; }
        public IReadOnlyList<TextPart> TextParts { get; init; }

        /// <summary>
        /// Goto: line number; StartSequence: sequence index; If/Else: index of the endif
        /// </summary>
        public int Target { get; set; }

        public int ElseIndex { get; set; } = -1;

        /// <summary>
        /// Lines that only structure the program and run without using a step
        /// </summary>
        public bool IsFree => Command is SequenceCommand.None or SequenceCommand.EndIf or SequenceCommand.Else;
    }

    internal sealed class Sequence
    {
        public Sequence(string name, int number)
        {
            Name = name;
            Number = number;
        }

        public string Name { get; }

        /// <summary>
        /// 1..8
        /// </summary>
        public int Number { get; }

        public List<SequenceLine> Lines { get; } = new();
    }
}
