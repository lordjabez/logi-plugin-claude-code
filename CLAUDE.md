# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

A Logi Actions SDK plugin (C#) that displays active Claude Code session status on a Logitech MX Creative Console keypad and MX Master 4 mouse. Each session gets a button showing its name with a color-coded background (red = working, blue = waiting, gray = idle). Pressing a button focuses the corresponding terminal session. Haptic feedback on the MX Master 4 alerts when sessions change state.

## Building

```bash
dotnet build
```

Building automatically links the plugin into the Logi Plugin Service and triggers a reload. The plugin should appear in Options+ after a successful build.

## Architecture

The plugin runs inside the Logi Plugin Service process. It follows the standard Loupedeck plugin structure:

- **ClaudeConsolePlugin.cs** - Plugin entry point. Creates a `SessionStore`, listens for UDP triggers on port 25283, and runs a 60-second fallback poll timer. Notifies commands to re-render button images each tick. Fires haptic events on session state changes.
- **ClaudeSessionCommand.cs** - Dynamic command with 9 parameterized slots. Each slot is an assignable action in Options+. Renders colored backgrounds with the session name. Button press focuses the terminal via iTerm AppleScript. Note: the SDK passes different parameter formats to `GetCommandImage` (GUIDs) vs `RunCommand` (original "0"-"8" names), so slot resolution uses two different methods. **Do not edit the button icon layout in Options+** (e.g. "Full Size") or the SDK will stop forwarding image updates from the plugin.
- **FocusPriorityCommand.cs** - Single-button action that focuses the session most in need of attention: longest-waiting first, then longest-working. Also available as a standalone script (`scripts/focus-priority.sh`) wrapped in a `.app` bundle for Options+ mouse button assignment.
- **SessionStore.cs** - Opens `~/.claude/claude-status.db` (written by the claude-status daemon) read-only. Joins `sessions` and `runtime` tables for sessions updated within the last 5 minutes, ordered by name. Slots are reassigned from scratch each poll. Tracks state entry times for priority focus ordering.
- **ITermFocus.cs** - Focuses a terminal session by matching the client tty to an iTerm session via AppleScript (`osascript`). Note: the AppleScript application name is `"iTerm"`, not `"iTerm2"`.
- **PluginLog.cs** - SDK helper wrapper for logging.

Data flow: claude-status DB -> SessionStore.Poll() -> ClaudeSessionCommand.GetCommandImage() -> Options+ button display

## Session States

Three states from the `runtime` table: `working` (red background), `waiting` (blue background), and `idle` (gray background). Haptic feedback is triggered on the MX Master 4 when sessions change state (`sharp_state_change`), are launched (`knock`), or close (`damp_collision`).

## Key Dependencies

- `PluginApi.dll` - Logi Actions SDK (from LogiPluginService installation)
- `Microsoft.Data.Sqlite` - SQLite reader for the session database

## Project Structure

```text
src/
  ClaudeConsole.csproj                 # Project file with NuGet refs
  ClaudeConsolePlugin.cs               # Plugin entry point
  ClaudeConsoleApplication.cs          # Required SDK application stub
  Actions/ClaudeSessionCommand.cs      # Button command (9 slots)
  Actions/FocusPriorityCommand.cs      # Priority focus (MX Master 4 button)
  Helpers/SessionStore.cs              # SQLite poller + slot assignment
  Helpers/ITermFocus.cs                # Terminal focus (iTerm AppleScript)
  Helpers/PluginLog.cs                 # Logging wrapper
  package/metadata/LoupedeckPackage.yaml
  package/metadata/Icon256x256.png
scripts/
  focus-priority.sh                    # Standalone focus-priority for mouse button
  build-app.sh                         # Wraps focus-priority.sh into a .app for Options+
tests/
  ClaudeConsole.Tests.csproj           # xUnit test project
  SessionStoreSlotAssignmentTests.cs   # Slot assignment unit tests
  SessionStoreIntegrationTests.cs      # SQLite integration tests
  SessionStorePriorityFocusTests.cs     # Priority focus logic tests
  ITermFocusTests.cs                   # ProcessStartInfo builder tests
```

## Code Quality

```bash
dotnet build                       # zero warnings (analyzers + warnings-as-errors)
dotnet format --verify-no-changes  # editorconfig style check
dotnet test                        # xUnit tests
```

## Notes

- macOS-only due to iTerm AppleScript integration
- The SQLite database is external (written by claude-status daemon); this plugin only reads it
- Requires Options+ running with MX Creative Console connected
- Plugin logs to `~/Library/Application Support/Logi/LogiPluginService/Logs/plugin_logs/ClaudeConsole.log`
- If the plugin fails to reload after a build (`plugin already loaded` error in logs), restart the service with `pkill -f LogiPluginService` (Options+ will relaunch it automatically)
