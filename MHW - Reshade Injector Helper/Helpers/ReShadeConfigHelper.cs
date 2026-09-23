using MadMilkman.Ini;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace MHW___Reshade_Injector_Helper.Helpers
{
    public static class ReShadeConfigHelper
    {
        public const string SupportedReShadeVersion = "6.8.0";

        public static ReShadePreparationResult Prepare(string helperDirectory, string gameDirectory)
        {
            if (string.IsNullOrWhiteSpace(helperDirectory))
                throw new ArgumentException("The helper directory is missing.", nameof(helperDirectory));
            if (string.IsNullOrWhiteSpace(gameDirectory) || !Directory.Exists(gameDirectory))
                throw new DirectoryNotFoundException($"Game directory not found: {gameDirectory}");

            helperDirectory = Path.GetFullPath(helperDirectory);
            gameDirectory = Path.GetFullPath(gameDirectory);

            var reshadeDllPath = Path.Combine(helperDirectory, "ReShade64.dll");
            if (!File.Exists(reshadeDllPath))
                throw new FileNotFoundException("ReShade64.dll is missing next to the helper.", reshadeDllPath);

            var injectorPath = Path.Combine(helperDirectory, "inject.exe");
            if (!File.Exists(injectorPath))
                throw new FileNotFoundException("inject.exe is missing next to the helper.", injectorPath);

            // An injected ReShade64.dll uses the target executable directory as its base path,
            // so the active configuration belongs next to the game executable.
            var iniPath = Path.Combine(gameDirectory, "ReShade.ini");
            var ini = new IniFile();
            if (File.Exists(iniPath))
                ini.Load(iniPath);
            var general = ini.Sections["GENERAL"] ?? ini.Sections.Add("GENERAL");

            var effectDirectories = ExistingDirectories(
                Path.Combine(gameDirectory, "reshade-shaders", "Shaders"));

            var textureDirectories = ExistingDirectories(
                Path.Combine(gameDirectory, "reshade-shaders", "Textures"));

            var effectCount = effectDirectories.Sum(path =>
                Directory.GetFiles(path, "*.fx", SearchOption.AllDirectories).Length);

            if (effectCount == 0)
            {
                throw new InvalidOperationException(
                    "No ReShade effects were found under the game's reshade-shaders\\Shaders folder. " +
                    "Run the official installer again and select at least one shader package.");
            }

            SetValue(general, "EffectSearchPaths", JoinSearchPaths(effectDirectories));
            SetValue(general, "TextureSearchPaths", JoinSearchPaths(textureDirectories));

            var presetPath = FindPresetPath(general.Keys["PresetPath"]?.Value, gameDirectory);
            SetValue(general, "PresetPath", presetPath);

            var screenshotDirectory = Path.Combine(gameDirectory, "Screenshots");
            Directory.CreateDirectory(screenshotDirectory);
            SetValue(general, "ScreenshotPath", screenshotDirectory);

            var screenshots = ini.Sections["SCREENSHOTS"] ?? ini.Sections.Add("SCREENSHOTS");
            SetValue(screenshots, "SavePath", screenshotDirectory);

            // ReShade 6.8 automatically localizes its UI from the Windows language.
            // The installer's legacy ProggyClean main font has no CJK glyphs, which
            // turns every localized label into question marks. An empty main Font
            // lets ReShade select Microsoft YaHei/JhengHei for Chinese automatically.
            var style = ini.Sections["STYLE"] ?? ini.Sections.Add("STYLE");
            var mainFont = style.Keys["Font"];
            if (mainFont != null &&
                string.Equals(mainFont.Value?.Trim(), "ProggyClean.ttf", StringComparison.OrdinalIgnoreCase))
            {
                mainFont.Value = string.Empty;
            }

            ini.Save(iniPath);

            var version = FileVersionInfo.GetVersionInfo(reshadeDllPath).ProductVersion ?? "unknown";
            return new ReShadePreparationResult(version, effectCount, iniPath, presetPath, effectDirectories);
        }

        private static List<string> ExistingDirectories(params string[] paths)
        {
            return paths
                .Where(Directory.Exists)
                .Select(Path.GetFullPath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string JoinSearchPaths(IEnumerable<string> paths)
        {
            return string.Join(",", paths.Select(path => Path.Combine(path, "**")));
        }

        private static string FindPresetPath(string configuredPath, string gameDirectory)
        {
            var resolvedConfiguredPath = ResolveGamePath(configuredPath, gameDirectory);
            if (resolvedConfiguredPath != null && File.Exists(resolvedConfiguredPath))
                return resolvedConfiguredPath;

            var preferredNames = new[] { "DefaultPreset.ini", "ReShadePreset.ini" };
            foreach (var directory in new[] { gameDirectory })
            {
                foreach (var name in preferredNames)
                {
                    var candidate = Path.Combine(directory, name);
                    if (File.Exists(candidate))
                        return candidate;
                }
            }

            var discoveredPreset = Directory
                .GetFiles(gameDirectory, "*.ini", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(path =>
                    !string.Equals(Path.GetFileName(path), "ReShade.ini", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(Path.GetFileName(path), "graphics_option.ini", StringComparison.OrdinalIgnoreCase));

            if (discoveredPreset != null)
                return discoveredPreset;

            var newPreset = Path.Combine(gameDirectory, "ReShadePreset.ini");
            if (!File.Exists(newPreset))
                File.WriteAllText(newPreset, "Techniques=\r\nTechniqueSorting=\r\n");
            return newPreset;
        }

        private static string ResolveGamePath(string configuredPath, string gameDirectory)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
                return null;

            var path = configuredPath.Trim().Trim('"');
            try
            {
                return Path.GetFullPath(
                    Path.IsPathRooted(path)
                        ? path
                        : Path.Combine(gameDirectory, path));
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (NotSupportedException)
            {
                return null;
            }
            catch (PathTooLongException)
            {
                return null;
            }
        }

        private static void SetValue(IniSection section, string keyName, string value)
        {
            var key = section.Keys[keyName];
            if (key == null)
                section.Keys.Add(keyName, value);
            else
                key.Value = value;
        }
    }

    public sealed class ReShadePreparationResult
    {
        public ReShadePreparationResult(
            string version,
            int effectCount,
            string iniPath,
            string presetPath,
            IReadOnlyList<string> effectDirectories)
        {
            Version = version;
            EffectCount = effectCount;
            IniPath = iniPath;
            PresetPath = presetPath;
            EffectDirectories = effectDirectories;
        }

        public string Version { get; }
        public int EffectCount { get; }
        public string IniPath { get; }
        public string PresetPath { get; }
        public IReadOnlyList<string> EffectDirectories { get; }
    }
}
