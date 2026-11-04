"""SRPRecorder2027 0.2.13. Python stdlib; packaged as a macOS .app in CI."""
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

VERSION = '0.2.13'
# Stable one-time data-cleanup marker introduced in 0.2.0. Do not bump this
# when publishing an ordinary app release; doing so would erase existing data.
DATA_VERSION = '0.2.0'
APP_ID = 'ResearchProgressRecorder'
DEFAULT_REPO = 'bacoyc0928-art/research-data'

HELP_TEXT = """SRPRecorder2027 使用说明 / User Guide
适用版本 / Version 0.2.13 · 中文在前，English follows each section

本程序帮助课题组记录和交换研究资料。团队资料保存在私有 GitHub 仓库 bacoyc0928-art/research-data；程序本身从公开仓库 research-progress-recorder 下载，不需要登录。没有服务器，也没有实时同步：上传和下载都由你手动发起。
The app helps the group record and exchange research files. Team data lives in the private GitHub repository bacoyc0928-art/research-data; the app is downloaded from the public repository research-progress-recorder without signing in. There is no server and no real-time sync: you start every upload and download yourself.


【1】第一次使用 / Getting started

1. 接受邀请：用你的 GitHub 账号接受 research-data 仓库的协作邀请。
2. 创建令牌：GitHub → Settings → Developer settings → Personal access tokens → Tokens (classic)，勾选 repo，设置有效期。成员必须用 classic 令牌；fine-grained 令牌访问他人的私有仓库一律返回 404。
3. 登录：Windows 在窗口左下角，macOS 在窗口底部。粘贴令牌，点「登录」。
4. 可选「记住登录」：设置 6 位 PIN，令牌存入系统凭据库（Windows 凭据管理器 / macOS 钥匙串）。下次启动只输 PIN；连续 5 次输错会清除保存的令牌。
5. 「退出」会在确认后直接关闭程序。

1. Accept the invitation to the research-data repository with your GitHub account.
2. Create a token: GitHub → Settings → Developer settings → Personal access tokens → Tokens (classic), tick repo and set an expiry. Members must use a classic token; fine-grained tokens always get 404 on another account's private repository.
3. Sign in at the bottom-left (Windows) or bottom bar (macOS): paste the token and press Sign In.
4. Optional "Remember": set a 6-digit PIN and the token is stored in the system credential store (Windows Credential Manager / macOS Keychain). Next time you only enter the PIN; five wrong PINs clear the saved token.
5. "Sign Out" asks for confirmation and then closes the app.

未登录时：同步、发帖、使用说明工具栏的按钮都是灰色的，参考资料页不能进入，程序不写任何配置。
While signed out, sync, posting and Help toolbar buttons are greyed out, the References page is locked and the app writes no settings.


【2】六个页面 / The six pages

成员资料：列出所有成员及其状态（已连接 · 最近活动 / 已上传 / 已加入 · 未连接 / 待接受邀请）。打开他人的文件夹会先下载其文件再列出（macOS 先选中成员，再点「打开成员文件」）；自己的卡片打开本机 Reference 文件夹。
Quick Post：给团队发短消息。点卡片看全文；「已阅」后移入「历史记录」；只能删除自己的帖子。
参考资料：只读浏览团队默认资料库、我的参考资料，以及临时下载的他人内容。
本地记录：选择本地记录文件夹；「新建文档」在 Mat_Local 中生成 Word 文档；下方列出 Mat_Local 中的文件。
进度同步：所有上传、下载和撤除操作都在这里（见第 3 节）。
使用说明：本页。登录后可编辑并上传给所有成员。

Members: every member and their state (Connected · last active / Uploaded / Joined · not connected / Invitation pending). Opening another member's folder downloads and then lists their files (on macOS, select the member and press Open Member Files); your own card opens your local Reference folder.
Quick Post: short team messages. Click a card for the full text; "Mark Read" moves it to History; you can delete only your own posts.
References: read-only view of the team default library, your references and others' temporary downloads.
Local Records: choose your local records folder; "New Document" creates a Word file in Mat_Local; the list shows the files in Mat_Local.
Progress Sync: every upload, download and removal (see section 3).
Help: this page. When signed in you can edit it and upload it for everyone.

页面名称旁的红点表示该页有新内容，进入页面后消失。界面可用 EN / 中文 按钮切换语言（Windows 在左上角，macOS 在右上角）。
A red dot next to a page name means something new; it clears when you open the page. Use the EN / 中文 button to switch the interface language (top-left on Windows, top-right on macOS).


【3】同步操作 / Sync actions（进度同步页 / Progress Sync page）

每个操作都会先列出清单，只处理你勾选的项目。
Each action first shows a checklist and processes only the items you tick.

- 下载他人内容：把选中成员的文件下载到 Public_Temp\\<成员>\\。临时区在启动和退出时清空。
- 下载默认资料库：把团队默认资料下载到 References\\Default\\，覆盖前保留旧副本。
- 上传参考资料：默认列出 Reference 中的文件，也可添加其他文件；上传到团队区你自己的目录。
- 上传本地记录：默认列出 Mat_Local 中的文件，保持原有的子文件夹结构。
- 撤除我上传的参考资料：从团队区删除你自己上传的文件，同时删除本机 Reference 中的副本，其他人从此无法下载。只能撤除自己的。
- 查看活动日志：查看全员的登录、上传、下载、撤除、发帖和改说明记录。

- Download team files: selected members' files go to Public_Temp\\<member>\\, which is cleared on start and exit.
- Download default library: team default files go to References\\Default\\; older copies are kept before overwriting.
- Upload references: lists files in Reference by default (you can add others) and uploads them to your own team folder.
- Upload local records: lists files in Mat_Local by default and keeps their sub-folder structure.
- Remove my uploaded references: deletes your own uploads from the team area and the copy in your local Reference, so others can no longer download them. You can remove only your own.
- Activity log: everyone's sign-ins, uploads, downloads, removals, posts and help edits.

需要记住的规则 / Rules to remember
- 上传前先保存文件；本地保存不等于上传。
- 两人上传同名文件时，后上传的会覆盖先上传的，程序不做合并。请按各自目录分工。
- 单个文件不超过 50 MiB。
- 上传只新增或覆盖，不会删除团队区里的旧文件。

- Save before uploading; saving locally is not uploading.
- If two people upload the same file name, the later upload wins; nothing is merged. Keep to your own folder.
- Each file must be 50 MiB or smaller.
- Uploads only add or overwrite; they never delete older files in the team area.


【4】文件放在哪里 / Where files live

Windows：程序所在的 SRCRec 文件夹
macOS：~/Library/Application Support/ResearchProgressRecorder/SRCRec

PersonalWorkspaceSRRec\\Mat_Local      你的工作资料，上传本地记录的默认来源 / your working files; default source for local-record uploads
PersonalWorkspaceSRRec\\Reference      你的参考资料，上传参考资料的默认来源 / your references; default source for reference uploads
PersonalWorkspaceSRRec\\Public_Temp    他人内容的临时区，启动和退出时清空 / temporary area for others' files, cleared on start and exit
References\\Default                    团队默认资料库，只读 / team default library, read-only
*.json                                程序配置，请勿手动修改 / app settings, do not edit by hand

令牌不会写入这些文件。替换或升级程序不会删除上面的资料。
The token is never written to these files. Replacing or upgrading the app keeps all of the above.


【5】更新与升级 / Updates

Windows：点左下角「检查更新」。程序下载新版并校验 SHA-256，退出后只替换程序文件，然后自动重启；上一版保留为 SRPRecorder2027.exe.previous。
macOS：点窗口左下角「检查更新」。程序下载并校验 DMG 后打开它，请退出程序，把新的 app 拖进「应用程序」替换旧版。较早版本若检查更新立即失败，请用浏览器从发布页下载对应芯片的 DMG：Apple 芯片用 arm64，Intel 用 x64。
发布页：https://github.com/bacoyc0928-art/research-progress-recorder/releases/latest

Windows: press "Check updates" at the bottom-left. The app downloads the new version, verifies its SHA-256, replaces only program files after exit and restarts; the previous exe is kept as SRPRecorder2027.exe.previous.
macOS: press "Check updates" at the bottom-left. The app downloads and verifies the DMG and opens it; quit the app and drag the new app into Applications to replace the old one. If an older version fails immediately, download the DMG for your chip from the release page in a browser: arm64 for Apple silicon, x64 for Intel.
Release page: https://github.com/bacoyc0928-art/research-progress-recorder/releases/latest


【6】常见问题 / Troubleshooting

- 登录提示 401：令牌无效或已过期，请重新创建。
- 登录提示 403 / 404：确认已接受仓库邀请，并且使用的是勾选了 repo 的 classic 令牌。
- 看不到成员或帖子：先确认已登录，再在成员资料页点「刷新」。
- Windows 首次运行出现 SmartScreen：点「更多信息 → 仍要运行」。程序没有购买代码签名证书。
- macOS 提示无法验证开发者：确认 DMG 来自上述发布页后，先尝试打开一次，再到「系统设置 → 隐私与安全性」点「仍要打开」。如果提示文件已损坏或会损害电脑，请停止使用并反馈原文。
- 上传只成功一部分：多文件上传不是一次性完成的，重新上传即可，同名文件会被覆盖。
- 检查更新失败：多为网络问题或 GitHub 暂时限流，请稍后重试，或直接从发布页下载。

- 401 on sign-in: the token is invalid or expired; create a new one.
- 403 / 404 on sign-in: make sure you accepted the invitation and use a classic token with repo.
- No members or posts: check that you are signed in, then press Refresh on Members.
- SmartScreen on first run (Windows): choose "More info → Run anyway". The app has no paid code-signing certificate.
- "Developer cannot be verified" (macOS): after confirming the DMG came from the release page, try to open the app once, then choose Open Anyway in System Settings → Privacy & Security. If macOS says the file is damaged or harmful, stop and report the exact message.
- Upload only partly succeeded: multi-file uploads are not all-or-nothing; upload again and same-named files are overwritten.
- Update check failed: usually the network or a temporary GitHub rate limit; retry later or download from the release page.


【7】安全与隐私 / Security and privacy

- 令牌只用于连接 GitHub，不会上传、打印或写进日志。请不要把令牌发给别人；不用时可在 GitHub 上撤销。
- 能写入 research-data 的人在技术上可以修改任何成员的目录。请只邀请信任的成员。
- 卸载程序不会撤销令牌，请到 GitHub 自行撤销。

- The token is used only to talk to GitHub; it is never uploaded, printed or logged. Do not share it; revoke it on GitHub when no longer needed.
- Anyone who can write to research-data can technically change any member's folder. Invite only trusted members.
- Uninstalling does not revoke the token; revoke it on GitHub yourself."""


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

