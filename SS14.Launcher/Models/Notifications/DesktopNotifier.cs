using System;
using System.Diagnostics;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace SS14.Launcher.Models.Notifications;

/// <summary>
/// Shows operating system notifications by calling what each OS already has (no extra packages).
/// Best effort: failures are only logged.
/// </summary>
public static class DesktopNotifier
{
    public static void Show(string title, string text)
    {
        Task.Run(() =>
        {
            try
            {
                if (OperatingSystem.IsWindows())
                    ShowWindows(title, text);
                else if (OperatingSystem.IsMacOS())
                    ShowMac(title, text);
                else
                    ShowLinux(title, text);
            }
            catch (Exception e)
            {
                Log.Warning(e, "Failed to show a system notification");
            }
        });
    }

    private static void ShowWindows(string title, string text)
    {
        static string Xml(string s) => SecurityElement.Escape(s) ?? "";

        var toastXml =
            $"<toast><visual><binding template=\"ToastGeneric\"><text>{Xml(title)}</text><text>{Xml(text)}</text></binding></visual></toast>";

        // Single-quoted PowerShell string: double any quote.
        var xmlLiteral = toastXml.Replace("'", "''");

        // Windows PowerShell's own app id, so that no shortcut registration is needed.
        const string appId = @"{1AC14E77-02E7-4E5D-B744-2EB1AE5198B7}\WindowsPowerShell\v1.0\powershell.exe";

        var script = $@"
[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
[Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null
$xml = New-Object Windows.Data.Xml.Dom.XmlDocument
$xml.LoadXml('{xmlLiteral}')
$toast = New-Object Windows.UI.Notifications.ToastNotification $xml
[Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('{appId}').Show($toast)
";

        // EncodedCommand avoids every command-line quoting problem.
        var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));

        Run("powershell.exe", "-NoProfile", "-NonInteractive", "-WindowStyle", "Hidden", "-EncodedCommand", encoded);
    }

    private static void ShowMac(string title, string text)
    {
        static string Esc(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

        Run("osascript", "-e", $"display notification \"{Esc(text)}\" with title \"{Esc(title)}\"");
    }

    private static void ShowLinux(string title, string text)
    {
        Run("notify-send", "--app-name=SS14 Launcher", title, text);
    }

    private static void Run(string file, params string[] args)
    {
        var info = new ProcessStartInfo(file)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (var arg in args)
            info.ArgumentList.Add(arg);

        using var process = Process.Start(info);
        process?.WaitForExit(10_000);
    }
}
