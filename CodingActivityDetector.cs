using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace AiBuddy;

public static class CodingActivityDetector
{
    private static readonly HashSet<string> CodingProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Code", "Cursor", "Windsurf", "devenv", "idea64", "rider64", "pycharm64", "webstorm64",
        "studio64", "notepad++", "sublime_text", "atom", "eclipse", "codeblocks", "WindowsTerminal",
        "WindowsTerminalPreview", "wt", "pwsh", "powershell", "cmd", "bash", "mintty", "ConEmu64"
    };

    private static readonly string[] CodingTitleMarkers =
    [
        "Visual Studio Code", "IntelliJ IDEA", "PyCharm", "WebStorm", "JetBrains Rider", "Android Studio",
        "Visual Studio", "Jupyter", "Replit", "CodeSandbox", "StackBlitz", "GitHub Codespaces", "CodePen"
    ];

    public static bool IsCodingWindowForeground()
    {
        var handle = GetForegroundWindow();
        if (handle == IntPtr.Zero) return false;

        var titleBuilder = new StringBuilder(512);
        _ = GetWindowText(handle, titleBuilder, titleBuilder.Capacity);
        var title = titleBuilder.ToString();
        if (CodingTitleMarkers.Any(marker => title.Contains(marker, StringComparison.OrdinalIgnoreCase)))
            return true;

        _ = GetWindowThreadProcessId(handle, out var processId);
        if (processId == 0) return false;

        try
        {
            using var process = Process.GetProcessById((int)processId);
            return CodingProcesses.Contains(process.ProcessName);
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr window, StringBuilder text, int maxCount);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
}
