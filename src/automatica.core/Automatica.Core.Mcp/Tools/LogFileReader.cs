using System.Text;

namespace Automatica.Core.Mcp.Tools
{
    public record LogFileInfo(string File, long SizeBytes, DateTime ModifiedAt);

    public record LogTail(
        string File,
        long SizeBytes,
        DateTime ModifiedAt,
        int LineCount,
        bool ReachedStartOfFile,
        bool ScanLimitReached,
        IList<string> Lines);

    /// <summary>
    /// Reads the server log files for the MCP log tools. Files are addressed relative to the log directory and never
    /// outside of it. The files are still being written (and can be 128 MB), so they are opened shared and read
    /// backwards from the end.
    /// </summary>
    public static class LogFileReader
    {
        public const int DefaultLines = 200;
        public const int MaxLines = 2000;
        public const int MaxLineLength = 4000;
        public const long MaxScanBytes = 64L * 1024 * 1024;

        private const int ChunkSize = 64 * 1024;

        private static StringComparison PathComparison =>
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        public static IList<LogFileInfo> List(string directory, string query = null)
        {
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                return new List<LogFileInfo>();
            }

            var root = Path.GetFullPath(directory);
            return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Select(a => new FileInfo(a))
                .Select(a => new LogFileInfo(ToRelative(root, a.FullName), a.Length, a.LastWriteTime))
                .Where(a => string.IsNullOrWhiteSpace(query) || a.File.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(a => a.ModifiedAt)
                .ThenBy(a => a.File, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// The full path of a log file, or <see cref="FileNotFoundException"/> when the file does not exist or lies
        /// outside the log directory.
        /// </summary>
        public static string Resolve(string directory, string file)
        {
            if (string.IsNullOrWhiteSpace(file) || string.IsNullOrEmpty(directory))
            {
                throw new FileNotFoundException("No log file given");
            }

            var root = Path.GetFullPath(directory);
            var rootWithSeparator = Path.EndsInDirectorySeparator(root) ? root : root + Path.DirectorySeparatorChar;
            var relative = file.Replace('\\', '/').TrimStart('/');
            var full = Path.GetFullPath(Path.Combine(root, relative));

            if (!full.StartsWith(rootWithSeparator, PathComparison) || !File.Exists(full))
            {
                throw new FileNotFoundException($"'{file}' is not a log file. Use list_log_files for the available files.");
            }

            return full;
        }

        /// <summary>
        /// The last <paramref name="lines"/> lines of a log file (oldest first), only lines containing
        /// <paramref name="contains"/> when it is given. Scans at most <see cref="MaxScanBytes"/> from the end.
        /// </summary>
        public static LogTail Tail(string directory, string file, int lines = DefaultLines, string contains = null)
        {
            var path = Resolve(directory, file);
            var wanted = Math.Clamp(lines, 1, MaxLines);
            var filter = string.IsNullOrWhiteSpace(contains) ? null : contains;
            var collected = new List<string>(Math.Min(wanted, 256));

            var info = new FileInfo(path);
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

            var position = stream.Length;
            var carry = Array.Empty<byte>();
            var buffer = new byte[ChunkSize];
            long scanned = 0;
            var scanLimitReached = false;

            void Add(byte[] bytes, int start, int count)
            {
                if (count <= 0)
                {
                    return;
                }

                var line = Encoding.UTF8.GetString(bytes, start, count).TrimEnd('\r');
                if (line.Length == 0 || (filter != null && !line.Contains(filter, StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                collected.Add(line.Length > MaxLineLength ? line[..MaxLineLength] + " …" : line);
            }

            while (position > 0 && collected.Count < wanted)
            {
                if (scanned >= MaxScanBytes)
                {
                    scanLimitReached = true;
                    break;
                }

                var read = (int)Math.Min(ChunkSize, position);
                position -= read;
                stream.Position = position;
                stream.ReadExactly(buffer, 0, read);
                scanned += read;

                // the chunk plus the start of the line that continues after it
                var block = new byte[read + carry.Length];
                Buffer.BlockCopy(buffer, 0, block, 0, read);
                Buffer.BlockCopy(carry, 0, block, read, carry.Length);

                var end = block.Length;
                for (var i = block.Length - 1; i >= 0 && collected.Count < wanted; i--)
                {
                    if (block[i] == (byte)'\n')
                    {
                        Add(block, i + 1, end - i - 1);
                        end = i;
                    }
                }

                carry = block[..end];
            }

            // the first line of the file has no newline in front of it
            if (position == 0 && !scanLimitReached && collected.Count < wanted)
            {
                Add(carry, 0, carry.Length);
            }

            collected.Reverse();
            return new LogTail(
                ToRelative(Path.GetFullPath(directory), path),
                info.Length,
                info.LastWriteTime,
                collected.Count,
                position == 0 && collected.Count < wanted,
                scanLimitReached,
                collected);
        }

        private static string ToRelative(string root, string fullPath)
        {
            return Path.GetRelativePath(root, fullPath).Replace('\\', '/');
        }
    }
}
