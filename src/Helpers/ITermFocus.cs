namespace Loupedeck.ClaudeConsolePlugin
{
    using System;
    using System.Diagnostics;

    internal static class ITermFocus
    {
        public static void Focus(String tty, String tmuxTarget)
        {
            if (String.IsNullOrEmpty(tty))
            {
                return;
            }

            try
            {
                Process.Start(BuildITermStartInfo(tty));
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Failed to focus session (tty={tty}, tmux={tmuxTarget})");
            }
        }

        internal static ProcessStartInfo BuildITermStartInfo(String tty)
        {
            var script = $@"
tell application ""iTerm""
    activate
    repeat with w in windows
        repeat with t in tabs of w
            repeat with s in sessions of t
                if tty of s is ""/dev/{tty}"" then
                    select w
                    tell t to select
                    return
                end if
            end repeat
        end repeat
    end repeat
end tell";

            var info = new ProcessStartInfo
            {
                FileName = "osascript",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            info.ArgumentList.Add("-e");
            info.ArgumentList.Add(script);
            return info;
        }
    }
}
