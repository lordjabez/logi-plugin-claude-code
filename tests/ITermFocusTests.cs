namespace Loupedeck.ClaudeConsolePlugin.Tests
{
    using System;
    using System.Linq;
    using Xunit;

    public class ITermFocusTests
    {
        [Fact]
        public void BuildITermStartInfo_UsesOsascript()
        {
            var info = ITermFocus.BuildITermStartInfo("ttys001");
            Assert.Equal("osascript", info.FileName);
        }

        [Fact]
        public void BuildITermStartInfo_PassesScriptAsSingleArgument()
        {
            var info = ITermFocus.BuildITermStartInfo("ttys042");
            var args = info.ArgumentList.ToList();

            Assert.Equal(2, args.Count);
            Assert.Equal("-e", args[0]);
            Assert.Contains("/dev/ttys042", args[1]);
        }

        [Fact]
        public void BuildITermStartInfo_ReferencesITerm()
        {
            var info = ITermFocus.BuildITermStartInfo("ttys001");
            var script = info.ArgumentList.ToList()[1];

            Assert.Contains("\"iTerm\"", script);
            Assert.Contains("activate", script);
        }

        [Fact]
        public void BuildITermStartInfo_RedirectsOutput()
        {
            var info = ITermFocus.BuildITermStartInfo("ttys001");
            Assert.False(info.UseShellExecute);
            Assert.True(info.RedirectStandardOutput);
            Assert.True(info.RedirectStandardError);
            Assert.True(info.CreateNoWindow);
        }
    }
}
