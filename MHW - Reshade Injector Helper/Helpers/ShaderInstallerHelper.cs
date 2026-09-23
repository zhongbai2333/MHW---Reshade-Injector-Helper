using System;
using System.Diagnostics;
using System.IO;

namespace MHW___Reshade_Injector_Helper.Helpers
{
    public static class ShaderInstallerHelper
    {
        private const string InstallerFileName = "ReShade_Setup_6.8.0.exe";
        private const string RequiredVersion = "6.8.0";

        public static void EnsureReShadeInstallation(string helperDirectory, string gameExecutable)
        {
            ValidateBundledInjectorDll(helperDirectory);

            var gameDirectory = Path.GetDirectoryName(gameExecutable);
            var effectCount = CountEffects(gameDirectory);
            if (effectCount == 0)
            {
                RunOfficialInstaller(helperDirectory, gameExecutable);
                effectCount = CountEffects(gameDirectory);
            }

            if (effectCount == 0)
            {
                throw new InvalidOperationException(
                    "The installer closed, but no .fx files were found under the game's reshade-shaders\\Shaders folder. " +
                    "Run the helper again and select at least one shader package in the official installer.");
            }

            Console.WriteLine($"Game-local shader files found: {effectCount}");
        }

        private static void ValidateBundledInjectorDll(string helperDirectory)
        {
            var dllPath = Path.Combine(helperDirectory, "ReShade64.dll");
            if (!File.Exists(dllPath))
                throw new FileNotFoundException("ReShade64.dll is missing next to inject.exe.", dllPath);

            var versionInfo = FileVersionInfo.GetVersionInfo(dllPath);
            var description = versionInfo.FileDescription ?? string.Empty;
            if (versionInfo.ProductVersion == null ||
                !versionInfo.ProductVersion.StartsWith(RequiredVersion, StringComparison.OrdinalIgnoreCase) ||
                description.IndexOf("ReShade", StringComparison.OrdinalIgnoreCase) < 0)
            {
                throw new InvalidOperationException($"The bundled injection DLL is not official ReShade {RequiredVersion}.");
            }
        }

        private static void RunOfficialInstaller(string helperDirectory, string gameExecutable)
        {
            var installerPath = Path.Combine(helperDirectory, InstallerFileName);
            if (!File.Exists(installerPath))
                throw new FileNotFoundException("The official ReShade 6.8.0 installer is missing next to the helper.", installerPath);

            Console.WriteLine("No shaders were found in the game directory.");
            Console.WriteLine("Starting the official ReShade 6.8.0 installer.");
            Console.WriteLine("Select DirectX 10/11/12 and at least one shader package, then close the installer.");

            var setup = Process.Start(new ProcessStartInfo
            {
                FileName = installerPath,
                Arguments = $"\"{gameExecutable}\"",
                WorkingDirectory = helperDirectory,
                UseShellExecute = true
            });

            if (setup == null)
                throw new InvalidOperationException("The ReShade installer could not be started.");

            setup.WaitForExit();
        }

        private static int CountEffects(string gameDirectory)
        {
            var shaderDirectory = Path.Combine(gameDirectory, "reshade-shaders", "Shaders");
            return Directory.Exists(shaderDirectory)
                ? Directory.GetFiles(shaderDirectory, "*.fx", SearchOption.AllDirectories).Length
                : 0;
        }
    }
}
