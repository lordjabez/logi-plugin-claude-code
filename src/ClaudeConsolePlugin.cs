namespace Loupedeck.ClaudeConsolePlugin
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    public class ClaudeConsolePlugin : Plugin
    {
        public override Boolean UsesApplicationApiOnly => true;
        public override Boolean HasNoApplication => true;

        private static readonly Dictionary<SessionChangeKind, String> _hapticWaveforms = new Dictionary<SessionChangeKind, String>
        {
            { SessionChangeKind.Launched, "knock" },
            { SessionChangeKind.Closed, "damp_collision" },
            { SessionChangeKind.StateChanged, "sharp_state_change" },
        };

        private SessionStore _store;
        private Timer _pollTimer;

        public ClaudeConsolePlugin()
        {
            PluginLog.Init(this.Log);
        }

        public override void Load()
        {
            foreach (var waveform in _hapticWaveforms.Values)
            {
                this.PluginEvents.AddEvent(waveform, waveform, null);
            }

            this._store = new SessionStore();
            this._pollTimer = new Timer(this.OnPollTick, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
        }

        public override void Unload()
        {
            this._pollTimer?.Dispose();
            this._pollTimer = null;
            this._store = null;
        }

        internal SessionStore Store => this._store;

        private void OnPollTick(Object state)
        {
            try
            {
                if (this._store == null)
                {
                    return;
                }

                this._store.Poll();

                foreach (var change in this._store.LastChanges)
                {
                    if (_hapticWaveforms.TryGetValue(change.Kind, out var waveform))
                    {
                        PluginLog.Info($"Haptic: {waveform} for session {change.SessionId}");
                        this.PluginEvents.RaiseEvent(waveform);
                    }
                }

                // Always notify so buttons re-render with current session data.
                // The initial GetCommandImage calls happen before the first poll,
                // so buttons need a refresh once data is available.
                foreach (var command in this.DynamicCommands)
                {
                    if (command is ClaudeSessionCommand sessionCommand)
                    {
                        sessionCommand.NotifyImageChanged();
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Poll tick failed");
            }
        }
    }
}