RELEASE_REPO=DEFAULT_REPO.replace('/research-data','/research-progress-recorder')
RELEASE_ASSET=re.compile(r'research-progress-recorder-\d+\.\d+\.\d+-(?:Windows\.zip|macOS-(?:arm64|x64)\.dmg)')

def release_info():
    """公开 Release，无需令牌。GitHub API 对匿名请求按 IP 限流（共享出口网络常见）；
    遇到 403/429 时改走不受该额度限制的发布页与下载地址。"""
    req=urllib.request.Request('https://api.github.com/repos/'+RELEASE_REPO+'/releases/latest',
        headers={'User-Agent':APP_ID,'Accept':'application/vnd.github+json'})
    try:
        with urllib.request.urlopen(req,timeout=30,context=network_context()) as resp: return json.loads(resp.read().decode('utf-8'))
    except urllib.error.HTTPError as error:
        if error.code not in (403,429): raise
        return release_info_from_page()

def release_info_from_page():
    """从 github.com/<repo>/releases/latest 的跳转得到标签，再从同一 Release 的 SHA256SUMS.txt 得到安装包清单。
    返回与 API 相同结构（tag_name/assets），下载后仍按 SHA256SUMS 校验。"""
    base='https://github.com/'+RELEASE_REPO+'/releases'
    with urllib.request.urlopen(urllib.request.Request(base+'/latest',headers={'User-Agent':APP_ID}),timeout=30,context=network_context()) as resp:
        final=urllib.parse.urlparse(resp.geturl())
    match=re.fullmatch(r'/'+re.escape(RELEASE_REPO)+r'/releases/tag/(v\d+\.\d+\.\d+)',final.path)
    if final.scheme!='https' or final.netloc!='github.com' or not match:
        raise UpdateError('The public release page did not identify a version tag.')
    tag=match.group(1); download=base+'/download/'+tag+'/'
    with urllib.request.urlopen(urllib.request.Request(download+'SHA256SUMS.txt',headers={'User-Agent':APP_ID}),timeout=60,context=network_context()) as resp:
        lines=resp.read().decode('utf-8').splitlines()
    names=[]
    for line in lines:
        parts=line.split()
        if len(parts)>=2 and re.fullmatch(r'[0-9a-fA-F]{64}',parts[0]):
            name=parts[-1].lstrip('*')
            if RELEASE_ASSET.fullmatch(name) and name not in names: names.append(name)
    if not names: raise UpdateError('The public release checksum file lists no packages.')
    assets=[dict(name=name,browser_download_url=download+name) for name in names]
    assets.append(dict(name='SHA256SUMS.txt',browser_download_url=download+'SHA256SUMS.txt'))
    return dict(tag_name=tag,body='',assets=assets,source='release-page',html_url=base+'/tag/'+tag)

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

