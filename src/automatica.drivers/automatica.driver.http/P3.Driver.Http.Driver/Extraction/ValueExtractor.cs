using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Automatica.Core.Base.Templates;

namespace P3.Driver.Http.Driver.Extraction
{
    public enum ExtractMode
    {
        /// <summary>Loxone command recognition, e.g. \iTemperature:\i\v</summary>
        [EnumName("HTTP.MODE.PATTERN")]
        Pattern = 0,
        /// <summary>JSON path, e.g. $.sensors[0].temp</summary>
        [EnumName("HTTP.MODE.JSON")]
        Json = 1,
        /// <summary>Regular expression, the first group (or the whole match) is the value</summary>
        [EnumName("HTTP.MODE.REGEX")]
        Regex = 2,
        /// <summary>The whole response</summary>
        [EnumName("HTTP.MODE.TEXT")]
        Text = 3
    }

    /// <summary>
    /// Extracts a value from a response body. Every mode returns the value as text (null when nothing matches),
    /// <see cref="ToNumber"/> / <see cref="ToBool"/> convert it for numeric and digital inputs.
    /// </summary>
    internal static class ValueExtractor
    {
        /// <summary>
        /// A regular expression of a user may backtrack badly, it gets this much time per response
        /// </summary>
        internal static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);

        public static string Extract(string body, ExtractMode mode, string pattern)
        {
            if (body == null)
            {
                return null;
            }

            switch (mode)
            {
                case ExtractMode.Text:
                    return body.Trim();
                case ExtractMode.Json:
                    return JsonPath(body, pattern);
                case ExtractMode.Regex:
                    return RegexValue(body, pattern);
                default:
                    return LoxonePattern.Extract(body, pattern);
            }
        }

        private static string RegexValue(string body, string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                throw new FormatException("the regular expression is empty");
            }

            Regex regex;
            try
            {
                regex = new Regex(pattern, RegexOptions.CultureInvariant, RegexTimeout);
            }
            catch (ArgumentException ex)
            {
                throw new FormatException($"invalid regular expression: {ex.Message}");
            }

