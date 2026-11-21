using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Splat;
using SS14.Launcher.Models.Data;

namespace SS14.Launcher.Utility;

/// <summary>
/// Collects everything needed to look into "it doesn't start" reports into one zip archive:
/// launcher and client logs, system info, selected settings and a quick network check.
/// Never includes the database (it holds login tokens) or any account names.
/// </summary>
public static class DiagnosticsExporter
{
    private const int MaxLogFiles = 12;

    /// <returns>Path of the created archive.</returns>
    public static async Task<string> CreateAsync(CancellationToken cancel = default)
    {
        var cfg = Locator.Current.GetRequiredService<DataManager>();
        var http = Locator.Current.GetRequiredService<HttpClient>();

        // Network checks first (async), the rest is plain file work.
        var network = await RunNetworkChecks(http, cancel);

        var systemInfo = BuildSystemInfo(cfg);
        var path = GetOutputPath();

        await Task.Run(() =>
        {
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
            using var zip = new ZipArchive(stream, ZipArchiveMode.Create);

            AddText(zip, "system-info.txt", systemInfo);
            AddText(zip, "network-check.txt", network);
            AddLogs(zip);
        }, cancel);

        return path;
    }

    /// <summary>Opens the folder with the archive, selecting the file where the OS supports it.</summary>
    public static void Reveal(string path)
    {
        try
        {
            if (OperatingSystem.IsWindows())
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
            else if (OperatingSystem.IsMacOS())
                Process.Start("open", new[] { "-R", path });
            else
                Process.Start(new ProcessStartInfo("xdg-open", $"\"{Path.GetDirectoryName(path)}\"") { UseShellExecute = false });
        }
        catch (Exception)
        {
            // Not being able to open the folder is not an error: the path is shown to the user anyway.
        }
    }

    private static string GetOutputPath()
    {
        var dir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            dir = LauncherPaths.DirUserData;

        Directory.CreateDirectory(dir);
        return Path.Combine(dir, $"SS14Launcher-diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}.zip");
    }

    private static string BuildSystemInfo(DataManager cfg)
    {
        var sb = new StringBuilder();

        void Line(string key, object? value) => sb.AppendLine($"{key}: {value}");

        Line("Created (UTC)", DateTime.UtcNow.ToString("u"));
        Line("Launcher version", LauncherVersion.Version);
        Line("OS", RuntimeInformation.OSDescription);
        Line("OS architecture", RuntimeInformation.OSArchitecture);
        Line("Process architecture", RuntimeInformation.ProcessArchitecture);
        Line(".NET", RuntimeInformation.FrameworkDescription);
        Line("Culture", System.Globalization.CultureInfo.CurrentUICulture.Name);
        Line("Time zone", TimeZoneInfo.Local.Id);
        Line("Processors", Environment.ProcessorCount);
        Line("Launcher install dir", LauncherPaths.DirLauncherInstall);
        Line("User data dir", LauncherPaths.DirUserData);

        try
        {
            var root = Path.GetPathRoot(Path.GetFullPath(LauncherPaths.DirUserData));
            if (root != null)
                Line("Free disk space (user data drive)", $"{new DriveInfo(root).AvailableFreeSpace / (1024 * 1024)} MiB");
        }
        catch (Exception e)
        {
            Line("Free disk space", $"unknown ({e.GetType().Name})");
        }

        sb.AppendLine();
        sb.AppendLine("Settings:");
        Line("  CompatMode", cfg.GetCVar(CVars.CompatMode));
        Line("  LogLauncherVerbose", cfg.GetCVar(CVars.LogLauncherVerbose));
        Line("  OverrideAssets", cfg.GetCVar(CVars.OverrideAssets));
        Line("  PrefetchFavoriteServers", cfg.GetCVar(CVars.PrefetchFavoriteServers));
        Line("  Language", cfg.GetCVar(CVars.Language) ?? "(system)");
        Line("  Theme", cfg.GetCVar(CVars.Theme));
        Line("  ThemeParticles", cfg.GetCVar(CVars.ThemeParticles));
        Line("  MultiAccounts", cfg.GetCVar(CVars.MultiAccounts));
        Line("  Notifications", cfg.GetCVar(CVars.NotificationsEnabled));

        sb.AppendLine();
        Line("Accounts (count only)", cfg.Logins.Count);
        Line("Favorite servers", cfg.FavoriteServers.Count);
        Line("History entries", cfg.ConnectionHistory.Count);

        sb.AppendLine();
        sb.AppendLine("Installed engine versions:");
        sb.AppendLine(ListDirectories(LauncherPaths.DirEngineInstallations));

        sb.AppendLine("Server content folders:");
        sb.AppendLine(ListDirectories(LauncherPaths.DirServerContent, maxItems: 50));

        return sb.ToString();
    }

    private static string ListDirectories(string path, int maxItems = 100)
    {
        try
        {
            if (!Directory.Exists(path))
                return "  (none)";

            var names = Directory.GetDirectories(path).Select(Path.GetFileName).Take(maxItems).ToArray();
            return names.Length == 0 ? "  (none)" : string.Join(Environment.NewLine, names.Select(n => "  " + n));
        }
        catch (Exception e)
        {
            return $"  (error: {e.GetType().Name})";
        }
    }

    private static async Task<string> RunNetworkChecks(HttpClient http, CancellationToken cancel)
    {
        var targets = new List<(string Name, string Url)>
        {
            ("Auth server", ConfigConstants.AuthUrl.Urls[0]),
            ("Hub", ConfigConstants.DefaultHubUrls[0].Urls[0]),
            ("Engine builds", ConfigConstants.RobustBuildsManifest.Urls[0]),
            ("Launcher data", ConfigConstants.UrlLauncherInfo.Urls[0]),
        };

        var results = await Task.WhenAll(targets.Select(t => CheckUrl(http, t.Name, t.Url, cancel)));
        return string.Join(Environment.NewLine, results) + Environment.NewLine;
    }

    private static async Task<string> CheckUrl(HttpClient http, string name, string url, CancellationToken cancel)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel);
            cts.CancelAfter(TimeSpan.FromSeconds(8));

            var sw = Stopwatch.StartNew();
            using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            sw.Stop();

            return $"{name} ({url}): HTTP {(int)response.StatusCode} in {sw.ElapsedMilliseconds} ms";
        }
        catch (OperationCanceledException) when (!cancel.IsCancellationRequested)
        {
            return $"{name} ({url}): TIMEOUT";
        }
        catch (Exception e)
        {
            return $"{name} ({url}): FAILED ({e.GetType().Name}: {e.Message})";
        }
    }

    private static void AddText(ZipArchive zip, string name, string text)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(text);
    }

    private static void AddLogs(ZipArchive zip)
    {
        if (!Directory.Exists(LauncherPaths.DirLogs))
            return;

        var files = new DirectoryInfo(LauncherPaths.DirLogs)
            .EnumerateFiles()
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .Take(MaxLogFiles);

        foreach (var file in files)
        {
            try
            {
                // Logs of a running launcher/game are open for writing, so share the file.
                using var source = new FileStream(file.FullName, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);
                var entry = zip.CreateEntry("logs/" + file.Name, CompressionLevel.Optimal);
                using var target = entry.Open();
                source.CopyTo(target);
            }
            catch (Exception e)
            {
                AddText(zip, $"logs/{file.Name}.error.txt", $"Could not read the file: {e.GetType().Name}: {e.Message}");
            }
        }
    }
}
