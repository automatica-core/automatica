using System;
using System.Globalization;

namespace P3.Logic.StringOperations.CommandRecognition
{
    /// <summary>
    /// Loxone command recognition: the pattern walks through the text from its start, characters without a backslash must
    /// match literally.
    /// <list type="bullet">
    /// <item>extract: \v number, \1..\4 byte n of the value (1 = lowest), \h hex number</item>
    /// <item>navigate: \. any character, \w any word, \# any number, \d digit, \m letter or digit, \a letter,
    /// \s12 skip 12 characters, \iText\i jump behind "Text"</item>
    /// <item>special: \x09 character 0x09, \\ backslash, \t tab, \b space or tab, \r return, \n newline</item>
    /// </list>
    /// The result is the last extracted value; null when the text does not match or the pattern extracts nothing.
    /// </summary>
    internal static class CommandPattern
    {
        internal const int MaxBytes = 4;

        internal static double? Extract(string pattern, string text, bool signed)
        {
            if (string.IsNullOrEmpty(pattern) || text == null)
            {
                return null;
            }

            var pos = 0;
            double? last = null;
            long bytes = 0;
            var maxByte = 0;
            var lastIsBytes = false;

            for (var i = 0; i < pattern.Length; i++)
            {
                var c = pattern[i];
                if (c != '\\')
                {
                    if (pos >= text.Length || text[pos] != c)
                    {
                        return null;
                    }
                    pos++;
                    continue;
                }

                if (++i >= pattern.Length)
                {
                    // a trailing backslash is a literal one
                    if (pos >= text.Length || text[pos] != '\\')
                    {
                        return null;
                    }
                    pos++;
                    break;
                }

                var command = pattern[i];
                switch (command)
                {
                    case 'v':
                    {
                        SkipBlanks(text, ref pos);
                        if (!ReadNumber(text, ref pos, out var value))
                        {
                            return null;
                        }
                        last = value;
                        lastIsBytes = false;
                        break;
                    }
                    case 'h':
                    {
                        SkipBlanks(text, ref pos);
                        var start = pos;
                        while (pos < text.Length && Uri.IsHexDigit(text[pos]) && pos - start < 15)
                        {
                            pos++;
                        }
                        if (pos == start)
                        {
                            return null;
                        }
                        last = long.Parse(text.Substring(start, pos - start), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                        lastIsBytes = false;
                        break;
                    }
                    case >= '1' and <= '4':
                    {
                        if (pos >= text.Length)
                        {
                            return null;
                        }
                        var index = command - '1';
                        bytes = (bytes & ~(0xFFL << (index * 8))) | ((long)(text[pos] & 0xFF) << (index * 8));
                        maxByte = Math.Max(maxByte, index + 1);
                        pos++;
                        lastIsBytes = true;
                        break;
                    }
                    case '.':
                        if (pos >= text.Length)
                        {
                            return null;
                        }
                        pos++;
                        break;
                    case 'w':
                    {
                        SkipBlanks(text, ref pos);
                        var start = pos;
                        while (pos < text.Length && !char.IsWhiteSpace(text[pos]))
                        {
                            pos++;
                        }
                        if (pos == start)
                        {
                            return null;
                        }
                        break;
                    }
                    case '#':
                        SkipBlanks(text, ref pos);
                        if (!ReadNumber(text, ref pos, out _))
                        {
                            return null;
                        }
                        break;
                    case 'd':
                        if (!Single(text, ref pos, char.IsDigit))
                        {
                            return null;
                        }
                        break;
                    case 'm':
                        if (!Single(text, ref pos, ch => ch < 128 && char.IsLetterOrDigit(ch)))
                        {
                            return null;
                        }
                        break;
                    case 'a':
                        if (!Single(text, ref pos, ch => ch < 128 && char.IsLetter(ch)))
                        {
                            return null;
                        }
                        break;
                    case 's':
                    {
                        var start = i + 1;
                        while (i + 1 < pattern.Length && char.IsDigit(pattern[i + 1]))
                        {
                            i++;
                        }
                        var count = start <= i ? int.Parse(pattern.Substring(start, i - start + 1), CultureInfo.InvariantCulture) : 1;
                        if (pos + count > text.Length)
                        {
                            return null;
                        }
                        pos += count;
                        break;
                    }
                    case 'i':
                    {
                        var end = pattern.IndexOf("\\i", i + 1, StringComparison.Ordinal);
                        if (end < 0)
                        {
                            end = pattern.Length;
                        }
                        var search = Unescape(pattern.Substring(i + 1, end - i - 1));
                        i = Math.Min(end + 1, pattern.Length);
                        var found = search.Length == 0 ? pos : text.IndexOf(search, pos, StringComparison.Ordinal);
                        if (found < 0)
                        {
                            return null;
                        }
                        pos = found + search.Length;
                        break;
                    }
                    case 'b':
                        if (pos >= text.Length || (text[pos] != ' ' && text[pos] != '\t'))
                        {
                            return null;
                        }
                        pos++;
                        break;
                    default:
                    {
                        var literal = Escape(pattern, ref i);
                        if (literal == null || pos >= text.Length || text[pos] != literal.Value)
                        {
                            return null;
                        }
                        pos++;
                        break;
                    }
                }
            }

            if (lastIsBytes)
            {
                var value = bytes;
                if (signed && maxByte > 0 && maxByte < 8)
                {
                    var bits = maxByte * 8;
                    var signBit = 1L << (bits - 1);
                    if ((value & signBit) != 0)
                    {
                        value -= 1L << bits;
                    }
                }
                return value;
            }
            return last;
        }

        /// <summary>
        /// The special characters \x09, \\, \t, \r, \n; i points at the character after the backslash
        /// </summary>
        private static char? Escape(string pattern, ref int i)
        {
            switch (pattern[i])
            {
                case '\\':
                    return '\\';
                case 't':
                    return '\t';
                case 'r':
                    return '\r';
                case 'n':
                    return '\n';
                case 'x':
                    if (i + 2 < pattern.Length && Uri.IsHexDigit(pattern[i + 1]) && Uri.IsHexDigit(pattern[i + 2]))
                    {
                        var code = Convert.ToInt32(pattern.Substring(i + 1, 2), 16);
                        i += 2;
                        return (char)code;
                    }
                    return null;
                default:
                    // unknown command: the character itself (\: or \" in Loxone patterns)
                    return pattern[i];
            }
        }

        /// <summary>
        /// The search text of \i...\i may contain the special characters
        /// </summary>
        private static string Unescape(string text)
        {
            var builder = new System.Text.StringBuilder();
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] == '\\' && i + 1 < text.Length)
                {
                    i++;
                    var escaped = Escape(text, ref i);
                    if (escaped != null)
                    {
                        builder.Append(escaped.Value);
                    }
                    continue;
                }
                builder.Append(text[i]);
            }
            return builder.ToString();
        }

        private static bool Single(string text, ref int pos, Func<char, bool> accept)
        {
            if (pos >= text.Length || !accept(text[pos]))
            {
                return false;
            }
            pos++;
            return true;
        }

        private static void SkipBlanks(string text, ref int pos)
        {
            while (pos < text.Length && (text[pos] == ' ' || text[pos] == '\t'))
            {
                pos++;
            }
        }

        /// <summary>
        /// [+-]digits[.digits] (a decimal comma is accepted when digits follow)
        /// </summary>
        private static bool ReadNumber(string text, ref int pos, out double value)
        {
            value = 0;
            var start = pos;
            var p = pos;
            if (p < text.Length && (text[p] == '-' || text[p] == '+'))
            {
                p++;
            }
            var digitsStart = p;
            while (p < text.Length && char.IsDigit(text[p]))
            {
                p++;
            }
            var hasDigits = p > digitsStart;

            if (p + 1 < text.Length && (text[p] == '.' || text[p] == ',') && char.IsDigit(text[p + 1]))
            {
                p++;
                while (p < text.Length && char.IsDigit(text[p]))
                {
                    p++;
                }
                hasDigits = true;
            }

            if (!hasDigits)
            {
                return false;
            }

            var number = text.Substring(start, p - start).Replace(',', '.');
            if (!double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return false;
            }
            pos = p;
            return true;
        }
    }
}
