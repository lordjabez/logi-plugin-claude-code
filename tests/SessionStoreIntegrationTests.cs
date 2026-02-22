namespace Loupedeck.ClaudeConsolePlugin.Tests
{
    using System;
    using System.IO;
    using Microsoft.Data.Sqlite;
    using Xunit;

    public class SessionStoreIntegrationTests : IDisposable
    {
        private readonly String _dbPath;
        private readonly SessionStore _store;

        public SessionStoreIntegrationTests()
        {
            this._dbPath = Path.Combine(Path.GetTempPath(), $"claude-test-{Guid.NewGuid()}.db");
            this.CreateSchema();
            this._store = new SessionStore(this._dbPath);
        }

        public void Dispose()
        {
            this._store.Dispose();
            if (File.Exists(this._dbPath))
            {
                File.Delete(this._dbPath);
            }
        }

        private String ConnectionString => new SqliteConnectionStringBuilder
        {
            DataSource = this._dbPath,
        }.ToString();

        private void CreateSchema()
        {
            using var conn = new SqliteConnection(this.ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE sessions (
                    session_id TEXT PRIMARY KEY,
                    custom_title TEXT,
                    slug TEXT,
                    project_path TEXT
                );
                CREATE TABLE runtime (
                    session_id TEXT PRIMARY KEY,
                    tty TEXT,
                    tmux_target TEXT,
                    state TEXT,
                    updated_at TEXT
                );";
            cmd.ExecuteNonQuery();
        }

        private void InsertSession(String id, String state, String minutesAgo = "0", String customTitle = null, String slug = null)
        {
            using var conn = new SqliteConnection(this.ConnectionString);
            conn.Open();

            using var sessionCmd = conn.CreateCommand();
            sessionCmd.CommandText = "INSERT INTO sessions (session_id, custom_title, slug, project_path) VALUES ($id, $title, $slug, '/tmp')";
            sessionCmd.Parameters.AddWithValue("$id", id);
            sessionCmd.Parameters.AddWithValue("$title", (Object)customTitle ?? DBNull.Value);
            sessionCmd.Parameters.AddWithValue("$slug", (Object)slug ?? DBNull.Value);
            sessionCmd.ExecuteNonQuery();

            using var runtimeCmd = conn.CreateCommand();
            runtimeCmd.CommandText = @"INSERT INTO runtime (session_id, tty, tmux_target, state, updated_at)
                VALUES ($id, 'ttys000', '', $state, datetime('now', $offset))";
            runtimeCmd.Parameters.AddWithValue("$id", id);
            runtimeCmd.Parameters.AddWithValue("$state", state);
            runtimeCmd.Parameters.AddWithValue("$offset", $"-{minutesAgo} minutes");
            runtimeCmd.ExecuteNonQuery();
        }

        [Fact]
        public void Poll_PopulatesSlots()
        {
            this.InsertSession("s1", "active", customTitle: "My Session");
            this.InsertSession("s2", "idle", slug: "project-slug");

            this._store.Poll();

            Assert.NotNull(this._store.GetSession(0));
            Assert.NotNull(this._store.GetSession(1));
        }

        [Fact]
        public void Poll_CustomTitleTakesPrecedenceOverSlug()
        {
            this.InsertSession("s1", "idle", customTitle: "Custom", slug: "slug-name");

            this._store.Poll();

            Assert.Equal("Custom", this._store.GetSession(0).Name);
        }

        [Fact]
        public void Poll_FallsBackToSlug()
        {
            this.InsertSession("s1", "idle", slug: "my-slug");

            this._store.Poll();

            Assert.Equal("my-slug", this._store.GetSession(0).Name);
        }

        [Fact]
        public void Poll_OldSessionsExcluded()
        {
            this.InsertSession("old", "idle", minutesAgo: "10");

            this._store.Poll();

            Assert.Null(this._store.GetSession(0));
        }

        [Fact]
        public void Poll_MissingDbHandledGracefully()
        {
            var store = new SessionStore("/nonexistent/path/claude-status.db");
            store.Poll(); // should not throw
            Assert.Null(store.GetSession(0));
            store.Dispose();
        }
    }
}
