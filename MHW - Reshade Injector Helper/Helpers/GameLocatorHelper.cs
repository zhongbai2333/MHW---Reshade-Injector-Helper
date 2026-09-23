using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MHW___Reshade_Injector_Helper.Helpers
{
    public static class GameLocatorHelper
    {
        public const string MonsterHunterWorldAppId = "582010";
        private const string GameExecutableName = "MonsterHunterWorld.exe";
        private const string DefaultInstallDirectory = "Monster Hunter World";

        public static string FindMonsterHunterWorld(IEnumerable<string> steamRoots = null)
        {
            var roots = steamRoots == null
                ? DiscoverSteamRoots().Concat(DiscoverCommonSteamRoots())
                : steamRoots;

            foreach (var root in NormalizeExistingDirectories(roots))
            {
                foreach (var library in DiscoverLibraries(root))
                {
                    var executable = FindInLibrary(library);
                    if (executable != null)
                        return executable;
                }
            }

            return null;
        }

        public static string FindSteamUserDataPath(string gameExecutable)
        {
            var candidates = new List<string>();
            var marker = $"{Path.DirectorySeparatorChar}steamapps{Path.DirectorySeparatorChar}";
            var markerIndex = gameExecutable.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex > 0)
                candidates.Add(Path.Combine(gameExecutable.Substring(0, markerIndex), "userdata"));

            candidates.AddRange(DiscoverSteamRoots().Select(root => Path.Combine(root, "userdata")));
            candidates.AddRange(DiscoverCommonSteamRoots().Select(root => Path.Combine(root, "userdata")));

            return candidates
                .Where(Directory.Exists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault() ?? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "userdata");
        }

        private static IEnumerable<string> DiscoverSteamRoots()
        {
            var roots = new List<string>();
            AddRegistryValue(roots, RegistryHive.CurrentUser, RegistryView.Default, @"Software\Valve\Steam", "SteamPath");
            AddRegistryValue(roots, RegistryHive.LocalMachine, RegistryView.Registry32, @"Software\Valve\Steam", "InstallPath");
            AddRegistryValue(roots, RegistryHive.LocalMachine, RegistryView.Registry64, @"Software\Valve\Steam", "InstallPath");
            return roots;
        }

        private static IEnumerable<string> DiscoverCommonSteamRoots()
        {
            var roots = new List<string>
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam")
            };

            foreach (var drive in DriveInfo.GetDrives().Where(drive => drive.IsReady && drive.DriveType == DriveType.Fixed))
            {
                roots.Add(Path.Combine(drive.RootDirectory.FullName, "Steam"));
                roots.Add(Path.Combine(drive.RootDirectory.FullName, "SteamLibrary"));
                roots.Add(Path.Combine(drive.RootDirectory.FullName, "Games", "Steam"));
                roots.Add(Path.Combine(drive.RootDirectory.FullName, "Games", "SteamLibrary"));
            }

            return roots;
        }

        private static IEnumerable<string> DiscoverLibraries(string steamRoot)
        {
            var libraries = new List<string> { steamRoot };
            var libraryFile = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(libraryFile))
                return libraries;

            try
            {
                var contents = File.ReadAllText(libraryFile);
                var matches = Regex.Matches(contents, "\"(?:path|\\d+)\"\\s+\"([^\"]+)\"", RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                    libraries.Add(match.Groups[1].Value.Replace("\\\\", "\\"));
            }
            catch (IOException)
            {
                // Continue with the Steam root when the library list is temporarily locked.
            }
            catch (UnauthorizedAccessException)
            {
                // Continue with accessible locations only.
            }

            return NormalizeExistingDirectories(libraries);
        }

        private static string FindInLibrary(string libraryRoot)
        {
            var installDirectory = DefaultInstallDirectory;
            var manifest = Path.Combine(libraryRoot, "steamapps", $"appmanifest_{MonsterHunterWorldAppId}.acf");
            if (File.Exists(manifest))
            {
                try
                {
                    var match = Regex.Match(File.ReadAllText(manifest), "\"installdir\"\\s+\"([^\"]+)\"", RegexOptions.IgnoreCase);
                    if (match.Success)
                        installDirectory = match.Groups[1].Value;
                }
                catch (IOException)
                {
                    // The standard directory name below is still worth checking.
                }
                catch (UnauthorizedAccessException)
                {
                    // The standard directory name below is still worth checking.
                }
            }

            var executable = Path.Combine(libraryRoot, "steamapps", "common", installDirectory, GameExecutableName);
            return File.Exists(executable) ? Path.GetFullPath(executable) : null;
        }

        private static IEnumerable<string> NormalizeExistingDirectories(IEnumerable<string> paths)
        {
            return paths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => path.Replace('/', Path.DirectorySeparatorChar))
                .Where(Directory.Exists)
                .Select(Path.GetFullPath)
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static void AddRegistryValue(List<string> results, RegistryHive hive, RegistryView view, string keyPath, string valueName)
        {
            try
            {
                using (var baseKey = RegistryKey.OpenBaseKey(hive, view))
                using (var key = baseKey.OpenSubKey(keyPath))
                {
                    var value = key?.GetValue(valueName) as string;
                    if (!string.IsNullOrWhiteSpace(value))
                        results.Add(value);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Registry access is optional; filesystem candidates are checked next.
            }
        }
    }
}
