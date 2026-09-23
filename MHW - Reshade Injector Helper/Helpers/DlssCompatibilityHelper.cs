using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace MHW___Reshade_Injector_Helper.Helpers
{
    /// <summary>
    /// MHW loads nvngx_dlss.dll under DX12 even when DLSS is disabled. Some DLLs
    /// installed by DLSS swapping tools can crash during ReShade 6.8 startup.
    /// Temporarily hides that optional DLL only while DLSS is explicitly Off.
    /// </summary>
    internal sealed class DlssCompatibilitySession : IDisposable
    {
        private readonly string dlssPath;
        private readonly string backupPath;
        private bool restoreRequired;

        private DlssCompatibilitySession(string dlssPath, string backupPath, bool restoreRequired)
        {
            this.dlssPath = dlssPath;
            this.backupPath = backupPath;
            this.restoreRequired = restoreRequired;
        }

        public static DlssCompatibilitySession Prepare(string gameDirectory)
        {
            var dlssPath = Path.Combine(gameDirectory, "nvngx_dlss.dll");
            var backupPath = dlssPath + ".reshade-helper-disabled";
            var graphicsOptionsPath = Path.Combine(gameDirectory, "graphics_option.ini");

            var dlssEnabled = ReadOption(graphicsOptionsPath, "NVIDIA DLSS");
            var dx12Enabled = ReadOption(graphicsOptionsPath, "DirectX12Enable");
            var versionSource = File.Exists(dlssPath) ? dlssPath : backupPath;
            var dlssVersion = File.Exists(versionSource) ? GetVersion(versionSource) : null;
            var normalizedVersion = NormalizeVersion(dlssVersion);
            // Only quarantine the exact version observed in the crash dump. Other
            // swapped versions (for example 1.2.14) may work and must remain present
            // so the user can enable DLSS after launch.
            var isKnownCrashingVersion = normalizedVersion.StartsWith("1.3.2", StringComparison.OrdinalIgnoreCase);
            var shouldDisable = IsOff(dlssEnabled) && IsOn(dx12Enabled) && isKnownCrashingVersion;

            // Recover a DLL left hidden if the helper was force-closed previously.
            if (File.Exists(backupPath) && !File.Exists(dlssPath) && !shouldDisable)
            {
                File.Move(backupPath, dlssPath);
                Console.WriteLine("Recovered nvngx_dlss.dll left by a previous interrupted run.");
            }

            if (!shouldDisable || (!File.Exists(dlssPath) && !File.Exists(backupPath)))
            {
                if (File.Exists(dlssPath))
                    PrintVersion(dlssPath);

                if (IsOn(dlssEnabled))
                    Console.WriteLine("DLSS is enabled; its DLL will not be changed by the helper.");
                else if (!string.IsNullOrWhiteSpace(dlssVersion))
                    Console.WriteLine("No DLSS compatibility workaround is needed for this version.");

                return new DlssCompatibilitySession(dlssPath, backupPath, false);
            }

            if (File.Exists(dlssPath) && File.Exists(backupPath))
                throw new IOException("Both nvngx_dlss.dll and its helper backup exist. Remove or restore the .reshade-helper-disabled copy before retrying.");

            if (File.Exists(dlssPath))
            {
                PrintVersion(dlssPath);
                File.Move(dlssPath, backupPath);
                Console.WriteLine("DLSS is Off: temporarily disabled nvngx_dlss.dll for ReShade 6.8 compatibility.");
            }
            else
            {
                Console.WriteLine("Continuing a previous temporary DLSS compatibility session.");
            }

            return new DlssCompatibilitySession(dlssPath, backupPath, true);
        }

        public void Dispose()
        {
            if (!restoreRequired)
                return;

            restoreRequired = false;

            try
            {
                if (File.Exists(backupPath) && !File.Exists(dlssPath))
                {
                    File.Move(backupPath, dlssPath);
                    Console.WriteLine("Restored the original nvngx_dlss.dll.");
                }
                else if (File.Exists(backupPath))
                {
                    Console.Error.WriteLine("Warning: nvngx_dlss.dll was not restored because a file now exists at the original path.");
                }
            }
            catch (Exception ex)
            {
                ErrorLogHelper.Log("Failed to restore nvngx_dlss.dll. Restore the .reshade-helper-disabled file manually.", ex);
            }
        }

        private static string ReadOption(string path, string key)
        {
            if (!File.Exists(path))
                return null;

            try
            {
                var prefix = key + "=";
                var line = File.ReadLines(path)
                    .Select(value => value.Trim())
                    .LastOrDefault(value => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
                return line == null ? null : line.Substring(prefix.Length).Trim();
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }

        private static bool IsOn(string value)
        {
            return string.Equals(value, "On", StringComparison.OrdinalIgnoreCase) || value == "1";
        }

        private static bool IsOff(string value)
        {
            return string.Equals(value, "Off", StringComparison.OrdinalIgnoreCase) || value == "0";
        }

        private static void PrintVersion(string path)
        {
            var version = GetVersion(path) ?? "unknown";
            Console.WriteLine($"Detected nvngx_dlss.dll version: {version}");
        }

        private static string GetVersion(string path)
        {
            return FileVersionInfo.GetVersionInfo(path).FileVersion;
        }

        private static string NormalizeVersion(string version)
        {
            return (version ?? string.Empty)
                .Replace(", ", ".")
                .Replace(",", ".")
                .Replace(" ", string.Empty);
        }
    }
}
