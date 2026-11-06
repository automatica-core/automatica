using System;
using System.IO;

namespace Automatica.Core.WebApi
{
    internal static class UploadPathHelper
    {
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
