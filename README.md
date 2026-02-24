# Claude Code Console

A Logi Actions SDK plugin that displays active Claude Code session status on a Logitech MX Creative Console keypad and MX Master 4 mouse. Each session gets a button showing its name and current state with color coding. Pressing a button focuses the corresponding terminal session.

## Actions

### Claude Session (keypad)

Nine assignable slots for the MX Creative Console keypad. Each slot displays a session's name and state with a color-coded background:

- **Red** - working (Claude is generating)
- **Blue** - waiting (Claude needs your attention)
- **Gray** - idle

Pressing a slot button focuses the corresponding terminal session.

### Focus Priority (mouse button)

Focuses the session most in need of attention: the first waiting session, then the first working session. Does nothing if all sessions are idle. Available two ways:

- **Plugin command** (`FocusPriorityCommand`) - usable from the Creative Console keypad
- **Standalone script** (`scripts/focus-priority.sh`) - assignable to an MX Master 4 button via Options+ "Open application", or bound to a keyboard shortcut. Uses `sqlite3` (ships with macOS) to query the session DB directly.

### Haptic Feedback

The plugin triggers haptic feedback on the MX Master 4 for session events:

- **knock** - session launched, or a session enters the waiting state
- **sharp_state_change** - session changes state (non-waiting transitions)
- **damp_collision** - session closed
- A periodic waiting reminder fires every 30 seconds while any session is in the waiting state

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
4. Assign "Claude Session > Slot 1" through "Slot 9" to the desired keypad buttons
5. To add focus-priority to an MX Master 4 button:

   ```bash
   scripts/build-app.sh
   ```

   Then in Options+, select the mouse, choose a button, set it to "Open application", and pick `scripts/FocusPriority.app`

Each slot automatically fills with active Claude Code sessions ordered by name. Slots are reassigned from scratch each poll cycle.

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

The plugin reads `~/.claude/claude-status.db` (written by the claude-status daemon). It joins the `sessions` and `runtime` tables to get session names, states, and terminal info for sessions active in the last 5 minutes. Up to 9 sessions are assigned to button slots with color-coded backgrounds.

Polling is triggered two ways: a UDP packet on port 25283 (sent by the claude-status daemon on change for low latency), and a 60-second fallback timer.

Pressing a keypad button uses iTerm2 AppleScript to focus the corresponding terminal session.
