namespace Loupedeck.ClaudeConsolePlugin
{
    using System;

    public class FocusPriorityCommand : PluginDynamicCommand
    {
        public FocusPriorityCommand()
            : base("Focus Priority", "Focuses the Claude session most in need of attention", "Claude Sessions")
        {
        }

        protected override void RunCommand(String actionParameter)
        {
            var plugin = (ClaudeConsolePlugin)this.Plugin;
            var session = plugin.Store?.GetPriorityFocusSession();
            if (session == null)
            {
                return;
            }

            ITermFocus.Focus(session.Tty, session.TmuxTarget);
        }
    }
}
