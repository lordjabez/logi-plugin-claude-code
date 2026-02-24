namespace Loupedeck.ClaudeConsolePlugin
{
    using System;
    using System.Collections.Generic;

    public class ClaudeSessionCommand : PluginDynamicCommand
    {
        private const Int32 MaxSlots = 9;
        private static readonly BitmapColor _workingColor = new BitmapColor(0xCC, 0x33, 0x33);
        private static readonly BitmapColor _waitingColor = new BitmapColor(0x33, 0x55, 0xAA);
        private static readonly BitmapColor _idleColor = new BitmapColor(0x33, 0x33, 0x33);
        private readonly Dictionary<String, Int32> _actionToSlot = new Dictionary<String, Int32>();
        private Int32 _nextSlot;

        public ClaudeSessionCommand()
            : base("Claude Session", "Shows Claude Code session status", "Claude Sessions")
        {
            this.MakeProfileAction("list");
            for (var i = 0; i < MaxSlots; i++)
            {
                this.AddParameter(i.ToString(System.Globalization.CultureInfo.InvariantCulture), $"Slot {i + 1}", "Claude Sessions");
            }
        }

        // GetCommandImage receives SDK-assigned GUIDs; map them to sequential slots
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

        // RunCommand receives the original "0"-"8" parameter names
        private static Int32 ParseSlot(String actionParameter)
        {
            if (Int32.TryParse(actionParameter, out var slot) && slot >= 0 && slot < MaxSlots)
            {
                return slot;
            }

            return -1;
        }

        protected override void RunCommand(String actionParameter)
        {
            var slot = ParseSlot(actionParameter);
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
            builder.DrawText(session.Name, new BitmapColor(255, 255, 255), 16);

            return builder.ToImage();
        }

        internal void NotifyImageChanged()
        {
            this.ActionImageChanged();
        }
    }
}
