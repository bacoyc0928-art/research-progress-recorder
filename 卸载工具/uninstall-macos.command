#!/bin/bash
# Local candidate: matches the published app's bundle and data paths.
set -euo pipefail
APP_USER="$HOME/Applications/ResearchProgressRecorder.app"
APP_SYSTEM="/Applications/ResearchProgressRecorder.app"
DATA="$HOME/Library/Application Support/ResearchProgressRecorder"
printf 'This moves ResearchProgressRecorder.app to Trash. Research data is preserved.\nType UNINSTALL to continue: '
read -r answer
if [[ "$answer" != UNINSTALL ]]; then echo 'Cancelled.'; exit 0; fi
trash="$HOME/.Trash/ResearchProgressRecorder-uninstall-$(date +%Y%m%d-%H%M%S)-$$"
mkdir -p "$trash"
index=0
for app in "$APP_USER" "$APP_SYSTEM"; do
  if [[ -d "$app" ]]; then
    if [[ -L "$app" ]]; then echo 'Refusing a linked application path.'; exit 1; fi
    bundle=$(/usr/libexec/PlistBuddy -c 'Print CFBundleIdentifier' "$app/Contents/Info.plist")
    if [[ "$bundle" != org.syncresearch.recorder ]]; then echo 'Unexpected app identifier; stopped.'; exit 1; fi
    index=$((index + 1))
    destination="$trash/ResearchProgressRecorder-$index.app"
    mv "$app" "$destination"
  fi
done
if [[ -d "$DATA" ]]; then
  printf 'Research data remains in %s\nType DELETE to also move this data to Trash, or press Enter to keep it: ' "$DATA"
  read -r answer
  if [[ "$answer" == DELETE ]]; then
    if [[ -L "$DATA" ]]; then echo 'Refusing a linked data directory.'; exit 1; fi
    mv "$DATA" "$trash/ResearchProgressRecorder-data"
  fi
fi
echo 'App removal finished. External workspaces and the Keychain token were preserved.'
echo 'To remove the token, use Keychain Access: service SRLogbook.github_token, account ResearchProgressRecorder.'
