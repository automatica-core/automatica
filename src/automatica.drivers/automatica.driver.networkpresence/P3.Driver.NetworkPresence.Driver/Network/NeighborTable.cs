using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace P3.Driver.NetworkPresence.Driver.Network
{
    internal interface INeighborSource
    {
        Task<IReadOnlyList<NeighborEntry>> ReadAsync(CancellationToken token = default);
    }

    /// <summary>
    /// Reads the neighbor table of the host: /proc/net/arp on Linux (also inside a container with host networking), "arp -a" elsewhere
    /// </summary>
    internal sealed class SystemNeighborSource : INeighborSource
    {
        private const string ProcNetArp = "/proc/net/arp";

        public async Task<IReadOnlyList<NeighborEntry>> ReadAsync(CancellationToken token = default)
        {
            if (File.Exists(ProcNetArp))
            {
                return NeighborTableParser.ParseProcNetArp(await File.ReadAllTextAsync(ProcNetArp, token));
            }

            using var process = Process.Start(new ProcessStartInfo("arp", "-a")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (process == null)
            {
                return Array.Empty<NeighborEntry>();
            }
            var output = await process.StandardOutput.ReadToEndAsync(token);
            await process.WaitForExitAsync(token);
            return NeighborTableParser.ParseArpCommand(output);
        }
    }

    internal static class NeighborTableParser
    {
        // ATF_COM: the entry is resolved. Incomplete and failed entries have flags 0x0 and an all-zero MAC.
        private const int AtfCom = 0x2;

        /// <summary>
        /// "IP address  HW type  Flags  HW address  Mask  Device"
        /// </summary>
        public static IReadOnlyList<NeighborEntry> ParseProcNetArp(string content)
        {
            var result = new List<NeighborEntry>();
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            for (var i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 4 || !IPAddress.TryParse(parts[0], out var ip))
                {
                    continue;
                }

                var flagsText = parts[2].StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? parts[2][2..] : parts[2];
                int.TryParse(flagsText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var flags);
                var mac = NeighborEntry.NormalizeMac(parts[3]);

                result.Add(new NeighborEntry(ip, mac, mac != null && (flags & AtfCom) != 0));
            }
            return result;
        }

        private static readonly Regex ArpLine = new(@"^\s*\(?(?<ip>\d{1,3}(\.\d{1,3}){3})\)?\s+(at\s+)?(?<mac>([0-9a-fA-F]{1,2}[:-]){5}[0-9a-fA-F]{1,2})", RegexOptions.Compiled);

        /// <summary>
        /// Windows ("  192.168.1.10   aa-bb-cc-dd-ee-ff   dynamic") and BSD/macOS ("? (192.168.1.10) at aa:bb:cc:dd:ee:ff on en0") output
        /// </summary>
        public static IReadOnlyList<NeighborEntry> ParseArpCommand(string output)
        {
            var result = new List<NeighborEntry>();
            foreach (var raw in output.Split('\n'))
            {
                var line = raw.TrimStart('?', ' ');
                var match = ArpLine.Match(line);
                if (!match.Success || !IPAddress.TryParse(match.Groups["ip"].Value, out var ip))
                {
                    continue;
                }

                // BSD prints single digit octets ("a:b:c:1:2:3")
                var octets = match.Groups["mac"].Value.Split(':', '-');
                var mac = NeighborEntry.NormalizeMac(string.Concat(Array.ConvertAll(octets, o => o.PadLeft(2, '0'))));
                if (mac == null)
                {
                    continue;
                }
                result.Add(new NeighborEntry(ip, mac, true));
            }
            return result;
        }
    }
}
