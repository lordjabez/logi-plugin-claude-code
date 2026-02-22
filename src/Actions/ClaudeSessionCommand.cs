namespace Loupedeck.ClaudeConsolePlugin
{
    using System;
    using System.Collections.Generic;

    public class ClaudeSessionCommand : PluginDynamicCommand
    {
        private const Int32 MaxSlots = 9;
        private readonly Dictionary<String, Int32> _actionToSlot = new Dictionary<String, Int32>();
        private Int32 _nextSlot = 0;

        public ClaudeSessionCommand()
            : base("Claude Session", "Shows Claude Code session status", "Claude Sessions")
        {
            this.MakeProfileAction("list");
            for (var i = 0; i < MaxSlots; i++)
            {
                this.AddParameter(i.ToString(), $"Slot {i + 1}", "Claude Sessions");
            }
        }

        private Int32 ResolveSlot(String actionParameter)
        {
            if (String.IsNullOrEmpty(actionParameter))
            {
                return -1;
            }

            if (this._actionToSlot.TryGetValue(actionParameter, out var existing))
            {
                return existing;
            }

            if (this._nextSlot >= MaxSlots)
            {
                return -1;
            }

            var slot = this._nextSlot++;
            this._actionToSlot[actionParameter] = slot;
            return slot;
        }

        protected override void RunCommand(String actionParameter)
        {
            var slot = this.ResolveSlot(actionParameter);
            if (slot < 0)
            {
                return;
            }

            var plugin = (ClaudeConsolePlugin)this.Plugin;
            var session = plugin.Store?.GetSession(slot);
            if (session == null)
            {
                return;
            }

            ITermFocus.Focus(session.Tty, session.TmuxTarget);
        }

        // Return empty string to suppress the SDK's default "Slot X" text overlay
        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize)
        {
            return " ";
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var slot = this.ResolveSlot(actionParameter);

            var plugin = (ClaudeConsolePlugin)this.Plugin;
            var session = slot >= 0 ? plugin.Store?.GetSession(slot) : null;

            var builder = new BitmapBuilder(imageSize);

            if (session == null)
            {
                builder.Clear(BitmapColor.Black);
                return builder.ToImage();
            }

            var bgColor = session.State == "active"
                ? new BitmapColor(0xCC, 0x33, 0x33)
                : new BitmapColor(0x33, 0x33, 0x33);
            builder.Clear(bgColor);

            var stateLabel = session.State == "active" ? "working" : "idle";
            builder.DrawText($"{session.Name}\n{stateLabel}", new BitmapColor(255, 255, 255), 12);

            return builder.ToImage();
        }

        internal void NotifyImageChanged()
        {
            this.ActionImageChanged();
        }
    }
}
