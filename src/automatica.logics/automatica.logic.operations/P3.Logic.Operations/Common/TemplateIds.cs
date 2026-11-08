using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace P3.Logic.Operations.Common
{
    /// <summary>
    /// Stable template ids for repeated interfaces (circuit 1..n): derived from a fixed name, never change the names
    /// </summary>
    internal static class TemplateIds
    {
        internal static Guid Of(string name)
        {
            return new Guid(MD5.HashData(Encoding.UTF8.GetBytes(name)));
        }

        internal static Guid[] Series(string prefix, int count)
        {
            return Enumerable.Range(1, count).Select(i => Of($"{prefix}{i}")).ToArray();
        }
    }
}
