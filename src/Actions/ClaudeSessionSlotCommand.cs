namespace Loupedeck.ClaudeConsolePlugin
{
    using System;

    public abstract class ClaudeSessionSlotCommand : PluginDynamicCommand
    {
        private static readonly BitmapColor _iconColor = new BitmapColor(0xFF, 0xFF, 0xFF, 0x99);
        private const Int32 IconSize = 80;
        private const Int32 IconY = -30;

        private static (BitmapColor Background, String Icon) GetStateStyle(String state)
        {
            switch (state)
            {
                case "working": return (new BitmapColor(0xCC, 0x33, 0x33), "\u2699");
                case "waiting": return (new BitmapColor(0x33, 0x55, 0xAA), "?");
                default: return (new BitmapColor(0x33, 0x33, 0x33), null);
            }
        }

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

            var (bgColor, icon) = GetStateStyle(session.State);
            builder.Clear(bgColor);

            if (icon != null)
            {
                builder.DrawText(icon, 0, IconY, builder.Width, builder.Height - IconY, _iconColor, IconSize, IconSize, 0);
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
