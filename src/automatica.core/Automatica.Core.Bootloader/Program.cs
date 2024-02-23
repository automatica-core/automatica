﻿
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using Automatica.Core.Base.Common;

namespace Automatica.Core.Bootloader
{
    class Program
    {
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
            CopyDirectory(targetDir, tmpDirectory);

            // DeleteAllFilesInDirectory(targetDir);

            Console.WriteLine("Copy new binaries...");
            if (!CopyDirectory(sourceDir, targetDir))
            {
                Console.WriteLine("Copy failed...restore old binaries");
                Directory.Delete(targetDir, true);
                CopyDirectory(tmpDirectory, targetDir);
                Directory.Delete(tmpDirectory, true);
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


        private static bool CopyDirectory( string source, string destination)
        {
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                try
                {
                    if (dirPath == "persistent" || dirPath == "logs" || dirPath == "framework")
                    {
                        Console.WriteLine("Ignoring logs/framework directory...");
                        continue;
                    }
                    Directory.CreateDirectory(dirPath.Replace(source, destination));
                }
                catch(Exception e)
                {
                    Console.Error.WriteLine($"{e}, Could not create directory (source: {dirPath}, target: {dirPath.Replace(source, destination)})");
                    return false;
                }
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(source, "*.*", SearchOption.AllDirectories))
            {
                try
                {
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

                    var newFile = newPath.Replace(source, destination);
                    Console.WriteLine($"Copy {newPath} to {newFile}");
                    File.Copy(newPath, newFile, true); 
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"{e}, Could not copy file (source: {newPath}, target: newPath.Replace(source, destination))");
                }
            }
            return true;
        }
    }
}
