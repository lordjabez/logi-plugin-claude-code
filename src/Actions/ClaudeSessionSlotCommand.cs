namespace Loupedeck.ClaudeConsolePlugin
{
    using System;

    public abstract class ClaudeSessionSlotCommand : PluginDynamicCommand
    {
        private static readonly BitmapColor _workingColor = new BitmapColor(0xCC, 0x33, 0x33);
        private static readonly BitmapColor _waitingColor = new BitmapColor(0x33, 0x55, 0xAA);
        private static readonly BitmapColor _idleColor = new BitmapColor(0x33, 0x33, 0x33);

        protected ClaudeSessionSlotCommand(Int32 slot)
            : base($"Session Slot {slot + 1}", $"Shows Claude Code session in slot {slot + 1}", "Claude Sessions")
        {
            this.Slot = slot;
        }

        internal Int32 Slot { get; }

        protected override void RunCommand(String actionParameter)
        {
            var plugin = (ClaudeConsolePlugin)this.Plugin;
            var session = plugin.Store?.GetSession(this.Slot);
            if (session == null)
            {
                return;
            }

            ITermFocus.Focus(session.Tty, session.TmuxTarget);
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize)
        {
            var plugin = (ClaudeConsolePlugin)this.Plugin;
            var session = plugin.Store?.GetSession(this.Slot);
            if (session == null)
            {
                return " ";
            }

            return session.Name;
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var plugin = (ClaudeConsolePlugin)this.Plugin;
            var session = plugin.Store?.GetSession(this.Slot);

            var builder = new BitmapBuilder(imageSize);

            if (session == null)
            {
                builder.Clear(BitmapColor.Black);
                return builder.ToImage();
            }

            BitmapColor bgColor;
            switch (session.State)
            {
                case "working":
                    bgColor = _workingColor;
                    break;
                case "waiting":
                    bgColor = _waitingColor;
                    break;
                default:
                    bgColor = _idleColor;
                    break;
            }

            builder.Clear(bgColor);

            var iconColor = new BitmapColor(0xFF, 0xFF, 0xFF, 0x99);
            var iconSize = 80;
            var iconY = -30;

            switch (session.State)
            {
                case "working":
                    builder.DrawText("\u2699", 0, iconY, builder.Width, builder.Height - iconY, iconColor, iconSize, iconSize, 0);
                    break;
                case "waiting":
                    builder.DrawText("?", 0, iconY, builder.Width, builder.Height - iconY, iconColor, iconSize, iconSize, 0);
                    break;
            }

            return builder.ToImage();
        }

        internal void NotifyImageChanged()
        {
            this.ActionImageChanged();
        }
    }

    public class ClaudeSessionSlot0Command : ClaudeSessionSlotCommand { public ClaudeSessionSlot0Command() : base(0) { } }
    public class ClaudeSessionSlot1Command : ClaudeSessionSlotCommand { public ClaudeSessionSlot1Command() : base(1) { } }
    public class ClaudeSessionSlot2Command : ClaudeSessionSlotCommand { public ClaudeSessionSlot2Command() : base(2) { } }
    public class ClaudeSessionSlot3Command : ClaudeSessionSlotCommand { public ClaudeSessionSlot3Command() : base(3) { } }
    public class ClaudeSessionSlot4Command : ClaudeSessionSlotCommand { public ClaudeSessionSlot4Command() : base(4) { } }
    public class ClaudeSessionSlot5Command : ClaudeSessionSlotCommand { public ClaudeSessionSlot5Command() : base(5) { } }
    public class ClaudeSessionSlot6Command : ClaudeSessionSlotCommand { public ClaudeSessionSlot6Command() : base(6) { } }
    public class ClaudeSessionSlot7Command : ClaudeSessionSlotCommand { public ClaudeSessionSlot7Command() : base(7) { } }
    public class ClaudeSessionSlot8Command : ClaudeSessionSlotCommand { public ClaudeSessionSlot8Command() : base(8) { } }
}
