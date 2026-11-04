"""SRPRecorder2027 0.2.12. Python stdlib; packaged as a macOS .app in CI."""
from __future__ import annotations
import base64
import copy
import ctypes
import datetime as dt
from decimal import Decimal
import hashlib
import hmac
import json
import os
from pathlib import Path
import platform
import queue
import re
import shutil
import ssl
import subprocess
import sys
import tempfile
import threading
import urllib.error
import urllib.parse
import urllib.request
import uuid
import zipfile
from xml.sax.saxutils import escape
from file_channels import FileChannels, no_links, child, member_folder, upload_files

VERSION = '0.2.12'
# Stable one-time data-cleanup marker introduced in 0.2.0. Do not bump this
# when publishing an ordinary app release; doing so would erase existing data.
DATA_VERSION = '0.2.0'
APP_ID = 'ResearchProgressRecorder'
DEFAULT_REPO = 'bacoyc0928-art/research-data'

HELP_TEXT = """SRPRecorder2027 · 0.2.12 · 中英双语界面 / Bilingual UI
数据架构与使用说明 / Data architecture and usage guide

【0】总览 / Overview
本程序没有自建服务器：团队数据全部存放在私有 GitHub 仓库 bacoyc0928-art/research-data，本机只保留工作副本、缓存与配置；程序源码、安装包和文档在公开仓库 research-progress-recorder，下载不需要登录。
There is no self-hosted server: all team data lives in the private GitHub repository bacoyc0928-art/research-data; each machine keeps only working copies, caches and settings. Source, installers and docs live in the public repository research-progress-recorder and need no sign-in.

  本机 Local  ⇄  api.github.com  ⇄  私有仓库 research-data  ⇄  其他成员 other members
                        └─ 公开仓库 research-progress-recorder：只放程序与说明 / program and docs only

同步是手动的：上传＝把勾选的文件复制到远端；下载＝把远端文件复制到本机（临时区或默认库）。没有后台实时同步，也没有自动冲突合并。
Sync is manual: upload copies checked files to the remote; download copies remote files to this machine (temp area or default library). There is no background sync and no automatic conflict merging.
第一次使用：左下角登录（可勾选“记住登录”，设置 6 位 PIN）→ 团队研究刷新成员 → 本地进度选择专用文件夹 → 团队同步按需上传/下载。
First run: sign in at the bottom-left (optionally tick Remember and set a 6-digit PIN) → refresh members on Team Research → pick a dedicated folder on Local Progress → upload/download from Team Sync.

【1】Windows 本机目录 / Windows local layout
SRCRec\\                            程序与数据根目录（exe 所在处，可整体搬移）
├─ SRPRecorder2027.exe             程序 0.2.12
├─ SRPRecorder2027.exe.config      高 DPI / 长路径配置，必须随 exe 保留
├─ research_log.json               旧版研究记录
├─ workspace.json                  工作区路径
├─ progress.json                   进度文件夹路径（改过文件夹才有）
├─ sync_config.json                绑定仓库信息（owner/repo/branch，无令牌）
├─ news.json                       各栏目未读红点状态
├─ data_version.txt                一次性迁移标记，固定 0.2.0
├─ upgrade_cleanup.log             迁移删除记录
├─ help_local.md                   使用说明本机草稿
├─ References\\
│  └─ Default\\                    团队默认资料库镜像（只读）
└─ PersonalWorkspaceSRRec\\        默认本地进度文件夹
   ├─ Mat_Local\\                  个人非参考资料（上传本地进度的默认来源）
   ├─ Reference\\                  个人参考资料（上传参考资料的默认来源）
   ├─ Public_Temp\\                他人内容临时区（启动清空、退出清空）
   └─ references.json             上传后缓存的资料清单
SRCRec\\ is the program and data root next to the exe; the whole folder is portable. References\\Default is the read-only team library. PersonalWorkspaceSRRec is the default progress workspace: Mat_Local for non-reference files (default progress-upload source), Reference for personal references (default reference-upload source), Public_Temp for other members' files (cleared on start and exit).

【2】macOS 本机目录 / macOS local layout
/Applications/ResearchProgressRecorder.app        只放程序 / program only
~/Library/Application Support/ResearchProgressRecorder/
├─ config.json                     进度文件夹、登录名、未读状态
├─ research_log.json               旧版研究记录
├─ data_version.txt                迁移标记 0.2.0
├─ upgrade_cleanup.log
└─ SRCRec\\
   ├─ References\\Default\\          团队默认资料库
   └─ PersonalWorkspaceSRRec\\
      ├─ Mat_Local\\                个人非参考资料
      ├─ Reference\\                个人参考资料（上传副本直接放在 Reference\\）
      └─ Public_Temp\\              他人内容（启动/退出清空）
macOS keeps user data in Application Support; the app bundle holds only the program. The SRCRec subfolder mirrors the Windows layout with the same three workspace folders (uploaded reference copies are stored directly in Reference\\ on both platforms).

【3】私有仓库 research-data / Private team repository
team\\<账号>\\
├─ references\\<文件>               上传的参考资料本体
├─ references.json                 该账号的资料清单
├─ progress\\<相对路径>             本地进度文件夹的镜像
└─ profile.json                    首次上传时自动创建
default\\<文件>                     团队默认资料库（成员只读）
logs\\public\\activity.json          公开活动日志（≤2000 条，全员可见）
logs\\private\\<账号>\\activity.json  本人明细（≤500 条）
help\\help.md                       全员共享的使用说明（本页）
help\\changelog.json                说明改动日志（≤200 条）
posts\\<UTC时间>-<账号>-<4位随机>.txt  Quick Post 帖子
posts\\read\\<账号>.json             该账号已阅的帖子 id
sync\\                              0.0.2 遗留，只读保留
team/<login>/references holds your uploaded files and references.json is their manifest; progress mirrors your progress folder; default is the team library; logs/public is shared and logs/private/<login> is yours; help and posts are shared; sync/ is 0.0.2 legacy and read-only.

【4】同步动作与数据流向 / Sync actions and data flow
上传参考资料 Upload refs：Reference\\ → 勾选 → team\\<我>\\references\\ + references.json（同时在本机 Reference 留一份副本 / also keeps a local copy）
上传本地进度 Upload progress：Mat_Local\\ → 勾选 → team\\<我>\\progress\\<相对路径>（按进度文件夹的相对路径镜像 / mirrors the relative path）
下载他人内容 Download team：team\\<成员>\\ → Public_Temp\\<成员>\\（退出即清空 / cleared on exit）
下载默认库 Download default：default\\ → References\\Default\\（覆盖前保留 .bak-时间戳 / keeps a .bak copy）
撤除参考资料 Remove refs：只删本人上传的文件与清单条目，团队区立即不可下载；本机 Reference 中的副本需自行清理 / removes your own remote files and manifest entries only; delete local copies manually
Quick Post：发布 posts\\<id>.txt；已阅 posts\\read\\<我>.json；只能删自己的帖子 / publish, mark read, delete your own posts
活动日志 Activity log：登录、上传、下载、撤除、发帖、删帖、改说明 → logs\\public + logs\\private\\<我> / sign-in, uploads, downloads, removals, posts, help edits

【5】清理、保留与升级 / Cleanup, retention and upgrade
- Public_Temp 每次启动和每次退出都会清空，只用于临时查看他人内容。
- References 下只保留 Default；References\\<账号>\\、References\\references\\、References\\references.json、personal.json 都是废弃路径，启动时自动删除。
- data_version.txt 固定 0.2.0：只有从更早版本首次升级时清理一次，0.2.12 不会重复清理。
- 升级不会删除本地进度文件夹、Reference 中的资料和下载缓存；默认库可登录后重新下载。
- 远端上传只新增或覆盖，不自动删除旧文件。
Cleanup rules: Public_Temp is cleared on every start and exit; under References only Default is kept, and the legacy paths References/<login>, References/references, References/references.json and personal.json are deleted at startup; data_version.txt stays 0.2.0 so the one-time cleanup never repeats; upgrades never delete your progress folder, Reference files or download cache, and the default library can be re-downloaded; remote uploads never delete older files.

【6】限制与安全 / Limits and safety
- 单文件上限 50 MiB；不支持符号链接/目录联接；非法文件名会被拒绝。
- 未登录不能进入“参考文献与资料”，不能上传、下载、发帖、改说明，也不会写任何 JSON。
- 令牌默认只保存在内存，每次启动重新登录；勾选“记住登录”后，令牌存入系统凭据库（Windows 凭据管理器 / macOS 钥匙串），用 6 位 PIN 解锁，连续 5 次错误会清除。PIN 只是本机便捷锁，真正的保护是系统账户。
- 同一文件被两人先后上传时，后上传覆盖先上传，请按成员目录分工并备份。
Limits: 50 MiB per file; symlinks and invalid filenames are rejected; while signed out you cannot open References, upload, download, post or edit this page, and no JSON is written. The token lives only in memory by default; with Remember ticked it is stored in the system credential store (Windows Credential Manager / macOS Keychain) and unlocked with a 6-digit PIN after five wrong attempts clear it. The PIN is a local convenience lock; the real protection is your system account. If two people upload the same path the later upload wins, so divide work by member folder and keep backups.

【7】本页 / This help page
本页可编辑：点“上传到 GitHub”写入 help\\help.md，全员登录后读到同一份，并在 help\\changelog.json 留下改动日志；“恢复默认”回到本文件，“读取云端版本”重新拉取。
Windows 左下侧栏（登录面板下方）与 macOS 窗口底部左侧的“检查更新 / Check updates”会从 GitHub 读取最新版本、按 SHA-256 校验后下载，重启时只替换程序文件；本地进度、参考资料与配置不会被删除。两处都显示当前版本号。
This page is editable: Upload to GitHub writes help\\help.md for everyone and appends help\\changelog.json; Restore Default returns to this built-in text; Load From GitHub re-fetches the cloud copy.
“Check updates” in the bottom-left of the sidebar on Windows and at the bottom-left of the window on macOS downloads the latest release from GitHub, verifies its SHA-256 and replaces only program files after restart; local progress, references and settings are not touched. Both show the current version."""


def version_key(value):
    try:
        parts = [int(part) for part in re.findall(r'\d+', value or '')]
        return tuple((parts + [0, 0, 0, 0])[:4])
    except (TypeError, ValueError):
        return (0, 0, 0, 0)

def now(): return dt.datetime.now(dt.timezone.utc).isoformat().replace('+00:00', 'Z')
def uid(): return uuid.uuid4().hex
def stamp(value):
    try:
        d = dt.datetime.fromisoformat((value or '').replace('Z', '+00:00'))
        if d.tzinfo is None: d=d.replace(tzinfo=dt.timezone.utc)
        fraction=re.search(r'\.(\d+)',value or '')
        return Decimal(int(d.replace(microsecond=0).timestamp())) + (Decimal('0.'+fraction[1]) if fraction else 0)
    except (ValueError, TypeError): return 0

def empty():
    return dict(Entries=[], Categories=[], Refs=[], Documents=[], DeletedEntries=[],
                DeletedCategories=[], CategoriesInitialized=True)

def normalize(s):
    if not isinstance(s, dict) or not any(k in s for k in ('Entries', 'Categories')):
        raise ValueError('Invalid research JSON: Entries or Categories is required.')
    for key in empty():
        if key == 'CategoriesInitialized': continue
        if not isinstance(s.get(key), list): s[key] = []
        s[key] = [x for x in s[key] if isinstance(x, dict)]
    for c in s['Categories']:
        c['Links'] = [x for x in (c.get('Links') or []) if isinstance(x, dict)]
    return s

def merge(local, remote, scope):
    out = normalize(copy.deepcopy(local)); remote = normalize(copy.deepcopy(remote))
    for key, deleted in [('Entries', 'DeletedEntries'), ('Categories', 'DeletedCategories')]:
        if key.lower() not in scope: continue
        tomb = {x.get('Id'): x for x in out[deleted] if x.get('Id')}
        for d in remote[deleted]:
            ident = d.get('Id')
            if ident and (ident not in tomb or stamp(d.get('At')) > stamp(tomb[ident].get('At'))): tomb[ident] = d
        items = {x.get('Id'): x for x in out[key] if x.get('Id')}
        for r in remote[key]:
            ident = r.get('Id'); old = items.get(ident)
            if not ident: continue
            if old is None or stamp(r.get('Updated')) > stamp(old.get('Updated')):
                if key == 'Categories' and old and old.get('Name') != r.get('Name'):
                    for e in out['Entries'] + out['Documents']:
                        if e.get('Category') == old.get('Name'): e['Category'] = r.get('Name', '')
                items[ident] = r
        if key == 'Categories':
            deleted_names = {x.get('Name') for ident, x in items.items() if ident in tomb}
            for e in out['Entries'] + out['Documents']:
                if e.get('Category') in deleted_names: e['Category'] = ''
        out[key] = [v for k, v in items.items() if k not in tomb]
        out[deleted] = list(tomb.values())
    return out

def payload(local, remote, scope):
    out = empty(); remote = normalize(copy.deepcopy(remote or empty()))
    for key, deleted in [('Entries', 'DeletedEntries'), ('Categories', 'DeletedCategories')]:
        src = local if key.lower() in scope else remote
        out[key] = copy.deepcopy(src.get(key) or []); out[deleted] = copy.deepcopy(src.get(deleted) or [])
    out['Refs'] = remote['Refs']; out['Documents'] = remote['Documents']
    return out

def atomic(path, data):
    path = Path(path); path.parent.mkdir(parents=True, exist_ok=True)
    text = json.dumps(data, ensure_ascii=False, indent=2)
    fd, name = tempfile.mkstemp(prefix=path.name, suffix='.tmp', dir=path.parent)
    try:
        with os.fdopen(fd, 'w', encoding='utf-8') as f: f.write(text); f.flush(); os.fsync(f.fileno())
        if path.exists():
            # Keep the last valid JSON file as a recovery copy.
            old = path.read_text(encoding='utf-8-sig'); json.loads(old)
            path.with_suffix(path.suffix + '.bak').write_text(old, encoding='utf-8')
        os.replace(name, path)
    finally:
        if os.path.exists(name): os.unlink(name)

