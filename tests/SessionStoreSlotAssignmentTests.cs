namespace Loupedeck.ClaudeConsolePlugin.Tests
{
    using System;
    using System.Collections.Generic;
    using Xunit;

    using static TestHelpers;

    public class SessionStoreSlotAssignmentTests
    {
        private readonly SessionStore _store;

        public SessionStoreSlotAssignmentTests()
        {
            this._store = new SessionStore(dbPath: "/dev/null/nonexistent");
        }

        [Fact]
        public void AssignSlots_FillsInOrder()
        {
            var sessions = new List<SessionInfo>
            {
                MakeSession("a", "alpha"),
                MakeSession("b", "bravo"),
                MakeSession("c", "charlie"),
            };

            this._store.AssignSlots(sessions);

            Assert.Equal("alpha", this._store.Slots[0].Name);
            Assert.Equal("bravo", this._store.Slots[1].Name);
            Assert.Equal("charlie", this._store.Slots[2].Name);
            for (var i = 3; i < 9; i++)
            {
                Assert.Null(this._store.Slots[i]);
            }
        }

        [Fact]
        public void AssignSlots_ClearsRemovedSessions()
        {
            this._store.AssignSlots(new List<SessionInfo>
            {
                MakeSession("a"),
                MakeSession("b"),
                MakeSession("c"),
            });

            this._store.AssignSlots(new List<SessionInfo>
            {
                MakeSession("a"),
                MakeSession("c"),
            });

            Assert.Equal("a", this._store.Slots[0].SessionId);
            Assert.Equal("c", this._store.Slots[1].SessionId);
            Assert.Null(this._store.Slots[2]);
        }

        [Fact]
        public void AssignSlots_UpdatesState()
        {
            this._store.AssignSlots(new List<SessionInfo> { MakeSession("a", state: "idle") });
            this._store.AssignSlots(new List<SessionInfo> { MakeSession("a", state: "active") });

            Assert.Equal("active", this._store.Slots[0].State);
        }

        [Fact]
        public void AssignSlots_CapsAtNine()
        {
            var sessions = new List<SessionInfo>();
            for (var i = 0; i < 12; i++)
            {
                sessions.Add(MakeSession(i.ToString()));
            }

            this._store.AssignSlots(sessions);

            for (var i = 0; i < 9; i++)
            {
                Assert.NotNull(this._store.Slots[i]);
            }
        }

        [Fact]
        public void AssignSlots_EmptyListClearsAll()
        {
            this._store.AssignSlots(new List<SessionInfo>
            {
                MakeSession("a"),
                MakeSession("b"),
            });

            this._store.AssignSlots(new List<SessionInfo>());

            for (var i = 0; i < 9; i++)
            {
                Assert.Null(this._store.Slots[i]);
            }
        }

        [Fact]
        public void GetSession_ValidSlot_ReturnsSession()
        {
            this._store.AssignSlots(new List<SessionInfo> { MakeSession("a") });
            Assert.NotNull(this._store.GetSession(0));
            Assert.Equal("a", this._store.GetSession(0).SessionId);
        }

        [Fact]
        public void GetSession_OutOfBounds_ReturnsNull()
        {
            Assert.Null(this._store.GetSession(-1));
            Assert.Null(this._store.GetSession(9));
            Assert.Null(this._store.GetSession(100));
        }
    }
}
