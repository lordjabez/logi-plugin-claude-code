namespace Loupedeck.ClaudeConsolePlugin
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Data.Sqlite;

    internal class SessionChange
    {
        public String SessionId { get; set; }
        public String PreviousState { get; set; }
        public String NewState { get; set; }
    }

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
        private Dictionary<String, String> _previousStates = new Dictionary<String, String>();
        private Dictionary<String, DateTime> _stateEntryTimes = new Dictionary<String, DateTime>();
        private Boolean _hasPreviousPoll;

        public List<SessionChange> LastChanges { get; private set; } = new List<SessionChange>();

        public Boolean HasWaitingSessions
        {
            get
            {
                for (var i = 0; i < MaxSlots; i++)
                {
                    if (this._slots[i]?.State == "waiting")
                    {
                        return true;
                    }
                }

                return false;
            }
        }

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
                PluginLog.Verbose($"DB not found at {this._dbPath}");
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
                ORDER BY name
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

        public SessionInfo GetPriorityFocusSession()
        {
            SessionInfo bestWaiting = null;
            var bestWaitingTime = DateTime.MaxValue;
            SessionInfo bestWorking = null;
            var bestWorkingTime = DateTime.MaxValue;

            for (var i = 0; i < MaxSlots; i++)
            {
                var session = this._slots[i];
                if (session == null)
                {
                    continue;
                }

                if (!this._stateEntryTimes.TryGetValue(session.SessionId, out var entryTime))
                {
                    continue;
                }

                if (session.State == "waiting" && entryTime < bestWaitingTime)
                {
                    bestWaiting = session;
                    bestWaitingTime = entryTime;
                }
                else if (session.State == "working" && entryTime < bestWorkingTime)
                {
                    bestWorking = session;
                    bestWorkingTime = entryTime;
                }
            }

            return bestWaiting ?? bestWorking;
        }

        internal SessionInfo[] Slots => this._slots;

        internal Dictionary<String, DateTime> StateEntryTimes => this._stateEntryTimes;

        internal void AssignSlots(List<SessionInfo> sessions)
        {
            var changes = new List<SessionChange>();
            var currentStates = new Dictionary<String, String>();

            foreach (var session in sessions)
            {
                currentStates[session.SessionId] = session.State;
            }

            if (this._hasPreviousPoll)
            {
                foreach (var session in sessions)
                {
                    if (this._previousStates.TryGetValue(session.SessionId, out var previousState)
                        && previousState != session.State)
                    {
                        changes.Add(new SessionChange
                        {
                            SessionId = session.SessionId,
                            PreviousState = previousState,
                            NewState = session.State,
                        });
                    }
                }
            }

            var newEntryTimes = new Dictionary<String, DateTime>();
            foreach (var session in sessions)
            {
                var stateUnchanged = this._previousStates.TryGetValue(session.SessionId, out var prevState)
                    && prevState == session.State;
                if (stateUnchanged && this._stateEntryTimes.TryGetValue(session.SessionId, out var entryTime))
                {
                    newEntryTimes[session.SessionId] = entryTime;
                }
                else
                {
                    newEntryTimes[session.SessionId] = DateTime.UtcNow;
                }
            }

            this._stateEntryTimes = newEntryTimes;

            this._previousStates = currentStates;
            this._hasPreviousPoll = true;
            this.LastChanges = changes;

            for (var i = 0; i < MaxSlots; i++)
            {
                this._slots[i] = i < sessions.Count ? sessions[i] : null;
            }
        }
    }
}
