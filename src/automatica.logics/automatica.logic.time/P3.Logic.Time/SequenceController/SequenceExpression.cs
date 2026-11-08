using System;
using System.Collections.Generic;
using System.Globalization;

namespace P3.Logic.Time.SequenceController
{
    /// <summary>
    /// Arithmetic expressions of the sequence controller, the same language as the formula block (math plugin) but with
    /// named values: numbers with '.' as decimal separator, constants pi and e, + - * / % ^, comparisons &lt; &lt;= &gt; &gt;= == !=
    /// (1 or 0), &amp;&amp; || ! (also "and", "or", "not"), functions abs, sign, min, max, round(x[,digits]), floor, ceil, sqrt,
    /// pow, exp, ln, log, sin, cos, tan, clamp(x,lo,hi) and if(condition,then,else).
    /// Names are resolved to slots of a value array at parse time, an unknown name is a parse error.
    /// No loops, no calls into .NET: an expression cannot do anything but compute a number.
    /// </summary>
    internal sealed class SequenceExpressionParser
    {
        private readonly string _text;
        private readonly Func<string, int?> _resolve;
        private int _pos;

        public SequenceExpressionParser(string text, int start, Func<string, int?> resolve)
        {
            _text = text ?? "";
            _pos = start;
            _resolve = resolve;
        }

        public int Position => _pos;

        public bool AtEnd
        {
            get
            {
                SkipWhitespace();
                return _pos >= _text.Length;
            }
        }

        public char Peek()
        {
            SkipWhitespace();
            return _pos < _text.Length ? _text[_pos] : '\0';
        }

        public void Advance(int count) => _pos += count;