class Keychain:
    """Use macOS Keychain directly; never pass a token through a shell or command line."""
    def __init__(self, service=b'SRLogbook.github_token', account=b'ResearchProgressRecorder'):
        self.service=service; self.account=account
        self.lib = ctypes.CDLL('/System/Library/Frameworks/Security.framework/Security')
        self.lib.SecKeychainFindGenericPassword.argtypes = [ctypes.c_void_p, ctypes.c_uint32, ctypes.c_char_p, ctypes.c_uint32, ctypes.c_char_p, ctypes.POINTER(ctypes.c_uint32), ctypes.POINTER(ctypes.c_void_p), ctypes.POINTER(ctypes.c_void_p)]
        self.lib.SecKeychainAddGenericPassword.argtypes = [ctypes.c_void_p, ctypes.c_uint32, ctypes.c_char_p, ctypes.c_uint32, ctypes.c_char_p, ctypes.c_uint32, ctypes.c_void_p, ctypes.c_void_p]
        self.lib.SecKeychainItemModifyAttributesAndData.argtypes = [ctypes.c_void_p, ctypes.c_void_p, ctypes.c_uint32, ctypes.c_void_p]
        self.lib.SecKeychainItemDelete.argtypes = [ctypes.c_void_p]
        self.lib.SecKeychainItemFreeContent.argtypes = [ctypes.c_void_p, ctypes.c_void_p]
        self.cf = ctypes.CDLL('/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation'); self.cf.CFRelease.argtypes = [ctypes.c_void_p]
    def find(self):
        n = ctypes.c_uint32(); data = ctypes.c_void_p(); item = ctypes.c_void_p()
        code = self.lib.SecKeychainFindGenericPassword(None, len(self.service), self.service, len(self.account), self.account, ctypes.byref(n), ctypes.byref(data), ctypes.byref(item))
        if code == -25300: return '', None
        if code != 0: raise RuntimeError(f'Keychain error {code}')
        try: return ctypes.string_at(data, n.value).decode(), item
        finally: self.lib.SecKeychainItemFreeContent(None, data)
    def load(self):
        token, item = self.find()
        if item: self.cf.CFRelease(item)
        return token
    def save(self, token):
        _, item = self.find(); data = token.encode()
        try:
            code = self.lib.SecKeychainItemModifyAttributesAndData(item, None, len(data), data) if item else self.lib.SecKeychainAddGenericPassword(None, len(self.service), self.service, len(self.account), self.account, len(data), data, None)
            if code: raise RuntimeError(f'Keychain error {code}')
        finally:
            if item: self.cf.CFRelease(item)
    def delete(self):
        _, item = self.find()
        if not item: return
        try: self.lib.SecKeychainItemDelete(item)
        finally: self.cf.CFRelease(item)

REMEMBER_SERVICE = b'SRLogbook.remembered_login'
REMEMBER_ACCOUNT = b'ResearchProgressRecorder'
PIN_ITERATIONS = 120000

def valid_pin(pin):
    return bool(re.fullmatch(r'\d{6}', pin or ''))

def pin_hash(pin, salt, iterations=PIN_ITERATIONS):
    return hashlib.pbkdf2_hmac('sha256', pin.encode('utf-8'), salt, iterations, 32)

def remembered_save(login, token, pin):
    """把令牌存进钥匙串，并用 6 位 PIN 的 PBKDF2 校验值做应用层锁。"""
    if not valid_pin(pin) or not login or not token: return False
    salt=os.urandom(16)
    blob=dict(v=1, login=login, token=token,
              salt=base64.b64encode(salt).decode(),
              hash=base64.b64encode(pin_hash(pin,salt)).decode(),
              iter=PIN_ITERATIONS)
    try:
        Keychain(REMEMBER_SERVICE, REMEMBER_ACCOUNT).save(json.dumps(blob, ensure_ascii=False))
        return True
    except Exception:
        return False

def remembered_blob():
    try: raw=Keychain(REMEMBER_SERVICE, REMEMBER_ACCOUNT).load()
    except Exception: return None
    if not raw: return None
    try: return json.loads(raw)
    except Exception: return None

def remembered_unlock(pin):
    blob=remembered_blob()
    if not blob or not valid_pin(pin): return None
    try:
        salt=base64.b64decode(blob['salt']); expected=base64.b64decode(blob['hash'])
        actual=pin_hash(pin, salt, int(blob.get('iter', PIN_ITERATIONS)))
        return blob if hmac.compare_digest(expected, actual) else None
    except Exception:
        return None

def remembered_delete():
    try: Keychain(REMEMBER_SERVICE, REMEMBER_ACCOUNT).delete()
    except Exception: pass

def version_tuple(value):
    parts=[int(x) for x in re.findall(r'\d+', value or '')]
    return tuple((parts+[0,0,0])[:3])

def network_context():
    """Use verified HTTPS for both data sync and public updater requests."""
    context=ssl.create_default_context()
    if sys.platform=='darwin' and Path('/etc/ssl/cert.pem').is_file():
        context.load_verify_locations(cafile='/etc/ssl/cert.pem')
    return context

class UpdateError(ValueError):
    pass

def update_error_message(error,en=False):
    reason=getattr(error,'reason',error)
    if isinstance(reason,ssl.SSLCertVerificationError) or 'CERTIFICATE_VERIFY_FAILED' in str(reason):
        return ('HTTPS certificate verification failed while contacting GitHub. This is a network certificate issue, not an app-signing error. Check the date/time and HTTPS proxy, or download the update from the official release page in your browser. No update was installed.\n\n'
                if en else '连接 GitHub 时 HTTPS 证书校验失败。这是网络证书问题，不是应用未签名提示。请核对系统时间和 HTTPS 代理，或用浏览器从官方发布页下载更新。尚未安装更新。\n\n')+str(error)
    if isinstance(error,urllib.error.HTTPError):
        if error.code in (403,429):
            return ('GitHub refused the update check or rate-limited this connection. Retry later or use the official release page.\n\n' if en else 'GitHub 拒绝更新请求，或此网络触发了请求次数限制。请稍后重试，或从官方发布页下载。\n\n')+str(error)
    if isinstance(error,PermissionError):
        return ('The update location is not writable. Quit the app and install the official DMG manually.\n\n' if en else '更新位置没有写入权限。请退出程序后，用官方 DMG 手动安装。\n\n')+str(error)
    return ('Update failed; no successful installation has been confirmed.\n\n' if en else '更新失败，尚未确认安装成功。\n\n')+str(error)

def release_info():
    """公开 Release，无需令牌。"""
    req=urllib.request.Request('https://api.github.com/repos/'+DEFAULT_REPO.replace('/research-data','/research-progress-recorder')+'/releases/latest',
        headers={'User-Agent':APP_ID,'Accept':'application/vnd.github+json'})
    with urllib.request.urlopen(req,timeout=30,context=network_context()) as resp: return json.loads(resp.read().decode('utf-8'))

def update_asset(rel):
    arch='arm64' if platform.machine()=='arm64' else 'x64'
    for asset in rel.get('assets') or []:
        if str(asset.get('name','')).endswith('macOS-'+arch+'.dmg'): return asset
    return None

def download_update(rel, asset):
    """下载 DMG，并用同一 Release 的 SHA256SUMS.txt 校验；返回本地路径。"""
    if not any(x.get('name')=='SHA256SUMS.txt' for x in rel.get('assets') or []):
        raise ValueError('Release has no SHA256SUMS.txt; update cancelled.')
    name=str(asset.get('name') or '')
    if not name or Path(name).name!=name or '/' in name or '\\' in name:
        raise ValueError('Invalid update asset name')
    folder=Path(tempfile.mkdtemp(prefix='SRPRecorder2027-update-'))
    target=folder/name
    with urllib.request.urlopen(urllib.request.Request(asset['browser_download_url'],headers={'User-Agent':APP_ID}),timeout=120,context=network_context()) as resp, open(target,'wb') as out:
        shutil.copyfileobj(resp,out)
    sums=None
    for extra in rel.get('assets') or []:
        if str(extra.get('name',''))=='SHA256SUMS.txt': sums=extra
    if sums:
        with urllib.request.urlopen(urllib.request.Request(sums['browser_download_url'],headers={'User-Agent':APP_ID}),timeout=60,context=network_context()) as resp:
            lines=resp.read().decode('utf-8').splitlines()
        expected=None
        for line in lines:
            parts=line.split()
            if len(parts)>=2 and parts[-1].lstrip('*')==asset['name']: expected=parts[0].lower()
        digest=hashlib.sha256(target.read_bytes()).hexdigest()
        if not expected or digest!=expected:
            shutil.rmtree(folder,ignore_errors=True)
            raise ValueError('SHA-256 mismatch; the download was discarded.')
    return str(target)

def app_bundle_path():
    exe=Path(sys.executable).resolve()
    if exe.parent.name=='MacOS' and exe.parent.parent.name=='Contents' and exe.parents[2].suffix=='.app':
        return exe.parents[2]
    return None

def application_team(app):
    """Ad-hoc integrity signatures are not Developer ID distribution identities."""
    try:
        result=subprocess.run(['/usr/bin/codesign','-dv','--verbose=4',str(app)],capture_output=True,text=True,timeout=15)
    except (OSError,subprocess.SubprocessError): return None
    details=(result.stdout or '')+'\n'+(result.stderr or '')
    team=re.search(r'^TeamIdentifier=([A-Z0-9]{10})$',details,re.MULTILINE)
    if result.returncode or 'Authority=Developer ID Application:' not in details or not team: return None
    return team[1]

def update_install_plan(app):
    if app is None: return dict(automatic=False,reason='not-installed')
    app=Path(app)
    if 'AppTranslocation' in app.parts or app.as_posix().startswith('/Volumes/'):
        return dict(automatic=False,reason='disk-image')
    if not os.access(app.parent,os.W_OK): return dict(automatic=False,reason='not-writable')
    team=application_team(app)
    if not team: return dict(automatic=False,reason='unnotarized-build')
    return dict(automatic=True,reason='developer-id',team=team)

UPDATE_SCRIPT = r'''#!/bin/sh
set -eu
PID="$1"; DMG="$2"; APP="$3"; TEAM="$4"; STATUS="$5"; LOG="$6"
exec >>"$LOG" 2>&1
PHASE=waiting; MNT=""; MOVED=0; OLD="$APP.previous.$$"; NEW="$APP.new.$$"
report() { printf '%s\n' "$1" >"$STATUS.tmp"; mv "$STATUS.tmp" "$STATUS"; }
finish() {
  CODE=$?; trap - EXIT
  if [ "$CODE" -ne 0 ]; then
    if [ "$MOVED" -eq 1 ] && [ -d "$OLD" ]; then
      if [ -d "$APP" ]; then mv "$APP" "$APP.failed.$$" || true; fi
      mv "$OLD" "$APP" || true
    fi
    report "failed:$PHASE"
    open "$DMG" || true
  fi
  if [ -n "$MNT" ]; then hdiutil detach "$MNT" >/dev/null 2>&1 || true; rmdir "$MNT" 2>/dev/null || true; fi
  exit "$CODE"
}
trap finish EXIT
report pending
while kill -0 "$PID" 2>/dev/null; do sleep 0.5; done
PHASE=mount; MNT=$(mktemp -d)
hdiutil attach -nobrowse -readonly -mountpoint "$MNT" "$DMG" >/dev/null
SRC="$MNT/ResearchProgressRecorder.app"
[ -d "$SRC" ] && [ -d "$APP" ]
PHASE=signature
codesign --verify --deep --strict "$SRC"
codesign -dv --verbose=4 "$SRC" 2>&1 | grep -Fx "TeamIdentifier=$TEAM"
PHASE=gatekeeper
spctl --assess --type execute --verbose=2 "$SRC"
PHASE=copy
ditto "$SRC" "$NEW"
codesign --verify --deep --strict "$NEW"
PHASE=replace
mv "$APP" "$OLD"; MOVED=1
mv "$NEW" "$APP"
PHASE=launch
open "$APP"
report launched
'''

def install_update(dmg,log_directory=None):
    """Current ad-hoc builds use a visible manual install, preserving the running app."""
    app=app_bundle_path(); plan=update_install_plan(app)
    if not plan['automatic']:
        subprocess.run(['/usr/bin/open',str(dmg)],check=True,timeout=15)
        return plan
    directory=Path(log_directory or tempfile.mkdtemp(prefix='SRPRecorder2027-apply-'))
    directory.mkdir(parents=True,exist_ok=True)
    script=directory/('apply-update-'+uuid.uuid4().hex+'.sh')
    script.write_text(UPDATE_SCRIPT,encoding='utf-8'); script.chmod(0o755)
    status=directory/'last-update.txt'; log=directory/('update-'+uuid.uuid4().hex+'.log')
    subprocess.Popen(['/bin/sh',str(script),str(os.getpid()),str(dmg),str(app),plan['team'],str(status),str(log)],start_new_session=True)
    return dict(plan,log=str(log),status=str(status))

