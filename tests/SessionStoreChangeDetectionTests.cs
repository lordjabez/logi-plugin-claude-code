namespace Loupedeck.ClaudeConsolePlugin.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    using static TestHelpers;

    public class SessionStoreChangeDetectionTests
    {
        private readonly SessionStore _store;

        public SessionStoreChangeDetectionTests()
        {
            this._store = new SessionStore(dbPath: "/dev/null/nonexistent");
        }

        [Fact]
        public void FirstPoll_NoChanges()
        {
            this._store.AssignSlots(new List<SessionInfo> { MakeSession("a") });

            Assert.Empty(this._store.LastChanges);
        }

        [Fact]
        public void StateChange_ReportsStateChanged()
        {
            this._store.AssignSlots(new List<SessionInfo> { MakeSession("a", state: "idle") });
            this._store.AssignSlots(new List<SessionInfo> { MakeSession("a", state: "working") });

            Assert.Single(this._store.LastChanges);
            Assert.Equal("a", this._store.LastChanges[0].SessionId);
            Assert.Equal("idle", this._store.LastChanges[0].PreviousState);
            Assert.Equal("working", this._store.LastChanges[0].NewState);
        }

        [Fact]
        public void SameState_NoChanges()
        {
            this._store.AssignSlots(new List<SessionInfo> { MakeSession("a", state: "idle") });
            this._store.AssignSlots(new List<SessionInfo> { MakeSession("a", state: "idle") });

            Assert.Empty(this._store.LastChanges);
        }

        [Fact]
        public void NewSession_NoChangeReported()
        {
            this._store.AssignSlots(new List<SessionInfo> { MakeSession("a") });
            this._store.AssignSlots(new List<SessionInfo> { MakeSession("a"), MakeSession("b") });

            Assert.Empty(this._store.LastChanges);
        }

        [Fact]
        public void RemovedSession_NoChangeReported()
        {
            this._store.AssignSlots(new List<SessionInfo> { MakeSession("a"), MakeSession("b") });
            this._store.AssignSlots(new List<SessionInfo> { MakeSession("a") });

            Assert.Empty(this._store.LastChanges);
        }

        [Fact]
        public void MultipleStateChanges_AllReported()
        {
            this._store.AssignSlots(new List<SessionInfo>
            {
                MakeSession("a", state: "idle"),
                MakeSession("b", state: "working"),
            });
            this._store.AssignSlots(new List<SessionInfo>
            {
                MakeSession("a", state: "working"),
                MakeSession("b", state: "waiting"),
            });

            var changes = this._store.LastChanges.Select(c => (c.SessionId, c.PreviousState, c.NewState)).ToList();
            Assert.Contains(("a", "idle", "working"), changes);
            Assert.Contains(("b", "working", "waiting"), changes);
            Assert.Equal(2, this._store.LastChanges.Count);
        }

        [Fact]
        public void ChangesResetEachPoll()
        {
            this._store.AssignSlots(new List<SessionInfo> { MakeSession("a", state: "idle") });
            this._store.AssignSlots(new List<SessionInfo> { MakeSession("a", state: "working") });

            Assert.Single(this._store.LastChanges);

            this._store.AssignSlots(new List<SessionInfo> { MakeSession("a", state: "working") });

            Assert.Empty(this._store.LastChanges);
        }
    }
}
