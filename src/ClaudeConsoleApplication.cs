namespace Loupedeck.ClaudeConsolePlugin
{
    using System;

    public class ClaudeConsoleApplication : ClientApplication
    {
        public ClaudeConsoleApplication()
        {
        }

        protected override String GetProcessName() => "";

        protected override String GetBundleName() => "";

        public override ClientApplicationStatus GetApplicationStatus() => ClientApplicationStatus.Unknown;
    }
}
