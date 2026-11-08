using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using P3.Logic.StringOperations.Common;

namespace P3.Logic.StringOperations.Status
{
    /// <summary>
    /// Condition of a status rule, e.g. <c>I1 &gt;= 20</c>, <c>I2 *= Alarm</c>, <c>I1 == &lt;v2&gt;</c> or
    /// <c>I1 &gt; 20 &amp;&amp; I2 == 1</c>.
    /// <para>Operators (Loxone): == (also =), !=, &gt;, &gt;=, &lt;, &lt;=, *= contains, !* does not contain, : starts with,
    /// !: does not start with. Conditions are joined with &amp;&amp; / and, || / or (and binds stronger).</para>
    /// <para>Operands: I1..I8 or &lt;v1&gt;..&lt;v8&gt; (inputs), numbers, "quoted text" or a single word. A condition that
    /// starts with the operator compares input 1 (<c>&gt; 20</c>).</para>
    /// <para>&lt;, &lt;=, &gt;, &gt;= need numbers on both sides (false otherwise, like Loxone); == and != compare numbers when
    /// both sides are numbers, text otherwise. Text comparisons ignore the case.</para>
    /// </summary>
    internal sealed class StatusCondition
    {
        internal enum Operator
        {
            Equal,
            NotEqual,
            Greater,
            GreaterOrEqual,
            Less,
            LessOrEqual,
            Contains,
            NotContains,
            StartsWith,
            NotStartsWith
        }

        private sealed class Operand
        {
            public int Input; // 1..8, 0 = literal
            public string Literal;
        }

        private sealed class Comparison
        {
            public Operand Left;
            public Operator Op;
            public Operand Right;
        }

        // OR of ANDs
        private readonly List<List<Comparison>> _alternatives;

        private StatusCondition(List<List<Comparison>> alternatives)
        {
            _alternatives = alternatives;
        }

        internal const int InputCount = 8;

        /// <summary>
        /// Parses a condition; empty conditions and syntax errors return false (error says why)
        /// </summary>
        internal static bool TryParse(string text, out StatusCondition condition, out string error)
        {
            condition = null;
            error = null;
            if (string.IsNullOrWhiteSpace(text))
            {
                error = "empty";
                return false;
            }

            try
            {
                var tokens = Tokenize(text);
                var position = 0;
                var alternatives = new List<List<Comparison>>();
                var current = new List<Comparison>();

                while (true)
                {
                    current.Add(ParseComparison(tokens, ref position, current.Count == 0 && alternatives.Count == 0));

                    if (position >= tokens.Count)
                    {
                        break;
                    }

                    var join = tokens[position];
                    if (join.Kind == TokenKind.And)
                    {
                        position++;
                    }
                    else if (join.Kind == TokenKind.Or)
                    {
                        position++;
                        alternatives.Add(current);
                        current = new List<Comparison>();
                    }
                    else
                    {
                        throw new FormatException($"&& or || expected at '{join.Text}'");
                    }
                }

                alternatives.Add(current);
                condition = new StatusCondition(alternatives);
                return true;
            }
            catch (FormatException e)
            {
                error = e.Message;
                return false;
            }
        }

        /// <param name="inputs">values of input 1..8 (index 0 = input 1), null when not received yet</param>
        internal bool Evaluate(object[] inputs)
        {
            foreach (var all in _alternatives)
            {
                var ok = true;
                foreach (var comparison in all)
                {
                    if (!Compare(Resolve(comparison.Left, inputs), comparison.Op, Resolve(comparison.Right, inputs)))
                    {
                        ok = false;
                        break;
                    }
                }
                if (ok)
                {
                    return true;
                }
            }
            return false;
        }

        private static object Resolve(Operand operand, object[] inputs)
        {
            if (operand.Input == 0)
            {
                return operand.Literal;
            }
            var index = operand.Input - 1;
            return index < inputs.Length ? inputs[index] : null;
        }

