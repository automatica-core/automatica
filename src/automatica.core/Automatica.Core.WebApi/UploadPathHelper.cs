using System;
using System.IO;

namespace Automatica.Core.WebApi
{
    internal static class UploadPathHelper
    {
        /// <summary>
        /// A random file name that keeps a short, plain extension of the client name (the importers check it)
        /// </summary>
        public static string CreateRandomFileName(string clientFileName)
        {
            var extension = Path.GetExtension(Path.GetFileName(clientFileName ?? string.Empty)) ?? string.Empty;
            if (extension.Length > 16 || !System.Text.RegularExpressions.Regex.IsMatch(extension, "^(\\.[A-Za-z0-9]+)?$"))
            {
                extension = string.Empty;
            }

            return Guid.NewGuid().ToString("N") + extension.ToLowerInvariant();
        }

        /// <summary>
        /// Builds the target path for an uploaded file inside <paramref name="targetDirectory"/>. Only the file name part of
        /// the client supplied name is used and the resulting full path must stay inside the target directory.
        /// </summary>
        public static string GetSafeFilePath(string targetDirectory, string clientFileName)
        {
            var fileName = Path.GetFileName(clientFileName ?? string.Empty);

            if (string.IsNullOrWhiteSpace(fileName) || fileName == "." || fileName == ".." ||
                fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new ArgumentException("Invalid file name", nameof(clientFileName));
            }

            var fullTarget = Path.GetFullPath(targetDirectory);
            if (!Path.EndsInDirectorySeparator(fullTarget))
            {
                fullTarget += Path.DirectorySeparatorChar;
            }

            var fullPath = Path.GetFullPath(Path.Combine(fullTarget, fileName));
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            if (!fullPath.StartsWith(fullTarget, comparison))
            {
                throw new ArgumentException("Invalid file name", nameof(clientFileName));
            }

            return fullPath;
        }
    }
}
