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

        internal static ProcessStartInfo BuildTmuxStartInfo(String tmuxTarget)
        {
            return new ProcessStartInfo
            {
                FileName = "tmux",
                Arguments = $"select-window -t {tmuxTarget} \\; select-pane -t {tmuxTarget}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
        }

        internal static ProcessStartInfo BuildITermStartInfo(String tty)
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

            return new ProcessStartInfo
            {
                FileName = "osascript",
                Arguments = $"-e '{script}'",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
        }

        private static void FocusViaTmux(String tmuxTarget)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = BuildTmuxStartInfo(tmuxTarget),
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
                var process = new Process
                {
                    StartInfo = BuildITermStartInfo(tty),
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
