namespace Loupedeck.ClaudeConsolePlugin.Tests
{
    using System;
    using Xunit;

    public class ITermFocusTests
    {
        [Fact]
        public void BuildTmuxStartInfo_SetsCorrectFileName()
        {
            var info = ITermFocus.BuildTmuxStartInfo("mysession:0.1");
            Assert.Equal("tmux", info.FileName);
        }

        [Fact]
        public void BuildTmuxStartInfo_ContainsSelectWindowAndPane()
        {
            var info = ITermFocus.BuildTmuxStartInfo("mysession:0.1");
            Assert.Contains("select-window -t mysession:0.1", info.Arguments);
            Assert.Contains("select-pane -t mysession:0.1", info.Arguments);
        }

        [Fact]
        public void BuildTmuxStartInfo_RedirectsOutput()
        {
            var info = ITermFocus.BuildTmuxStartInfo("s:0");
            Assert.False(info.UseShellExecute);
            Assert.True(info.RedirectStandardOutput);
            Assert.True(info.RedirectStandardError);
            Assert.True(info.CreateNoWindow);
        }

        [Fact]
        public void BuildITermStartInfo_SetsOsascript()
        {
            var info = ITermFocus.BuildITermStartInfo("ttys001");
            Assert.Equal("osascript", info.FileName);
        }

        [Fact]
        public void BuildITermStartInfo_ContainsTtyInScript()
        {
            var info = ITermFocus.BuildITermStartInfo("ttys042");
            Assert.Contains("/dev/ttys042", info.Arguments);
        }

        [Fact]
        public void BuildITermStartInfo_ContainsITerm2Reference()
        {
            var info = ITermFocus.BuildITermStartInfo("ttys001");
            Assert.Contains("iTerm2", info.Arguments);
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
