namespace Loupedeck.ClaudeConsolePlugin.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    public class SessionStoreSlotAssignmentTests : IDisposable
    {
        private readonly SessionStore _store;

        public SessionStoreSlotAssignmentTests()
        {
            // Use a non-existent path so Poll() is never called; we test AssignSlots directly
            this._store = new SessionStore(dbPath: "/dev/null/nonexistent");
        }

        public void Dispose()
        {
            this._store.Dispose();
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
        public void FirstFill_AssignsSequentialSlots()
        {
            var sessions = new List<SessionInfo>
            {
                MakeSession("a"),
                MakeSession("b"),
                MakeSession("c"),
            };

            this._store.AssignSlots(sessions);

            Assert.Equal("a", this._store.Slots[0].SessionId);
            Assert.Equal("b", this._store.Slots[1].SessionId);
            Assert.Equal("c", this._store.Slots[2].SessionId);
            for (var i = 3; i < 9; i++)
            {
                Assert.Null(this._store.Slots[i]);
            }
        }

        [Fact]
        public void RemovalFreesSlot()
        {
            var sessions = new List<SessionInfo>
            {
                MakeSession("a"),
                MakeSession("b"),
                MakeSession("c"),
            };
            this._store.AssignSlots(sessions);

            // Remove "b"
            this._store.AssignSlots(new List<SessionInfo>
            {
                MakeSession("a"),
                MakeSession("c"),
            });

            Assert.Equal("a", this._store.Slots[0].SessionId);
            Assert.Null(this._store.Slots[1]);
            Assert.Equal("c", this._store.Slots[2].SessionId);
        }

        [Fact]
        public void VacatedSlotIsReused()
        {
            var sessions = new List<SessionInfo>
            {
                MakeSession("a"),
                MakeSession("b"),
                MakeSession("c"),
            };
            this._store.AssignSlots(sessions);

            // Remove "b", freeing slot 1
            this._store.AssignSlots(new List<SessionInfo>
            {
                MakeSession("a"),
                MakeSession("c"),
            });

            // Add "d" - should fill the vacated slot 1
            this._store.AssignSlots(new List<SessionInfo>
            {
                MakeSession("a"),
                MakeSession("c"),
                MakeSession("d"),
            });

            Assert.Equal("a", this._store.Slots[0].SessionId);
            Assert.Equal("d", this._store.Slots[1].SessionId);
            Assert.Equal("c", this._store.Slots[2].SessionId);
        }

        [Fact]
        public void StateChangeIsDetected()
        {
            this._store.AssignSlots(new List<SessionInfo> { MakeSession("a", "idle") });
            _ = this._store.HasChanged; // consume

            this._store.AssignSlots(new List<SessionInfo> { MakeSession("a", "active") });

            Assert.True(this._store.HasChanged);
            Assert.Equal("active", this._store.Slots[0].State);
        }

        [Fact]
        public void NineSlotCap_ExtraSessionsIgnored()
        {
            var sessions = Enumerable.Range(0, 12)
                .Select(i => MakeSession(i.ToString()))
                .ToList();

            this._store.AssignSlots(sessions);

            Assert.Equal(9, this._store.SessionSlots.Count);
            // Slots 0-8 filled, extras dropped
            for (var i = 0; i < 9; i++)
            {
                Assert.NotNull(this._store.Slots[i]);
            }
        }

        [Fact]
        public void EmptyListClearsAllSlots()
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

            Assert.Empty(this._store.SessionSlots);
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