        internal static bool Compare(object left, Operator op, object right)
        {
            var leftIsNumber = TryNumber(left, out var l);
            var rightIsNumber = TryNumber(right, out var r);
            var bothNumbers = leftIsNumber && rightIsNumber;

            switch (op)
            {
                case Operator.Equal:
                    return bothNumbers ? NumbersEqual(l, r) : TextEquals(left, right);
                case Operator.NotEqual:
                    return bothNumbers ? !NumbersEqual(l, r) : !TextEquals(left, right);
                case Operator.Greater:
                    return bothNumbers && l > r;
                case Operator.GreaterOrEqual:
                    return bothNumbers && (l > r || NumbersEqual(l, r));
                case Operator.Less:
                    return bothNumbers && l < r;
                case Operator.LessOrEqual:
                    return bothNumbers && (l < r || NumbersEqual(l, r));
                case Operator.Contains:
                    return Text(left).IndexOf(Text(right), StringComparison.OrdinalIgnoreCase) >= 0;
                case Operator.NotContains:
                    return Text(left).IndexOf(Text(right), StringComparison.OrdinalIgnoreCase) < 0;
                case Operator.StartsWith:
                    return Text(left).StartsWith(Text(right), StringComparison.OrdinalIgnoreCase);
                case Operator.NotStartsWith:
                    return !Text(left).StartsWith(Text(right), StringComparison.OrdinalIgnoreCase);
                default:
                    return false;
            }
        }

        private static bool TryNumber(object value, out double number)
        {
            // an input that never got a value is no number - "I1 < 5" must not be true at startup
            number = 0;
            return value != null && TextValue.TryToDouble(value, out number);
        }

        private static bool NumbersEqual(double a, double b) => Math.Abs(a - b) <= 1e-9 * Math.Max(1, Math.Max(Math.Abs(a), Math.Abs(b)));

        private static string Text(object value) => TextValue.ToText(value);

        private static bool TextEquals(object left, object right) => string.Equals(Text(left), Text(right), StringComparison.OrdinalIgnoreCase);

        #region parser

        private enum TokenKind
        {
            Value,
            Text, // quoted, never an input reference
            Operator,
            And,
            Or
        }

        private sealed class Token
        {
            public TokenKind Kind;
            public string Text;
            public Operator Op;
        }

        private static Comparison ParseComparison(List<Token> tokens, ref int position, bool first)
        {
            if (position >= tokens.Count)
            {
                throw new FormatException("condition expected");
            }

            Operand left;
            if (tokens[position].Kind == TokenKind.Operator)
            {
                if (!first)
                {
                    throw new FormatException($"value expected before '{tokens[position].Text}'");
                }
                // "> 20" compares input 1
                left = new Operand { Input = 1 };
            }
            else
            {
                left = ParseOperand(tokens[position++]);
            }

            if (position >= tokens.Count || tokens[position].Kind != TokenKind.Operator)
            {
                throw new FormatException("operator expected (==, !=, >, >=, <, <=, *=, !*, :, !:)");
            }
            var op = tokens[position++].Op;

            if (position >= tokens.Count || (tokens[position].Kind != TokenKind.Value && tokens[position].Kind != TokenKind.Text))
            {
                throw new FormatException("value expected after the operator");
            }
            var right = ParseOperand(tokens[position++]);

            return new Comparison { Left = left, Op = op, Right = right };
        }

        private static Operand ParseOperand(Token token)
        {
            if (token.Kind == TokenKind.Text)
            {
                return new Operand { Literal = token.Text };
            }
            if (token.Kind != TokenKind.Value)
            {
                throw new FormatException($"value expected at '{token.Text}'");
            }

            var input = InputReference(token.Text);
            return input > 0 ? new Operand { Input = input } : new Operand { Literal = token.Text };
        }

