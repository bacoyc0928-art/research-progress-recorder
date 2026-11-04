# SRPRecorder2027 0.2.14 · Complete User Guide (English)

> Maintained together with the in-app Help page and `help/help.md` in the private repository; the data-storage section is authoritative.
> 中文说明: [使用说明.md](使用说明.md)

---

## 0. Version and downloads

| Platform | File | Requirement |
|---|---|---|
| Windows 64-bit | [research-progress-recorder-0.2.14-Windows.zip](https://github.com/bacoyc0928-art/research-progress-recorder/releases/download/v0.2.14/research-progress-recorder-0.2.14-Windows.zip) | Windows 10/11 + .NET Framework 4.8 |
| Apple Silicon | [research-progress-recorder-0.2.14-macOS-arm64.dmg](https://github.com/bacoyc0928-art/research-progress-recorder/releases/download/v0.2.14/research-progress-recorder-0.2.14-macOS-arm64.dmg) | macOS 14 or later |
| Intel Mac | [research-progress-recorder-0.2.14-macOS-x64.dmg](https://github.com/bacoyc0928-art/research-progress-recorder/releases/download/v0.2.14/research-progress-recorder-0.2.14-macOS-x64.dmg) | macOS 15 or later |
| Checksums | [SHA256SUMS.txt](https://github.com/bacoyc0928-art/research-progress-recorder/releases/download/v0.2.14/SHA256SUMS.txt) | SHA-256 of the three packages |

Releases: https://github.com/bacoyc0928-art/research-progress-recorder/releases/latest

No GitHub sign-in is needed to download. The Windows build reports `0.2.14.0`; macOS reports `0.2.14`. Both DMGs are functionally identical and differ only in the bundled runtime architecture — arm64 for Apple Silicon, x64 for Intel.

```powershell
Get-FileHash .\research-progress-recorder-0.2.14-Windows.zip -Algorithm SHA256
```

```bash
shasum -a 256 research-progress-recorder-0.2.14-macOS-arm64.dmg
```

---

## 1. Installation and first launch

### Windows

1. Extract the ZIP completely into a writable folder (e.g. `D:\Research\SRPRecorder2027\`). The app is portable: no installer, no admin rights.
2. Run `SRCRec\SRPRecorder2027.exe`. Do not move the exe alone — the adjacent `SRPRecorder2027.exe.config` controls high-DPI and long-path behaviour.
3. First launch creates `PersonalWorkspaceSRRec\` (with `Mat_Local`, `Reference`, `Public_Temp`) and `References\Default\`.
4. If SmartScreen appears, choose “More info → Run anyway”. The app has no commercial code-signing certificate.
5. Only one instance runs at a time; launching again asks you to switch to the existing window.

### macOS

1. Open the DMG and drag `ResearchProgressRecorder.app` into `Applications` (quit the old version first).
2. For an unidentified-developer warning, confirm the official download source, try opening once, then personally use System Settings → Privacy & Security → Open Anyway. Stop and report damaged-app or malware warnings instead of treating all alerts as signing warnings.
3. User data lives in `~/Library/Application Support/ResearchProgressRecorder/`, outside the app bundle; replacing the app keeps it.
4. `uninstall-macos.command` in the DMG moves the app to the Trash and keeps research data by default.

---

## 2. Signing in and tokens

- Prerequisite: your GitHub account is invited to the private repository `bacoyc0928-art/research-data` and has accepted.
- Where: the bottom-left panel on Windows, the bottom bar on macOS. Paste **your own** token and press “Sign In”.
- Token type: the repository owner may use a fine-grained PAT limited to `research-data` (Contents read/write). **External collaborators must use a classic PAT with `repo`** — fine-grained tokens always return 404 on another account’s private repository.
- Sign-in is two-stage: identity first, repository access second, each with its own message.
- The token **stays in memory by default**; every launch needs a fresh sign-in. With “Remember” ticked it is stored in the system credential store (Windows Credential Manager / macOS Keychain) and unlocked with a 6-digit PIN; five wrong PINs clear it. The PIN is a local convenience lock — the real protection is your system account.
- “Show” reveals the token temporarily; “Sign Out” asks for confirmation and then closes the program.
- While signed out you cannot open References, upload, download, post, edit Help, create documents or edit categories; no JSON is written.

| Message | Meaning | Fix |
|---|---|---|
| `401` | Token invalid, expired or revoked | Create a new token |
| `403` | No write access | Classic PAT with `repo`; fine-grained with Contents read/write |
| `404` | Invitation missing, wrong token type or repository name | Open the repository in a browser first; external collaborators need a classic PAT |

---

## 3. The six pages

The interface can be switched between Chinese and English (next to `SR` on Windows, at the right of the header on macOS). The choice is remembered; version 0.2.14 switches in place on both platforms, preserving sign-in and unfinished edits.

The sections are **Members, Quick Post, References & Materials, Local Records, Progress Sync and Help**. Each has its own unread dot, cleared when you enter it; dots are computed only while signed in.

### 3.1 Members

- After signing in, press “Refresh” to load project members: everyone who has uploaded, repository collaborators and pending invitations are all listed.
- Each member shows a connection state:
  - **Connected · last active <time>** (green): the account has at least one “login” entry in the public activity log;
  - **Uploaded · no login record**: a `team/` folder exists but no login entry;
  - **Joined · not connected**: accepted the invitation but has not signed in or uploaded;
  - **Invitation pending**: invited but not accepted (visible to the owner only).
- Your own card shows “Connected to GitHub” and offers “Open Reference” and “Remove my references”.
- Other members: **Open Folder** (select the member on macOS) downloads their `references/` and `progress/` into `Public_Temp\<member>\`.
- **Download Team References** pulls every member’s `references/` into `Public_Temp\<member>\references\` and adds them to the references list; it does not copy them into your own `Reference`.

### 3.2 Quick Post

- Write in the multi-line box and press “Publish”; posts are stored as `posts/<UTC time>-<login>-<4 random hex>.txt` and are visible to everyone.
- Sticky-note cards show author · time plus a two-line preview; click to read the full text.
- “Mark Read” removes a post from the unread list and files it under History (double-click to reopen); you can delete only your own posts.
- New posts light the section’s red dot; the page refreshes about every 90 seconds.

### 3.3 References & Materials

- Three blocks: **Default Library** (`References\Default\`, read-only mirror), **My References** (`PersonalWorkspaceSRRec\Reference\`), and **Others (temp)** (`Public_Temp\<member>\`, cleared on exit).
- Double-click a card/file to open it; the page is read-only — add, upload and remove happen on Progress Sync.
- Signed-out users cannot open it.

### 3.4 Local Records

- The top shows the current progress folder (default `PersonalWorkspaceSRRec`); “Choose Folder” switches to a dedicated research folder (the program folder or its parents are rejected).
- The three fixed folders appear as **feature cards**:
  - **Working files** (`Mat_Local`): non-reference material, the default source for progress uploads;
  - **My references** (`Reference`): the default source for reference uploads;
  - **Others (temp)** (`Public_Temp`): team downloads, cleared on exit.
- **New Document**: enter a project name → creates `Document_<date> <project>.docx` in `Mat_Local` and opens it. The template is title → project → date → purpose → blank page; Chinese text in KaiTi, Latin text in Times New Roman.
- The list below shows only files in `Mat_Local`; double-click to open, “Refresh” to re-read. Local saves do not upload.

### 3.5 Progress Sync

- Only shows “Bound repository: bacoyc0928-art/research-data · main”; nothing to configure.
- Every action first opens a checklist and processes only the checked items.

### 3.6 Help

- The body is editable and shared by everyone as `help/help.md`.
- “Upload to GitHub” writes `help/help.md`, appends `help/changelog.json` and the activity log; “Load From GitHub” re-fetches; “Restore Default” returns to the built-in text; “Change Log” lists past edits.
- Windows keeps a draft in `help_local.md`; on macOS the draft lives only in the current window.
- While signed out the body is read-only.

---

## 4. Sync actions and destinations

| Action | What it does | Destination |
|---|---|---|
| Download Team Files | Lists all members (those without uploads are marked), downloads the checked ones | `PersonalWorkspaceSRRec\Public_Temp\<member>\` |
| Download Default Library | Lists `default/` files marked new/existing, downloads the checked ones | `References\Default\`, keeping `.bak-<timestamp>` before overwrite |
| Upload References | Lists files in `Reference`; “Add files” for others; duplicate names rejected | `team\<login>\references\<file>` plus a merged `references.json` |
| Upload Local Progress | Lists `Mat_Local` (falls back to the progress folder), uploads the checked files | `team\<login>\progress\<relative path>` |
| Remove My Uploaded References | Lists only **your own** uploads, asks for confirmation | Deletes remote files and manifest entries |
| Activity Log | Reads the public log plus your own detail | Read-only |

- Uploads are per file and may partially succeed; retrying overwrites same-named files and never deletes remote older files.
- Removal only affects the team area and the manifest; same-named copies in your local `Reference` must be cleaned up manually.
- For Upload Local Progress, if you use “Add files” outside the progress folder, copy those files into `Mat_Local` first.
- Logged actions: sign-in, uploads, downloads, removals, posts, help updates. The public log holds up to 2,000 entries and your private detail up to 500; all times are UTC.

---

## 5. Data storage

There is no self-hosted server: all team data lives in the private repository `bacoyc0928-art/research-data`; each machine keeps only working copies, caches and settings. Source code, installers and docs are in the public repository `research-progress-recorder`.

### 5.1 Windows local

```
SRCRec\
├─ SRPRecorder2027.exe             app 0.2.14
├─ SRPRecorder2027.exe.config      high-DPI / long-path config, keep next to the exe
├─ research_log.json               legacy research entries
├─ workspace.json                  workspace path
├─ progress.json                   progress folder path (created after changing it)
├─ sync_config.json                bound repository (owner/repo/branch, no token)
├─ news.json                       unread-dot signatures
├─ data_version.txt                one-time migration marker, fixed at 0.2.0
├─ upgrade_cleanup.log             migration removal log
├─ help_local.md                   local help draft
├─ References\
│  └─ Default\                    read-only team library mirror
└─ PersonalWorkspaceSRRec\        default local progress folder
   ├─ Mat_Local\                  personal non-reference files (default progress-upload source)
   ├─ Reference\                  personal references (default reference-upload source)
   ├─ Public_Temp\                others' files, cleared on start and exit
   └─ references.json             cached manifest after uploads
```

| Path | Contents | Rule |
|---|---|---|
| `SRCRec\*.json` | settings and state | app-written, do not edit |
| `References\Default\` | team default library | updated only by “Download Default Library” |
| `PersonalWorkspaceSRRec\Mat_Local\` | personal non-reference files | default progress-upload source |
| `PersonalWorkspaceSRRec\Reference\` | personal references | shown on References; default reference-upload source |
| `PersonalWorkspaceSRRec\Public_Temp\` | others' files | cleared on start and exit |
| Windows Credential Manager `SRLogbook/remembered_login` | saved token and PIN verifier | written only after “Remember” |

### 5.2 macOS local

```
/Applications/ResearchProgressRecorder.app        program only
~/Library/Application Support/ResearchProgressRecorder/
├─ config.json                     progress folder, login, unread state, language
├─ research_log.json               legacy research entries
├─ data_version.txt                migration marker 0.2.0
├─ upgrade_cleanup.log
└─ SRCRec\
   ├─ References\Default\          team default library
   └─ PersonalWorkspaceSRRec\
      ├─ Mat_Local\                personal non-reference files
      ├─ Reference\                personal references
      └─ Public_Temp\              others' files (cleared on start/exit)
```

The app bundle holds only the program; replacing it does not affect Application Support. A remembered sign-in is stored in the Keychain under `SRLogbook.remembered_login`.

### 5.3 Private repository research-data

```
research-data/
├─ team\<login>\
│  ├─ references\<file>            uploaded reference files
│  ├─ references.json              that account's manifest
│  ├─ progress\<relative path>     mirror of the local progress folder
│  └─ profile.json                 created on first upload
├─ default\<file>                  team default library (members read-only)
├─ logs\public\activity.json       public activity log (max 2,000)
├─ logs\private\<login>\activity.json  private detail (max 500)
├─ help\help.md                    shared help page
├─ help\changelog.json             help change log (max 200)
├─ posts\<UTC time>-<login>-<4 hex>.txt  Quick Post posts
├─ posts\read\<login>.json         read post ids
└─ sync\                           0.0.2 legacy, read-only
```

`team/<login>` is an application convention, not a GitHub permission boundary; anyone who can write the repository can technically edit another member’s folder, so invite only trusted collaborators.

### 5.4 What is and is not uploaded

- **Uploaded**: the checked files, the references manifest, `profile.json` on first upload, activity logs, posts and the help page.
- **Not uploaded**: the token, local config JSON (`workspace.json`, `sync_config.json`, `news.json`, `progress.json`), unchecked files, other members’ files in `Public_Temp`, unselected files in `Mat_Local`.

---

## 6. Sync mechanics and limits

- **Fully manual**: no watcher or real-time sync; you upload after editing, others download to receive.
- **No merging**: if two people change the same path, the later upload wins; there is no diff or conflict resolution — divide work by member folder and keep backups.
- **No remote deletion on upload**: uploads only add/overwrite; only “Remove My Uploaded References” deletes remote files.
- **Atomic writes**: local writes go to a temp file first; manifest/log writes retry three times on 409/422.
- **Network**: relies on `api.github.com`; large files, slow links or proxies may time out; failures never destroy old data.
- **Limits**: 50 MiB per file at the app layer; GitHub caps a file at 100 MB and recommends repositories under 1 GB.

---

## 7. Security and privacy

- The token stays in memory by default and is sent as `Authorization: Bearer` to `api.github.com`; with Remember it is stored in the system credential store, never printed, logged or committed.
- Team data lives in a private repository; downloaded names/paths are validated, rejecting escapes, invalid names and symlinks.
- Uploads are capped at 50 MiB per file; junctions and Windows-invalid names are refused.
- CI build jobs are `contents: read`; the publish job uses a short-lived token valid only for the public program repository and never touches the data repository.
- macOS builds currently carry only ad-hoc integrity signatures, without Developer ID distribution signing or Apple notarization. Gatekeeper can block an updated app, not just its first install. SHA-256 integrity, code-signature integrity and permission to launch are separate checks. Replacing the app should preserve Application Support data.

---

## 8. Upgrade, backup and uninstall

- **Upgrade**: quit the app → on Windows replace the whole `SRCRec\` folder (keep `.exe.config`); on macOS overwrite the old app in `Applications`. Config, logs, download cache and personal files are preserved.
- **In-app update**: Windows continues to download, verify and replace program files after exit. Mac 0.2.14 now uses the same verified system-CA context for updater and sync requests. Current ad-hoc builds open the verified DMG for manual replacement rather than promising unattended installation/relaunch. Developer ID builds can use the automatic helper, which checks the new app’s developer identity and Gatekeeper assessment. Read-only, disk-image and App Translocation locations fall back to manual installation. Users whose older update check fails can download a DMG in their browser, quit the app and replace the app in Applications.
- **Migration marker**: `data_version.txt` stays `0.2.0`; cleanup runs only when upgrading from an older release and never repeats on 0.2.14.
- **Backup**: copy the whole `SRCRec\` folder (Windows) or `~/Library/Application Support/ResearchProgressRecorder/` (macOS).
- **Uninstall**: run `卸载SRPRecorder2027.exe` on Windows (research files kept by default); run `uninstall-macos.command` from the DMG on macOS (app to Trash, data kept). Uninstalling does not revoke the token.

---

## 9. FAQ

1. **Signed in but no members or posts?** Accept the private-repo invitation, then press Refresh. A fine-grained token returns 404 on another account’s private repository — use a classic PAT.
2. **404 or a broken download?** Download from the Releases page, not legacy files in the repo root; check the full URL and `SHA256SUMS.txt`.
3. **A member shows “not connected”?** That account has no “login” entry in the public activity log yet; after they sign in once, refresh and it becomes “Connected”.
4. **Dot missing or stuck?** Dots are computed only when signed in, clear when you enter the section, and are baselined on first launch.
5. **Upload says “partly succeeded”?** Multi-file uploads are not transactional; retrying overwrites same-named files and never deletes older remote files.
6. **“File exceeds 50 MiB”?** Split or compress it; GitHub also enforces a hard 100 MB per-file limit.
7. **Word file will not open or creation fails?** Close the Word instance holding the file; make sure `Mat_Local` is writable.
8. **Mac update fails?** An immediate CERTIFICATE_VERIFY_FAILED error concerns HTTPS certificates, not app signing. Use the official release page in your browser. For an unidentified-developer warning after download, follow section 1 personally. Stop and report damaged-app or malware warnings.
9. **Two people edit the same file?** The later upload wins; divide work by member folder and back up important files.

---

## 10. Known limitations

- Manual sync only; no real-time collaboration or conflict merging.
- “Connected” is a proxy based on the latest login/activity, not live presence; the activity log keeps at most 2,000 entries, so older records are truncated.
- Badges depend on activity-log wording; editing files directly on GitHub does not trigger every badge.
- Real two-account/two-device, first-install acceptance by a colleague is still pending; passing automated tests does not prove absence of defects.
- Windows is unsigned and macOS is not notarized, so first launch shows a system warning.
- Access to `api.github.com` is required; corporate proxies or firewalls may cause timeouts.

---

*Document version: 0.2.14 · 2026-11-03 · maintained with `help/help.md` and the in-app Help page.*
