# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

A Logi Actions SDK plugin (C#) that displays active Claude Code session status on a Logitech MX Creative Console keypad. Each session gets a button showing its name and state (working/idle) with color coding. Pressing a button focuses the corresponding terminal session.

## Building

```bash
dotnet build
```

Building automatically links the plugin into the Logi Plugin Service and triggers a reload. The plugin should appear in Options+ after a successful build.

## Architecture

The plugin runs inside the Logi Plugin Service process. It follows the standard Loupedeck plugin structure:

- **ClaudeConsolePlugin.cs** - Plugin entry point. Creates a `SessionStore` and starts a 2-second poll timer. When sessions change, notifies the command to re-render button images.
- **ClaudeSessionCommand.cs** - Dynamic command with 9 parameterized slots. Each slot is an assignable action in Options+. Renders colored backgrounds with session name/state text. Button press focuses the terminal session.
- **SessionStore.cs** - Opens `~/.claude/claude-status.db` (written by the claude-status daemon) read-only. Joins `sessions` and `runtime` tables for sessions updated within the last 5 minutes. Maintains slot stability: sessions keep their assigned slot until they disappear.
- **ITermFocus.cs** - Focuses a terminal session. Uses tmux `select-pane` when a tmux target is available, falls back to iTerm2 AppleScript via `osascript`.
- **PluginLog.cs** - SDK helper wrapper for logging.

Data flow: claude-status DB -> SessionStore.Poll() -> ClaudeSessionCommand.GetCommandImage() -> Options+ button display

## Session States

Two states from the `runtime` table: `active` (red background, "working" label) and `idle` (gray background, "idle" label).

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
  Helpers/SessionStore.cs              # SQLite poller + slot assignment
  Helpers/ITermFocus.cs                # Terminal focus (tmux/iTerm2)
  Helpers/PluginLog.cs                 # Logging wrapper
  package/metadata/LoupedeckPackage.yaml
  package/metadata/Icon256x256.png
tests/
  ClaudeConsole.Tests.csproj           # xUnit test project
  SessionStoreSlotAssignmentTests.cs   # Slot assignment unit tests
  SessionStoreIntegrationTests.cs      # SQLite integration tests
  ITermFocusTests.cs                   # ProcessStartInfo builder tests
```

## Code Quality

```bash
dotnet build                       # zero warnings (analyzers + warnings-as-errors)
dotnet format --verify-no-changes  # editorconfig style check
dotnet test                        # xUnit tests
```

## Notes

- macOS-only due to iTerm2 AppleScript and tmux integration
- The SQLite database is external (written by claude-status daemon); this plugin only reads it
- Requires Options+ running with MX Creative Console connected