        public static Func<double[], double> ParseComplete(string text, Func<string, int?> resolve)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new FormatException("expression expected");
            }
            var parser = new SequenceExpressionParser(text, 0, resolve);
            var result = parser.ParseExpression();
            parser.ExpectEnd();
            return result;
        }

        public void ExpectEnd()
        {
            SkipWhitespace();
            if (_pos < _text.Length)
            {
                throw Error($"unexpected '{_text[_pos]}'");
            }
        }

        private FormatException Error(string message) => new($"{message} (column {_pos + 1})");

        private void SkipWhitespace()
        {
            while (_pos < _text.Length && char.IsWhiteSpace(_text[_pos]))
            {
                _pos++;
            }
        }

        private bool Accept(string token)
        {
            SkipWhitespace();
            if (string.CompareOrdinal(_text, _pos, token, 0, token.Length) == 0)
            {
                // "<" must not consume the first char of "<=", "!" not the one of "!=", "=" not the one of "=="
                if (token.Length == 1 && "<>!=".IndexOf(token[0]) >= 0 && _pos + 1 < _text.Length && _text[_pos + 1] == '=')
                {
                    return false;
                }
                _pos += token.Length;
                return true;
            }
            return false;
        }

        /// <summary>
        /// A keyword operator ("and", "or", "not") that is a whole word
        /// </summary>
        private bool AcceptWord(string word)
        {
            SkipWhitespace();
            if (_pos + word.Length > _text.Length || string.Compare(_text, _pos, word, 0, word.Length, StringComparison.OrdinalIgnoreCase) != 0)
            {
                return false;
            }
            var end = _pos + word.Length;
            if (end < _text.Length && (char.IsLetterOrDigit(_text[end]) || _text[end] == '_'))
            {
                return false;
            }
            _pos = end;
            return true;
        }

        private void Expect(string token)
        {
            if (!Accept(token))
            {
                throw Error($"'{token}' expected");
            }
        }

        private static double Bool(bool value) => value ? 1 : 0;

        public Func<double[], double> ParseExpression() => ParseOr();

        private Func<double[], double> ParseOr()
        {
            var left = ParseAnd();
            while (Accept("||") || AcceptWord("or"))
            {
                var l = left;
                var r = ParseAnd();
                left = v => Bool(l(v) != 0 || r(v) != 0);
            }
            return left;
        }

        private Func<double[], double> ParseAnd()
        {
            var left = ParseComparison();
            while (Accept("&&") || AcceptWord("and"))
            {
                var l = left;
                var r = ParseComparison();
                left = v => Bool(l(v) != 0 && r(v) != 0);
            }
            return left;
        }

        private Func<double[], double> ParseComparison()
        {
            var l = ParseAdditive();
            if (Accept("<=")) { var r = ParseAdditive(); return v => Bool(l(v) <= r(v)); }
            if (Accept(">=")) { var r = ParseAdditive(); return v => Bool(l(v) >= r(v)); }
            if (Accept("==")) { var r = ParseAdditive(); return v => Bool(l(v) == r(v)); }
            if (Accept("!=")) { var r = ParseAdditive(); return v => Bool(l(v) != r(v)); }
            if (Accept("<")) { var r = ParseAdditive(); return v => Bool(l(v) < r(v)); }
            if (Accept(">")) { var r = ParseAdditive(); return v => Bool(l(v) > r(v)); }
            return l;
        }

        private Func<double[], double> ParseAdditive()
        {
            var left = ParseMultiplicative();
            while (true)
            {
                var l = left;
                if (Accept("+")) { var r = ParseMultiplicative(); left = v => l(v) + r(v); }
                else if (Accept("-")) { var r = ParseMultiplicative(); left = v => l(v) - r(v); }
                else return left;
            }
        }

        private Func<double[], double> ParseMultiplicative()
        {
            var left = ParseUnary();
            while (true)
            {
                var l = left;
                if (Accept("*")) { var r = ParseUnary(); left = v => l(v) * r(v); }
                else if (Accept("/")) { var r = ParseUnary(); left = v => l(v) / r(v); }
                else if (Accept("%")) { var r = ParseUnary(); left = v => l(v) % r(v); }
                else return left;
            }
        }

        private Func<double[], double> ParseUnary()
        {
            if (Accept("-")) { var o = ParseUnary(); return v => -o(v); }
            if (Accept("+")) { return ParseUnary(); }
            if (Accept("!") || AcceptWord("not")) { var o = ParseUnary(); return v => Bool(o(v) == 0); }
            return ParsePower();
        }

        private Func<double[], double> ParsePower()
        {
            var b = ParsePrimary();
            if (Accept("^"))
            {
                var e = ParseUnary();
                return v => Math.Pow(b(v), e(v));
            }
            return b;
        }

        private Func<double[], double> ParsePrimary()
        {
            SkipWhitespace();
            if (_pos >= _text.Length)
            {
                throw Error("value expected");
            }

            var c = _text[_pos];
            if (Accept("("))
            {
                var inner = ParseExpression();
                Expect(")");
                return inner;
            }

            if (char.IsDigit(c) || c == '.')
            {
                return ParseNumber();
            }

            if (char.IsLetter(c) || c == '_')
            {
                var start = _pos;
                while (_pos < _text.Length && (char.IsLetterOrDigit(_text[_pos]) || _text[_pos] == '_'))
                {
                    _pos++;
                }
                var name = _text.Substring(start, _pos - start).ToLowerInvariant();

                if (Accept("("))
                {
                    var args = new List<Func<double[], double>>();
                    if (!Accept(")"))
                    {
                        do
                        {
                            args.Add(ParseExpression());
                        } while (Accept(","));
                        Expect(")");
                    }
                    return Function(name, args);
                }

                switch (name)
                {
                    case "pi": return _ => Math.PI;
                    case "e": return _ => Math.E;
                    case "true": return _ => 1;
                    case "false": return _ => 0;
                }

                var slot = _resolve(name);
                if (slot == null)
                {
                    _pos = start;
                    throw Error($"unknown name '{name}'");
                }
                var index = slot.Value;
                return v => v[index];
            }

            throw Error($"unexpected '{c}'");
        }

        private Func<double[], double> ParseNumber()
        {
            var start = _pos;
            while (_pos < _text.Length && (char.IsDigit(_text[_pos]) || _text[_pos] == '.'))
            {
                _pos++;
            }
            if (_pos < _text.Length && (_text[_pos] == 'e' || _text[_pos] == 'E'))
            {
                var save = _pos;
                _pos++;
                if (_pos < _text.Length && (_text[_pos] == '+' || _text[_pos] == '-'))
                {
                    _pos++;
                }
                if (_pos < _text.Length && char.IsDigit(_text[_pos]))
                {
                    while (_pos < _text.Length && char.IsDigit(_text[_pos]))
                    {
                        _pos++;
                    }
                }
                else
                {
                    _pos = save;
                }
            }

            var token = _text.Substring(start, _pos - start);
            if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            {
                _pos = start;
                throw Error($"invalid number '{token}'");
            }
            return _ => number;
        }

        private Func<double[], double> Function(string name, List<Func<double[], double>> a)
        {
            void Arity(int min, int max)
            {
                if (a.Count < min || a.Count > max)
                {
                    throw Error(min == max ? $"{name} expects {min} argument(s)" : $"{name} expects {min} to {max} arguments");
                }
            }

            switch (name)
            {
                case "abs": Arity(1, 1); return v => Math.Abs(a[0](v));
                case "sign": Arity(1, 1); return v => Math.Sign(a[0](v));
                case "floor": Arity(1, 1); return v => Math.Floor(a[0](v));
                case "ceil": Arity(1, 1); return v => Math.Ceiling(a[0](v));
                case "sqrt": Arity(1, 1); return v => Math.Sqrt(a[0](v));
                case "exp": Arity(1, 1); return v => Math.Exp(a[0](v));
                case "ln": Arity(1, 1); return v => Math.Log(a[0](v));
                case "log": Arity(1, 1); return v => Math.Log10(a[0](v));
                case "sin": Arity(1, 1); return v => Math.Sin(a[0](v));
                case "cos": Arity(1, 1); return v => Math.Cos(a[0](v));
                case "tan": Arity(1, 1); return v => Math.Tan(a[0](v));
                case "pow": Arity(2, 2); return v => Math.Pow(a[0](v), a[1](v));
                case "round":
                    Arity(1, 2);
                    if (a.Count == 1)
                    {
                        return v => Math.Round(a[0](v), MidpointRounding.AwayFromZero);
                    }
                    return v => Math.Round(a[0](v), Math.Clamp((int)a[1](v), 0, 15), MidpointRounding.AwayFromZero);
                case "min":
                    Arity(1, int.MaxValue);
                    return v =>
                    {
                        var m = a[0](v);
                        for (var i = 1; i < a.Count; i++) m = Math.Min(m, a[i](v));
                        return m;
                    };
                case "max":
                    Arity(1, int.MaxValue);
                    return v =>
                    {
                        var m = a[0](v);
                        for (var i = 1; i < a.Count; i++) m = Math.Max(m, a[i](v));
                        return m;
                    };
                case "clamp": Arity(3, 3); return v => Math.Min(Math.Max(a[0](v), a[1](v)), a[2](v));
                case "if": Arity(3, 3); return v => a[0](v) != 0 ? a[1](v) : a[2](v);
                default: throw Error($"unknown function '{name}'");
            }
        }
    }
}
