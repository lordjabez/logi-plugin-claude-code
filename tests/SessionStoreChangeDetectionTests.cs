namespace Loupedeck.ClaudeConsolePlugin.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    public class SessionStoreChangeDetectionTests
    {
        private readonly SessionStore _store;

        public SessionStoreChangeDetectionTests()
        {
            this._store = new SessionStore(dbPath: "/dev/null/nonexistent");
        }

        private static SessionInfo MakeSession(String id, String state = "idle")
        {
            return new SessionInfo
            {
                SessionId = id,
                Name = $"session-{id}",
                ProjectPath = "/tmp",
                Tty = "ttys000",
                TmuxTarget = "",
                State = state,
            };
        }

        [Fact]
        public void FirstPoll_NoChanges()
        {
            this._store.AssignSlots(new List<SessionInfo> { MakeSession("a") });

            Assert.Empty(this._store.LastChanges);
        }

        [Fact]
        public void NewSession_ReportsLaunched()
        {
            this._store.AssignSlots(new List<SessionInfo> { MakeSession("a") });
            this._store.AssignSlots(new List<SessionInfo> { MakeSession("a"), MakeSession("b") });

            Assert.Single(this._store.LastChanges);
            Assert.Equal(SessionChangeKind.Launched, this._store.LastChanges[0].Kind);
            Assert.Equal("b", this._store.LastChanges[0].SessionId);
        }

        [Fact]
        public void RemovedSession_ReportsClosed()
        {
            this._store.AssignSlots(new List<SessionInfo> { MakeSession("a"), MakeSession("b") });
            this._store.AssignSlots(new List<SessionInfo> { MakeSession("a") });

            Assert.Single(this._store.LastChanges);
            Assert.Equal(SessionChangeKind.Closed, this._store.LastChanges[0].Kind);
            Assert.Equal("b", this._store.LastChanges[0].SessionId);
        }

        [Fact]
        public void StateChange_ReportsStateChanged()
        {
            this._store.AssignSlots(new List<SessionInfo> { MakeSession("a", "idle") });
            this._store.AssignSlots(new List<SessionInfo> { MakeSession("a", "active") });

            Assert.Single(this._store.LastChanges);
            Assert.Equal(SessionChangeKind.StateChanged, this._store.LastChanges[0].Kind);
            Assert.Equal("a", this._store.LastChanges[0].SessionId);
        }

        [Fact]
        public void SameState_NoChanges()
        {
            this._store.AssignSlots(new List<SessionInfo> { MakeSession("a", "idle") });
            this._store.AssignSlots(new List<SessionInfo> { MakeSession("a", "idle") });

            Assert.Empty(this._store.LastChanges);
        }

        [Fact]
        public void MultipleChanges_AllReported()
        {
            this._store.AssignSlots(new List<SessionInfo>
            {
                MakeSession("a", "idle"),
                MakeSession("b", "idle"),
            });
            this._store.AssignSlots(new List<SessionInfo>
            {
                MakeSession("a", "active"),
                MakeSession("c", "idle"),
            });

            var kinds = this._store.LastChanges.Select(c => (c.Kind, c.SessionId)).ToList();
            Assert.Contains((SessionChangeKind.StateChanged, "a"), kinds);
            Assert.Contains((SessionChangeKind.Launched, "c"), kinds);
            Assert.Contains((SessionChangeKind.Closed, "b"), kinds);
            Assert.Equal(3, this._store.LastChanges.Count);
        }

        [Fact]
        public void AllSessionsClosed_ReportsEachClosed()
        {
            this._store.AssignSlots(new List<SessionInfo> { MakeSession("a"), MakeSession("b") });
            this._store.AssignSlots(new List<SessionInfo>());

            Assert.Equal(2, this._store.LastChanges.Count);
            Assert.All(this._store.LastChanges, c => Assert.Equal(SessionChangeKind.Closed, c.Kind));
        }

        [Fact]
        public void ChangesResetEachPoll()
        {
            this._store.AssignSlots(new List<SessionInfo> { MakeSession("a") });
            this._store.AssignSlots(new List<SessionInfo> { MakeSession("a"), MakeSession("b") });

            Assert.Single(this._store.LastChanges);

            this._store.AssignSlots(new List<SessionInfo> { MakeSession("a"), MakeSession("b") });

            Assert.Empty(this._store.LastChanges);
        }
    }
}
