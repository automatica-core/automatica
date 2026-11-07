using System;
using System.Collections.Generic;
using System.Globalization;

namespace P3.Logic.Math.BasicOperations.Formula
{
    /// <summary>
    /// Small, safe arithmetic expression language for the formula block.
    /// Variables I1..I4, numbers with '.' as decimal separator, constants pi and e,
    /// operators + - * / % ^ (power), comparisons &lt; &lt;= &gt; &gt;= == != (1 or 0), &amp;&amp; || !,
    /// functions abs, sign, min, max, round(x[,digits]), floor, ceil, sqrt, pow, exp, ln, log (base 10),
    /// sin, cos, tan (radians), clamp(x,lo,hi) and if(condition,then,else).
    /// </summary>
    public sealed class FormulaExpression
    {
        public const int VariableCount = 4;

        private readonly Func<double[], double> _evaluate;

        private FormulaExpression(string text, Func<double[], double> evaluate)
        {
            Text = text;
            _evaluate = evaluate;
        }

        public string Text { get; }

        public double Evaluate(params double[] inputs)
        {
            var values = new double[VariableCount];
            Array.Copy(inputs, values, System.Math.Min(inputs.Length, VariableCount));
            return _evaluate(values);
        }

        /// <exception cref="FormatException">The formula is empty or invalid</exception>
        public static FormulaExpression Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new FormatException("The formula is empty");
            }

            var parser = new Parser(text);
            var evaluate = parser.ParseExpression();
            parser.ExpectEnd();
            return new FormulaExpression(text, evaluate);
        }

        private sealed class Parser
        {
            private readonly string _text;
            private int _pos;

            public Parser(string text)
            {
                _text = text;
            }

            public Func<double[], double> ParseExpression() => ParseOr();

            public void ExpectEnd()
            {
                SkipWhitespace();
                if (_pos < _text.Length)
                {
                    throw Error($"unexpected '{_text[_pos]}'");
                }
            }

            private FormatException Error(string message) => new($"Invalid formula at position {_pos + 1}: {message}");

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
                    // "<" must not consume the first char of "<=", "!" not the one of "!="
                    if (token.Length == 1 && "<>!=".IndexOf(token[0]) >= 0 && _pos + 1 < _text.Length && _text[_pos + 1] == '=')
                    {
                        return false;
                    }
                    _pos += token.Length;
                    return true;
                }
                return false;
            }

            private void Expect(string token)
            {
                if (!Accept(token))
                {
                    throw Error($"'{token}' expected");
                }
            }

            private static double Bool(bool value) => value ? 1 : 0;

            private Func<double[], double> ParseOr()
            {
                var left = ParseAnd();
                while (Accept("||"))
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
                while (Accept("&&"))
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
                if (Accept("!")) { var o = ParseUnary(); return v => Bool(o(v) == 0); }
                return ParsePower();
            }

            private Func<double[], double> ParsePower()
            {
                var b = ParsePrimary();
                if (Accept("^"))
                {
                    // right associative, and 2^-1 is allowed
                    var e = ParseUnary();
                    return v => System.Math.Pow(b(v), e(v));
                }
                return b;
            }

            private Func<double[], double> ParsePrimary()
            {
                SkipWhitespace();
                if (_pos >= _text.Length)
                {
                    throw Error("unexpected end of the formula");
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

                if (char.IsLetter(c))
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

                    return Identifier(name);
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
                // exponent: 1e3, 2.5E-2
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

            private Func<double[], double> Identifier(string name)
            {
                switch (name)
                {
                    case "i1": return v => v[0];
                    case "i2": return v => v[1];
                    case "i3": return v => v[2];
                    case "i4": return v => v[3];
                    case "pi": return _ => System.Math.PI;
                    case "e": return _ => System.Math.E;
                    default: throw Error($"unknown name '{name}'");
                }
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
                    case "abs": Arity(1, 1); return v => System.Math.Abs(a[0](v));
                    case "sign": Arity(1, 1); return v => System.Math.Sign(a[0](v));
                    case "floor": Arity(1, 1); return v => System.Math.Floor(a[0](v));
                    case "ceil": Arity(1, 1); return v => System.Math.Ceiling(a[0](v));
                    case "sqrt": Arity(1, 1); return v => System.Math.Sqrt(a[0](v));
                    case "exp": Arity(1, 1); return v => System.Math.Exp(a[0](v));
                    case "ln": Arity(1, 1); return v => System.Math.Log(a[0](v));
                    case "log": Arity(1, 1); return v => System.Math.Log10(a[0](v));
                    case "sin": Arity(1, 1); return v => System.Math.Sin(a[0](v));
                    case "cos": Arity(1, 1); return v => System.Math.Cos(a[0](v));
                    case "tan": Arity(1, 1); return v => System.Math.Tan(a[0](v));
                    case "pow": Arity(2, 2); return v => System.Math.Pow(a[0](v), a[1](v));
                    case "round":
                        Arity(1, 2);
                        if (a.Count == 1)
                        {
                            return v => System.Math.Round(a[0](v), MidpointRounding.AwayFromZero);
                        }
                        return v => System.Math.Round(a[0](v), System.Math.Clamp((int)a[1](v), 0, 15), MidpointRounding.AwayFromZero);
                    case "min":
                        Arity(1, int.MaxValue);
                        return v =>
                        {
                            var m = a[0](v);
                            for (var i = 1; i < a.Count; i++) m = System.Math.Min(m, a[i](v));
                            return m;
                        };
                    case "max":
                        Arity(1, int.MaxValue);
                        return v =>
                        {
                            var m = a[0](v);
                            for (var i = 1; i < a.Count; i++) m = System.Math.Max(m, a[i](v));
                            return m;
                        };
                    case "clamp": Arity(3, 3); return v => System.Math.Min(System.Math.Max(a[0](v), a[1](v)), a[2](v));
                    case "if": Arity(3, 3); return v => a[0](v) != 0 ? a[1](v) : a[2](v);
                    default: throw Error($"unknown function '{name}'");
                }
            }
        }
    }
}