def help_newlines(text):
    """使用说明统一为 LF 换行（Windows 客户端上传的是 CRLF）。"""
    return (text or '').replace('\r\n','\n').replace('\r','\n')

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
            if kind in ('TLabel','TButton','TCheckbutton','Label','Button','TLabelframe'):
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
    def button(self,parent,zh,en,command,primary=False):
        b=self.ttk.Button(parent,text=self.tr(zh,en),command=lambda:self.guarded(command))
        # 主要操作用系统默认按钮样式（macOS 上为蓝色），与 Windows 的蓝底主要按钮对应。
        if primary: b.configure(default='active')
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
        self.login_btn=self.ttk.Button(self.token_frame,text=self.tr('登录','Sign In'),command=self.sign_in,default='active')
        self.login_btn.pack(side='left',padx=4)
        self.pin_frame=self.ttk.Frame(bar)
        self.login_pin=self.tk.StringVar()
        self.pin_entry=self.ttk.Entry(self.pin_frame,textvariable=self.login_pin,show='●',width=8,validate='key',validatecommand=(self.root.register(self.validate_pin),'%P'))
        self.pin_entry.pack(side='left',padx=6)
        self.pin_entry.bind('<Return>',lambda e:self.unlock_saved())
        self.unlock_btn=self.ttk.Button(self.pin_frame,text=self.tr('解锁','Unlock'),command=self.unlock_saved,default='active')
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
        self.login_actions+=[self.button(bar,'发布','Publish',self.publish_post,primary=True),
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
        # 第一行：常用操作（新建文档为主要按钮）；第二行：打开三个固定文件夹。原先 7 个按钮挤在一行且有重复。
        bar=self.flow_bar(p,pady=(10,2))
        self.button(bar,'新建文档','New Document',self.new_document,primary=True)
        self.button(bar,'选择文件夹','Choose Folder',self.choose_progress)
        self.button(bar,'刷新','Refresh',self.refresh_progress_files)
        folders=self.ttk.Frame(p); folders.pack(fill='x',pady=(0,8))
        self.ttk.Label(folders,text=self.tr('打开文件夹：','Open folder:'),foreground='#5e697a').pack(side='left',padx=(4,2))
        self.button(folders,'工作资料 Mat_Local','Working files (Mat_Local)',lambda:self.open_path(str(self.mat_local)))
        self.button(folders,'个人参考 Reference','My references (Reference)',lambda:self.open_path(str(self.reference_dir)))
        self.button(folders,'他人内容 Public_Temp','Others, temporary (Public_Temp)',lambda:self.open_path(str(self.public_temp)))
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
        def action(parent,zh,en,command,primary=False):
            button=self.button(parent,zh,en,command,primary=primary); self.sync_actions.append(button)
        self.ttk.Label(p,text=self.tr('已绑定仓库：','Bound repository: ')+DEFAULT_REPO+' · main',font=('',11,'bold')).pack(anchor='w')
        self.ttk.Label(p,text=self.tr('登录后在下方直接操作，无需填写任何配置。','Sign in first; no configuration needed below.'),foreground='#c47a2a').pack(anchor='w',pady=(4,10))
        # 与 Windows 一致：按用途分三组，每组一句话说明文件去向。
        def group(zh,en,note_zh,note_en):
            frame=self.ttk.LabelFrame(p,text=self.tr(zh,en),padding=(12,4,12,8)); frame.pack(fill='x',pady=(0,10))
            self.wrapped_label(frame,self.tr(note_zh,note_en),foreground='#5e697a')
            return self.flow_bar(frame,pady=(2,0))
        downloads=group('从团队获取','Get from the team','下载到本机查看，不会改动团队资料。','Downloads to this computer; team data is not changed.')
        action(downloads,'从 GitHub 下载他人内容','Download Team Files',self.pull_others)
        action(downloads,'从 GitHub 下载默认资料库','Download Default Library',self.pull_default)
        uploads=group('分享我的资料','Share my files','把你勾选的文件上传到团队区你自己的文件夹；每个文件最大 50 MiB，只新增或覆盖，不删除旧文件。',
                      'Uploads the files you tick to your own team folder; up to 50 MiB each, only adds or overwrites, never deletes older files.')
        action(uploads,'上传参考资料','Upload References',self.upload_references,primary=True)
        action(uploads,'上传本地记录','Upload Local Records',self.upload_progress,primary=True)
        removals=group('管理我的上传','Manage my uploads','撤除只影响你自己上传的文件；活动日志记录每个人的操作。',"Removal affects only files you uploaded; the activity log shows everyone's actions.")
        action(removals,'撤除我上传的参考资料','Remove My Uploaded References',self.remove_my_references)
        action(removals,'查看活动日志','View Activity Log',self.view_activity)
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
        # 换行宽度随弹窗宽度变化；固定 520 会在最小尺寸（约 360 宽）时截断提示文字。
        self.wrapped_label(win,self.tr('选择要从团队区永久删除的参考资料（其他人将无法再下载）：','Select uploaded references to remove permanently from the team area:'),pack_options={'padx':12,'pady':8})
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
        content=help_newlines(draft.read_text(encoding='utf-8-sig')) if draft.exists() else (en if self.en else zh)
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
            if not notes and rel.get('source')=='release-page':
                notes=self.tr('（GitHub 接口暂时限流，已改用发布页检查；更新说明见 ','(GitHub API is rate-limited; checked via the release page. Release notes: ')+str(rel.get('html_url',''))+self.tr('）',')')
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
        # Windows 客户端上传的 help.md 用 CRLF；统一成 LF，避免 Tk 中残留回车符。
        text=help_newlines(text)
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
    # 同时实测不受 API 额度限制的发布页回退路径，确保限流时更新检查仍可用。
    page=release_info_from_page()
    if page['tag_name']!=release.get('tag_name') or not update_asset(page):
        raise RuntimeError('Release-page fallback disagrees with the release information')
    plan=update_install_plan(app_bundle_path())
    result=dict(version=VERSION,platform=sys.platform,architecture=platform.machine(),
                tls_verified=True,release=release.get('tag_name'),checksum_download=True,
                release_source=release.get('source','api'),page_fallback=page['tag_name'],
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
