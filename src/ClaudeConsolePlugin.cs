namespace Loupedeck.ClaudeConsolePlugin
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;

    public class ClaudeConsolePlugin : Plugin
    {
        public override Boolean UsesApplicationApiOnly => true;
        public override Boolean HasNoApplication => true;

        internal const Int32 UdpPort = 25283;
        private const String WaitingReminderWaveform = "knock";
        private static readonly TimeSpan _waitingReminderInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan _fallbackPollInterval = TimeSpan.FromSeconds(60);

        private const String StateChangeWaveform = "sharp_state_change";

        private readonly Object _pollLock = new Object();
        private readonly Stopwatch _waitingReminderTimer = new Stopwatch();
        private SessionStore _store;
        private Timer _pollTimer;
        private UdpClient _udpListener;
        private Thread _udpThread;
        private volatile Boolean _running;

        public ClaudeConsolePlugin()
        {
            PluginLog.Init(this.Log);
        }

        public override void Load()
        {
            this.PluginEvents.AddEvent(StateChangeWaveform, StateChangeWaveform, null);
            this.PluginEvents.AddEvent(WaitingReminderWaveform, WaitingReminderWaveform, null);

            this._store = new SessionStore();
            this._running = true;

            try
            {
                this._udpListener = new UdpClient(UdpPort);
                this._udpThread = new Thread(this.UdpListenLoop)
                {
                    IsBackground = true,
                    Name = "ClaudeConsole-UDP",
                };
                this._udpThread.Start();
                PluginLog.Info($"UDP listener started on port {UdpPort}");
            }
            catch (SocketException ex)
            {
                PluginLog.Warning(ex, $"Failed to bind UDP port {UdpPort}, using timer-only polling");
            }

            this._pollTimer = new Timer(this.OnPollTick, null, TimeSpan.Zero, _fallbackPollInterval);
        }

        public override void Unload()
        {
            this._running = false;
            this._pollTimer?.Dispose();
            this._pollTimer = null;
            this._udpListener?.Dispose();
            this._udpListener = null;
            this._store = null;
        }

        internal SessionStore Store => this._store;

        private void UdpListenLoop()
        {
            var endpoint = new IPEndPoint(IPAddress.Any, 0);
            while (this._running)
            {
                try
                {
                    this._udpListener.Receive(ref endpoint);
                    this.DoPoll();
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (SocketException) when (!this._running)
                {
                    break;
                }
                catch (Exception ex)
                {
                    PluginLog.Warning(ex, "UDP receive error");
                }
            }
        }

        private void OnPollTick(Object state) => this.DoPoll();

        private void DoPoll()
        {
            lock (this._pollLock)
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
                        var isCompletionTransition =
                            (change.PreviousState == "working" && change.NewState == "waiting")
                            || (change.PreviousState == "working" && change.NewState == "idle")
                            || (change.PreviousState == "waiting" && change.NewState == "idle");

                        if (!isCompletionTransition)
                        {
                            continue;
                        }

                        var waveform = change.NewState == "waiting"
                            ? WaitingReminderWaveform
                            : StateChangeWaveform;
                        PluginLog.Info($"Haptic: {waveform} for session {change.SessionId}");
                        this.PluginEvents.RaiseEvent(waveform);
                    }

                    if (this._store.HasWaitingSessions)
                    {
                        if (!this._waitingReminderTimer.IsRunning)
                        {
                            this._waitingReminderTimer.Restart();
                        }
                        else if (this._waitingReminderTimer.Elapsed >= _waitingReminderInterval)
                        {
                            PluginLog.Info("Haptic: waiting reminder");
                            this.PluginEvents.RaiseEvent(WaitingReminderWaveform);
                            this._waitingReminderTimer.Restart();
                        }
                    }
                    else
                    {
                        this._waitingReminderTimer.Reset();
                    }

                    foreach (var command in this.DynamicCommands)
                    {
                        if (command is ClaudeSessionSlotCommand slotCommand)
                        {
                            slotCommand.NotifyImageChanged();
                        }
                    }
                }
                catch (Exception ex)
                {
                    PluginLog.Error(ex, "Poll failed");
                }
            }
        }
    }
}
