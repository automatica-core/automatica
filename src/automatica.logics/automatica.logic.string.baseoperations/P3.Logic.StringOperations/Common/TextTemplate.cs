using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace P3.Logic.StringOperations.Common
{
    /// <summary>
    /// Placeholders in the texts of the text blocks, Loxone style:
    /// <c>&lt;v1&gt;</c> .. <c>&lt;v8&gt;</c> value of input 1..8, <c>&lt;v&gt;</c> the (first) value,
    /// <c>&lt;v1.2&gt;</c> / <c>&lt;v.1&gt;</c> the value with 2 / 1 decimals, <c>&lt;n&gt;</c> the name of the source.
    /// Placeholders the block does not know stay in the text.
    /// </summary>
    internal static class TextTemplate
    {
        private static readonly Regex Placeholder = new(@"<(?<key>v(?<index>[1-9])?|n)(?:\.(?<decimals>\d{1,2}))?>",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        /// <param name="template">the text with placeholders</param>
        /// <param name="value">value of input 1..8 (0 = &lt;v&gt;), returns false when the block has no such input</param>
        /// <param name="name">the source name for &lt;n&gt;, null when the block has none</param>
        internal static string Render(string template, Func<int, (bool Known, object Value)> value, string name = null)
        {
            if (string.IsNullOrEmpty(template))
            {
                return string.Empty;
            }

            return Placeholder.Replace(template, match =>
            {
                var key = match.Groups["key"].Value;
                if (key.Equals("n", StringComparison.OrdinalIgnoreCase))
                {
                    return name ?? match.Value;
                }

                var index = match.Groups["index"].Success ? int.Parse(match.Groups["index"].Value, CultureInfo.InvariantCulture) : 0;
                var (known, v) = value(index);
                if (!known)
                {
                    return match.Value;
                }

                return match.Groups["decimals"].Success
                    ? TextValue.Format(v, int.Parse(match.Groups["decimals"].Value, CultureInfo.InvariantCulture))
                    : TextValue.ToText(v);
            });
        }

        /// <summary>
        /// Placeholders of blocks with numbered inputs (values[0] = input 1); &lt;v&gt; is input 1
        /// </summary>
        internal static string Render(string template, object[] values, string name = null)
        {
            return Render(template, index =>
            {
                var i = index == 0 ? 0 : index - 1;
                return i < values.Length ? (true, values[i]) : (false, null);
            }, name);
        }
    }
}
