# Claude Code Console

A Logi Actions SDK plugin that displays active Claude Code session status on a Logitech MX Creative Console keypad. Each session gets a button showing its name and current state with color coding. Pressing a button focuses the corresponding terminal session.

## Session States

- **Red** - active (Claude is working)
- **Gray** - idle

## Prerequisites

- macOS
- .NET 8 SDK (version pinned via `global.json`)
- [Logitech Options+](https://www.logitech.com/software/logi-options-plus.html) with MX Creative Console connected
- [claude-status-tool](https://github.com/lordjabez/claude-status-tool) daemon running (provides the session database)
- [iTerm2](https://iterm2.com/) for button-press focus via AppleScript, or [tmux](https://github.com/tmux/tmux) (preferred when Claude Code sessions run inside tmux)

## Build

```bash
dotnet build
```

Building automatically creates a `.link` file in the Logi Plugin Service plugins directory and sends a reload command. The plugin should appear in Options+ after a successful build.

## Setup

1. Build the plugin (see above)
2. Open Logitech Options+ and select your MX Creative Console
3. Go to the keypad button configuration
4. Assign "Claude Session > Slot 1" through "Slot 9" to the desired buttons

Each slot automatically fills with active Claude Code sessions. Sessions keep their assigned slot across polls until they disappear, at which point the slot is freed for a new session.

## Code Quality

### Linting and Formatting

The `.editorconfig` enforces code style conventions. Check for violations without modifying files:

```bash
dotnet format --verify-no-changes
```

Auto-fix any style drift:

```bash
dotnet format
```

### Static Analysis

The project enables .NET analyzers at `latest-recommended` level with warnings treated as errors. Analysis runs automatically during `dotnet build` and will fail the build on any violation.

### Tests

```bash
dotnet test
```

Tests use xUnit and live in the `tests/` directory. Integration tests create temporary SQLite databases that are cleaned up automatically.

### Logs

The plugin logs to the Logi Plugin Service log directory:

```bash
tail -f ~/Library/Application\ Support/Logi/LogiPluginService/Logs/plugin_logs/ClaudeConsole.log
```

## How It Works

The plugin polls `~/.claude/claude-status.db` (written by the claude-status daemon) every 2 seconds. It joins the `sessions` and `runtime` tables to get session names, states, and terminal info for sessions active in the last 5 minutes. Up to 9 sessions are assigned to stable button slots with color-coded backgrounds.

Pressing a button uses tmux (if a tmux target is available) or iTerm2 AppleScript to focus the corresponding terminal session.
