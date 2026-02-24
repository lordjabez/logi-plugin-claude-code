#!/usr/bin/env bash

set -euo pipefail

DB="$HOME/.claude/claude-status.db"

if [[ ! -f "$DB" ]]; then
    exit 0
fi

tty=$(sqlite3 "$DB" "
    SELECT r.tty
    FROM sessions s
    JOIN runtime r ON s.session_id = r.session_id
    WHERE r.updated_at > datetime('now', '-5 minutes')
    ORDER BY CASE r.state WHEN 'waiting' THEN 0 WHEN 'working' THEN 1 ELSE 2 END,
             COALESCE(s.custom_title, s.slug)
    LIMIT 1
")

if [[ -z "$tty" ]]; then
    exit 0
fi

osascript -e "
tell application \"iTerm\"
    activate
    repeat with w in windows
        repeat with t in tabs of w
            repeat with s in sessions of t
                if tty of s is \"/dev/$tty\" then
                    select w
                    tell t to select
                    return
                end if
            end repeat
        end repeat
    end repeat
end tell
"
