namespace Loupedeck.ClaudeConsolePlugin
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Data.Sqlite;

    internal class SessionInfo
    {
        public String SessionId { get; set; }
        public String Name { get; set; }
        public String ProjectPath { get; set; }
        public String Tty { get; set; }
        public String TmuxTarget { get; set; }
        public String State { get; set; }
    }

    internal class SessionStore
    {
        private const Int32 MaxSlots = 9;

        private readonly String _dbPath;
        private readonly SessionInfo[] _slots = new SessionInfo[MaxSlots];
        private readonly Dictionary<String, Int32> _sessionSlots = new Dictionary<String, Int32>();
        public SessionStore(String dbPath = null)
        {
            this._dbPath = dbPath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".claude",
                "claude-status.db");
        }

        public SessionInfo GetSession(Int32 slot)
        {
            if (slot < 0 || slot >= MaxSlots)
            {
                return null;
            }

            return this._slots[slot];
        }

        public void Poll()
        {
            if (!File.Exists(this._dbPath))
            {
                PluginLog.Info($"DB not found at {this._dbPath}");
                return;
            }

            List<SessionInfo> sessions;
            try
            {
                sessions = this.QuerySessions();
            }
            catch (Exception ex)
            {
                PluginLog.Warning(ex, "Failed to query sessions DB");
                return;
            }

            this.AssignSlots(sessions);
        }

        private List<SessionInfo> QuerySessions()
        {
            var results = new List<SessionInfo>();
            var connStr = new SqliteConnectionStringBuilder
            {
                DataSource = this._dbPath,
                Mode = SqliteOpenMode.ReadOnly,
            }.ToString();

            using var conn = new SqliteConnection(connStr);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    s.session_id,
                    COALESCE(s.custom_title, s.slug) AS name,
                    s.project_path,
                    r.tty,
                    r.tmux_target,
                    r.state
                FROM sessions s
                JOIN runtime r ON s.session_id = r.session_id
                WHERE r.updated_at > datetime('now', '-5 minutes')
                ORDER BY r.updated_at DESC
                LIMIT 9";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new SessionInfo
                {
                    SessionId = reader.GetString(0),
                    Name = reader.IsDBNull(1) ? "unnamed" : reader.GetString(1),
                    ProjectPath = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Tty = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    TmuxTarget = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    State = reader.GetString(5),
                });
            }

            return results;
        }

        internal SessionInfo[] Slots => this._slots;

        internal IReadOnlyDictionary<String, Int32> SessionSlots => this._sessionSlots;

        internal void AssignSlots(List<SessionInfo> sessions)
        {
            var currentIds = new HashSet<String>(sessions.Select(s => s.SessionId));

            // Remove sessions that are no longer present
            var toRemove = this._sessionSlots
                .Where(kv => !currentIds.Contains(kv.Key))
                .Select(kv => kv.Key)
                .ToList();
            foreach (var id in toRemove)
            {
                var slot = this._sessionSlots[id];
                this._slots[slot] = null;
                this._sessionSlots.Remove(id);
            }

            // Update existing and assign new sessions
            foreach (var session in sessions)
            {
                if (this._sessionSlots.TryGetValue(session.SessionId, out var existingSlot))
                {
                    this._slots[existingSlot] = session;
                }
                else
                {
                    // Find first empty slot
                    var emptySlot = -1;
                    for (var i = 0; i < MaxSlots; i++)
                    {
                        if (this._slots[i] == null)
                        {
                            emptySlot = i;
                            break;
                        }
                    }

                    if (emptySlot >= 0)
                    {
                        this._slots[emptySlot] = session;
                        this._sessionSlots[session.SessionId] = emptySlot;
                    }
                }
            }
        }
    }
}
