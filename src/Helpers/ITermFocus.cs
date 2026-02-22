namespace Loupedeck.ClaudeConsolePlugin
{
    using System;
    using System.Diagnostics;

    internal static class ITermFocus
    {
        public static void Focus(String tty, String tmuxTarget)
        {
            // Prefer tmux if we have a target
            if (!String.IsNullOrEmpty(tmuxTarget))
            {
                FocusViaTmux(tmuxTarget);
                return;
            }

            if (!String.IsNullOrEmpty(tty))
            {
                FocusViaITerm(tty);
                return;
            }
        }

        private static void FocusViaTmux(String tmuxTarget)
        {
            try
            {
                var parts = tmuxTarget.Split(':');
                if (parts.Length < 2)
                {
                    return;
                }

                var tmuxSession = parts[0];
                var pane = parts[1];

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "tmux",
                        Arguments = $"select-window -t {tmuxTarget} \\; select-pane -t {tmuxTarget}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                    },
                };
                process.Start();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Failed to focus tmux target {tmuxTarget}");
            }
        }

        private static void FocusViaITerm(String tty)
        {
            try
            {
                var script = $@"
                    tell application ""iTerm2""
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

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "osascript",
                        Arguments = $"-e '{script}'",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                    },
                };
                process.Start();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Failed to focus iTerm2 session for {tty}");
            }
        }
    }
}