class GitHub:
    def __init__(self, config, token):
        self.repo = config.get('repo', DEFAULT_REPO); self.branch = config.get('branch') or 'main'; self.token = token
        if not re.fullmatch(r'[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+', self.repo): raise ValueError('Repository must be owner/repo')
        if not token: raise ValueError('Enter your own GitHub token')
    def request(self, method, endpoint, data=None, raw=False, binary=False):
        url = 'https://api.github.com' + endpoint
        req = urllib.request.Request(url, data=None if data is None else json.dumps(data).encode(), method=method,
            headers={'Authorization': 'Bearer '+self.token, 'User-Agent': APP_ID,
                     'Accept': 'application/vnd.github.raw+json' if raw else 'application/vnd.github+json',
                     'X-GitHub-Api-Version': '2022-11-28', 'Content-Type': 'application/json'})
        context=network_context()
        with urllib.request.urlopen(req, timeout=120, context=context) as r:
            content = r.read(72_000_001)
            if len(content)>72_000_000: raise ValueError('GitHub response too large')
            if binary: return content
            text=content.decode('utf-8-sig')
            return text if raw else json.loads(text)
    def endpoint(self, path): return f'/repos/{self.repo}/contents/'+urllib.parse.quote(path, safe='/')
    def get(self, path):
        endpoint = self.endpoint(path)+'?ref='+urllib.parse.quote(self.branch, safe='')
        try: data = self.request('GET', endpoint)
        except urllib.error.HTTPError as e:
            if e.code == 404: return None, None
            raise
        if not isinstance(data, dict) or not data.get('sha'): raise ValueError('Not a readable GitHub file')
        text = self.request('GET', endpoint, raw=True) if data.get('encoding') == 'none' else base64.b64decode(data['content']).decode('utf-8-sig')
        return json.loads(text), data['sha']
    def put(self, path, data, sha):
        body = dict(message='sync: Research Progress Recorder '+VERSION, branch=self.branch,
                    content=base64.b64encode(json.dumps(data, ensure_ascii=False).encode()).decode())
        if sha: body['sha'] = sha
        self.request('PUT', self.endpoint(path), body)
    def login(self): return self.request('GET', '/user')['login']
    def members(self):
        try: return [x['name'] for x in self.request('GET', self.endpoint('team')+'?ref='+urllib.parse.quote(self.branch, safe='')) if x['type']=='dir']
        except urllib.error.HTTPError as e:
            if e.code == 404: return []
            raise
    def history(self, login, scope):
        for attempt in range(3):
            items, sha = self.get('sync/history.json'); items = items or []
            if not isinstance(items, list): raise ValueError('Invalid history JSON')
            items.append(dict(time=now(), user=login, scope=','.join(scope)))
            try: self.put('sync/history.json', items[-500:], sha); return
            except urllib.error.HTTPError as e:
                if attempt == 2 or e.code not in (409,422): raise
    def sync(self, local, scope, path, upload=True):
        for attempt in range(3):
            remote, sha = self.get(path); merged = merge(local, remote or empty(), scope)
            if not upload: return merged, 'Pulled'
            try: self.put(path, payload(merged, remote, scope), sha)
            except urllib.error.HTTPError as e:
                if attempt == 2 or e.code not in (409,422): raise
                continue
            note = 'Synced'
            try: self.history(self.login(), scope)
            except Exception as e: note += '; history update failed: '+str(e)
            return merged, note
    def personal(self, local, folder, research):
        folder = Path(folder)
        if not folder.is_dir(): raise ValueError('Choose a personal folder first')
        login = self.login(); profile, sha = self.get(f'team/{login}/profile.json')
        if profile is None:
            try: self.put(f'team/{login}/profile.json', dict(login=login, joined=now()), None)
            except urllib.error.HTTPError as e:
                if e.code not in (409,422): raise
        files = {'research.json':local['Entries'], 'documents.json':local['Documents']} if research else {'references.json':local['Refs']}
        for name, data in files.items():
            atomic(folder/name, data); _, sha = self.get(f'team/{login}/{name}'); self.put(f'team/{login}/{name}', data, sha)
        return 'Uploaded to team/'+login

def docx(folder, project, category, title, direct=False):
    folder = Path(folder) if direct else Path(folder)/'本地文档'; folder.mkdir(parents=True,exist_ok=True)
    safe = re.sub(r'[\x00-\x1f/\\:*?"<>|]', '_', title).strip(' .')[:100] or 'Untitled'
    target = folder/(safe+'.docx'); n=1
    while target.exists(): n+=1; target=folder/f'{safe} ({n}).docx'
    def para(text, bold=False):
        return '<w:p><w:r><w:rPr><w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman" w:cs="Times New Roman" w:eastAsia="楷体" w:hint="eastAsia"/>'+('<w:b/>' if bold else '')+'</w:rPr><w:t xml:space="preserve">'+escape(text)+'</w:t></w:r></w:p>'
    document = '<?xml version="1.0" encoding="UTF-8"?><w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:body>'+para(title,True)+para('项目 / Project：'+project)+para('日期 / Date：'+str(dt.date.today()))+para('')+para('此文档的目的 / Purpose',True)+para('')+''.join(para('') for _ in range(24))+'</w:body></w:document>'
    with zipfile.ZipFile(target,'x',zipfile.ZIP_DEFLATED) as z:
        z.writestr('[Content_Types].xml','<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/></Types>')
        z.writestr('_rels/.rels','<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/></Relationships>')
        z.writestr('word/document.xml',document)
    return str(target)

def within(path, root):
    try: Path(path).resolve().relative_to(Path(root).resolve()); return True
    except ValueError: return False

def repo_problem_text(error, en):
    code=getattr(error,'code',None)
    if code==401:
        return ('GitHub rejected the token (HTTP 401): it is invalid, expired or revoked.\n\nCreate a new token: Settings -> Developer settings -> Personal access tokens -> Tokens (classic), tick the repo scope, then paste it here.'
                if en else 'GitHub 拒绝了该令牌（HTTP 401）：令牌无效、已过期或被撤销。\n\n请到 GitHub -> Settings -> Developer settings -> Personal access tokens -> Tokens (classic) 重新生成，勾选 repo 权限，再粘贴到这里。')
    if code==403:
        return ('GitHub refused access (HTTP 403): the token most likely lacks write access to this repository.\n\nA classic PAT needs the repo scope; fine-grained tokens are not supported for repositories owned by another account.'
                if en else 'GitHub 拒绝访问（HTTP 403）：令牌很可能没有该仓库的写入权限。\n\nclassic PAT 请勾选 repo；他人账号下的私有仓库不支持 fine-grained 令牌。')
    if code==404:
        return ('GitHub returned 404: this token cannot see the repository.\n\n1. Is the collaborator invitation accepted? Open the repository in a browser as yourself.\n2. Fine-grained tokens cannot access a repository owned by another account - external collaborators need a classic PAT with the repo scope.\n3. Is the repository name exactly owner/repo?\n4. Is the token unexpired?'
                if en else 'GitHub 返回 404：这个令牌看不到该仓库。\n\n1. 协作者邀请是否已接受？用本人账号在浏览器能打开该仓库才算生效。\n2. fine-grained 令牌无法访问他人账号的私有仓库，外部协作者必须用 classic PAT 并勾选 repo。\n3. 仓库名是否完全等于 owner/repo？\n4. 令牌是否已过期？')
    return str(error)

def append_activity_to(api,path,login,action,detail,limit):
    for attempt in range(3):
        old,sha=api.get(path); rows=normalize_activity(old)
        rows.append(dict(time=now(),user=login,action=action,detail=detail))
        try: api.put(path,rows[-limit:],sha); return
        except urllib.error.HTTPError as error:
            if attempt==2 or error.code not in (409,422): raise

def append_activity(api, login, action, detail, to_public=True):
    targets=[(f'logs/private/{login}/activity.json',500)]
    if to_public: targets.append(('logs/public/activity.json',2000))
    for path,limit in targets: append_activity_to(api,path,login,action,detail,limit)

def normalize_activity(data):
    if not isinstance(data,list): return []
    return [{str(key).lower():value for key,value in row.items()}
            for row in data if isinstance(row,dict)]

def read_activity(api, path):
    data, sha = api.get(path)
    return normalize_activity(data)

def post_parse(name):
    if not name.endswith('.txt'): return None
    pid=name[:-4]
    if len(pid)<20 or not pid[:14].isdigit() or pid[14]!='-': return None
    rest=pid[15:]; dash=rest.rfind('-')
    if dash<=0: return None
    ts=pid[:14]
    return dict(id=pid,author=rest[:dash],time=f'{ts[:4]}-{ts[4:6]}-{ts[6:8]} {ts[8:10]}:{ts[10:12]}')

def list_posts(api):
    try: rows=api.request('GET',api.endpoint('posts')+'?ref='+urllib.parse.quote(api.branch,safe=''))
    except urllib.error.HTTPError as error:
        if error.code==404: return []
        raise
    out=[]
    for row in rows:
        if row.get('type')!='file' or not row.get('name','').endswith('.txt'): continue
        item=post_parse(row['name'])
        if not item: continue
        try: item['body']=api.request('GET',api.endpoint('posts/'+row['name'])+'?ref='+urllib.parse.quote(api.branch,safe=''),raw=True)
        except urllib.error.HTTPError: item['body']=''
        out.append(item)
    out.sort(key=lambda x:x['id'],reverse=True)
    return out

def read_ids(api,login):
    data,sha=api.get(f'posts/read/{login}.json')
    return set(data) if isinstance(data,list) else set()

def mark_read(api,login,pid):
    for attempt in range(3):
        data,sha=api.get(f'posts/read/{login}.json')
        ids=list(data) if isinstance(data,list) else []
        if pid not in ids: ids.append(pid)
        try: api.put(f'posts/read/{login}.json',ids[-2000:],sha); return
        except urllib.error.HTTPError as error:
            if attempt==2 or error.code not in (409,422): raise

def put_text(api,path,text):
    endpoint=api.endpoint(path); sha=None
    try: sha=api.request('GET',endpoint+'?ref='+urllib.parse.quote(api.branch,safe=''))['sha']
    except urllib.error.HTTPError as error:
        if error.code!=404: raise
    body={'message':'post: SRPRecorder2027 '+VERSION,'branch':api.branch,'content':base64.b64encode(text.encode()).decode()}
    if sha: body['sha']=sha
    api.request('PUT',endpoint,body)

def publish_post(api,login,body):
    pid=dt.datetime.utcnow().strftime('%Y%m%d%H%M%S')+'-'+login+'-'+uuid.uuid4().hex[:4]
    put_text(api,'posts/'+pid+'.txt',body); return pid

def delete_post(api,pid):
    endpoint=api.endpoint('posts/'+pid+'.txt')
    info=api.request('GET',endpoint+'?ref='+urllib.parse.quote(api.branch,safe=''))
    api.request('DELETE',endpoint,{'message':'post: delete','branch':api.branch,'sha':info['sha']})

