"""SRPRecorder2027 0.1.1. Python stdlib; packaged as a macOS .app in CI."""
from __future__ import annotations
import base64
import copy
import ctypes
import datetime as dt
from decimal import Decimal
import json
import os
from pathlib import Path
import queue
import re
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

VERSION = '0.1.1'
APP_ID = 'ResearchProgressRecorder'
DEFAULT_REPO = 'bacoyc0928-art/research-data'

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
    service = b'SRLogbook.github_token'; account = b'ResearchProgressRecorder'
    def __init__(self):
        self.lib = ctypes.CDLL('/System/Library/Frameworks/Security.framework/Security')
        self.lib.SecKeychainFindGenericPassword.argtypes = [ctypes.c_void_p, ctypes.c_uint32, ctypes.c_char_p, ctypes.c_uint32, ctypes.c_char_p, ctypes.POINTER(ctypes.c_uint32), ctypes.POINTER(ctypes.c_void_p), ctypes.POINTER(ctypes.c_void_p)]
        self.lib.SecKeychainAddGenericPassword.argtypes = [ctypes.c_void_p, ctypes.c_uint32, ctypes.c_char_p, ctypes.c_uint32, ctypes.c_char_p, ctypes.c_uint32, ctypes.c_void_p, ctypes.c_void_p]
        self.lib.SecKeychainItemModifyAttributesAndData.argtypes = [ctypes.c_void_p, ctypes.c_void_p, ctypes.c_uint32, ctypes.c_void_p]
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
        context=ssl.create_default_context()
        if sys.platform=='darwin' and Path('/etc/ssl/cert.pem').exists():
            context.load_verify_locations(cafile='/etc/ssl/cert.pem')
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
        return '<w:p><w:r><w:rPr><w:rFonts w:ascii="Times New Roman" w:eastAsia="楷体"/>'+('<w:b/>' if bold else '')+'</w:rPr><w:t xml:space="preserve">'+escape(text)+'</w:t></w:r></w:p>'
    document = '<?xml version="1.0" encoding="UTF-8"?><w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:body>'+para(title,True)+para(project+' / '+category+' / '+str(dt.date.today()))+para('研究进展 / Progress',True)+para('')+para('下一步 / Next steps',True)+para('')+'</w:body></w:document>'
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

def append_activity(api, login, action, detail, to_public=True):
    targets=[(f'logs/private/{login}/activity.json',500)]
    if to_public: targets.append(('logs/public/activity.json',2000))
    for path,limit in targets:
        for attempt in range(3):
            old, sha = api.get(path)
            rows = old if isinstance(old, list) else []
            rows.append(dict(time=now(), user=login, action=action, detail=detail))
            if len(rows) > limit: rows = rows[-limit:]
            try:
                api.put(path, rows, sha); break
            except urllib.error.HTTPError as error:
                if attempt == 2 or error.code not in (409,422): raise

def read_activity(api, path):
    data, sha = api.get(path)
    return data if isinstance(data, list) else []

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

