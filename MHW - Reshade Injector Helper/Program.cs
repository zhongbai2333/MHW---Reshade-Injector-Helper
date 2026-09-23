using MadMilkman.Ini;
using MHW___Reshade_Injector_Helper.Helpers;
using MHW___Reshade_Injector_Helper.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

namespace MHW___Reshade_Injector_Helper
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            DlssCompatibilitySession dlssCompatibilitySession = null;
            CancellationTokenSource backupCancellation = null;
            Task backupTask = null;
            CancellationTokenSource aspectPatchCancellation = null;
            Task aspectPatchTask = null;
            Process injectorProcess = null;
            var windowHidden = false;

            try
            {
                var helperDirectory = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
                Environment.CurrentDirectory = helperDirectory;

                if (args.Length == 2 && string.Equals(args[0], "--prepare-only", StringComparison.OrdinalIgnoreCase))
                {
                    var gameExecutable = Path.GetFullPath(args[1]);
                    if (!File.Exists(gameExecutable))
                        throw new FileNotFoundException("Game executable not found.", gameExecutable);

                    ShaderInstallerHelper.EnsureReShadeInstallation(helperDirectory, gameExecutable);
                    PrintReShadeStatus(ReShadeConfigHelper.Prepare(helperDirectory, Path.GetDirectoryName(gameExecutable)));
                    Console.WriteLine("ReShade configuration repaired. No game was launched.");
                    return;
                }

                if (args.Any(arg => string.Equals(arg, "--show-steam-option", StringComparison.OrdinalIgnoreCase)))
                {
                    Console.WriteLine("Paste this into Monster Hunter: World > Properties > Launch Options:");
                    Console.WriteLine();
                    Console.WriteLine($"\"{Process.GetCurrentProcess().MainModule.FileName}\" --from-steam %command%");
                    Console.WriteLine();
                    if (!Console.IsInputRedirected)
                    {
                        Console.WriteLine("Press any key to close...");
                        Console.ReadKey(true);
                    }
                    return;
                }

                var fromSteam = args.Length > 0 &&
                    string.Equals(args[0], "--from-steam", StringComparison.OrdinalIgnoreCase);
                var keepWindowVisible = args.Any(arg =>
                    string.Equals(arg, "--no-hide", StringComparison.OrdinalIgnoreCase));
                var enableAspectPatch = args.Any(arg =>
                    string.Equals(arg, "--enable-aspect-patch", StringComparison.OrdinalIgnoreCase));

                var iniFileInfo = new FileInfo(Path.Combine(
                    helperDirectory,
                    $"{Process.GetCurrentProcess().ProcessName}.ini"));

                // Only save settings after a valid game executable has been found. This also
                // recovers from empty INI files left behind by older builds after cancellation.
                if (!HasUsableSettings(iniFileInfo.FullName))
                    CreateSettings(iniFileInfo.FullName);

                var settings = new SettingsIni(iniFileInfo.FullName);
                var configuredGameExecutable = Path.Combine(
                    settings.ApplicationFilePath,
                    $"{settings.ApplicationName}.exe");

                ShaderInstallerHelper.EnsureReShadeInstallation(helperDirectory, configuredGameExecutable);
                PrintReShadeStatus(ReShadeConfigHelper.Prepare(helperDirectory, settings.ApplicationFilePath));

                dlssCompatibilitySession = DlssCompatibilitySession.Prepare(settings.ApplicationFilePath);

                // A stale game process would make the injector attach to the wrong run.
                ProcessHelper.Kill(settings.ApplicationName);
                await Task.Delay(1000);

                injectorProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = Path.Combine(helperDirectory, "inject.exe"),
                    WorkingDirectory = helperDirectory,
                    Arguments = $"{settings.ApplicationName}.exe",
                    UseShellExecute = false
                });

                await Task.Delay(1500);
                LaunchGame(args, fromSteam, configuredGameExecutable, settings);

                var gameProcess = await WaitForGameProcessAsync(settings.ApplicationName, TimeSpan.FromSeconds(90));
                if (gameProcess == null)
                    throw new TimeoutException("The game did not start within 90 seconds.");

                Console.WriteLine($"Game started (PID {gameProcess.Id}). ReShade injection is active.");
                Console.WriteLine("The helper will stay in the background and exit automatically with the game.");

                Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
                backupCancellation = new CancellationTokenSource();
                backupTask = Task.Run(() =>
                    SaveManagerHelper.ScheduledBackupSaveAsync(settings, backupCancellation.Token));

                if (enableAspectPatch && !Debugger.IsAttached)
                {
                    aspectPatchCancellation = new CancellationTokenSource();
                    aspectPatchTask = Task.Run(async () =>
                    {
                        await Task.Delay(
                            TimeSpan.FromSeconds(30),
                            aspectPatchCancellation.Token);
                        if (ProcessHelper.Exists(settings.ApplicationName))
                            new AspectRatioPatcher().ApplyPatch(settings);
                    }, aspectPatchCancellation.Token);
                }

                // Leave the console visible long enough for the injector result to be readable.
                await Task.Delay(TimeSpan.FromSeconds(5));
                if (!keepWindowVisible)
                {
                    CloseWindowHelper.SetVisible(
                        Process.GetCurrentProcess().MainWindowHandle,
                        false);
                    windowHidden = true;
                }

                await Task.Run(() => gameProcess.WaitForExit());

                aspectPatchCancellation?.Cancel();
                await IgnoreCancellationAsync(aspectPatchTask);
                aspectPatchTask = null;

                backupCancellation.Cancel();
                await IgnoreCancellationAsync(backupTask);
                backupTask = null;

                StopProcess(injectorProcess);
                injectorProcess = null;

                ScreenshotHelper.MoveCaptures();
            }
            catch (Exception ex)
            {
                if (windowHidden)
                {
                    CloseWindowHelper.SetVisible(
                        Process.GetCurrentProcess().MainWindowHandle,
                        true);
                    windowHidden = false;
                }

                ErrorLogHelper.Log(ex);
                Console.Error.WriteLine();
                Console.Error.WriteLine($"ERROR: {ex.Message}");
                Console.Error.WriteLine("Details were written to ErrorLog.txt next to the helper.");
                Environment.ExitCode = 1;

                if (!Console.IsInputRedirected)
                {
                    Console.WriteLine("Press any key to close...");
                    Console.ReadKey(true);
                }
            }
            finally
            {
                backupCancellation?.Cancel();
                aspectPatchCancellation?.Cancel();
                await IgnoreCancellationAsync(backupTask);
                await IgnoreCancellationAsync(aspectPatchTask);
                StopProcess(injectorProcess);
                dlssCompatibilitySession?.Dispose();
            }
        }

        private static void LaunchGame(
            string[] args,
            bool fromSteam,
            string configuredGameExecutable,
            SettingsIni settings)
        {
            if (!fromSteam)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"steam://run/{settings.SteamAppId}",
                    UseShellExecute = true
                });
                return;
            }

            var forwardedArgs = args
                .Skip(1)
                .Where(arg =>
                    !string.Equals(arg, "--no-hide", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(arg, "--enable-aspect-patch", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            var executable = forwardedArgs.Length > 0
                ? Path.GetFullPath(forwardedArgs[0])
                : configuredGameExecutable;

            if (!File.Exists(executable) ||
                !string.Equals(
                    Path.GetFileName(executable),
                    Path.GetFileName(configuredGameExecutable),
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Steam did not pass a valid MonsterHunterWorld.exe command. Remove the custom launch option and use the helper directly.");
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = executable,
                WorkingDirectory = Path.GetDirectoryName(executable),
                Arguments = string.Join(" ", forwardedArgs.Skip(1).Select(QuoteArgument)),
                UseShellExecute = false
            });
        }

        private static async Task<Process> WaitForGameProcessAsync(string processName, TimeSpan timeout)
        {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < timeout)
            {
                var processes = Process.GetProcessesByName(processName);
                if (processes.Length > 0)
                    return processes[0];

                await Task.Delay(500);
            }

            return null;
        }

        private static string QuoteArgument(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "\"\"";
            if (!value.Any(char.IsWhiteSpace) && !value.Contains("\""))
                return value;
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        private static async Task IgnoreCancellationAsync(Task task)
        {
            if (task == null)
                return;

            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
            }
            catch (AggregateException ex) when (ex.InnerExceptions.All(inner => inner is OperationCanceledException))
            {
            }
            catch (Exception ex)
            {
                ErrorLogHelper.Log("A background helper task stopped unexpectedly.", ex);
            }
        }

        private static void StopProcess(Process process)
        {
            if (process == null)
                return;

            try
            {
                if (!process.HasExited)
                    process.Kill();
            }
            catch (InvalidOperationException)
            {
            }
            catch (System.ComponentModel.Win32Exception)
            {
            }
            finally
            {
                process.Dispose();
            }
        }

        private static bool HasUsableSettings(string path)
        {
            if (!File.Exists(path))
                return false;

            try
            {
                var settings = new SettingsIni(path);
                if (string.IsNullOrWhiteSpace(settings.ApplicationFilePath) ||
                    string.IsNullOrWhiteSpace(settings.ApplicationName))
                    return false;
                if (settings.SteamAppId != int.Parse(GameLocatorHelper.MonsterHunterWorldAppId) ||
                    string.IsNullOrWhiteSpace(settings.SteamDataPath))
                    return false;

                return File.Exists(Path.Combine(
                    settings.ApplicationFilePath,
                    $"{settings.ApplicationName}.exe"));
            }
            catch
            {
                return false;
            }
        }

        private static void CreateSettings(string path)
        {
            Console.WriteLine("Scanning Steam libraries for Monster Hunter: World...");
            var applicationFileName = GameLocatorHelper.FindMonsterHunterWorld();

            if (applicationFileName != null)
            {
                Console.WriteLine($"Game found automatically: {applicationFileName}");
            }
            else
            {
                Console.WriteLine("Automatic scan did not find the game. Please select MonsterHunterWorld.exe.");
                applicationFileName = OpenFileDialogHelper.ShowOpenDialog();
            }

            if (string.IsNullOrWhiteSpace(applicationFileName) || !File.Exists(applicationFileName))
                throw new OperationCanceledException(
                    "No game was selected. Nothing was saved; run the helper again to retry.");

            var applicationFileInfo = new FileInfo(applicationFileName);
            var tmpSettings = new IniFile();
            var mainSection = tmpSettings.Sections.Add("Main");

            var resolutionX = "-1";
            var resolutionY = "-1";
            try
            {
                var displayQuery = new ManagementObjectSearcher(
                    "SELECT CurrentHorizontalResolution, CurrentVerticalResolution FROM Win32_VideoController");
                foreach (ManagementObject record in displayQuery.Get())
                {
                    resolutionX = record["CurrentHorizontalResolution"]?.ToString() ?? "-1";
                    resolutionY = record["CurrentVerticalResolution"]?.ToString() ?? "-1";
                    break;
                }
            }
            catch (ManagementException)
            {
                // Resolution is only used by the optional aspect-ratio patcher.
            }

            mainSection.Keys.Add("ResolutionX", resolutionX);
            mainSection.Keys.Add("ResolutionY", resolutionY);
            mainSection.Keys.Add("ApplicationFilePath", applicationFileInfo.DirectoryName);
            mainSection.Keys.Add("ApplicationName", Path.GetFileNameWithoutExtension(applicationFileInfo.Name));
            mainSection.Keys.Add("SteamAppId", GameLocatorHelper.MonsterHunterWorldAppId);
            mainSection.Keys.Add("SteamDataPath", GameLocatorHelper.FindSteamUserDataPath(applicationFileInfo.FullName));
            mainSection.Keys.Add("AddressRangeStart", "0");
            mainSection.Keys.Add("AddressRangeEnd", "130000000");
            mainSection.Keys.Add("HUDAddressRangeStart", "0");
            mainSection.Keys.Add("HUDAddressRangeEnd", "130000000");

            // Saving is deliberately the last step, so cancellation cannot leave a broken INI.
            tmpSettings.Save(path);
        }

        private static void PrintReShadeStatus(ReShadePreparationResult result)
        {
            Console.WriteLine($"ReShade DLL version: {result.Version}");
            if (!result.Version.StartsWith(
                ReShadeConfigHelper.SupportedReShadeVersion,
                StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(
                    $"Warning: this build was tested with ReShade {ReShadeConfigHelper.SupportedReShadeVersion}.");
            }

            Console.WriteLine($"ReShade effects found: {result.EffectCount}");
            foreach (var path in result.EffectDirectories)
                Console.WriteLine($"  Shader path: {path}");
            Console.WriteLine($"Preset: {result.PresetPath}");
        }
    }
}