def dialog_geometry(width,height,screen,parent):
    """计算弹窗尺寸和位置：screen 为 Tk 报告的主显示器 (x,y,w,h)，parent 为主窗口 (x,y,w,h)。
    Tk 在 macOS 上只知道主显示器；主窗口在副屏时改以主窗口自身为边界，避免把弹窗拉回主屏。"""
    sx,sy,sw,sh=screen; px,py,pw,ph=parent
    cx,cy=px+pw//2,py+ph//2
    if sx<=cx<sx+sw and sy<=cy<sy+sh:
        # 主显示器：为菜单栏、标题栏和 Dock 留出空间。
        bx,by,bw,bh,top_gap,bottom_gap,side_gap=sx,sy,sw,sh,28,48,32
    else:
        # 副显示器：主窗口一定完整落在某块屏幕上，以它为边界最安全。
        bx,by,bw,bh,top_gap,bottom_gap,side_gap=px,py,max(1,pw),max(1,ph),0,0,0
    width=max(1,min(width,bw-side_gap)); height=max(1,min(height,bh-top_gap-bottom_gap-24))
    left=max(bx,min(px+(pw-width)//2,bx+bw-width))
    top=max(by+top_gap,min(py+(ph-height)//2,by+bh-height-bottom_gap))
    return width,height,left,top

class Client:
    def clean_previous_data(self):
        """0.2.0 一次性迁移：清掉更早版本的软件数据（配置、日志、默认库副本、成员缓存），
        保留用户自己的本地进度文件夹。清理清单写到 upgrade_cleanup.log。"""
        marker=self.home/'data_version.txt'
        try:
            if marker.exists() and version_key(marker.read_text(encoding='utf-8-sig',errors='ignore').strip()) >= version_key(DATA_VERSION): return
        except Exception: pass
        removed=[]
        for name in ('config.json','research_log.json','progress.json','personal.json','workspace.json',
                     'sync_config.json','news.json','help_local.md','startup_error.txt'):
            p=self.home/name
            if p.exists():
                try: p.unlink(); removed.append(str(p))
                except Exception: pass
        refs=self.home/'SRCRec'/'References'
        if refs.is_dir():
            default=refs/'Default'
            if default.is_dir():
                for f in default.rglob('*'):
                    if f.is_file() and f.name.lower()!='readme.txt':
                        try: f.unlink(); removed.append(str(f))
                        except Exception: pass
            for d in refs.iterdir():
                if not d.is_dir(): continue
                team=d/'TeamConRef'
                if team.is_dir():
                    try: shutil.rmtree(team); removed.append(str(team))
                    except Exception: pass
                try:
                    for f in d.glob('references.json'):
                        f.unlink(); removed.append(str(f))
                except Exception: pass
        try: marker.write_text(DATA_VERSION,encoding='utf-8')
        except Exception: pass
        try: (self.home/'upgrade_cleanup.log').write_text('\n'.join(removed),encoding='utf-8')
        except Exception: pass
    def __init__(self, root, home=None, smoke=False):
        import tkinter as tk
        from tkinter import ttk, filedialog, messagebox, simpledialog
        self.tk,self.ttk,self.fd,self.mb,self.ask=tk,ttk,filedialog,messagebox,simpledialog
        self.root=root; self.home=Path(home or Path.home()/'Library'/'Application Support'/APP_ID)
        self.home.mkdir(parents=True,exist_ok=True)
        self.files_root=self.home/'SRCRec'; self.files_root.mkdir(exist_ok=True)
        self.default=self.files_root/'References'/'Default'; self.default.mkdir(parents=True,exist_ok=True)
        # 工作区：PersonalWorkspaceSRRec + Mat_Local / Reference / Public_Temp（与 Windows 同构）
        self.workspace=self.files_root/'PersonalWorkspaceSRRec'
        self.mat_local=self.workspace/'Mat_Local'
        self.reference_dir=self.workspace/'Reference'
        self.public_temp=self.workspace/'Public_Temp'
        for _folder in (self.workspace,self.mat_local,self.reference_dir,self.public_temp): _folder.mkdir(parents=True,exist_ok=True)
        self.clear_public_temp()
        self.migrate_legacy()
        self.configfile=self.home/'config.json'; self.datafile=self.home/'research_log.json'
        self.clean_previous_data()
        self.config=json.loads(self.configfile.read_text(encoding='utf-8-sig')) if self.configfile.exists() else {}
        # Keep legacy research JSON intact. 0.1.0 file channels never rewrite it.
        if self.datafile.exists(): normalize(json.loads(self.datafile.read_text(encoding='utf-8-sig')))
        self.en=self.config.get('language','zh')=='en'; self.busy=False; self.events=queue.Queue()
        self.smoke=smoke; self.disabled=[]; self.login=''
        self.session_token=''; self.session_login=''
        self.saved=remembered_blob(); self.use_token_mode=False
        progress=Path(self.config.get('progress') or self.files_root/'PersonalWorkspaceSRRec')
        if not progress.is_dir(): progress=self.files_root/'PersonalWorkspaceSRRec'
        progress.mkdir(parents=True,exist_ok=True); self.config['progress']=str(progress)
        self.config.setdefault('workspace',str(self.files_root)); atomic(self.configfile,self.config)
        root.title('SRPRecorder2027 · '+VERSION); root.geometry('1120x760'); root.minsize(940,640)
        self.build(); root.protocol('WM_DELETE_WINDOW',self.close); root.after(100,self.poll)
        if not smoke: root.after(300,self.show_update_result)
    def tr(self,zh,en):
        if not hasattr(self,'_translations'): self._translations={}
        self._translations[zh]=(zh,en); self._translations[en]=(zh,en)
        return en if getattr(self,'en',False) else zh
    def display_language(self,text):
        pairs=getattr(self,'_translations',{})
        pair=pairs.get(text)
        if pair: return pair[1 if self.en else 0]
        # Dynamic statuses append an account or count to a translated prefix.
        for source in sorted(pairs,key=len,reverse=True):
            if source and source[-1:] in (' ', '：', ':') and text.startswith(source):
                return pairs[source][1 if self.en else 0]+text[len(source):]
        return text
    def toggle_language(self):
        if self.busy: return
        self.en=not self.en
        self.config['language']='en' if self.en else 'zh'
        atomic(self.configfile,self.config)
        def translate(widget):
            kind=widget.winfo_class()
            if kind in ('TLabel','TButton','TCheckbutton','Label','Button'):
                widget.configure(text=self.display_language(str(widget.cget('text'))))
            elif kind=='Treeview':
                for column in widget['columns']:
                    widget.heading(column,text=self.display_language(widget.heading(column,'text')))
            for child in widget.winfo_children(): translate(child)
        translate(self.root)
        for i,(zh,en) in enumerate(self.tab_labels):
            self.tab_base[i]=self.tr(zh,en)
            old=str(self.book.tab(i,'text'))
            self.book.tab(i,text=('🔴 ' if old.startswith('🔴') else '')+self.tab_base[i])
        self.lang_btn.configure(text='中文' if self.en else 'EN')
        self.status.set(self.display_language(self.status.get()))
        self.update_login_ui()
        self.personal_label.configure(text=self.personal_display())
        for item in self.members_tree.get_children():
            values=list(self.members_tree.item(item,'values'))
            if len(values)>1:
                values[1]=self.display_language(values[1]); self.members_tree.item(item,values=values)
    def button(self,parent,zh,en,command):
        b=self.ttk.Button(parent,text=self.tr(zh,en),command=lambda:self.guarded(command))
        if hasattr(parent,'_flow_buttons'):
            parent._flow_buttons.append(b); self.root.after_idle(parent._reflow)
        else: b.pack(side='left',padx=4,pady=5)
        return b
    def flow_bar(self,parent,**pack_options):
        bar=self.ttk.Frame(parent,height=34); bar.pack(fill='x',**pack_options)
        bar._flow_buttons=[]; bar._flow_key=None
        def reflow(event=None):
            available=max(1,bar.winfo_width())
            key=(available,tuple((b.winfo_reqwidth(),b.winfo_reqheight()) for b in bar._flow_buttons))
            if key==bar._flow_key: return
            bar._flow_key=key; x=4; y=5; line=0
            for b in bar._flow_buttons:
                width=min(b.winfo_reqwidth(),max(1,available-8)); height=b.winfo_reqheight()
                if x>4 and x+width+4>available: x=4; y+=line+10; line=0
                b.place(x=x,y=y,width=width,height=height)
                x+=width+8; line=max(line,height)
            bar.configure(height=y+line+5)
        bar._reflow=reflow; bar.bind('<Configure>',reflow)
        return bar
    def bottom_bar(self,win,flow=False,**pad):
        """弹窗按钮栏：放在底部并排在打包顺序最前，弹窗缩小时先压缩内容而不是按钮。"""
        bar=self.flow_bar(win) if flow else self.ttk.Frame(win)
        first=[w for w in win.pack_slaves() if w is not bar]
        bar.pack_configure(side='bottom',fill='x',**pad,**({'before':first[0]} if first else {}))
        return bar
    def wrapped_label(self,parent,text,pack_options=None,**options):
        label=self.ttk.Label(parent,text=text,wraplength=100,**options)
        label.pack(fill='x',**(pack_options or {}))
        label.bind('<Configure>',lambda e:label.configure(wraplength=max(100,e.width-4)))
        return label
    def guarded(self,fn):
        if self.busy: return
        try: fn()
        except Exception as error: self.mb.showerror(self.tr('操作失败','Operation failed'),str(error),parent=self.root)
    def close(self):
        if self.busy: self.mb.showinfo('SRPRecorder2027',self.tr('请等待当前传输完成。','Wait for the current transfer to finish.')); return
        if getattr(self,'help_dirty',False):
            self.save_help_draft()
        self.clear_public_temp()
        self.root.destroy()
    def poll(self):
        try:
            done,result,error=self.events.get_nowait(); self.busy=False
            for w,state in self.disabled:
                try: w.configure(state=state)
                except Exception: pass
            if error:
                self.status.set(self.tr('更新未完成，详见错误说明。','Update incomplete; see the error details.') if isinstance(error,UpdateError) else self.tr('操作失败；上传可能部分完成，旧下载副本保留。','Failed; uploads may be partially complete. Previous downloads were preserved.'))
                self.mb.showerror('SRPRecorder2027',str(error),parent=self.root)
            else:
                try: done(result)
                except Exception as failure: self.mb.showerror('SRPRecorder2027',str(failure),parent=self.root)
        except queue.Empty: pass
        if self.root.winfo_exists(): self.root.after(100,self.poll)
    def run(self,work,done):
        if self.busy: return
        self.busy=True; self.status.set(self.tr('正在处理…','Working…')); self.disabled=[]
        def disable(w):
            for child in w.winfo_children(): disable(child)
            try:
                state=w.cget('state'); self.disabled.append((w,state)); w.configure(state='disabled')
            except Exception: pass
        disable(self.root)
        def job():
            try: self.events.put((done,work(),None))
            except Exception as error: self.events.put((done,None,error))
        threading.Thread(target=job,daemon=True).start()
    def build(self):
        ttk=self.ttk
        self.root.columnconfigure(0,weight=1); self.root.rowconfigure(1,weight=1)
        header=ttk.Frame(self.root,padding=12); header.grid(row=0,column=0,sticky='ew')
        ttk.Label(header,text='SRPRecorder2027  '+VERSION,font=('',17,'bold')).pack(side='left')
        self.lang_btn=ttk.Button(header,text=('中文' if self.en else 'EN'),command=self.toggle_language,width=6)
        self.lang_btn.pack(side='right',padx=6)
        self.button(header,'更换工作文件夹','Change Workspace',self.choose_workspace)
        self.book=ttk.Notebook(self.root); self.book.grid(row=1,column=0,sticky='nsew',padx=12,pady=6)
        self.book.bind('<<NotebookTabChanged>>', lambda e:self.on_tab_changed(e))
        self.tabs=[]; self.tab_base=[]
        self.tab_labels=[('成员资料','Members'),('Quick Post','Quick Post'),('参考资料','Refs'),('本地记录','Local Records'),('进度同步','Progress Sync'),('使用说明','Help')]
        for zh,en in self.tab_labels:
            label=('Quick Post' if zh=='Quick Post' else self.tr(zh,en))
            panel=ttk.Frame(self.book,padding=16); self.book.add(panel,text=label); self.tabs.append(panel); self.tab_base.append(label)
        self.news_seen=dict(self.config.get('news') or {})
        self.build_login_bar()
        self.status=self.tk.StringVar(value=self.tr('就绪','Ready'))
        bottom=ttk.Frame(self.root,padding=(12,4)); bottom.grid(row=3,column=0,sticky='ew')
        bottom.columnconfigure(1,weight=1)
        self.update_btn=ttk.Button(bottom,text=self.tr('检查更新','Check updates'),command=self.check_updates)
        self.update_btn.grid(row=0,column=0,pady=2,sticky='w')
        status_label=ttk.Label(bottom,textvariable=self.status,padding=6,width=1)
        status_label.grid(row=0,column=1,sticky='ew')
        status_label.bind('<Configure>',lambda e:status_label.configure(wraplength=max(100,e.width-12)))
        self.version_label=ttk.Label(bottom,text='v'+VERSION,foreground='#8a93a3')
        self.version_label.grid(row=0,column=2,padx=6,sticky='e')
        self.build_team(); self.build_posts(); self.build_references(); self.build_progress(); self.build_sync(); self.build_help()
        self.update_login_ui()
    def build_login_bar(self):
        bar=self.ttk.Frame(self.root,padding=(12,6)); bar.grid(row=2,column=0,sticky='ew')
        self.avatar=self.tk.Canvas(bar,width=40,height=40,highlightthickness=0)
        try: self.avatar.configure(bg=self.root.cget('bg'))
        except Exception: pass
        self.avatar.pack(side='left')
        info=self.ttk.Frame(bar); info.pack(side='left',padx=10,anchor='w')
        self.login_name=self.ttk.Label(info,text='',font=('',11,'bold')); self.login_name.pack(anchor='w')
        self.login_hint=self.ttk.Label(info,text='',foreground='#5e697a'); self.login_hint.pack(anchor='w')
        self.token_frame=self.ttk.Frame(bar)
        self.login_token=self.tk.StringVar()
        self.login_entry=self.ttk.Entry(self.token_frame,textvariable=self.login_token,show='●',width=16)
        self.login_entry.pack(side='left',padx=6,fill='x',expand=True)
        self.login_show=self.tk.BooleanVar(value=False)
        self.login_show_btn=self.ttk.Checkbutton(self.token_frame,text=self.tr('显示','Show'),variable=self.login_show,command=self.toggle_login_show)
        self.login_show_btn.pack(side='left',padx=3)
        self.remember_var=self.tk.BooleanVar(value=False)
        self.remember_btn=self.ttk.Checkbutton(self.token_frame,text=self.tr('记住登录','Remember'),variable=self.remember_var)
        self.remember_btn.pack(side='left',padx=3)
        self.login_btn=self.ttk.Button(self.token_frame,text=self.tr('登录','Sign In'),command=self.sign_in)
        self.login_btn.pack(side='left',padx=4)
        self.pin_frame=self.ttk.Frame(bar)
        self.login_pin=self.tk.StringVar()
        self.pin_entry=self.ttk.Entry(self.pin_frame,textvariable=self.login_pin,show='●',width=8,validate='key',validatecommand=(self.root.register(self.validate_pin),'%P'))
        self.pin_entry.pack(side='left',padx=6)
        self.pin_entry.bind('<Return>',lambda e:self.unlock_saved())
        self.unlock_btn=self.ttk.Button(self.pin_frame,text=self.tr('解锁','Unlock'),command=self.unlock_saved)
        self.unlock_btn.pack(side='left',padx=3)
        self.use_token_btn=self.ttk.Button(self.pin_frame,text=self.tr('改用令牌','Use token'),command=self.switch_to_token)
        self.use_token_btn.pack(side='left',padx=3)
        self.forget_btn=self.ttk.Button(self.pin_frame,text=self.tr('忘记','Forget'),command=self.forget_saved)
        self.forget_btn.pack(side='left',padx=3)
        self.logout_btn=self.ttk.Button(bar,text=self.tr('退出','Sign Out'),command=self.sign_out)
    def validate_pin(self,value):
        return value=='' or (value.isdigit() and len(value)<=6)
    def saved_login(self):
        return (self.saved or {}).get('login')
    def draw_avatar(self):
        if not hasattr(self,'avatar'): return
        c=self.avatar; c.delete('all')
        who=self.session_login or self.saved_login()
        initial=(who[:1].upper() if who else '?')
        color='#2d6cdf' if who else '#404e68'
        c.create_oval(2,2,38,38,fill=color,outline='')
        c.create_text(20,20,text=initial,fill='white',font=('',13,'bold'))
    def update_login_ui(self):
        if not hasattr(self,'login_name'): return
        signed=bool(self.session_login)
        saved=(self.saved is not None) and not getattr(self,'use_token_mode',False) and not signed
        who=self.session_login if signed else (self.saved_login() if saved else None)
        self.login_name.configure(text=who if who else self.tr('未登录','Not signed in'))
        self.login_hint.configure(text=self.tr('已登录 GitHub','Signed in with GitHub') if signed else (self.tr('6 位 PIN 解锁','Unlock with PIN') if saved else self.tr('输入密钥后登录','Enter token, then Sign In')))
        if signed:
            self.token_frame.pack_forget(); self.pin_frame.pack_forget(); self.logout_btn.pack(side='left',padx=4)
        elif saved:
            self.token_frame.pack_forget(); self.logout_btn.pack_forget(); self.pin_frame.pack(side='left',padx=4)
            self.pin_entry.focus_set()
        else:
            self.pin_frame.pack_forget(); self.logout_btn.pack_forget(); self.token_frame.pack(side='left',padx=0,fill='x',expand=True)
        if hasattr(self,'help_text'): self.help_text.configure(state='normal' if signed else 'disabled')
        for action in getattr(self,'sync_actions',[])+getattr(self,'login_actions',[]): action.configure(state='normal' if signed else 'disabled')
        if hasattr(self,'post_text'): self.post_text.configure(state='normal' if signed else 'disabled')
        self.draw_avatar()
        self.update_github_state()
    def update_github_state(self):
        if not hasattr(self,'github_state'): return
        signed=bool(self.session_login)
        self.github_state.configure(text=(self.tr('已连接 GitHub：','Connected to GitHub: ')+self.session_login) if signed else self.tr('未连接 GitHub','Not connected'))
        self.github_state.configure(foreground='#1a8a56' if signed else '#5e697a')
    def toggle_login_show(self):
        self.login_entry.configure(show='' if self.login_show.get() else '●')
    def ask_pin(self,confirm=True):
        pin=self.ask.askstring(self.tr('设置 6 位 PIN','Set a 6-digit PIN'),self.tr('下次启动用这个 PIN 解锁；忘记只能重新输入令牌。','You will unlock with this PIN next time; if you forget it, sign in with your token again.'),show='●',parent=self.root)
        if pin is None: return None
        pin=pin.strip()
        if not valid_pin(pin):
            self.mb.showinfo('SRPRecorder2027',self.tr('请输入 6 位数字。','Enter exactly 6 digits.'),parent=self.root); return None
        if confirm:
            again=self.ask.askstring(self.tr('确认 PIN','Confirm PIN'),self.tr('再次输入 6 位 PIN','Enter the 6 digits again'),show='●',parent=self.root)
            if again is None: return None
            if again.strip()!=pin:
                self.mb.showinfo('SRPRecorder2027',self.tr('两次输入不一致。','The two PINs do not match.'),parent=self.root); return None
        return pin
    def sign_in(self):
        token=self.login_token.get().strip()
        if not token:
            self.mb.showinfo('SRPRecorder2027',self.tr('请粘贴 GitHub 访问密钥。','Paste your GitHub token first.'),parent=self.root); return
        def work():
            try:
                api=GitHub({'repo':DEFAULT_REPO,'branch':'main'},token)
                login=api.login(); api.login_cached=login
                try: append_activity(api,login,'登录 / Sign in','身份验证成功 / verified')
                except Exception: pass
                return login,None
            except Exception as error:
                return None,str(error)
        def done(result):
            login,error=result
            if error:
                self.mb.showerror('SRPRecorder2027',self.tr('登录失败：','Sign-in failed: ')+error,parent=self.root)
                return
            self.session_login=login; self.session_token=token; self.login=login
            self.login_token.set(''); self.use_token_mode=False
            if self.remember_var.get():
                pin=self.ask_pin(True)
                if pin and remembered_save(login, token, pin):
                    self.saved=remembered_blob(); self.config['pin_fails']=0; atomic(self.configfile,self.config)
                    self.mb.showinfo('SRPRecorder2027',self.tr('已保存登录，下次用 6 位 PIN 解锁。','Saved; next time unlock with your 6-digit PIN.'),parent=self.root)
                elif pin:
                    self.mb.showwarning('SRPRecorder2027',self.tr('保存到钥匙串失败。','Could not save to the Keychain.'),parent=self.root)
                self.remember_var.set(False)
            self.update_login_ui(); self.team(); self.refresh_posts(); self.update_news(); self._news_tick()
            self.status.set(self.tr('已登录 ','Signed in as ')+login)
        self.run(work,done)
    def unlock_saved(self):
        if not self.saved or self.busy: return
        pin=self.login_pin.get().strip()
        if not valid_pin(pin):
            self.status.set(self.tr('请输入 6 位数字。','Enter 6 digits.')); return
        blob=remembered_unlock(pin)
        self.login_pin.set('')
        if not blob:
            fails=int(self.config.get('pin_fails',0))+1
            self.config['pin_fails']=fails; atomic(self.configfile,self.config)
            if fails>=5:
                remembered_delete(); self.saved=None; self.use_token_mode=False
                self.config['pin_fails']=0; atomic(self.configfile,self.config); self.update_login_ui()
                self.mb.showwarning('SRPRecorder2027',self.tr('连续 5 次 PIN 错误，已清除保存的登录。请改用令牌登录。','Too many wrong PINs; the saved sign-in was cleared. Sign in with your token.'),parent=self.root)
            else:
                self.login_hint.configure(text=self.tr('PIN 错误，还可尝试 ','Wrong PIN, tries left: ')+str(5-fails)+self.tr(' 次',''))
            return
        self.config['pin_fails']=0; atomic(self.configfile,self.config)
        self.session_login=blob.get('login',''); self.session_token=blob.get('token',''); self.login=self.session_login
        self.use_token_mode=False; self.update_login_ui()
        self.team(); self.refresh_posts(); self.update_news(); self._news_tick()
        self.status.set(self.tr('已解锁：','Unlocked: ')+self.session_login)
        def bg():
            try:
                api=GitHub({'repo':DEFAULT_REPO,'branch':'main'},self.session_token)
                append_activity(api,self.session_login,'登录 / Sign in','PIN 解锁 / unlocked with PIN')
            except Exception: pass
        threading.Thread(target=bg,daemon=True).start()
    def switch_to_token(self):
        self.use_token_mode=True; self.update_login_ui()
    def forget_saved(self):
        if not self.saved: return
        if not self.mb.askyesno('SRPRecorder2027',self.tr('忘记这台电脑上保存的登录？下次需要用令牌登录。','Forget the saved sign-in? You will need your token next time.'),parent=self.root): return
        remembered_delete(); self.saved=None; self.use_token_mode=False
        self.config['pin_fails']=0; atomic(self.configfile,self.config); self.update_login_ui()
    def sign_out(self):
        if not self.mb.askyesno('SRPRecorder2027',self.tr('退出登录将直接关闭程序，确定退出？','Signing out closes the app. Continue?'),parent=self.root): return
        self.session_login=''; self.session_token=''; self.login=''
        self.close()
    def require_login(self):
        if self.session_token: return True
        self.mb.showinfo('SRPRecorder2027',self.tr('请先在窗口底部登录 GitHub 账号。','Sign in at the bottom first.'),parent=self.root)
        return False
    def on_tab_changed(self,event=None):
        try: index=self.book.index(self.book.select())
        except Exception: return
        if index==2 and not self.session_token:
            self.status.set(self.tr('请先登录后查看参考文献与资料。','Sign in to view References.'))
            self.book.select(0); return
        self.mark_news_seen(index)
    def news_state(self):
        api=GitHub({'repo':DEFAULT_REPO,'branch':'main'}, self.session_token)
        posts=list_posts(api); read=set(read_ids(api,self.session_login))
        unread=len([p for p in posts if p.get('id') not in read])
        members=','.join(sorted(FileChannels(api).members()))
        acts=read_activity(api,'logs/public/activity.json')
        last=acts[-1].get('time') if acts else '-'
        def fp(*keys):
            rows=[a for a in acts if any(k in str(a.get('action') or '') for k in keys)]
            return '%d:%s'%(len(rows),rows[-1].get('time') if rows else '-')
        local=[]
        try:
            for folder in (self.mat_local,self.reference_dir):
                if folder.is_dir():
                    for file in upload_files(folder):
                        stat=file.stat(); local.append((str(file),stat.st_size,stat.st_mtime_ns))
        except Exception: pass
        local_signature=hashlib.sha256(repr(sorted(local)).encode('utf-8')).hexdigest()
        # 红点跟着“日志说的是哪个栏目”走，与 Windows 端规则一致
        return {0:'team:'+members,
                1:'posts:%d|%s'%(unread,fp('发布帖子','删除帖子')),
                2:'refs:'+fp('上传参考资料','撤除参考资料','下载默认资料库'),
                3:'progress:%s|%s'%(fp('上传本地进度'),local_signature),
                4:'act:%d:%s'%(len(acts),last),
                5:'help:'+fp('更新使用说明')}
    def update_news(self):
        """栏目未读红点：后台线程取状态（不禁用界面），完成后回到主线程更新标签。"""
        if not self.session_token or not getattr(self,'tab_base',None): return
        if getattr(self,'news_busy',False): return
        self.news_busy=True
        def job():
            try: sig=self.news_state()
            except Exception: sig=None
            try: self.root.after(0,lambda: self.apply_news(sig))
            except Exception: self.news_busy=False
        threading.Thread(target=job,daemon=True).start()
    def apply_news(self,sig):
        self.news_busy=False
        if not sig: return
        self.news_sig=sig
        current=self.book.index(self.book.select())
        for idx,value in sig.items():
            seen=self.news_seen.get(str(idx))
            if seen is None:
                self.news_seen[str(idx)]=value; continue
            badge=(seen!=value and idx!=current)
            try: self.book.tab(idx,text=(('🔴  ' if badge else '')+self.tab_base[idx]))
            except Exception: pass
        self.config['news']=self.news_seen; atomic(self.configfile,self.config)
    def mark_news_seen(self,idx):
        if getattr(self,'tab_base',None):
            try: self.book.tab(idx,text=self.tab_base[idx])
            except Exception: pass
        sig=getattr(self,'news_sig',None)
        value=sig.get(idx,sig.get(str(idx))) if sig else None
        if value is not None and self.news_seen.get(str(idx))!=value:
            self.news_seen[str(idx)]=value; self.config['news']=self.news_seen; atomic(self.configfile,self.config)
    def _news_tick(self):
        try: self.update_news()
        finally:
            try: self.root.after(90000,self._news_tick)
            except Exception: pass
    def log_activity(self,api,login,action,detail):
        try: append_activity(api,login,action,detail)
        except Exception as error: self.status.set(self.tr('活动日志写入失败：','Activity log failed: ')+str(error))
        try: self.update_news()
        except Exception: pass
    def view_activity(self):
        api,config=self.api()
        def work():
            login=api.login(); api.login_cached=login
            return login,read_activity(api,'logs/public/activity.json'),read_activity(api,f'logs/private/{login}/activity.json')
        def done(result):
            login,pub,mine=result
            self.remember(api,config)
            win=self.tk.Toplevel(self.root); win.title(self.tr('活动日志','Activity Log')); self.fit_dialog(win,900,520); win.transient(self.root); win.grab_set()
            self.ttk.Label(win,text=self.tr('公开日志 ','Public log ')+str(len(pub))+self.tr(' 条 · 我的日志 ',' entries · my log ')+str(len(mine))+self.tr(' 条',' entries'),padding=8).pack(anchor='w')
            tree=self.table(win,[self.tr('时间','Time'),self.tr('用户','User'),self.tr('动作','Action'),self.tr('详情','Detail')])
            rows=list(reversed(pub))+list(reversed(mine))
            for row in rows:
                tree.insert('','end',values=(row.get('time',''),row.get('user',''),row.get('action',''),row.get('detail','')))
            self.ttk.Button(self.bottom_bar(win),text=self.tr('关闭','Close'),command=win.destroy).pack(pady=8)
        self.run(work,done)
    def choose_workspace(self):
        if not self.require_login(): return
        value=self.fd.askdirectory(initialdir=self.config.get('workspace',str(self.files_root)))
        if value: no_links(value); self.config['workspace']=value; atomic(self.configfile,self.config)
    def language(self):
        # 界面固定中英并显，不再切换语言；保留方法仅为兼容旧调用。
        self.en=False
        self.status.set(self.tr('界面固定中英双语显示。','Interface is always bilingual; language switching was removed.'))
    def table(self,parent,columns):
        frame=self.ttk.Frame(parent); frame.pack(fill='both',expand=True)
        tree=self.ttk.Treeview(frame,columns=[str(i) for i in range(len(columns))],show='headings',selectmode='browse')
        for i,title in enumerate(columns): tree.heading(str(i),text=title); tree.column(str(i),width=240,stretch=True)
        scrollbar=self.ttk.Scrollbar(frame,orient='vertical',command=tree.yview); tree.configure(yscrollcommand=scrollbar.set)
        scrollbar.pack(side='right',fill='y'); tree.pack(fill='both',expand=True)
        return tree
    def api(self):
        if not self.session_token:
            raise ValueError(self.tr('请先在窗口底部登录 GitHub 账号。','Sign in at the bottom of the window first.'))
        config={'repo':DEFAULT_REPO,'branch':'main'}
        return GitHub(config,self.session_token),config
    def remember(self,api,config):
        self.login=api.login_cached if hasattr(api,'login_cached') else self.login
        if self.login: self.config['login']=self.login
        atomic(self.configfile,self.config); self.personal_label.configure(text=self.personal_display())
    def channels(self,api): return FileChannels(api)
    def finish(self,api,config,message):
        self.remember(api,config); self.refresh_references(); self.status.set(message)
    def personal_display(self): return (self.login or self.tr('未登录','Not configured'))+'  '+str(self.reference_dir)
    def personal_folder(self,login):
        # 0.2.2 规则：个人参考资料固定 PersonalWorkspaceSRRec/Reference，不再允许自选目录。
        self.reference_dir.mkdir(parents=True,exist_ok=True); return self.reference_dir
    def migrate_legacy(self):
        # 0.2.2 规则：References 下只保留 Default（团队默认库）；旧成员缓存/清单/personal 配置全部清掉。
        try:
            refs=self.files_root/'References'
            if refs.is_dir():
                for item in refs.iterdir():
                    if item.is_dir():
                        if item.name.lower()=='default': continue
                        shutil.rmtree(item,ignore_errors=True)
                    elif item.suffix.lower()=='.json':
                        try: item.unlink()
                        except Exception: pass
            if getattr(self,'config',None) is not None and self.config.pop('personal',None) is not None: atomic(self.configfile,self.config)
        except Exception: pass
    def team_destination(self,me,member): return child(self.public_temp,member)
    def clear_public_temp(self):
        # Public_Temp 只放本次临时下载的他人内容：启动与退出时都清空。
        try:
            for item in self.public_temp.iterdir():
                if item.is_dir(): shutil.rmtree(item,ignore_errors=True)
                else:
                    try: item.unlink()
                    except Exception: pass
        except Exception: pass
    def build_team(self):
        p=self.tabs[0]; bar=self.flow_bar(p)
        self.button(bar,'打开 Reference','Open Reference',self.open_personal)
        self.button(bar,'刷新成员','Refresh Members',self.team)
        self.button(bar,'打开成员文件','Open Member Files',self.member_research)
        self.button(bar,'下载并合并团队参考资料','Download Team References',self.team_references)
        self.github_state=self.wrapped_label(p,'',pack_options={'pady':(2,0)})
        self.personal_label=self.wrapped_label(p,self.personal_display(),pack_options={'pady':10})
        self.members_tree=self.table(p,[self.tr('GitHub 账号','GitHub login'),self.tr('状态','State')])
        self.members_tree.bind('<Double-1>',lambda _:self.guarded(self.member_research))
    def choose_personal(self):
        # 保留旧调用点：个人参考资料目录不可更换，直接打开 Reference。
        self.open_personal()
    def open_personal(self):
        self.reference_dir.mkdir(parents=True,exist_ok=True); self.open_path(self.reference_dir)
    def team(self):
        api,config=self.api()
        def work():
            files=self.channels(api); login=files.identity(); api.login_cached=login
            upload_names=files.members(); uploaded={m.lower() for m in upload_names}
            accepted=files.collaborators()
            invites=files.pending_invitations()
            names=sorted({m.lower():m for m in [*accepted,*invites,*upload_names,login]}.values(),key=str.lower)
            invited={m.lower() for m in invites}-{m.lower() for m in accepted}-uploaded
            last={}; logged=set()
            try:
                for row in read_activity(api,'logs/public/activity.json'):
                    user=str(row.get('user') or '').lower(); stamp=str(row.get('time') or '')
                    if not user or not stamp: continue
                    try:
                        when=dt.datetime.fromisoformat(stamp.replace('Z','+00:00'))
                        if when.tzinfo is None: when=when.replace(tzinfo=dt.timezone.utc)
                    except ValueError: continue
                    if user not in last or when>last[user]: last[user]=when
                    if '登录' in str(row.get('action') or '') or 'sign in' in str(row.get('action') or '').lower(): logged.add(user)
            except Exception: pass
            states={}
            for name in names:
                if name.lower()==login.lower():
                    states[name]=self.tr('已连接 GitHub 账号','Connected to GitHub account'); continue
                if name.lower() in invited:
                    states[name]=self.tr('待接受邀请','Invitation pending'); continue
                if name.lower() in logged:
                    when=last.get(name.lower())
                    stamp=when.astimezone().strftime('%m-%d %H:%M') if when else '-'
                    states[name]=self.tr('已连接 · 最近活动 ','Connected · last active ')+stamp
                elif name.lower() in uploaded:
                    states[name]=self.tr('已上传 · 无登录记录','Uploaded · no login record')
                else:
                    states[name]=self.tr('已加入 · 未连接','Joined · not connected')
            return names,states
        def done(result):
            names,states=result
            self.remember(api,config); self.members_tree.delete(*self.members_tree.get_children())
            for name in names: self.members_tree.insert('', 'end',values=(name,states.get(name,'')))
            self.status.set(self.tr('成员已刷新；未上传数据。','Members refreshed; no data uploaded.'))
        self.run(work,done)
    def selected_member(self):
        selection=self.members_tree.selection()
        return self.members_tree.item(selection[0],'values')[0] if selection else None
    def member_research(self):
        member=self.selected_member()
        if not member: return
        api,config=self.api()
        def work():
            files=self.channels(api); me=files.identity(); api.login_cached=me
            if member.lower()==me.lower(): return self.reference_dir
            destination=self.team_destination(me,member)
            if not files.download_member(member,destination): return None
            return destination
        def done(destination):
            self.remember(api,config)
            if destination: self.show_member_files(member,destination)
            else: self.status.set(self.tr('该成员尚未上传文件；旧副本保留。','No files uploaded by this member; old copies preserved.'))
        self.run(work,done)
    def show_member_files(self,member,folder):
        dialog=self.tk.Toplevel(self.root); dialog.title(member+' · SRPRecorder2027'); self.fit_dialog(dialog,850,520)
        tree=self.table(dialog,[self.tr('文件','File'),self.tr('目录','Folder')]); paths={}
        # upload_files 返回解析后的真实路径（macOS 上 /var → /private/var），相对路径也要基于解析后的根目录。
        root=Path(folder).resolve()
        for file in upload_files(folder):
            try: where=str(file.parent.resolve().relative_to(root))
            except ValueError: where=str(file.parent)
            ident=tree.insert('', 'end',values=(file.name,where)); paths[ident]=file
        bar=self.bottom_bar(dialog)
        def open_selected():
            selected=tree.selection()
            if selected: self.open_path(paths[selected[0]])
        self.button(bar,'打开文件','Open File',open_selected); self.button(bar,'打开文件夹','Open Folder',lambda:self.open_path(folder))
        self.button(bar,'关闭','Close',dialog.destroy); tree.bind('<Double-1>',lambda _:self.guarded(open_selected))
    def build_posts(self):
        p=self.tabs[1]
        self.post_text=self.tk.Text(p,height=4,wrap='word')
        self.post_text.pack(fill='x')
        bar=self.ttk.Frame(p); bar.pack(fill='x',pady=6)
        # 与 Windows 一致：未登录时发帖、历史、刷新都禁用，输入框只读（update_login_ui 统一切换）。
        self.login_actions=getattr(self,'login_actions',[])
        self.login_actions+=[self.button(bar,'发布','Publish',self.publish_post),
            self.button(bar,'历史记录','History',self.show_post_history),
            self.button(bar,'刷新','Refresh',self.refresh_posts)]
        self.post_status=self.ttk.Label(p,text=self.tr('登录后查看帖子。','Sign in to see posts.'))
        self.post_status.pack(anchor='w')
        wrap=self.ttk.Frame(p); wrap.pack(fill='both',expand=True)
        self.post_canvas=self.tk.Canvas(wrap,highlightthickness=0)
        sb=self.ttk.Scrollbar(wrap,orient='vertical',command=self.post_canvas.yview)
        self.post_canvas.configure(yscrollcommand=sb.set)
        sb.pack(side='right',fill='y'); self.post_canvas.pack(side='left',fill='both',expand=True)
        self.post_inner=self.ttk.Frame(self.post_canvas)
        self.post_canvas.create_window((0,0),window=self.post_inner,anchor='nw')
        self.post_inner.bind('<Configure>',lambda e:self.post_canvas.configure(scrollregion=self.post_canvas.bbox('all')))
        self._history_posts={}
    def refresh_posts(self):
        if not hasattr(self,'post_inner'): return
        for w in self.post_inner.winfo_children(): w.destroy()
        if not self.session_token:
            self.post_status.configure(text=self.tr('请先在窗口底部登录。','Sign in at the bottom first.')); return
        api,config=self.api()
        def work():
            login=api.login(); api.login_cached=login
            return login,list_posts(api),read_ids(api,login)
        def done(result):
            login,posts,read=result
            self.remember(api,config)
            for w in self.post_inner.winfo_children(): w.destroy()
            shown=0
            for post in posts:
                if post['id'] in read: continue
                self.add_post_card(post,login); shown+=1
            if shown:
                self.post_status.configure(text=self.tr('共 %d 条未读帖子' % shown,'%d unread post(s)' % shown))
            else:
                self.post_status.configure(text=self.tr('暂无未读帖子；已阅的在“历史记录”里。','No unread posts; see History for read ones.'))
            self.post_inner.update_idletasks(); self.post_canvas.configure(scrollregion=self.post_canvas.bbox('all'))
        self.run(work,done)
    def add_post_card(self,post,login):
        card=self.ttk.Frame(self.post_inner,relief='solid',borderwidth=1,padding=8)
        card.pack(fill='x',pady=4,padx=2)
        head=self.ttk.Label(card,text=post['author']+'  ·  '+post['time'],font=('',11,'bold'))
        head.pack(anchor='w')
        preview=' '.join(str(post.get('body','')).split())[:140]
        body=self.ttk.Label(card,text=preview or '(空)',wraplength=760,foreground='#5e697a')
        body.pack(anchor='w',pady=(2,6))
        bar=self.ttk.Frame(card); bar.pack(fill='x')
        self.ttk.Button(bar,text=self.tr('查看','Open'),command=lambda:self.view_post(post,login)).pack(side='left')
        self.ttk.Button(bar,text=self.tr('已阅','Mark Read'),command=lambda:self.mark_post_read(post,login)).pack(side='left',padx=4)
        if post['author'].lower()==login.lower():
            self.ttk.Button(bar,text=self.tr('删除','Delete'),command=lambda:self.delete_post(post,login)).pack(side='left',padx=4)
        for w in (card,head,body):
            w.bind('<Button-1>',lambda e,p=post:self.view_post(p,login))
    def publish_post(self):
        api,config=self.api()
        text=self.post_text.get('1.0','end-1c').strip()
        if not text:
            self.mb.showinfo('SRPRecorder2027',self.tr('请先写点内容。','Write something first.'),parent=self.root); return
        def work():
            login=api.login(); api.login_cached=login
            publish_post(api,login,text)
            try: append_activity(api,login,'发布帖子 / Publish post',str(len(text))+' 字符 / chars')
            except Exception: pass
            return True
        def done(_):
            self.post_text.delete('1.0','end')
            self.finish(api,config,self.tr('已发布。','Published.'))
            self.refresh_posts()
        self.run(work,done)
    def view_post(self,post,login):
        win=self.tk.Toplevel(self.root); win.title(post['author']+' · '+post['time'])
        self.fit_dialog(win,720,520); win.transient(self.root); win.grab_set()
        box=self.tk.Text(win,wrap='word',padx=12,pady=12); box.pack(fill='both',expand=True)
        box.insert('1.0',post.get('body','')); box.configure(state='disabled')
        bar=self.bottom_bar(win,pady=6)
        self.ttk.Button(bar,text=self.tr('已阅','Mark Read'),command=lambda:(win.destroy(),self.mark_post_read(post,login))).pack(side='right',padx=6)
        self.ttk.Button(bar,text=self.tr('关闭','Close'),command=win.destroy).pack(side='right')
    def mark_post_read(self,post,login):
        api,config=self.api()
        def work():
            mark_read(api,login,post['id']); return True
        def done(_):
            self.remember(api,config); self.refresh_posts()
        self.run(work,done)
    def delete_post(self,post,login):
        if post['author'].lower()!=login.lower():
            self.mb.showinfo('SRPRecorder2027',self.tr('只能删除自己的帖子。','You can only delete your own posts.'),parent=self.root); return
        if not self.mb.askyesno('SRPRecorder2027',self.tr('确定删除这条帖子？','Delete this post?'),parent=self.root): return
        api,config=self.api()
        def work():
            delete_post(api,post['id'])
            try: append_activity(api,login,'删除帖子 / Delete post',post['time'])
            except Exception: pass
            return True
        def done(_):
            self.remember(api,config); self.refresh_posts()
        self.run(work,done)
    def show_post_history(self):
        api,config=self.api()
        def work():
            login=api.login(); api.login_cached=login
            return login,list_posts(api),read_ids(api,login)
        def done(result):
            login,posts,read=result
            self.remember(api,config)
            seen=[post for post in posts if post['id'] in read]
            if not seen:
                self.mb.showinfo('SRPRecorder2027',self.tr('还没有已阅的帖子。','No read posts yet.'),parent=self.root); return
            win=self.tk.Toplevel(self.root); win.title(self.tr('历史记录（已阅）','History (read posts)'))
            self.fit_dialog(win,820,480); win.transient(self.root); win.grab_set()
            tree=self.table(win,[self.tr('作者','Author'),self.tr('时间','Time'),self.tr('内容','Content')])
            self._history_posts={}
            for post in seen:
                iid=tree.insert('','end',values=(post['author'],post['time'],' '.join(str(post.get('body','')).split())[:120]))
                self._history_posts[iid]=post
            tree.bind('<Double-1>',lambda e:self.open_history_item(tree,login))
            self.ttk.Button(self.bottom_bar(win),text=self.tr('关闭','Close'),command=win.destroy).pack(pady=8)
        self.run(work,done)
    def open_history_item(self,tree,login):
        selection=tree.selection()
        if not selection: return
        post=self._history_posts.get(selection[0])
        if post: self.view_post(post,login)
    def build_references(self):
        p=self.tabs[2]
        self.wrapped_label(p,self.tr('参考资料只读。默认库需登录私有团队仓库后下载；个人资料在团队同步上传。','Read-only references. Download the default library after joining the private data repository; upload personal files on Team Sync.'),pack_options={'pady':8})
        bar=self.ttk.Frame(p); bar.pack(fill='x'); self.button(bar,'刷新本地资料','Refresh Local Files',self.refresh_references)
        self.button(bar,'打开文件','Open File',self.open_reference); self.button(bar,'打开默认资料库','Open Default Library',lambda:self.open_path(self.default))
        self.ref_tree=self.table(p,[self.tr('文件','File'),self.tr('位置','Location')]); self.ref_tree.bind('<Double-1>',lambda _:self.guarded(self.open_reference))
        self.refresh_references()
    def refresh_references(self):
        if not hasattr(self,'ref_tree'): return
        self.ref_tree.delete(*self.ref_tree.get_children()); self.ref_paths={}
        references=self.files_root/'References'
        for directory,dirs,names in os.walk(references,followlinks=False):
            dirs[:]=[x for x in dirs if '.backup-' not in x and '.download-' not in x and not (Path(directory)/x).is_symlink()]
            for name in sorted(names):
                path=Path(directory)/name
                if path.is_symlink(): continue
                ident=self.ref_tree.insert('', 'end',values=(name,str(path.relative_to(references)))); self.ref_paths[ident]=path
        # 我的参考资料（工作区 Reference）+ 他人内容（Public_Temp\<成员>），与 Windows 一致
        for label,folder in (('我的参考资料 / My References',self.reference_dir),('他人内容 / Others (temp)',self.public_temp)):
            if not folder.is_dir(): continue
            for path in sorted(upload_files(folder)):
                ident=self.ref_tree.insert('', 'end',values=(path.name,label)); self.ref_paths[ident]=path
    def open_reference(self):
        selected=self.ref_tree.selection()
        if selected: self.open_path(self.ref_paths[selected[0]])
    def build_progress(self):
        p=self.tabs[3]; self.progress=self.tk.StringVar(value=self.config['progress'])
        self.ttk.Label(p,text=self.tr('本地记录文件夹','Local Records Folder'),font=('',14,'bold')).pack(anchor='w',pady=12)
        self.ttk.Entry(p,textvariable=self.progress,state='readonly').pack(fill='x')
        bar=self.flow_bar(p,pady=10)
        self.button(bar,'选择文件夹','Choose Folder',self.choose_progress); self.button(bar,'新建文档','New Document',self.new_document)
        self.button(bar,'工作资料 Mat_Local','Working files',lambda:self.open_path(str(self.mat_local)))
        self.button(bar,'个人参考 Reference','My references',lambda:self.open_path(str(self.reference_dir)))
        self.button(bar,'他人内容 Public_Temp','Others (temp)',lambda:self.open_path(str(self.public_temp)))
        self.button(bar,'打开工作资料','Open Working Files',lambda:self.open_path(str(self.mat_local)))
        self.button(bar,'刷新','Refresh',self.refresh_progress_files)
        self.progress_tree=self.table(p,[self.tr('名称','Name'),self.tr('大小','Size'),self.tr('修改','Modified')])
        self.progress_tree.column('0',width=420); self.progress_tree.column('1',width=140); self.progress_tree.column('2',width=200)
        self.progress_tree.bind('<Double-1>',lambda _:self.open_progress_item())
        self.wrapped_label(p,self.tr('新文档与列表使用 Mat_Local；所选文件夹是上传备用来源。本机保存不等于上传。','Documents and list use Mat_Local; the selected folder is an upload fallback. Local saves do not upload automatically.'),pack_options={'pady':12})
        self.refresh_progress_files()
    def refresh_progress_files(self):
        tree=getattr(self,'progress_tree',None)
        if tree is None: return
        tree.delete(*tree.get_children())
        folder=str(self.mat_local)
        if not Path(folder).is_dir(): return
        try:
            root=Path(folder)
            for d in sorted([x for x in root.iterdir() if x.is_dir()],key=lambda x:x.name.lower()):
                tree.insert('','end',values=(d.name+'\\','<folder>',dt.datetime.fromtimestamp(d.stat().st_mtime).strftime('%Y-%m-%d %H:%M')),tags=(str(d),))
            for f in sorted([x for x in root.iterdir() if x.is_file()],key=lambda x:x.name.lower()):
                tree.insert('','end',values=(f.name,str(max(1,f.stat().st_size//1024))+' KB',dt.datetime.fromtimestamp(f.stat().st_mtime).strftime('%Y-%m-%d %H:%M')),tags=(str(f),))
        except Exception as error:
            self.status.set(self.tr('读取文件夹失败：','Failed to list folder: ')+str(error))
    def open_progress_item(self):
        tree=getattr(self,'progress_tree',None)
        if tree is None: return
        picked=tree.selection()
        if not picked: return
        tags=tree.item(picked[0],'tags')
        if tags: self.open_path(tags[0])
    def choose_progress(self):
        if not self.require_login(): return
        value=self.fd.askdirectory(initialdir=self.progress.get())
        if value:
            no_links(value); self.config['progress']=value; self.progress.set(value); atomic(self.configfile,self.config)
            self.refresh_progress_files()
    def new_document(self):
        if not self.require_login(): return
        project=self.ask.askstring(self.tr('新建文档','New Document'),self.tr('项目名称','Project name'),parent=self.root)
        if project:
            title=str(dt.date.today())+' '+project
            self.mat_local.mkdir(parents=True,exist_ok=True)
            target=docx(str(self.mat_local),project,'',title,direct=True)
            self.refresh_progress_files(); self.open_path(target)
    def build_sync(self):
        p=self.tabs[4]
        self.sync_actions=[]
        def action(parent,zh,en,command):
            button=self.button(parent,zh,en,command); self.sync_actions.append(button)
        self.ttk.Label(p,text=self.tr('已绑定仓库：','Bound repository: ')+DEFAULT_REPO+' · main',font=('',11,'bold')).pack(anchor='w')
        self.ttk.Label(p,text=self.tr('登录后在下方直接操作，无需填写任何配置。','Sign in first; no configuration needed below.'),foreground='#c47a2a').pack(anchor='w',pady=(4,10))
        downloads=self.flow_bar(p,pady=(8,0))
        action(downloads,'从 GitHub 下载他人内容','Download Team Files',self.pull_others)
        action(downloads,'从 GitHub 下载默认资料库','Download Default Library',self.pull_default)
        uploads=self.flow_bar(p)
        action(uploads,'上传参考资料','Upload References',self.upload_references); action(uploads,'上传本地进度','Upload Local Progress',self.upload_progress)
        removals=self.flow_bar(p)
        action(removals,'撤除我上传的参考资料','Remove My Uploaded References',self.remove_my_references)
        action(removals,'查看活动日志','View Activity Log',self.view_activity)
        self.wrapped_label(p,self.tr('每个上传文件最大 50 MiB；不自动删除远端旧文件。下载成功后替换并保留旧副本；部分上传失败时请重试。','Maximum 50 MiB per uploaded file. Remote old files are not deleted. Downloads replace only after success and keep backups. Retry after partial upload failures.'),pack_options={'pady':16})
        self.wrapped_label(p,self.tr('个人仓库协作者使用 classic PAT 的 repo 权限。不要共享令牌。','Personal-repository collaborators use a classic PAT with repo scope. Never share tokens.'))
    def save_sync(self):
        api,config=self.api()
        def work():
            # Identity first: a valid token is kept even when the repository is not reachable,
            # so a wrong repository or a fine-grained token no longer looks like "not signed in".
            login=api.login()
            api.login_cached=login
            try:
                self.channels(api).identity()
                api.repo_problem=None
            except Exception as error:
                api.repo_problem=error
            return login
        def done(login):
            self.remember(api,config)
            problem=getattr(api,'repo_problem',None)
            if problem is None:
                self.status.set(self.tr('配置已验证并保存。','Configuration validated and saved.'))
                return
            self.status.set(self.tr('账号已登录，但访问仓库失败。','Signed in, but the repository is not reachable.'))
            self.mb.showwarning('SRPRecorder2027',self.tr('账号 ','Signed in as ')+login
                +self.tr(' 已登录，但访问仓库失败：\n\n',' but the repository is not reachable:\n\n')
                +repo_problem_text(problem,self.en),parent=self.root)
        self.run(work,done)
    def fit_dialog(self,win,width,height):
        self.root.update_idletasks()
        screen=(self.root.winfo_vrootx(),self.root.winfo_vrooty(),self.root.winfo_vrootwidth(),self.root.winfo_vrootheight())
        if screen[2]<=1 or screen[3]<=1: screen=(0,0,self.root.winfo_screenwidth(),self.root.winfo_screenheight())
        parent=(self.root.winfo_rootx(),self.root.winfo_rooty(),self.root.winfo_width(),self.root.winfo_height())
        width,height,left,top=dialog_geometry(width,height,screen,parent)
        win.geometry(f'{width}x{height}{left:+d}{top:+d}')
        win.minsize(min(360,width),min(220,height)); win.transient(self.root)
    def pick(self, title, hint, items, extra_files=False, labels=None):
        """先预览、再勾选：返回选中的条目（空列表表示取消）。"""
        result = {'picked': []}
        win = self.tk.Toplevel(self.root); win.title(title); self.fit_dialog(win,780,540)
        win.transient(self.root); win.grab_set()
        self.wrapped_label(win,hint,pack_options={'padx':12,'pady':8})
        body = self.ttk.Frame(win)
        box = self.tk.Listbox(body, selectmode='extended', height=18)
        sb = self.ttk.Scrollbar(body, orient='vertical', command=box.yview)
        box.configure(yscrollcommand=sb.set); box.pack(side='left', fill='both', expand=True); sb.pack(side='right', fill='y')
        rows = list(items)
        for row in rows: box.insert('end', (labels or {}).get(row,row))
        box.selection_set(0, 'end')
        bar = self.bottom_bar(win,flow=True,padx=12,pady=8)
        def add_files():
            for path in self.fd.askopenfilenames(parent=win):
                if path in rows: continue
                rows.append(path); box.insert('end', path); box.selection_set('end')
        def ok():
            result['picked'] = [rows[i] for i in box.curselection()]; win.destroy()
        if extra_files: self.button(bar,'添加文件','Add files',add_files)
        self.button(bar,'全选','Select all',lambda:box.selection_set(0,'end'))
        self.button(bar,'全不选','Select none',lambda:box.selection_clear(0,'end'))
        self.button(bar,'确定','OK',ok); self.button(bar,'取消','Cancel',win.destroy)
        body.pack(fill='both',expand=True,padx=12)
        self.root.wait_window(win)
        return result['picked']
    def pull_others(self):
        api,config=self.api()
        def work():
            files=self.channels(api); me=files.identity(); api.login_cached=me
            upload_names=files.members(); uploaded={m.lower() for m in upload_names}
            invites=files.pending_invitations()
            names={m.lower():m for m in [*files.collaborators(),*invites,*upload_names]}
            return me,sorted([m for m in names.values() if m.lower()!=me.lower()],key=str.lower),uploaded,{m.lower() for m in invites}
        def done(result):
            me,members,uploaded,invites=result
            if not members:
                self.status.set(self.tr('没有其他成员。','No other members.')); return
            picked=self.pick(self.tr('下载他人内容','Download team files'),
                self.tr('列出全部成员；未上传的标注“尚未上传”，只下载有内容的成员。','All members are listed; those without uploads are marked. Only members with files are downloaded.'),members,labels={m:m+(self.tr(' · 待接受邀请',' · Invitation pending') if m.lower() in invites and m.lower() not in uploaded else self.tr(' · 尚未上传',' · No upload yet') if m.lower() not in uploaded else '') for m in members})
            if not picked: return
            def work2():
                files=self.channels(api); count=0
                for member in picked:
                    if member.lower() not in uploaded: continue
                    if files.download_member(member,self.team_destination(me,member)): count+=1
                if count: self.log_activity(api,me,'下载他人内容 / Download team files',str(count)+' 位成员 / members')
                return count
            self.run(work2,lambda count:self.finish(api,config,self.tr('成员内容下载完成：','Team downloads completed: ')+str(count)))
        self.run(work,done)
    def pull_default(self):
        api,config=self.api()
        def work():
            files=self.channels(api); rows=files.listing('default',True)
            return sorted([x['name'] for x in rows if x.get('type')=='file'])
        def done(names):
            if not names:
                self.status.set(self.tr('远端默认资料库为空。','The remote default library is empty.')); return
            dest=Path(self.default)
            picked=self.pick(self.tr('下载默认资料库','Download default library'),
                self.tr('勾选要下载的文件；未勾选的不动，被覆盖的旧文件保留 .bak 备份。','Check files to download; unchecked files stay untouched, overwritten ones keep a .bak copy.'),names)
            if not picked: return
            def work2():
                files=self.channels(api); dest.mkdir(parents=True,exist_ok=True); count=0
                for name in picked:
                    data=files.get_bytes('default/'+name)
                    target=child(dest,name)
                    if target.exists(): target.replace(Path(str(target)+'.bak-'+dt.datetime.now().strftime('%Y%m%d%H%M%S')))
                    tmp=target.with_name(target.name+'.tmp-'+uuid.uuid4().hex[:8]); tmp.write_bytes(data); os.replace(tmp,target)
                    count+=1
                self.log_activity(api,self.session_login,'下载默认资料库 / Download default library',str(count)+' 个文件 / files')
                return count
            self.run(work2,lambda count:self.finish(api,config,self.tr('默认资料库已更新：','Default library updated: ')+str(count)))
        self.run(work,done)
    def team_references(self):
        api,config=self.api()
        def work():
            files=self.channels(api); me=files.identity(); api.login_cached=me; count=0
            for member in files.members():
                if files.download_member(member,self.team_destination(me,member),references_only=True): count+=1
            return count
        self.run(work,lambda count:self.finish(api,config,self.tr('团队参考资料下载完成：','Team reference downloads completed: ')+str(count)))
    def upload_progress(self):
        if not self.require_login(): return
        folder=self.progress.get()
        if not folder:
            self.status.set(self.tr('请先选择本地进度文件夹。','Choose the local progress folder first.')); return
        chosen=no_links(folder)
        protected=no_links(self.files_root)
        if chosen==protected or chosen in protected.parents:
            raise ValueError('Choose a dedicated progress folder, not the application folder or its parents')
        root=self.mat_local
        local=upload_files(root)
        if not local:
            root=Path(folder); local=upload_files(root)
        picked=self.pick(self.tr('上传本地进度','Upload progress'),
            self.tr('勾选要上传的文件；未勾选的不上传。','Check the files to upload; unchecked files are not sent.'),
            [str(p) for p in local],extra_files=True)
        if not picked: return
        api,config=self.api()
        def work():
            files=self.channels(api)
            count=files.upload_selected_progress(root,[Path(p) for p in picked])
            login=files.identity(); api.login_cached=login
            self.log_activity(api,login,'上传本地进度 / Upload progress',str(count)+' 个文件 / files')
            return count
        self.run(work,lambda count:self.finish(api,config,self.tr('本地进度已上传：','Progress files uploaded: ')+str(count)))
    def upload_references(self):
        if not self.require_login(): return
        login=self.session_login or self.login
        candidates=[]
        if self.reference_dir.is_dir(): candidates=[str(p) for p in sorted(upload_files(self.reference_dir))]
        picked=self.pick(self.tr('上传参考资料','Upload references'),
            self.tr('勾选要上传的文件，也可以用“添加文件”从别处挑选。','Check the files to upload, or add more from elsewhere.'),candidates,extra_files=True)
        if not picked: return
        api,config=self.api()
        def work():
            files=self.channels(api); me=files.identity(); api.login_cached=me
            result=files.upload_references([Path(p) for p in picked],self.personal_folder(me))
            self.log_activity(api,me,'上传参考资料 / Upload references',str(len(picked))+' 个文件 / files')
            return result
        self.run(work,lambda _:self.finish(api,config,self.tr('参考资料已上传，旧清单保留。','References uploaded; earlier manifest entries preserved.')))
    def remove_my_references(self):
        api,config=self.api()
        def work():
            files=self.channels(api); login,remote=files.remote_references(); return login,sorted(remote)
        def done(result):
            login,names=result
            if not names:
                self.status.set(self.tr('团队区里没有你上传的参考资料。','You have not uploaded any references.'))
                self.mb.showinfo('SRPRecorder2027',self.tr('团队区里没有你上传的参考资料。','You have not uploaded any references.'),parent=self.root); return
            self.choose_references_to_remove(api,config,login,names)
        self.run(work,done)
    def choose_references_to_remove(self,api,config,login,names):
        win=self.tk.Toplevel(self.root); win.title(self.tr('撤除我上传的参考资料','Remove My Uploaded References'))
        self.fit_dialog(win,560,420); win.transient(self.root); win.grab_set()
        self.ttk.Label(win,text=self.tr('选择要从团队区永久删除的参考资料（其他人将无法再下载）：','Select uploaded references to remove permanently from the team area:'),wraplength=520).pack(anchor='w',padx=12,pady=8)
        box=self.tk.Listbox(win,selectmode='extended',height=12); box.pack(fill='both',expand=True,padx=12)
        for name in names: box.insert('end',name)
        bar=self.bottom_bar(win,pady=8)
        def confirm():
            picked=[box.get(i) for i in box.curselection()]
            if not picked: return
            if not self.mb.askyesno('SRPRecorder2027',self.tr('将从团队区永久删除 %d 个参考资料。继续？' % len(picked),'Permanently remove %d uploaded reference(s)?' % len(picked)),parent=win): return
            win.destroy()
            def work():
                count=self.channels(api).remove_references(picked,self.personal_folder(login))
                self.log_activity(api,login,'撤除参考资料 / Remove references',str(count)+' 个文件 / files: '+'、'.join(picked))
                return count
            self.run(work,lambda count:self.finish(api,config,self.tr('已撤除 %d 个参考资料。' % count,'Removed %d uploaded reference(s).' % count)))
        self.ttk.Button(bar,text=self.tr('撤除所选','Remove Selected'),command=confirm).pack(side='right',padx=4)
        self.ttk.Button(bar,text=self.tr('取消','Cancel'),command=win.destroy).pack(side='right',padx=4)
    def open_path(self,path):
        path=no_links(path)
        if not path.exists(): raise FileNotFoundError(str(path))
        if self.smoke: return
        if sys.platform=='darwin': subprocess.Popen(['open',str(path)])
        elif sys.platform=='win32': os.startfile(path)
        else: subprocess.Popen(['xdg-open',str(path)])
    def build_help(self):
        panel=self.tabs[5]
        bar=self.flow_bar(panel)
        # 使用说明未登录只读：四个工具按钮都需要登录（与 Windows 一致）。
        self.login_actions=getattr(self,'login_actions',[])
        self.help_actions=[self.button(bar,'上传到 GitHub','Upload to GitHub',self.upload_help),
            self.button(bar,'读取云端版本','Load From GitHub',self.load_help),
            self.button(bar,'恢复默认','Restore Default',self.default_help),
            self.button(bar,'改动日志','Change Log',self.show_help_changelog)]
        self.login_actions+=self.help_actions
        body=self.ttk.Frame(panel); body.pack(fill='both',expand=True)
        self.help_text=self.tk.Text(body,wrap='word',padx=16,pady=16)
        scroll=self.ttk.Scrollbar(body,orient='vertical',command=self.help_text.yview)
        self.help_text.configure(yscrollcommand=scroll.set)
        scroll.pack(side='right',fill='y'); self.help_text.pack(side='left',fill='both',expand=True)
        self.help_text.bind('<<Modified>>',self.help_modified)
        text=self.help_text
        self.help_dirty=False
        zh=HELP_TEXT
        en=HELP_TEXT
        self._help_zh=zh; self._help_en=en
        draft=self.home/'help_local.md'
        content=draft.read_text(encoding='utf-8-sig') if draft.exists() else (en if self.en else zh)
        text.insert('1.0',content); text.edit_modified(False)

    def help_modified(self,event=None):
        try:
            if self.help_text.edit_modified():
                self.help_dirty=True; self.help_text.edit_modified(False)
        except Exception: pass
    def default_help_text(self):
        return self._help_en if self.en else self._help_zh
    def show_update_result(self):
        path=self.home/'updates/last-update.txt'
        if not path.is_file(): return
        state=path.read_text(encoding='utf-8').strip()
        if state.startswith('failed:'):
            self.mb.showwarning('SRPRecorder2027',self.tr('上一次自动更新未完成，已尝试保留/恢复旧程序。请手动安装官方 DMG；诊断日志位于：','The previous automatic update failed; it attempted to preserve/restore the old app. Install the official DMG manually. Diagnostic logs: ')+str(path.parent),parent=self.root)
            path.rename(path.with_name('last-update-reported-'+uuid.uuid4().hex+'.txt'))
    def update_job(self,work):
        try: return work()
        except Exception as error: raise UpdateError(update_error_message(error,self.en)) from error
    def check_updates(self):
        def done(rel):
            tag=str(rel.get('tag_name') or '')
            if version_tuple(tag)<=version_tuple(VERSION):
                self.mb.showinfo('SRPRecorder2027',self.tr('已是最新版本 ','Up to date: ')+VERSION,parent=self.root); return
            asset=update_asset(rel)
            if not asset:
                self.mb.showerror('SRPRecorder2027',self.tr('该版本没有适用于本机架构的安装包。','This release has no package for this architecture.'),parent=self.root); return
            notes=(rel.get('body') or '').strip()
            if len(notes)>1200: notes=notes[:1200]+'…'
            message=(self.tr('发现新版本 ','New version ')+tag+self.tr('（当前 ',' (current ')+VERSION+self.tr('）。\n\n',').\n\n')
                     +notes+self.tr('\n\n下载并校验更新？当前未公证测试版可能需要手动安装。本地资料不会被删除。','\n\nDownload and verify the update? Current unnotarized test builds may need manual installation. Local data is preserved.'))
            if not self.mb.askyesno('SRPRecorder2027',message,parent=self.root): return
            self.status.set(self.tr('正在下载更新…','Downloading update…'))
            def done2(path):
                self.status.set(self.tr('更新已下载并通过校验。','Update downloaded and verified.'))
                def installed(plan):
                    if plan['automatic']:
                        self.close(); return
                    reasons={
                        'unnotarized-build':self.tr('当前程序没有 Developer ID 分发签名，无法保证静默安装后获准启动。','The current app lacks a Developer ID distribution signature; an unattended launch cannot be guaranteed.'),
                        'disk-image':self.tr('当前程序正在 DMG 或系统转移位置运行。','The app is running from a disk image or App Translocation.'),
                        'not-writable':self.tr('当前安装目录不可写。','The installation directory is not writable.'),
                        'not-installed':self.tr('当前运行位置不是已安装的应用包。','This is not an installed application bundle.')}
                    self.status.set(self.tr('安装包已验证；等待手动安装，当前版本未改变。','Package verified; manual installation pending. The installed version is unchanged.'))
                    message=reasons.get(plan['reason'],'')+self.tr(
                        '\n\n已打开 DMG。请先保存内容并退出本程序，再将 ResearchProgressRecorder.app 拖入 Applications 替换。重新打开若提示“无法验证开发者”，确认来自官方发布页后，由本人在系统设置 → 隐私与安全性 → 仍要打开中处理。若提示文件损坏或会损害电脑，请停止安装并反馈原文。用户资料保留在 Application Support。',
                        '\n\nThe DMG is open. Save your work and quit this app, then drag ResearchProgressRecorder.app into Applications to replace it. If macOS cannot verify the developer, confirm the official download source and personally use System Settings → Privacy & Security → Open Anyway. Stop and report any damaged-app or malware warning. Data in Application Support is preserved.')
                    self.mb.showinfo('SRPRecorder2027',message,parent=self.root)
                self.run(lambda:self.update_job(lambda:install_update(path,self.home/'updates')),installed)
            self.run(lambda:self.update_job(lambda:download_update(rel,asset)),done2)
        self.run(lambda:self.update_job(release_info),done)
    def save_help_draft(self):
        target=self.home/'help_local.md'; content=self.help_text.get('1.0','end-1c')
        temporary=target.with_suffix('.md.tmp'); temporary.write_text(content,encoding='utf-8')
        if target.exists(): shutil.copy2(target,str(target)+'.bak')
        os.replace(temporary,target)
    def default_help(self):
        if not self.require_login(): return
        if not self.mb.askyesno('SRPRecorder2027',self.tr('恢复默认会替换当前草稿，继续？','Replace the current draft with built-in help?'),parent=self.root): return
        self.set_help_text(self.default_help_text())
        self.help_dirty=True
    def set_help_text(self,text):
        self.help_text.configure(state='normal')
        self.help_text.delete('1.0','end'); self.help_text.insert('1.0',text)
        self.help_text.edit_modified(False); self.help_dirty=False
        self.help_text.configure(state='normal' if self.session_login else 'disabled')
        self.status.set(self.tr('已恢复软件自带的使用说明。','Restored built-in help.'))
    def upload_help(self):
        api,config=self.api()
        text=self.help_text.get('1.0','end-1c')
        if not text.strip():
            self.mb.showinfo('SRPRecorder2027',self.tr('使用说明不能为空。','Help text cannot be empty.'),parent=self.root); return
        def work():
            login=api.login(); api.login_cached=login
            endpoint=api.endpoint('help/help.md'); sha=None
            try: sha=api.request('GET',endpoint+'?ref='+urllib.parse.quote(api.branch,safe=''))['sha']
            except urllib.error.HTTPError as error:
                if error.code!=404: raise
            body={'message':'help: SRPRecorder2027 '+VERSION,'branch':api.branch,'content':base64.b64encode(text.encode()).decode()}
            if sha: body['sha']=sha
            api.request('PUT',endpoint,body)
            append_activity_to(api,'help/changelog.json',login,'更新使用说明 / Update help',str(len(text))+' 字符 / chars',200)
            try: append_activity(api,login,'更新使用说明 / Update help',str(len(text))+' 字符 / chars')
            except Exception: pass
            return len(text)
        def done(_):
            self.save_help_draft(); self.help_dirty=False
            self.finish(api,config,self.tr('使用说明已上传，所有成员登录后都能读到。','Help uploaded for every member.'))
        self.run(work,done)
    def load_help(self,quiet=False):
        api,config=self.api()
        before=self.help_text.get('1.0','end-1c')
        if self.help_dirty and not quiet:
            if not self.mb.askyesno('SRPRecorder2027',self.tr('读取云端将替换当前草稿，继续？','Replace the current draft with cloud help?'),parent=self.root): return
        elif self.help_dirty: return
        def work():
            endpoint=api.endpoint('help/help.md')+'?ref='+urllib.parse.quote(api.branch,safe='')
            try: return api.request('GET',endpoint,raw=True)
            except urllib.error.HTTPError as error:
                if error.code==404: return None
                raise
        def done(text):
            self.remember(api,config)
            if text is None:
                if not quiet: self.mb.showinfo('SRPRecorder2027',self.tr('云端还没有上传过使用说明。','No uploaded help yet.'),parent=self.root)
                return
            if self.help_text.get('1.0','end-1c')!=before: return
            self.set_help_text(text)
            self.save_help_draft()
            self.status.set(self.tr('已读取云端使用说明。','Help loaded from GitHub.'))
        self.run(work,done)
    def show_help_changelog(self):
        api,config=self.api()
        def work():
            rows,sha=api.get('help/changelog.json')
            return normalize_activity(rows)
        def done(rows):
            self.remember(api,config)
            win=self.tk.Toplevel(self.root); win.title(self.tr('改动日志','Change Log')); self.fit_dialog(win,760,420); win.transient(self.root); win.grab_set()
            tree=self.table(win,[self.tr('时间','Time'),self.tr('用户','User'),self.tr('说明','Detail')])
            for row in reversed(rows):
                tree.insert('','end',values=(row.get('time',''),row.get('user',''),row.get('detail','')))
            self.ttk.Button(self.bottom_bar(win),text=self.tr('关闭','Close'),command=win.destroy).pack(pady=8)
        self.run(work,done)

def verify_update_runtime(output=None):
    """Release gate: test the bundled TLS runtime without opening a user workspace."""
    context=network_context()
    if not context.check_hostname or context.verify_mode!=ssl.CERT_REQUIRED:
        raise RuntimeError('Update runtime must verify TLS certificates and hostnames')
    release=release_info()
    sums=next((a for a in release.get('assets',[]) if a.get('name')=='SHA256SUMS.txt'),None)
    if not sums: raise RuntimeError('Public release has no checksum asset')
    req=urllib.request.Request(sums['browser_download_url'],headers={'User-Agent':APP_ID})
    with urllib.request.urlopen(req,timeout=60,context=context) as response:
        content=response.read().decode('utf-8')
    if not re.search(r'^[0-9a-fA-F]{64}\s+\S+',content,re.MULTILINE):
        raise RuntimeError('Public checksum asset is not readable')
    plan=update_install_plan(app_bundle_path())
    result=dict(version=VERSION,platform=sys.platform,architecture=platform.machine(),
                tls_verified=True,release=release.get('tag_name'),checksum_download=True,
                install_mode=plan['reason'])
    if output: Path(output).write_text(json.dumps(result,indent=2),encoding='utf-8')
    else: print(json.dumps(result))

def main():
    if '--verify-update-runtime' in sys.argv:
        index=sys.argv.index('--verify-update-runtime')
        verify_update_runtime(sys.argv[index+1] if len(sys.argv)>index+1 else None); return
    import tkinter as tk
    root=tk.Tk(); root.withdraw()
    try:
        smoke='--smoke-test' in sys.argv
        if smoke:
            with tempfile.TemporaryDirectory() as tmp:
                client=Client(root,tmp,smoke=True); root.update()
                assert len(client.tabs)==6
                for index in range(6): client.book.select(index); root.update()
                root.destroy(); print('SRPRecorder2027 '+VERSION+': six pages and the bilingual UI passed')
        else:
            Client(root); root.deiconify(); root.mainloop()
    except Exception as error:
        if '--smoke-test' in sys.argv: raise
        from tkinter import messagebox
        messagebox.showerror('SRPRecorder2027',str(error)+'\nExisting research data was preserved.')

if __name__=='__main__': main()
