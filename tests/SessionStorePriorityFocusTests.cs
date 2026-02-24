namespace Loupedeck.ClaudeConsolePlugin.Tests
{
    using System;
    using System.Collections.Generic;
    using Xunit;

    public class SessionStorePriorityFocusTests
    {
        private readonly SessionStore _store;

        public SessionStorePriorityFocusTests()
        {
            this._store = new SessionStore(dbPath: "/dev/null/nonexistent");
        }

        private static SessionInfo MakeSession(String id, String name = null, String state = "idle")
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

        [Fact]
        public void GetPriorityFocusSession_NoSessions_ReturnsNull()
        {
            this._store.AssignSlots(new List<SessionInfo>());
            Assert.Null(this._store.GetPriorityFocusSession());
        }

        [Fact]
        public void GetPriorityFocusSession_SingleWaiting_ReturnsThatSession()
        {
            this._store.AssignSlots(new List<SessionInfo>
            {
                MakeSession("a", state: "waiting"),
            });

            var result = this._store.GetPriorityFocusSession();

            Assert.NotNull(result);
            Assert.Equal("a", result.SessionId);
        }

        [Fact]
        public void GetPriorityFocusSession_MultipleWaiting_ReturnsEarliest()
        {
            // First poll: only session "a" is waiting
            this._store.AssignSlots(new List<SessionInfo>
            {
                MakeSession("a", state: "waiting"),
            });

            // Second poll: "b" also becomes waiting, but "a" has the earlier entry time
            this._store.AssignSlots(new List<SessionInfo>
            {
                MakeSession("a", state: "waiting"),
                MakeSession("b", state: "waiting"),
            });

            var result = this._store.GetPriorityFocusSession();

            Assert.NotNull(result);
            Assert.Equal("a", result.SessionId);
        }

        [Fact]
        public void GetPriorityFocusSession_NoWaiting_SingleWorking_ReturnsThatSession()
        {
            this._store.AssignSlots(new List<SessionInfo>
            {
                MakeSession("a", state: "working"),
            });

            var result = this._store.GetPriorityFocusSession();

            Assert.NotNull(result);
            Assert.Equal("a", result.SessionId);
        }

        [Fact]
        public void GetPriorityFocusSession_MultipleWorking_ReturnsEarliest()
        {
            this._store.AssignSlots(new List<SessionInfo>
            {
                MakeSession("a", state: "working"),
            });

            this._store.AssignSlots(new List<SessionInfo>
            {
                MakeSession("a", state: "working"),
                MakeSession("b", state: "working"),
            });

            var result = this._store.GetPriorityFocusSession();

            Assert.NotNull(result);
            Assert.Equal("a", result.SessionId);
        }

        [Fact]
        public void GetPriorityFocusSession_IdleOnly_ReturnsNull()
        {
            this._store.AssignSlots(new List<SessionInfo>
            {
                MakeSession("a", state: "idle"),
                MakeSession("b", state: "idle"),
            });

            Assert.Null(this._store.GetPriorityFocusSession());
        }

        [Fact]
        public void GetPriorityFocusSession_WaitingAndWorking_PrefersWaiting()
        {
            this._store.AssignSlots(new List<SessionInfo>
            {
                MakeSession("a", state: "working"),
                MakeSession("b", state: "waiting"),
            });

            var result = this._store.GetPriorityFocusSession();

            Assert.NotNull(result);
            Assert.Equal("b", result.SessionId);
        }

        [Fact]
        public void GetPriorityFocusSession_StateChange_ResetsEntryTime()
        {
            // "a" starts working, "b" starts idle
            this._store.AssignSlots(new List<SessionInfo>
            {
                MakeSession("a", state: "working"),
                MakeSession("b", state: "idle"),
            });

            // Manually backdate "a"'s entry time so it's clearly older
            this._store.StateEntryTimes["a"] = DateTime.UtcNow.AddMinutes(-10);

            // "b" transitions to working -- its entry time should be fresh (now)
            this._store.AssignSlots(new List<SessionInfo>
            {
                MakeSession("a", state: "working"),
                MakeSession("b", state: "working"),
            });

            var result = this._store.GetPriorityFocusSession();

            // "a" has the earlier entry time, so it should win
            Assert.NotNull(result);
            Assert.Equal("a", result.SessionId);
        }
    }
}