            var match = regex.Match(body);
            if (!match.Success)
            {
                return null;
            }
            var group = match.Groups.Cast<Group>().Skip(1).FirstOrDefault(g => g.Success);
            return (group ?? match.Groups[0]).Value;
        }

        /// <summary>
        /// $.a.b[0].c, a.b.0.c, $['a b'].c - objects by name, arrays by index
        /// </summary>
        internal static string JsonPath(string body, string path)
        {
            using var document = JsonDocument.Parse(body, new JsonDocumentOptions { MaxDepth = 64, AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
            var current = document.RootElement;

            foreach (var segment in ParsePath(path))
            {
                if (current.ValueKind == JsonValueKind.Array && int.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out var index))
                {
                    if (index >= current.GetArrayLength())
                    {
                        return null;
                    }
                    current = current[index];
                }
                else if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(segment, out var child))
                {
                    current = child;
                }
                else
                {
                    return null;
                }
            }

            return current.ValueKind switch
            {
                JsonValueKind.String => current.GetString(),
                JsonValueKind.Number => current.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null or JsonValueKind.Undefined => null,
                _ => current.GetRawText()
            };
        }

        internal static string[] ParsePath(string path)
        {
            path = (path ?? "").Trim();
            if (path.StartsWith("$"))
            {
                path = path.Substring(1);
            }

            var segments = new System.Collections.Generic.List<string>();
            var i = 0;
            while (i < path.Length)
            {
                var c = path[i];
                if (c == '.')
                {
                    i++;
                    continue;
                }
                if (c == '[')
                {
                    var end = path.IndexOf(']', i);
                    if (end < 0)
                    {
                        throw new FormatException("missing ] in the JSON path");
                    }
                    var inner = path.Substring(i + 1, end - i - 1).Trim();
                    if (inner.Length >= 2 && (inner[0] == '\'' || inner[0] == '"') && inner[^1] == inner[0])
                    {
                        inner = inner.Substring(1, inner.Length - 2);
                    }
                    segments.Add(inner);
                    i = end + 1;
                    continue;
                }
                var next = path.IndexOfAny(new[] { '.', '[' }, i);
                var name = next < 0 ? path.Substring(i) : path.Substring(i, next - i);
                segments.Add(name);
                i = next < 0 ? path.Length : next;
            }
            return segments.ToArray();
        }

        public static double? ToNumber(string text)
        {
            if (text == null)
            {
                return null;
            }
            text = text.Trim();
            if (bool.TryParse(text, out var b))
            {
                return b ? 1 : 0;
            }
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && !double.IsNaN(value) && !double.IsInfinity(value))
            {
                return value;
            }
            // "21,5" from a German device
            if (text.Count(c => c == ',') == 1 && !text.Contains('.') &&
                double.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return value;
            }
            return null;
        }

        public static bool? ToBool(string text)
        {
            if (text == null)
            {
                return null;
            }
            switch (text.Trim().ToLowerInvariant())
            {
                case "true":
                case "on":
                case "yes":
                case "open":
                    return true;
                case "false":
                case "off":
                case "no":
                case "closed":
                    return false;
            }
            var number = ToNumber(text);
            return number.HasValue ? number.Value != 0 : null;
        }
    }

    /// <summary>
    /// Loxone command recognition (Virtual HTTP Input). Text is matched from the start, these escapes move through it:
    /// \iText\i jump behind the next "Text"; \v read a number; \h read a hexadecimal number; \1..\4 read one character as
    /// byte 1..4 (low to high) of an integer; \sN skip N characters; \. any character; \d a digit; \# a number;
    /// \w a word; \m a letter or digit; \a a letter; \t tab; \r \n line breaks; \xNN the character NN (hex); \\ a backslash.
    /// Any other character must match exactly. The result is the number read by \v, \h or the bytes.
    /// </summary>
    internal static class LoxonePattern
    {
        public static string Extract(string text, string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                throw new FormatException("the command recognition is empty");
            }

            var pos = 0;
            string value = null;
            long bytes = 0;
            var usesBytes = false;
            var i = 0;

            while (i < pattern.Length)
            {
                var c = pattern[i];
                if (c != '\\')
                {
                    if (pos >= text.Length || text[pos] != c)
                    {
                        return null;
                    }
                    pos++;
                    i++;
                    continue;
                }

                if (i + 1 >= pattern.Length)
                {
                    throw new FormatException("the command recognition ends with a single \\");
                }
                var e = pattern[i + 1];
                i += 2;
                switch (e)
                {
                    case 'i':
                    {
                        var end = pattern.IndexOf("\\i", i, StringComparison.Ordinal);
                        if (end < 0)
                        {
                            throw new FormatException("\\i without the closing \\i");
                        }
                        var search = Unescape(pattern.Substring(i, end - i));
                        var found = text.IndexOf(search, pos, StringComparison.Ordinal);
                        if (found < 0)
                        {
                            return null;
                        }
                        pos = found + search.Length;
                        i = end + 2;
                        break;
                    }
                    case 'v':
                    {
                        while (pos < text.Length && char.IsWhiteSpace(text[pos]))
                        {
                            pos++;
                        }
                        var start = pos;
                        if (pos < text.Length && (text[pos] == '-' || text[pos] == '+'))
                        {
                            pos++;
                        }
                        while (pos < text.Length && (char.IsDigit(text[pos]) || text[pos] == '.'))
                        {
                            pos++;
                        }
                        if (pos < text.Length && (text[pos] == 'e' || text[pos] == 'E') && pos + 1 < text.Length &&
                            (char.IsDigit(text[pos + 1]) || ((text[pos + 1] == '-' || text[pos + 1] == '+') && pos + 2 < text.Length && char.IsDigit(text[pos + 2]))))
                        {
                            pos += 2;
                            while (pos < text.Length && char.IsDigit(text[pos]))
                            {
                                pos++;
                            }
                        }
                        var number = text.Substring(start, pos - start);
                        if (!double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                        {
                            return null;
                        }
                        value = number;
                        break;
                    }
                    case 'h':
                    {
                        var start = pos;
                        while (pos < text.Length && Uri.IsHexDigit(text[pos]))
                        {
                            pos++;
                        }
                        if (pos == start || pos - start > 15)
                        {
                            return null;
                        }
                        value = long.Parse(text.Substring(start, pos - start), NumberStyles.HexNumber, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                        break;
                    }
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                        if (pos >= text.Length)
                        {
                            return null;
                        }
                        usesBytes = true;
                        bytes |= (long)(text[pos] & 0xFF) << (8 * (e - '1'));
                        pos++;
                        break;
                    case 's':
                    {
                        var start = i;
                        while (i < pattern.Length && char.IsDigit(pattern[i]))
                        {
                            i++;
                        }
                        if (i == start)
                        {
                            throw new FormatException("\\s needs the number of characters, e.g. \\s12");
                        }
                        pos += int.Parse(pattern.Substring(start, i - start), CultureInfo.InvariantCulture);
                        if (pos > text.Length)
                        {
                            return null;
                        }
                        break;
                    }
                    case '.':
                        if (pos >= text.Length)
                        {
                            return null;
                        }
                        pos++;
                        break;
                    case 'd':
                        if (pos >= text.Length || !char.IsDigit(text[pos]))
                        {
                            return null;
                        }
                        pos++;
                        break;
                    case 'm':
                        if (pos >= text.Length || !char.IsLetterOrDigit(text[pos]))
                        {
                            return null;
                        }
                        pos++;
                        break;
                    case 'a':
                        if (pos >= text.Length || !char.IsLetter(text[pos]))
                        {
                            return null;
                        }
                        pos++;
                        break;
                    case '#':
                    {
                        var start = pos;
                        if (pos < text.Length && (text[pos] == '-' || text[pos] == '+'))
                        {
                            pos++;
                        }
                        while (pos < text.Length && (char.IsDigit(text[pos]) || text[pos] == '.'))
                        {
                            pos++;
                        }
                        if (pos == start)
                        {
                            return null;
                        }
                        break;
                    }
                    case 'w':
                    {
                        var start = pos;
                        while (pos < text.Length && char.IsLetterOrDigit(text[pos]))
                        {
                            pos++;
                        }
                        if (pos == start)
                        {
                            return null;
                        }
                        break;
                    }
                    default:
                    {
                        // \t \r \n \xNN \\ and other escaped characters must match exactly
                        var literal = UnescapeOne(pattern, ref i, e);
                        if (pos >= text.Length || text[pos] != literal)
                        {
                            return null;
                        }
                        pos++;
                        break;
                    }
                }
            }

            if (usesBytes)
            {
                return bytes.ToString(CultureInfo.InvariantCulture);
            }
            return value;
        }

        private static char UnescapeOne(string pattern, ref int i, char e)
        {
            switch (e)
            {
                case 't': return '\t';
                case 'r': return '\r';
                case 'n': return '\n';
                case 'x':
                    if (i + 2 > pattern.Length || !Uri.IsHexDigit(pattern[i]) || !Uri.IsHexDigit(pattern[i + 1]))
                    {
                        throw new FormatException("\\x needs two hexadecimal digits, e.g. \\x0A");
                    }
                    var c = (char)Convert.ToInt32(pattern.Substring(i, 2), 16);
                    i += 2;
                    return c;
                default:
                    return e;
            }
        }

        private static string Unescape(string text)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] == '\\' && i + 1 < text.Length)
                {
                    var j = i + 2;
                    builder.Append(UnescapeOne(text, ref j, text[i + 1]));
                    i = j - 1;
                }
                else
                {
                    builder.Append(text[i]);
                }
            }
            return builder.ToString();
        }
    }
}
