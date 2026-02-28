namespace Loupedeck.ClaudeConsolePlugin.Tests
{
    using System;

    internal static class TestHelpers
    {
        public static SessionInfo MakeSession(String id, String name = null, String state = "idle")
        {
            return new SessionInfo
            {
                SessionId = id,
                Name = name ?? $"session-{id}",
                ProjectPath = "/tmp",
                Tty = "ttys000",
                TmuxTarget = "",
                State = state,
            };
        }
    }
}
