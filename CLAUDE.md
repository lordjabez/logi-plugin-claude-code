# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

A Logi Actions SDK plugin (C#) that displays active Claude Code session status on a Logitech MX Creative Console keypad and MX Master 4 mouse. Each session gets a button with a color-coded background (red = working, blue = waiting, gray = idle) and the session name as the SDK display label. Pressing a button focuses the corresponding terminal session. Haptic feedback on the MX Master 4 alerts when sessions change state.

## Building

```bash
dotnet build
```

Building automatically links the plugin into the Logi Plugin Service and triggers a reload. The plugin should appear in Options+ after a successful build.

## Architecture

The plugin runs inside the Logi Plugin Service process. It follows the standard Loupedeck plugin structure:

- **ClaudeConsolePlugin.cs** - Plugin entry point. Creates a `SessionStore`, listens for UDP triggers on port 25283, and runs a 60-second fallback poll timer. Notifies commands to re-render button images each tick. Fires haptic events on session state changes.
- **ClaudeSessionSlotCommand.cs** - Abstract base with 9 concrete subclasses (`ClaudeSessionSlot0Command` through `ClaudeSessionSlot8Command`), one per assignable button in Options+. Each knows its own slot index, so rendering and button press both resolve directly via `store.GetSession(slot)`. Renders color-coded backgrounds via `GetCommandImage()` and session names via `GetCommandDisplayName()`; button press focuses the terminal via iTerm AppleScript.
- **FocusPriorityCommand.cs** - Single-button action that focuses the session most in need of attention: longest-waiting first, then longest-working. Also available as a standalone script (`scripts/focus-priority.sh`) wrapped in a `.app` bundle for Options+ mouse button assignment.
- **SessionStore.cs** - Opens `~/.claude/claude-status.db` (written by the claude-status daemon) read-only. Joins `sessions` and `runtime` tables for sessions updated within the last 5 minutes, ordered by name. Slots are reassigned from scratch each poll. Tracks state entry times for priority focus ordering.
- **ITermFocus.cs** - Focuses a terminal session by matching the client tty to an iTerm session via AppleScript (`osascript`). Note: the AppleScript application name is `"iTerm"`, not `"iTerm2"`.
- **PluginLog.cs** - SDK helper wrapper for logging.

Data flow: claude-status DB -> SessionStore.Poll() -> ClaudeSessionSlotCommand.GetCommandImage() + GetCommandDisplayName() -> Options+ button display

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
  Actions/ClaudeSessionSlotCommand.cs   # Button commands (9 fixed slots)
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

## "Full Size" Buttons Are Incompatible with Dynamic Images

**Do not use "Full Size" in Options+ for session slot buttons.** It creates a static `.ict` icon cache file that freezes the button. This is a Logi Actions SDK limitation, not a plugin bug.

### What was investigated

The SDK has two image paths: `GetCommandImage()` (dynamic, called by the plugin) and `.ict` files (static JSON with a base64 PNG snapshot, created by Options+ when the user edits icon layout). "Full Size" is a device layout mode that displays one button at a larger physical size.

Every approach was tested:

1. **Just `ActionImageChanged()`** - SDK creates `.ict` on Full Size click, then ignores `GetCommandImage()` forever. Button freezes.
2. **Patch `.ict` on disk, skip `ActionImageChanged()`** - Options+ UI updates, but physical device does not. The device requires `ActionImageChanged()` to push images.
3. **Patch `.ict` + `ActionImageChanged()` (no-arg)** - Device updates but Full Size resets to normal. The no-arg overload sets `AffectsAllParameters=true`.
4. **Patch `.ict` + `ActionImageChanged("")` (parameterized)** - Same result: Full Size resets.
5. **Dynamic `GetCommandDisplayName()` with static image** - Returned session name from `GetCommandDisplayName()` while using a plain color image. The `.ict` `Text` item is also frozen; display name text does not update on the device in Full Size mode.

Logging confirmed that `GetCommandImage()` is always called with `size=Width116` when `ActionImageChanged` fires, regardless of Full Size setting. The `.ict` is only used by the Options+ UI, not the device render path. `ActionImageChanged()` (both overloads) resets the layout. There is no alternative API to push images or text to the device.

### SDK internals (from reflection on PluginApi.dll v6.2.6)

- `PluginDynamicAction.ActionImageChanged()` - no-arg, sets `AffectsAllParameters=true`
- `PluginDynamicAction.ActionImageChanged(String actionParameter)` - parameterized overload
- `PluginActionImageChangedEventArgs` has fields: `ActionName` (String), `AffectsAllParameters` (Boolean)
- `PluginImageSize` enum: `None=0, Width90=1, Width60=2, Width116=3` (no Full Size variant)
- `BitmapImageBase.ToBase64String()` returns base64 PNG; `BitmapBuilder(Int32, Int32)` for arbitrary sizes
- No direct device framebuffer API exists; `ActionImageChanged` is the only path to push images

### .ict file details

Created at `~/Library/Application Support/Logi/LogiPluginService/Applications/Loupedeck70/@_defaultmac/Profiles/{GUID}/ActionIcons/`. Filename: `$ClaudeConsole___Loupedeck.ClaudeConsolePlugin.{ClassName}.ict`. JSON with `backgroundColor` (ARGB int32), `items` array containing an `Image` item (base64 PNG) and a `Text` item.

### Cleaning up stale .ict files

If `.ict` files cause frozen buttons (e.g., after renaming command classes):

```bash
find ~/Library/Application\ Support/Logi/LogiPluginService/Applications -name '*ClaudeConsole*ClaudeSession*' -type f -delete
pkill -f LogiPluginService  # Options+ will relaunch it
```

## Notes

- macOS-only due to iTerm AppleScript integration
- The SQLite database is external (written by claude-status daemon); this plugin only reads it
- Requires Options+ running with MX Creative Console connected
- Plugin logs to `~/Library/Application Support/Logi/LogiPluginService/Logs/plugin_logs/ClaudeConsole.log`
- If the plugin fails to reload after a build (`plugin already loaded` error in logs), restart the service with `pkill -f LogiPluginService` (Options+ will relaunch it automatically)