        /// <summary>
        /// I1..I8, &lt;v1&gt;..&lt;v8&gt;, &lt;v&gt; (= input 1); 0 for literals
        /// </summary>
        private static int InputReference(string text)
        {
            if (text.Length == 2 && (text[0] == 'I' || text[0] == 'i') && text[1] >= '1' && text[1] <= '0' + InputCount)
            {
                return text[1] - '0';
            }
            if (text.Equals("<v>", StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }
            if (text.Length == 4 && text.StartsWith("<v", StringComparison.OrdinalIgnoreCase) && text[3] == '>'
                && text[2] >= '1' && text[2] <= '0' + InputCount)
            {
                return text[2] - '0';
            }
            return 0;
        }

        private static readonly (string Text, Operator Op)[] Operators =
        {
            // longest first
            ("==", Operator.Equal),
            ("!=", Operator.NotEqual),
            (">=", Operator.GreaterOrEqual),
            ("<=", Operator.LessOrEqual),
            ("*=", Operator.Contains),
            ("!*", Operator.NotContains),
            ("!:", Operator.NotStartsWith),
            (">", Operator.Greater),
            ("<", Operator.Less),
            ("=", Operator.Equal),
            (":", Operator.StartsWith)
        };

        private static List<Token> Tokenize(string text)
        {
            var tokens = new List<Token>();
            var i = 0;
            while (i < text.Length)
            {
                var c = text[i];
                if (char.IsWhiteSpace(c))
                {
                    i++;
                    continue;
                }

                if (c == '"' || c == '\'')
                {
                    var end = text.IndexOf(c, i + 1);
                    if (end < 0)
                    {
                        throw new FormatException("closing quote missing");
                    }
                    tokens.Add(new Token { Kind = TokenKind.Text, Text = text.Substring(i + 1, end - i - 1) });
                    i = end + 1;
                    continue;
                }

                if (Matches(text, i, "&&") || Matches(text, i, "||"))
                {
                    tokens.Add(new Token { Kind = c == '&' ? TokenKind.And : TokenKind.Or, Text = text.Substring(i, 2) });
                    i += 2;
                    continue;
                }

                // <v1> is an input, not the operator <
                if (c == '<' && TryInputPlaceholder(text, i, out var length))
                {
                    tokens.Add(new Token { Kind = TokenKind.Value, Text = text.Substring(i, length) });
                    i += length;
                    continue;
                }

                var matched = false;
                foreach (var (opText, op) in Operators)
                {
                    if (Matches(text, i, opText))
                    {
                        tokens.Add(new Token { Kind = TokenKind.Operator, Text = opText, Op = op });
                        i += opText.Length;
                        matched = true;
                        break;
                    }
                }
                if (matched)
                {
                    continue;
                }

                var word = new StringBuilder();
                while (i < text.Length && !char.IsWhiteSpace(text[i]) && !StartsOperatorOrJoin(text, i))
                {
                    word.Append(text[i]);
                    i++;
                }

                var value = word.ToString();
                var lower = value.ToLowerInvariant();
                if (lower == "and" || lower == "or")
                {
                    tokens.Add(new Token { Kind = lower == "and" ? TokenKind.And : TokenKind.Or, Text = value });
                }
                else
                {
                    tokens.Add(new Token { Kind = TokenKind.Value, Text = value });
                }
            }
            return tokens;
        }

        private static bool TryInputPlaceholder(string text, int i, out int length)
        {
            length = 0;
            if (Matches(text, i, "<v>") || Matches(text, i, "<V>"))
            {
                length = 3;
                return true;
            }
            if (i + 3 < text.Length && (text[i + 1] == 'v' || text[i + 1] == 'V') && char.IsDigit(text[i + 2]) && text[i + 3] == '>')
            {
                length = 4;
                return true;
            }
            return false;
        }

        private static bool StartsOperatorOrJoin(string text, int i)
        {
            if (Matches(text, i, "&&") || Matches(text, i, "||"))
            {
                return true;
            }
            foreach (var (opText, _) in Operators)
            {
                if (Matches(text, i, opText))
                {
                    // a minus or a dot inside a number is no operator anyway; "!" alone is text
                    return true;
                }
            }
            return false;
        }

        private static bool Matches(string text, int i, string token)
        {
            return string.CompareOrdinal(text, i, token, 0, token.Length) == 0;
        }

        #endregion

        public override string ToString()
        {
            var parts = new List<string>();
            foreach (var all in _alternatives)
            {
                var ands = new List<string>();
                foreach (var c in all)
                {
                    ands.Add(string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", Describe(c.Left), c.Op, Describe(c.Right)));
                }
                parts.Add(string.Join(" && ", ands));
            }
            return string.Join(" || ", parts);
        }

        private static string Describe(Operand o) => o.Input > 0 ? "I" + o.Input : "\"" + o.Literal + "\"";
    }
}