class Client:
    def __init__(self, root, home=None, smoke=False):
        import tkinter as tk
        from tkinter import ttk, filedialog, messagebox, simpledialog
        self.tk,self.ttk,self.fd,self.mb,self.ask=tk,ttk,filedialog,messagebox,simpledialog
        self.root=root; self.home=Path(home or Path.home()/'Library'/'Application Support'/APP_ID)
        self.home.mkdir(parents=True,exist_ok=True)
        self.files_root=self.home/'SRCRec'; self.files_root.mkdir(exist_ok=True)
        self.default=self.files_root/'References'/'Default'; self.default.mkdir(parents=True,exist_ok=True)
        self.configfile=self.home/'config.json'; self.datafile=self.home/'research_log.json'
        self.config=json.loads(self.configfile.read_text(encoding='utf-8-sig')) if self.configfile.exists() else {}
        # Keep legacy research JSON intact. 0.1.0 file channels never rewrite it.
        if self.datafile.exists(): normalize(json.loads(self.datafile.read_text(encoding='utf-8-sig')))
        self.en=self.config.get('language','zh')=='en'; self.busy=False; self.events=queue.Queue()
        self.smoke=smoke; self.disabled=[]; self.login=''
        self.session_token=''; self.session_login=''
        progress=Path(self.config.get('progress') or self.files_root/'PersonalWorkspaceSRRec')
        if not progress.is_dir(): progress=self.files_root/'PersonalWorkspaceSRRec'
        progress.mkdir(parents=True,exist_ok=True); self.config['progress']=str(progress)
        self.config.setdefault('workspace',str(self.files_root)); atomic(self.configfile,self.config)
        root.title('SRPRecorder2027 · '+VERSION); root.geometry('1120x760'); root.minsize(940,640)
        self.build(); root.protocol('WM_DELETE_WINDOW',self.close); root.after(100,self.poll)
    def tr(self,zh,en): return en if self.en else zh
    def button(self,parent,zh,en,command):
        b=self.ttk.Button(parent,text=self.tr(zh,en),command=lambda:self.guarded(command)); b.pack(side='left',padx=4,pady=5); return b
    def guarded(self,fn):
        if self.busy: return
        try: fn()
        except Exception as error: self.mb.showerror(self.tr('操作失败','Operation failed'),str(error),parent=self.root)
    def close(self):
        if self.busy: self.mb.showinfo('SRPRecorder2027',self.tr('请等待当前传输完成。','Wait for the current transfer to finish.')); return
        self.root.destroy()
    def poll(self):
        try:
            done,result,error=self.events.get_nowait(); self.busy=False
            for w,state in self.disabled:
                try: w.configure(state=state)
                except Exception: pass
            if error:
                self.status.set(self.tr('操作失败；上传可能部分完成，旧下载副本保留。','Failed; uploads may be partially complete. Previous downloads were preserved.'))
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
        header=ttk.Frame(self.root,padding=12); header.pack(fill='x')
        ttk.Label(header,text='SRPRecorder2027  '+VERSION,font=('',17,'bold')).pack(side='left')
        self.button(header,'更换工作文件夹','Change Workspace',self.choose_workspace)
        self.button(header,'EN / 中文','中文 / EN',self.language)
        self.book=ttk.Notebook(self.root); self.book.pack(fill='both',expand=True,padx=12,pady=6)
        self.book.bind('<<NotebookTabChanged>>', lambda e:self.on_tab_changed(e))
        self.tabs=[]
        for zh,en in [('团队研究','Team Research'),('快速发行','Quick Post'),('参考文献与资料','References'),('本地进度','Local Progress'),('团队同步','Team Sync'),('使用说明','Help')]:
            panel=ttk.Frame(self.book,padding=16); self.book.add(panel,text=self.tr(zh,en)); self.tabs.append(panel)
        self.build_login_bar()
        self.status=self.tk.StringVar(value=self.tr('就绪','Ready')); ttk.Label(self.root,textvariable=self.status,padding=8).pack(fill='x')
        self.build_team(); self.build_posts(); self.build_references(); self.build_progress(); self.build_sync(); self.build_help()
        self.update_login_ui()
    def build_login_bar(self):
        bar=self.ttk.Frame(self.root,padding=(12,6)); bar.pack(fill='x')
        self.avatar=self.tk.Canvas(bar,width=40,height=40,highlightthickness=0)
        try: self.avatar.configure(bg=self.root.cget('bg'))
        except Exception: pass
        self.avatar.pack(side='left')
        info=self.ttk.Frame(bar); info.pack(side='left',padx=10,anchor='w')
        self.login_name=self.ttk.Label(info,text='',font=('',11,'bold')); self.login_name.pack(anchor='w')
        self.login_hint=self.ttk.Label(info,text='',foreground='#5e697a'); self.login_hint.pack(anchor='w')
        self.login_token=self.tk.StringVar()
        self.login_entry=self.ttk.Entry(bar,textvariable=self.login_token,show='●',width=46)
        self.login_entry.pack(side='left',padx=8)
        self.login_show=self.tk.BooleanVar(value=False)
        self.login_show_btn=self.ttk.Checkbutton(bar,text=self.tr('显示','Show'),variable=self.login_show,command=self.toggle_login_show)
        self.login_show_btn.pack(side='left',padx=4)
        self.login_btn=self.ttk.Button(bar,text=self.tr('登录','Sign In'),command=self.sign_in)
        self.login_btn.pack(side='left',padx=4)
        self.logout_btn=self.ttk.Button(bar,text=self.tr('退出','Sign Out'),command=self.sign_out)
        self.logout_btn.pack(side='left',padx=4)
    def draw_avatar(self):
        if not hasattr(self,'avatar'): return
        c=self.avatar; c.delete('all')
        initial=(self.session_login[:1].upper() if self.session_login else '?')
        color='#2d6cdf' if self.session_login else '#404e68'
        c.create_oval(2,2,38,38,fill=color,outline='')
        c.create_text(20,20,text=initial,fill='white',font=('',13,'bold'))
    def update_login_ui(self):
        if not hasattr(self,'login_name'): return
        signed=bool(self.session_login)
        self.login_name.configure(text=self.session_login if signed else self.tr('未登录','Not signed in'))
        self.login_hint.configure(text=self.tr('已登录 GitHub','Signed in with GitHub') if signed else self.tr('输入密钥后登录','Enter token, then Sign In'))
        if signed:
            self.login_entry.pack_forget(); self.login_show_btn.pack_forget(); self.login_btn.pack_forget(); self.logout_btn.pack(side='left',padx=4)
        else:
            self.logout_btn.pack_forget(); self.login_entry.pack(side='left',padx=8); self.login_show_btn.pack(side='left',padx=4); self.login_btn.pack(side='left',padx=4)
        self.draw_avatar()
    def toggle_login_show(self):
        self.login_entry.configure(show='' if self.login_show.get() else '●')
    def sign_in(self):
        token=self.login_token.get().strip()
        if not token:
            self.mb.showinfo('SRPRecorder2027',self.tr('请粘贴 GitHub 访问密钥。','Paste your GitHub token first.'),parent=self.root); return
        def work():
            try:
                api=GitHub({'repo':DEFAULT_REPO,'branch':'main'},token)
                login=api.login(); api.login_cached=login
                try: append_activity(api,login,'登录','身份验证成功')
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
            self.login_token.set(''); self.update_login_ui(); self.team(); self.refresh_posts()
            self.status.set(self.tr('已登录 ','Signed in as ')+login)
        self.run(work,done)
    def sign_out(self):
        if not self.mb.askyesno('SRPRecorder2027',self.tr('退出登录将直接关闭程序，确定退出？','Signing out closes the app. Continue?'),parent=self.root): return
        self.session_login=''; self.session_token=''; self.login=''
        self.root.destroy()
    def require_login(self):
        if self.session_token: return True
        self.mb.showinfo('SRPRecorder2027',self.tr('请先在窗口底部登录 GitHub 账号。','Sign in at the bottom first.'),parent=self.root)
        return False
    def on_tab_changed(self,event=None):
        try: index=self.book.index(self.book.select())
        except Exception: return
        if index==2 and not self.session_token:
            self.status.set(self.tr('请先登录后查看参考文献与资料。','Sign in to view References.'))
            self.book.select(0)
    def log_activity(self,api,login,action,detail):
        try: append_activity(api,login,action,detail)
        except Exception as error: self.status.set(self.tr('活动日志写入失败：','Activity log failed: ')+str(error))
    def view_activity(self):
        api,config=self.api()
        def work():
            login=api.login(); api.login_cached=login
            return login,read_activity(api,'logs/public/activity.json'),read_activity(api,f'logs/private/{login}/activity.json')
        def done(result):
            login,pub,mine=result
            self.remember(api,config)
            win=self.tk.Toplevel(self.root); win.title(self.tr('活动日志','Activity Log')); win.geometry('900x520'); win.transient(self.root); win.grab_set()
            self.ttk.Label(win,text=self.tr('公开日志 ','Public log ')+str(len(pub))+self.tr(' 条 · 我的日志 ',' entries · my log ')+str(len(mine))+self.tr(' 条',' entries'),padding=8).pack(anchor='w')
            tree=self.table(win,[self.tr('时间','Time'),self.tr('用户','User'),self.tr('动作','Action'),self.tr('详情','Detail')])
            rows=list(reversed(pub))+list(reversed(mine))
            for row in rows:
                tree.insert('','end',values=(row.get('time',''),row.get('user',''),row.get('action',''),row.get('detail','')))
            self.ttk.Button(win,text=self.tr('关闭','Close'),command=win.destroy).pack(pady=8)
        self.run(work,done)
    def choose_workspace(self):
        if not self.require_login(): return
        value=self.fd.askdirectory(initialdir=self.config.get('workspace',str(self.files_root)))
        if value: no_links(value); self.config['workspace']=value; atomic(self.configfile,self.config)
    def language(self):
        self.en=not self.en; self.config['language']='en' if self.en else 'zh'; atomic(self.configfile,self.config)
        for w in self.root.winfo_children(): w.destroy()
        self.build()
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
    def personal_display(self): return (self.login or self.tr('未登录','Not configured'))+'  '+self.config.get('personal','')
    def personal_folder(self,login):
        folder=Path(self.config.get('personal') or member_folder(self.files_root,login)); no_links(folder); folder.mkdir(parents=True,exist_ok=True); return folder
    def team_destination(self,me,member): return child(member_folder(self.files_root,me)/'TeamConRef',member)
    def build_team(self):
        p=self.tabs[0]; bar=self.ttk.Frame(p); bar.pack(fill='x')
        self.button(bar,'选择个人文件夹','Choose Personal Folder',self.choose_personal)
        self.button(bar,'打开个人文件夹','Open Personal Folder',self.open_personal)
        self.button(bar,'刷新成员','Refresh Members',self.team)
        self.button(bar,'打开成员文件','Open Member Files',self.member_research)
        self.button(bar,'下载并合并团队参考资料','Download Team References',self.team_references)
        self.personal_label=self.ttk.Label(p,text=self.personal_display(),wraplength=980); self.personal_label.pack(anchor='w',pady=10)
        self.members_tree=self.table(p,[self.tr('GitHub 账号','GitHub login')])
        self.members_tree.bind('<Double-1>',lambda _:self.guarded(self.member_research))
    def choose_personal(self):
        if not self.require_login(): return
        value=self.fd.askdirectory()
        if value: no_links(value); self.config['personal']=value; atomic(self.configfile,self.config); self.personal_label.configure(text=self.personal_display())
    def open_personal(self):
        if not self.login and not self.config.get('personal'): raise ValueError(self.tr('请先验证保存配置。','Validate and save the configuration first.'))
        self.open_path(self.personal_folder(self.login or 'unknown'))
    def team(self):
        api,config=self.api()
        def work():
            files=self.channels(api); login=files.identity(); api.login_cached=login
            return sorted(set([login,*files.members()]))
        def done(names):
            self.remember(api,config); self.members_tree.delete(*self.members_tree.get_children())
            for name in names: self.members_tree.insert('', 'end',values=(name,))
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
            files=self.channels(api); me=files.identity(); api.login_cached=me; destination=self.team_destination(me,member)
            if not files.download_member(member,destination): return None
            return destination
        def done(destination):
            self.remember(api,config)
            if destination: self.show_member_files(member,destination)
            else: self.status.set(self.tr('该成员尚未上传文件；旧副本保留。','No files uploaded by this member; old copies preserved.'))
        self.run(work,done)
    def show_member_files(self,member,folder):
        dialog=self.tk.Toplevel(self.root); dialog.title(member+' · SRPRecorder2027'); dialog.geometry('850x520')
        tree=self.table(dialog,[self.tr('文件','File'),self.tr('目录','Folder')]); paths={}
        for file in upload_files(folder):
            ident=tree.insert('', 'end',values=(file.name,str(file.parent.relative_to(folder)))); paths[ident]=file
        bar=self.ttk.Frame(dialog); bar.pack(fill='x')
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
        self.button(bar,'发布','Publish',self.publish_post)
        self.button(bar,'历史记录','History',self.show_post_history)
        self.button(bar,'刷新','Refresh',self.refresh_posts)
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
            try: append_activity(api,login,'发布快速发行',str(len(text))+' 字符')
            except Exception: pass
            return True
        def done(_):
            self.post_text.delete('1.0','end')
            self.finish(api,config,self.tr('已发布。','Published.'))
            self.refresh_posts()
        self.run(work,done)
    def view_post(self,post,login):
        win=self.tk.Toplevel(self.root); win.title(post['author']+' · '+post['time'])
        win.geometry('720x520'); win.transient(self.root); win.grab_set()
        box=self.tk.Text(win,wrap='word',padx=12,pady=12); box.pack(fill='both',expand=True)
        box.insert('1.0',post.get('body','')); box.configure(state='disabled')
        bar=self.ttk.Frame(win); bar.pack(fill='x',pady=6)
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
            try: append_activity(api,login,'删除快速发行',post['time'])
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
            win.geometry('820x480'); win.transient(self.root); win.grab_set()
            tree=self.table(win,[self.tr('作者','Author'),self.tr('时间','Time'),self.tr('内容','Content')])
            self._history_posts={}
            for post in seen:
                iid=tree.insert('','end',values=(post['author'],post['time'],' '.join(str(post.get('body','')).split())[:120]))
                self._history_posts[iid]=post
            tree.bind('<Double-1>',lambda e:self.open_history_item(tree,login))
            self.ttk.Button(win,text=self.tr('关闭','Close'),command=win.destroy).pack(pady=8)
        self.run(work,done)
    def open_history_item(self,tree,login):
        selection=tree.selection()
        if not selection: return
        post=self._history_posts.get(selection[0])
        if post: self.view_post(post,login)
    def build_references(self):
        p=self.tabs[2]; self.ttk.Label(p,text=self.tr('参考资料只读。默认库需登录私有团队仓库后下载；个人资料在团队同步上传。','Read-only references. Download the default library after joining the private data repository; upload personal files on Team Sync.'),wraplength=980).pack(anchor='w',pady=8)
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
        personal=self.config.get('personal')
        if personal:
            folder=Path(personal)/'references'
            if folder.is_dir():
                for path in upload_files(folder):
                    ident=self.ref_tree.insert('', 'end',values=(path.name,str(path))); self.ref_paths[ident]=path
    def open_reference(self):
        selected=self.ref_tree.selection()
        if selected: self.open_path(self.ref_paths[selected[0]])
    def build_progress(self):
        p=self.tabs[3]; self.progress=self.tk.StringVar(value=self.config['progress'])
        self.ttk.Label(p,text=self.tr('本地进度文件夹','Local Progress Folder'),font=('',14,'bold')).pack(anchor='w',pady=12)
        self.ttk.Entry(p,textvariable=self.progress,state='readonly').pack(fill='x')
        bar=self.ttk.Frame(p); bar.pack(fill='x',pady=10)
        self.button(bar,'选择文件夹','Choose Folder',self.choose_progress); self.button(bar,'新建文档','New Document',self.new_document)
        self.button(bar,'打开本地资料文件夹','Open Progress Folder',lambda:self.open_path(self.progress.get()))
        self.ttk.Label(p,text=self.tr('在此文件夹编辑文件，完成后到团队同步上传。本机自动保存不等于云端上传。','Edit files in this dedicated folder; upload them from Team Sync when ready. Local saves do not upload automatically.'),wraplength=980).pack(anchor='w',pady=12)
    def choose_progress(self):
        if not self.require_login(): return
        value=self.fd.askdirectory(initialdir=self.progress.get())
        if value: no_links(value); self.config['progress']=value; self.progress.set(value); atomic(self.configfile,self.config)
    def new_document(self):
        if not self.require_login(): return
        project=self.ask.askstring(self.tr('新建文档','New Document'),self.tr('项目名称','Project name'),parent=self.root)
        if project:
            title=str(dt.date.today())+' '+project
            target=docx(self.progress.get(),project,'',title,direct=True)
            self.open_path(target)
    def build_sync(self):
        p=self.tabs[4]
        self.ttk.Label(p,text=self.tr('已绑定仓库：','Bound repository: ')+DEFAULT_REPO+' · main',font=('',11,'bold')).pack(anchor='w')
        self.ttk.Label(p,text=self.tr('登录后在下方直接操作，无需填写任何配置。','Sign in first; no configuration needed below.'),foreground='#c47a2a').pack(anchor='w',pady=(4,10))
        downloads=self.ttk.Frame(p); downloads.pack(fill='x',pady=(8,0))
        self.button(downloads,'从 GitHub 下载他人内容','Download Team Files',self.pull_others)
        self.button(downloads,'从 GitHub 下载默认资料库','Download Default Library',self.pull_default)
        uploads=self.ttk.Frame(p); uploads.pack(fill='x')
        self.button(uploads,'上传参考资料','Upload References',self.upload_references); self.button(uploads,'上传本地进度','Upload Local Progress',self.upload_progress)
        removals=self.ttk.Frame(p); removals.pack(fill='x')
        self.button(removals,'撤除我上传的参考资料','Remove My Uploaded References',self.remove_my_references)
        self.button(removals,'查看活动日志','View Activity Log',self.view_activity)
        self.ttk.Label(p,text=self.tr('每个上传文件最大 50 MiB；不自动删除远端旧文件。下载成功后替换并保留旧副本；部分上传失败时请重试。','Maximum 50 MiB per uploaded file. Remote old files are not deleted. Downloads replace only after success and keep backups. Retry after partial upload failures.'),wraplength=980).pack(anchor='w',pady=16)
        self.ttk.Label(p,text=self.tr('个人仓库协作者使用 classic PAT 的 repo 权限。不要共享令牌。','Personal-repository collaborators use a classic PAT with repo scope. Never share tokens.'),wraplength=980).pack(anchor='w')
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
    def pull_others(self):
        api,config=self.api()
        def work():
            files=self.channels(api); me=files.identity(); api.login_cached=me; count=0
            for member in files.members():
                if member.lower()!=me.lower() and files.download_member(member,self.team_destination(me,member)): count+=1
            if count: self.log_activity(api,me,'下载他人内容',str(count)+' 位成员')
            return count
        self.run(work,lambda count:self.finish(api,config,self.tr('成员内容下载完成：','Team downloads completed: ')+str(count)))
    def pull_default(self):
        if not self.mb.askyesno('SRPRecorder2027',self.tr('下载成功后替换默认库，保留旧副本。继续？','Replace the default library after a successful download, keeping a backup?'),parent=self.root): return
        api,config=self.api()
        def work():
            self.channels(api).default_library(self.default)
            self.log_activity(api,self.session_login,'下载默认资料库','完成')
        self.run(work,lambda _:self.finish(api,config,self.tr('默认资料库已更新。','Default library updated.')))
    def team_references(self):
        api,config=self.api()
        def work():
            files=self.channels(api); me=files.identity(); api.login_cached=me; count=0
            for member in files.members():
                if files.download_member(member,self.team_destination(me,member),references_only=True): count+=1
            return count
        self.run(work,lambda count:self.finish(api,config,self.tr('团队参考资料下载完成：','Team reference downloads completed: ')+str(count)))
    def upload_progress(self):
        api,config=self.api(); folder=self.progress.get()
        def work():
            files=self.channels(api); result=files.upload_progress(folder,self.files_root); api.login_cached=files.identity()
            self.log_activity(api,files.identity(),'上传本地进度',str(result)+' 个文件')
            return result
        self.run(work,lambda count:self.finish(api,config,self.tr('本地进度已上传：','Progress files uploaded: ')+str(count)))
    def upload_references(self):
        selected=self.fd.askopenfilenames()
        if not selected: return
        api,config=self.api()
        def work():
            files=self.channels(api); login=files.identity(); api.login_cached=login
            result=files.upload_references(selected,self.personal_folder(login))
            self.log_activity(api,login,'上传参考资料',str(len(selected))+' 个文件')
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
        win.geometry('560x420'); win.transient(self.root); win.grab_set()
        self.ttk.Label(win,text=self.tr('选择要从团队区永久删除的参考资料（其他人将无法再下载）：','Select uploaded references to remove permanently from the team area:'),wraplength=520).pack(anchor='w',padx=12,pady=8)
        box=self.tk.Listbox(win,selectmode='extended',height=12); box.pack(fill='both',expand=True,padx=12)
        for name in names: box.insert('end',name)
        bar=self.ttk.Frame(win); bar.pack(fill='x',pady=8)
        def confirm():
            picked=[box.get(i) for i in box.curselection()]
            if not picked: return
            if not self.mb.askyesno('SRPRecorder2027',self.tr('将从团队区永久删除 %d 个参考资料。继续？' % len(picked),'Permanently remove %d uploaded reference(s)?' % len(picked)),parent=win): return
            win.destroy()
            def work():
                count=self.channels(api).remove_references(picked,self.personal_folder(login))
                self.log_activity(api,login,'撤除参考资料',str(count)+' 个文件：'+'、'.join(picked))
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
        bar=self.ttk.Frame(panel); bar.pack(fill='x')
        self.button(bar,'上传到 GitHub','Upload to GitHub',self.upload_help)
        self.button(bar,'读取云端版本','Load From GitHub',self.load_help)
        self.button(bar,'恢复默认','Restore Default',self.default_help)
        self.button(bar,'改动日志','Change Log',self.show_help_changelog)
        self.help_text=self.tk.Text(panel,wrap='word',padx=16,pady=16); self.help_text.pack(fill='both',expand=True)
        self.help_text.bind('<<Modified>>',self.help_modified)
        text=self.help_text
        self.help_dirty=False
        zh='SRPRecorder2027 0.1.1 公开测试版\n\n五栏与 Windows 一致：团队研究、参考资料、本地进度、团队同步、使用说明。\n\n先接受 research-data 邀请，使用自己的 classic PAT（repo）验证保存配置。浏览成员只读取；上传时才创建个人档案。\n\n本地进度以文件夹为中心；参考资料与本地进度的上传按钮会传输文件本体，每文件最大 50 MiB，不自动删除远端旧文件。上传多文件非事务，失败后可能部分成功，重试会更新同名文件。\n\n默认资料库属于私有团队，在登录后下载。下载先暂存，成功后替换并保留 .backup-* 旧副本；网络失败或空库不会清空旧资料。\n\n数据保留在 ~/Library/Application Support/ResearchProgressRecorder；新文件区为其 SRCRec 子目录。旧 0.0.2 的研究 JSON 和配置保留。升级退出后替换 .app 即可。\n\n令牌只在 macOS 钥匙串。程序公开下载不授予私有数据权限。每个仓库写入者仍可修改仓库其他目录，team/ 分隔是应用行为。\n\n关闭应用后可运行 DMG 中的卸载脚本；默认把应用移到废纸篓，研究数据与外部自选目录保留。\n\n本测试包使用临时签名，未经 Apple Developer ID 签名或公证。'
        en='SRPRecorder2027 0.1.1 Public Test\n\nFive pages match Windows: Team Research, References, Local Progress, Team Sync, Help.\n\nAccept the private research-data invitation, then validate your own classic PAT (repo scope). Refresh only reads; uploads create your profile.\n\nReferences and progress upload actual files (50 MiB per file). Remote older files are not deleted. Multi-file uploads are not transactional: failures may be partial; retry to update files.\n\nDownload the private default library after signing in. Downloads use staging and preserve .backup-* copies; failures/empty results do not erase existing files.\n\nData stays under ~/Library/Application Support/ResearchProgressRecorder. The SRCRec subfolder holds new file channels. Legacy 0.0.2 JSON and settings are preserved. Quit before replacing the .app to upgrade.\n\nTokens stay in Keychain. Public program downloads do not grant private data access. GitHub writers can edit other directories; team/ separation is an app convention.\n\nThe DMG includes an uninstall script. By default it moves the app to Trash and preserves research data and external folders.\n\nThis test app has an ad-hoc signature and is not Apple Developer ID signed or notarized.'
        self._help_zh=zh; self._help_en=en
        text.insert('1.0',en if self.en else zh); text.edit_modified(False)

    def help_modified(self,event=None):
        try:
            if self.help_text.edit_modified():
                self.help_dirty=True; self.help_text.edit_modified(False)
        except Exception: pass
    def default_help_text(self):
        return self._help_en if self.en else self._help_zh
    def default_help(self):
        self.help_text.delete('1.0','end'); self.help_text.insert('1.0',self.default_help_text()); self.help_dirty=False
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
            try: append_activity(api,login,'更新使用说明',str(len(text))+' 字符')
            except Exception: pass
            return len(text)
        self.run(work,lambda _:self.finish(api,config,self.tr('使用说明已上传，所有成员登录后都能读到。','Help uploaded for every member.')))
    def load_help(self,quiet=False):
        api,config=self.api()
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
            self.help_text.delete('1.0','end'); self.help_text.insert('1.0',text); self.help_dirty=False
            self.status.set(self.tr('已读取云端使用说明。','Help loaded from GitHub.'))
        self.run(work,done)
    def show_help_changelog(self):
        api,config=self.api()
        def work():
            rows,sha=api.get('help/changelog.json')
            return rows if isinstance(rows,list) else []
        def done(rows):
            self.remember(api,config)
            win=self.tk.Toplevel(self.root); win.title(self.tr('改动日志','Change Log')); win.geometry('760x420'); win.transient(self.root); win.grab_set()
            tree=self.table(win,[self.tr('时间','Time'),self.tr('用户','User'),self.tr('说明','Detail')])
            for row in reversed(rows):
                tree.insert('','end',values=(row.get('time',''),row.get('user',''),row.get('detail','')))
            self.ttk.Button(win,text=self.tr('关闭','Close'),command=win.destroy).pack(pady=8)
        self.run(work,done)

def main():
    import tkinter as tk
    root=tk.Tk(); root.withdraw()
    try:
        smoke='--smoke-test' in sys.argv
        if smoke:
            with tempfile.TemporaryDirectory() as tmp:
                client=Client(root,tmp,smoke=True); root.update()
                assert len(client.tabs)==6
                for index in range(6): client.book.select(index); root.update()
                client.language(); root.update()
                for index in range(6): client.book.select(index); root.update()
                root.destroy(); print('SRPRecorder2027 0.1.1: six pages and both languages passed')
        else:
            Client(root); root.deiconify(); root.mainloop()
    except Exception as error:
        if '--smoke-test' in sys.argv: raise
        from tkinter import messagebox
        messagebox.showerror('SRPRecorder2027',str(error)+'\nExisting research data was preserved.')

if __name__=='__main__': main()
