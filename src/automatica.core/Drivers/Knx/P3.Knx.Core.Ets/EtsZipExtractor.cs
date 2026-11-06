using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;

namespace P3.Knx.Core.Ets
{
    /// <summary>
    /// Extracts ETS project archives without leaving the target directory and with size limits
    /// </summary>
    internal sealed class EtsZipExtractor
    {
        internal const int DefaultMaxEntries = 20000;
        internal const long DefaultMaxEntrySize = 512L * 1024 * 1024;
        internal const long DefaultMaxTotalSize = 1024L * 1024 * 1024;

        private long _totalBytes;
        private int _totalEntries;

        public int MaxEntries { get; set; } = DefaultMaxEntries;
        public long MaxEntrySize { get; set; } = DefaultMaxEntrySize;
        public long MaxTotalSize { get; set; } = DefaultMaxTotalSize;

        /// <summary>
        /// Extracts <paramref name="zipPath"/> into <paramref name="targetDirectory"/>. The limits apply to all archives extracted with this instance.
        /// </summary>
        public void Extract(string zipPath, string targetDirectory, string password = null)
        {
            var root = Path.GetFullPath(targetDirectory);
            if (!root.EndsWith(Path.DirectorySeparatorChar))
            {
                root += Path.DirectorySeparatorChar;
            }
            Directory.CreateDirectory(root);

            using var zip = new ZipFile(zipPath);
            if (!string.IsNullOrEmpty(password))
            {
                zip.Password = password;
            }

            var buffer = new byte[81920];

            foreach (ZipEntry entry in zip)
            {
                if (++_totalEntries > MaxEntries)
                {
                    throw new EtsProjectParserInvalidZipFileException(new InvalidDataException($"archive contains more than {MaxEntries} entries"));
                }

                var destination = GetSafeDestination(root, entry.Name);

                if (entry.IsDirectory)
                {
                    Directory.CreateDirectory(destination);
                    continue;
                }

                if (!entry.IsFile)
                {
                    continue;
                }

                if (entry.Size > MaxEntrySize)
                {
                    throw new EtsProjectParserInvalidZipFileException(new InvalidDataException($"entry {entry.Name} is too large"));
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

                using var input = zip.GetInputStream(entry);
                using var output = File.Create(destination);

                long entryBytes = 0;
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    entryBytes += read;
                    _totalBytes += read;

                    // the sizes in the archive can be forged, count what is actually written
                    if (entryBytes > MaxEntrySize || _totalBytes > MaxTotalSize)
                    {
                        throw new EtsProjectParserInvalidZipFileException(new InvalidDataException("archive exceeds the size limit"));
                    }

                    output.Write(buffer, 0, read);
                }
            }
        }

        internal static string GetSafeDestination(string root, string entryName)
        {
            if (string.IsNullOrEmpty(entryName) || entryName.IndexOf('\0') >= 0 || entryName.IndexOf(':') >= 0)
            {
                throw new EtsProjectParserInvalidZipFileException(new InvalidDataException($"invalid entry name {entryName}"));
            }

            var normalized = entryName.Replace('\\', '/').TrimStart('/');
            var destination = Path.GetFullPath(Path.Combine(root, normalized));

            if (!destination.StartsWith(root, StringComparison.Ordinal) && destination + Path.DirectorySeparatorChar != root)
            {
                throw new EtsProjectParserInvalidZipFileException(new InvalidDataException($"entry {entryName} points outside the target directory"));
            }

            return destination;
        }
    }
}
