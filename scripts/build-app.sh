#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
SCRIPT_PATH="$SCRIPT_DIR/focus-priority.sh"
APP_PATH="$SCRIPT_DIR/FocusPriority.app"

rm -rf "$APP_PATH"

osacompile -o "$APP_PATH" -e "do shell script \"$SCRIPT_PATH\""

/usr/libexec/PlistBuddy -c "Add :LSUIElement bool true" "$APP_PATH/Contents/Info.plist"
/usr/libexec/PlistBuddy -c "Add :CFBundleIdentifier string com.lordjabez.FocusPriority" "$APP_PATH/Contents/Info.plist"

codesign --force --sign - "$APP_PATH"

echo "Built $APP_PATH"
