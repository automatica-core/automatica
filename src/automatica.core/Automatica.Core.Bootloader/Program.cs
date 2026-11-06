
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Automatica.Core.Base.Common;

namespace Automatica.Core.Bootloader
{
    class Program
    {
        /// <summary>
        /// Top level directories of the installation that hold user data, they are never backed up, deleted or restored by the update
        /// </summary>
        private static readonly string[] ExcludedDirectories = { "persistent", "logs", "framework" };

        static void Main(string[] args)
        {
            try
            {
                var fi = new FileInfo(Assembly.GetEntryAssembly().Location);

                var appName = Path.Combine(fi.DirectoryName, "..", "automatica", "Automatica.Core.Watchdog");
              
                ProcessStartInfo processInfo = null;
                if (!File.Exists(appName))
                {
                    if(!File.Exists(appName+".dll"))
                    {
                        Console.Error.WriteLine($"Could not fine {appName} or {appName}.dll - exiting startup...");
                        return;
                    }
                    //maybe we don't have built the app with correct environments? so start it with dotnet Automatica.Core.Watchdog dll
                    Console.WriteLine($"Starting with dotnet {appName}.dll");
                    processInfo = new ProcessStartInfo("dotnet", appName+".dll");
                }
                else
                {
                    processInfo = new ProcessStartInfo(appName);
                }
                Console.WriteLine($"Starting {appName}");
                processInfo.WorkingDirectory = Environment.CurrentDirectory;

                Process process = null;
                try
                {

                    while (true)
                    {
                        CheckForUpdateAndInstall();

                        process = Process.Start(processInfo);
                        process.WaitForExit();

                        Console.WriteLine($"Process exited with code {process.ExitCode}");

                        Thread.Sleep(500);
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"{e}");
                }
            }
            catch(Exception e)
            {
                Console.Error.WriteLine($"{e}");
            }
        }

        private static void CheckForUpdateAndInstall()
        {
            var fi = new FileInfo(Assembly.GetEntryAssembly().Location);
            var tmpPath = Path.Combine(ServerInfo.GetTempPath(), $"Automatica.Core.Update");

            var sourceDir = tmpPath;
            var targetDir = Path.Combine(fi.DirectoryName, "..", "automatica");

            if (!Directory.Exists(sourceDir))
            {
                Console.Error.WriteLine($"Source directory does not exist ({sourceDir})...");
                return;
            }
            if (!Directory.Exists(targetDir))
            {
                Console.Error.WriteLine($"Target directory does not exist ({targetDir})...");
                return;
            }

            Console.WriteLine("Starting update...");

            var tmpDirectory = Path.Combine(ServerInfo.GetTempPath(), Guid.NewGuid().ToString().Replace("-", ""));

            Console.WriteLine("Back up current binaries...");
            if (!CopyDirectory(targetDir, tmpDirectory, true))
            {
                Console.Error.WriteLine("Backup failed...skip update");
                TryDeleteDirectory(tmpDirectory);
                TryDeleteDirectory(tmpPath);
                return;
            }

            // DeleteAllFilesInDirectory(targetDir);

            Console.WriteLine("Copy new binaries...");
            if (!CopyDirectory(sourceDir, targetDir, false))
            {
                Console.WriteLine("Copy failed...restore old binaries");
                DeleteInstallation(targetDir);
                if (CopyDirectory(tmpDirectory, targetDir, true))
                {
                    Directory.Delete(tmpDirectory, true);
                }
                else
                {
                    // keep the backup, it is the only complete copy of the old binaries
                    Console.Error.WriteLine($"Restore failed, the backup of the old binaries is kept in {tmpDirectory}");
                }
            }
            else
            {
                Console.WriteLine("Copy success...remove old binaries");
                Directory.Delete(tmpDirectory, true);
                Directory.Delete(sourceDir, true);
            }


            if (Directory.Exists(tmpPath))
            {
                Directory.Delete(tmpPath, true);
            }

            Console.WriteLine("Starting update...done");
        }

        private static void DeleteAllFilesInDirectory(string directory)
        {
            var di = new DirectoryInfo(directory);

            foreach (var file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (var dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
        }


        private static bool IsExcluded(string root, string path)
        {
            var relativePath = Path.GetRelativePath(root, path);
            var firstSegment = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
            return ExcludedDirectories.Contains(firstSegment, StringComparer.OrdinalIgnoreCase);
        }

        private static void TryDeleteDirectory(string directory)
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"{e}, Could not delete {directory}");
            }
        }

        /// <summary>
        /// Deletes the installed binaries, the excluded directories (user data) are kept
        /// </summary>
        private static void DeleteInstallation(string directory)
        {
            var di = new DirectoryInfo(directory);

            foreach (var file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (var dir in di.GetDirectories())
            {
                if (IsExcluded(directory, dir.FullName))
                {
                    continue;
                }
                dir.Delete(true);
            }
        }

        private static string ToDestination(string source, string destination, string path)
        {
            return Path.Combine(destination, Path.GetRelativePath(source, path));
        }

        /// <param name="skipExcluded">skip the user data directories (backup and restore of the installation)</param>
        private static bool CopyDirectory(string source, string destination, bool skipExcluded)
        {
            Directory.CreateDirectory(destination);

            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                try
                {
                    if (skipExcluded && IsExcluded(source, dirPath))
                    {
                        continue;
                    }
                    Directory.CreateDirectory(ToDestination(source, destination, dirPath));
                }
                catch(Exception e)
                {
                    Console.Error.WriteLine($"{e}, Could not create directory (source: {dirPath}, target: {ToDestination(source, destination, dirPath)})");
                    return false;
                }
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(source, "*.*", SearchOption.AllDirectories))
            {
                try
                {
                    if (skipExcluded && IsExcluded(source, newPath))
                    {
                        continue;
                    }

                    var fileInfo = new FileInfo(newPath);
                    if(fileInfo.Name == "Automatica.Core.Bootloader" )
                    {
                        Console.WriteLine($"Ignore update of Automatica.Core.Bootloader");
                        continue;
                    }

                    if (fileInfo.Name.EndsWith(".log"))
                    {
                        Console.WriteLine($"Ignore log file...");
                        continue;
                    }

                    var newFile = ToDestination(source, destination, newPath);
                    Console.WriteLine($"Copy {newPath} to {newFile}");
                    File.Copy(newPath, newFile, true); 
                }
                catch (Exception e)
                {
                    // a missing file leaves a half updated installation, the caller restores the backup
                    Console.Error.WriteLine($"{e}, Could not copy file (source: {newPath}, target: {ToDestination(source, destination, newPath)})");
                    return false;
                }
            }
            return true;
        }
    }
}
