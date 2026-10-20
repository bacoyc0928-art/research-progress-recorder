"""Research Progress Recorder 0.0.2. Python stdlib; packaged as a macOS .app in CI."""
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

VERSION = '0.0.2'
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
    def request(self, method, endpoint, data=None, raw=False):
        url = 'https://api.github.com' + endpoint
        req = urllib.request.Request(url, data=None if data is None else json.dumps(data).encode(), method=method,
            headers={'Authorization': 'Bearer '+self.token, 'User-Agent': APP_ID,
                     'Accept': 'application/vnd.github.raw+json' if raw else 'application/vnd.github+json',
                     'X-GitHub-Api-Version': '2022-11-28', 'Content-Type': 'application/json'})
        context=ssl.create_default_context()
        if sys.platform=='darwin' and Path('/etc/ssl/cert.pem').exists():
            context.load_verify_locations(cafile='/etc/ssl/cert.pem')
        with urllib.request.urlopen(req, timeout=25, context=context) as r:
            text = r.read(30_000_000).decode('utf-8-sig')
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

def docx(folder, project, category, title):
    folder = Path(folder)/'本地文档'; folder.mkdir(exist_ok=True)
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

class Client:
    def __init__(self, root, home=None, smoke=False):
        import tkinter as tk
        from tkinter import ttk, filedialog, messagebox, simpledialog
        self.tk,self.ttk,self.fd,self.mb,self.ask=tk,ttk,filedialog,messagebox,simpledialog
        self.root=root; self.home=Path(home or Path.home()/'Library'/'Application Support'/APP_ID)
        self.home.mkdir(parents=True,exist_ok=True); self.datafile=self.home/'research_log.json'; self.configfile=self.home/'config.json'
        self.config=json.loads(self.configfile.read_text()) if self.configfile.exists() else {}
        self.data=normalize(json.loads(self.datafile.read_text(encoding='utf-8-sig'))) if self.datafile.exists() else empty()
        self.en=self.config.get('language','zh')=='en'; self.busy=False; self.events=queue.Queue(); self.keychain=None
        self.workspace=self.config.get('workspace',''); self.entry_id=None; self.pending=None; self.smoke=smoke
        if not smoke and (not self.workspace or not Path(self.workspace).is_dir()):
            self.workspace=self.fd.askdirectory(title='选择工作文件夹 / Choose Workspace')
            if not self.workspace: root.destroy(); raise SystemExit(0)
        self.config['workspace']=self.workspace; atomic(self.configfile,self.config)
        root.title('Research Progress Recorder · '+VERSION); root.geometry('1120x760'); root.minsize(940,640)
        self.build(); root.protocol('WM_DELETE_WINDOW',self.close); root.after(100,self.poll)
    def tr(self,zh,en): return en if self.en else zh
    def button(self,parent,zh,en,command):
        b=self.ttk.Button(parent,text=self.tr(zh,en),command=lambda:self.guarded(command)); b.pack(side='left',padx=4,pady=5); return b
    def guarded(self,fn):
        if self.busy: return
        try: fn()
        except Exception as e: self.mb.showerror('Error',str(e),parent=self.root)
    def close(self):
        if self.busy: self.mb.showinfo('Sync','Please wait for the current operation.'); return
        try: self.save_entry()
        except Exception as e: self.mb.showerror('Error',str(e)); return
        self.root.destroy()
    def persist(self): atomic(self.datafile,self.data)
    def poll(self):
        try:
            fn,result,error=self.events.get_nowait(); self.busy=False
            for w,state in self.disabled:
                try: w.configure(state=state)
                except Exception: pass
            if error: self.mb.showerror('Error',str(error)); self.status.set('Failed: '+str(error))
            else:
                try: fn(result)
                except Exception as e: self.mb.showerror('Error',str(e))
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
            try: result=work(); self.events.put((done,result,None))
            except Exception as e: self.events.put((done,None,e))
        threading.Thread(target=job,daemon=True).start()
    def build(self):
        tk,ttk=self.tk,self.ttk
        header=ttk.Frame(self.root,padding=12); header.pack(fill='x')
        ttk.Label(header,text='Research Progress Recorder  '+VERSION,font=('',17,'bold')).pack(side='left')
        self.button(header,'更换工作文件夹','Change Workspace',self.choose_workspace)
        self.button(header,'EN / 中文','中文 / EN',self.language)
        self.book=ttk.Notebook(self.root); self.book.pack(fill='both',expand=True,padx=12,pady=6)
        self.tabs=[]
        for zh,en in [('项目概览','Overview'),('团队研究','Team Research'),('参考文献与资料','References & Categories'),('本地进度','Local Progress'),('团队同步','Team Sync'),('使用说明','Help')]:
            p=ttk.Frame(self.book,padding=12); self.book.add(p,text=self.tr(zh,en)); self.tabs.append(p)
        self.status=tk.StringVar(value=self.workspace); ttk.Label(self.root,textvariable=self.status,padding=8).pack(fill='x')
        self.overview=ttk.Label(self.tabs[0],font=('',15),justify='left'); self.overview.pack(anchor='nw',padx=12,pady=20)
        self.build_team(); self.build_refs(); self.build_local(); self.build_sync(); self.build_help(); self.refresh()
    def choose_workspace(self):
        folder=self.fd.askdirectory(initialdir=self.workspace or str(Path.home()))
        if folder:
            self.workspace=folder; self.config['workspace']=folder; atomic(self.configfile,self.config); self.status.set(folder)
    def language(self):
        self.save_entry(); self.en=not self.en; self.config['language']='en' if self.en else 'zh'; atomic(self.configfile,self.config)
        for w in self.root.winfo_children(): w.destroy()
        self.build()
    def tree(self,parent,columns):
        frame=self.ttk.Frame(parent); frame.pack(fill='both',expand=True)
        tree=self.ttk.Treeview(frame,columns=columns,show='headings',selectmode='browse')
        for c in columns: tree.heading(c,text=c); tree.column(c,width=160,stretch=True)
        tree.pack(side='left',fill='both',expand=True)
        sb=self.ttk.Scrollbar(frame,command=tree.yview); sb.pack(side='right',fill='y'); tree.configure(yscrollcommand=sb.set)
        return tree
    def selected(self,tree,items):
        sel=tree.selection()
        if not sel: return None
        return items[int(sel[0])]
    def fill(self,tree,rows):
        tree.delete(*tree.get_children())
        for i,row in enumerate(rows): tree.insert('','end',iid=str(i),values=row)
    def build_refs(self):
        nb=self.ttk.Notebook(self.tabs[2]); nb.pack(fill='both',expand=True)
        refs=self.ttk.Frame(nb); cats=self.ttk.Frame(nb); nb.add(refs,text=self.tr('参考资料','References')); nb.add(cats,text=self.tr('研究类别','Categories'))
        bar=self.ttk.Frame(refs); bar.pack(fill='x')
        self.button(bar,'添加文件','Add File',lambda:self.add_ref(False)); self.button(bar,'添加文件夹','Add Folder',lambda:self.add_ref(True))
        self.button(bar,'打开','Open',lambda:self.open_selected(self.ref_tree,self.data['Refs']))
        self.button(bar,'删除到废纸篓','Move to Trash',self.remove_ref)
        self.ref_tree=self.tree(refs,('Title','Path')); self.ref_tree.bind('<Double-1>',lambda _:self.guarded(lambda:self.open_selected(self.ref_tree,self.data['Refs'])))
        bar=self.ttk.Frame(cats); bar.pack(fill='x'); self.button(bar,'添加类别','Add Category',lambda:self.edit_category(None))
        self.button(bar,'编辑','Edit',lambda:self.edit_category(self.selected(self.cat_tree,self.data['Categories'])))
        self.button(bar,'删除','Delete',self.delete_category)
        self.cat_tree=self.tree(cats,('Category','Tasks','Hours','Links'))
        self.cat_tree.bind('<Double-1>',lambda _:self.guarded(lambda:self.edit_category(self.selected(self.cat_tree,self.data['Categories']))))
    def add_ref(self,folder):
        paths=[self.fd.askdirectory()] if folder else self.fd.askopenfilenames()
        for p in paths:
            if not p or any(r.get('Path')==p for r in self.data['Refs']): continue
            rel=os.path.relpath(p,self.workspace) if within(p,self.workspace) else ''
            self.data['Refs'].append(dict(Title=Path(p).stem,Path=p,RelPath=rel,Kind='folder' if folder else Path(p).suffix.lstrip('.')))
        self.persist(); self.refresh()
    def resolve(self,item):
        p=Path(item.get('Path') or '')
        if item.get('Path') and p.exists(): return p
        if item.get('RelPath'):
            rel=Path(self.workspace)/item['RelPath'].replace('\\','/')
            if rel.exists() and within(rel,self.workspace): return rel
        return None
    def open_selected(self,tree,items):
        item=self.selected(tree,items)
        if not item: return
        p=self.resolve(item)
        if not p:
            value=self.fd.askopenfilename(title='Locate file')
            if not value: return
            item['Path']=value; p=Path(value); self.persist(); self.refresh()
        if sys.platform=='darwin': subprocess.Popen(['open',str(p)])
        elif sys.platform=='win32': os.startfile(p)
    def trash(self,p):
        p=Path(p)
        if within(self.workspace,p) or within(self.home,p) or p.resolve()==Path.home().resolve(): raise ValueError('Cannot delete workspace, application data or their parent folders')
        if p.is_symlink(): raise ValueError('Linked folders cannot be deleted here')
        trash=Path.home()/'.Trash'; trash.mkdir(exist_ok=True); dest=trash/p.name
        if dest.exists(): dest=trash/(p.stem+'-'+uid()[:8]+p.suffix)
        p.rename(dest)
    def remove_ref(self):
        r=self.selected(self.ref_tree,self.data['Refs'])
        if not r or not self.mb.askyesno('Trash',self.tr('将此文件/文件夹移到废纸篓？\n','Move this file/folder to Trash?\n')+str(r.get('Path'))): return
        p=self.resolve(r)
        if p: self.trash(p)
        self.data['Refs'].remove(r); self.persist(); self.refresh()
    def edit_category(self,c):
        c=copy.deepcopy(c) if c else dict(Id=uid(),Name='',TaskTotal=0,TaskDone=0,Hours=0,Links=[])
        dialog=self.tk.Toplevel(self.root); dialog.title(self.tr('研究类别','Category')); dialog.geometry('610x460'); dialog.transient(self.root); dialog.grab_set()
        fields={}
        for label,key in [('Name','Name'),('Total tasks','TaskTotal'),('Done tasks','TaskDone'),('Estimated hours','Hours')]:
            self.ttk.Label(dialog,text=label).pack(anchor='w',padx=12)
            value=self.tk.StringVar(value=str(c.get(key,''))); fields[key]=value; self.ttk.Entry(dialog,textvariable=value).pack(fill='x',padx=12)
        self.ttk.Label(dialog,text=self.tr('附件路径或网址，每行一个','Attachment paths or URLs, one per line')).pack(anchor='w',padx=12,pady=8)
        text=self.tk.Text(dialog,height=7); text.pack(fill='both',expand=True,padx=12); text.insert('1.0','\n'.join(x.get('Url','') for x in c.get('Links',[])))
        def save():
            name=fields['Name'].get().strip()
            if not name: raise ValueError('Category name is required')
            if any(x['Name']==name and x['Id']!=c['Id'] for x in self.data['Categories']): raise ValueError('Category name already exists')
            total=int(fields['TaskTotal'].get()); done=int(fields['TaskDone'].get()); hours=float(fields['Hours'].get())
            if not 0<=done<=total<=10000 or not 0<=hours<=100000: raise ValueError('Tasks: 0 <= done <= total <= 10000; hours: 0..100000')
            old=next((x for x in self.data['Categories'] if x['Id']==c['Id']),None)
            if old:
                for e in self.data['Entries']+self.data['Documents']:
                    if e.get('Category')==old['Name']: e['Category']=name; e['Updated']=now()
                self.data['Categories'].remove(old)
            c.update(Name=name,TaskTotal=total,TaskDone=done,Hours=hours,Updated=now(),Links=[dict(Title=Path(p).name,Url=p,Kind='url' if p.startswith(('https://','http://')) else 'file') for p in text.get('1.0','end').strip().splitlines() if p.strip()])
            self.data['Categories'].append(c); self.persist(); dialog.destroy(); self.refresh()
        self.button(dialog,'保存','Save',save)
    def delete_category(self):
        c=self.selected(self.cat_tree,self.data['Categories'])
        if not c or not self.mb.askyesno('Delete',self.tr('删除类别？保留研究记录。','Delete category? Research entries will be kept.')): return
        self.data['DeletedCategories'].append(dict(Id=c['Id'],At=now())); self.data['Categories'].remove(c)
        for e in self.data['Entries']+self.data['Documents']:
            if e.get('Category')==c['Name']: e['Category']=''; e['Updated']=now()
        self.persist(); self.refresh()
    def build_local(self):
        tk,ttk=self.tk,self.ttk
        nb=ttk.Notebook(self.tabs[3]); nb.pack(fill='both',expand=True)
        self.logtab=ttk.Frame(nb); docs=ttk.Frame(nb); files=ttk.Frame(nb)
        for p,zh,en in [(self.logtab,'研究日志','Research Log'),(docs,'本地文档','Local Docs'),(files,'本地资料','Local Materials')]: nb.add(p,text=self.tr(zh,en))
        left=ttk.Frame(self.logtab,width=300); left.pack(side='left',fill='y'); editor=ttk.Frame(self.logtab,padding=8); editor.pack(side='left',fill='both',expand=True)
        self.query=tk.StringVar(); search=ttk.Entry(left,textvariable=self.query); search.pack(fill='x'); self.query.trace_add('write',lambda *_:self.refresh_entries())
        self.button(left,'新建记录','New Entry',self.new_entry); self.entries=self.tree(left,('Date','Title')); self.entries.bind('<<TreeviewSelect>>',self.select_entry)
        self.fields={}
        for key in ['Title','Date','Category','Status','Progress']:
            ttk.Label(editor,text=key).pack(anchor='w'); value=tk.StringVar(); self.fields[key]=value
            if key in ('Category','Status'):
                control=ttk.Combobox(editor,textvariable=value,state='readonly',values=['','进行中','已完成','暂停'] if key=='Status' else [''])
                if key=='Category': self.entry_categories=control
            else: control=ttk.Entry(editor,textvariable=value)
            control.pack(fill='x'); control.bind('<KeyRelease>',self.schedule); control.bind('<<ComboboxSelected>>',self.schedule)
        self.notes=tk.Text(editor,height=8,undo=True); self.notes.pack(fill='both',expand=True,pady=6); self.notes.bind('<KeyRelease>',self.schedule)
        bar=ttk.Frame(editor); bar.pack(fill='x'); self.button(bar,'保存','Save',self.save_entry); self.button(bar,'删除记录','Delete Entry',self.delete_entry)
        bar=ttk.Frame(docs); bar.pack(fill='x'); self.button(bar,'新建 Word 文档','New Word Document',self.new_doc); self.button(bar,'打开','Open',lambda:self.open_selected(self.docs,self.data['Documents'])); self.button(bar,'删除到废纸篓','Move to Trash',self.delete_doc)
        self.docs=self.tree(docs,('Name','Category','Path'))
        bar=ttk.Frame(files); bar.pack(fill='x'); self.button(bar,'选择文件夹','Choose Folder',self.choose_local); self.button(bar,'补录文件','Add Files',self.copy_local); self.button(bar,'打开','Open',self.open_local)
        self.local=self.tree(files,('Name','Path'))
    def schedule(self,_=None):
        if self.pending: self.root.after_cancel(self.pending)
        self.pending=self.root.after(700,lambda:self.guarded(self.save_entry))
    def select_entry(self,_=None):
        if self.busy: return
        selection=self.entries.selection()
        if not selection: return
        ident=selection[0]
        if ident==self.entry_id: return
        self.save_entry(); self.entry_id=ident; self.load_entry()
    def load_entry(self):
        e=next((e for e in self.data['Entries'] if e['Id']==self.entry_id),{})
        for key,value in self.fields.items(): value.set(str(e.get(key,'')))
        self.notes.delete('1.0','end'); self.notes.insert('1.0',e.get('Content',''))
    def save_entry(self):
        if self.pending: self.root.after_cancel(self.pending); self.pending=None
        e=next((e for e in self.data['Entries'] if e['Id']==self.entry_id),None)
        if e is None: return
        values={k:v.get() for k,v in self.fields.items()}; values['Progress']=int(values['Progress'] or 0)
        dt.date.fromisoformat(values['Date'])
        if not 0<=values['Progress']<=100: raise ValueError('Progress must be 0..100')
        values['Content']=self.notes.get('1.0','end-1c')
        if any(e.get(k)!=v for k,v in values.items()): e.update(values,Updated=now()); self.persist(); self.status.set(self.tr('已保存','Saved'))
    def new_entry(self):
        self.save_entry(); e=dict(Id=uid(),Title=self.tr('新记录','New Entry'),Date=str(dt.date.today()),Category='',Status='进行中',Progress=0,Content='',Updated=now())
        self.data['Entries'].insert(0,e); self.entry_id=e['Id']; self.query.set(''); self.persist(); self.refresh(); self.load_entry()
    def delete_entry(self):
        e=next((e for e in self.data['Entries'] if e['Id']==self.entry_id),None)
        if not e or not self.mb.askyesno('Delete',self.tr('删除此记录？删除会同步给团队。','Delete this entry? Deletion will sync to the team.')): return
        if self.pending: self.root.after_cancel(self.pending); self.pending=None
        self.data['DeletedEntries'].append(dict(Id=e['Id'],At=now())); self.data['Entries'].remove(e); self.entry_id=None; self.persist(); self.refresh(); self.load_entry()
    def refresh_entries(self):
        if not hasattr(self,'entries'): return
        q=self.query.get().lower(); self.entries.delete(*self.entries.get_children())
        for e in self.data['Entries']:
            if q in (str(e.get('Title',''))+' '+str(e.get('Content',''))).lower(): self.entries.insert('','end',iid=e['Id'],values=(e.get('Date',''),e.get('Title','')))
    def new_doc(self):
        title=self.ask.askstring('Word',self.tr('项目名称','Project name'))
        if not title: return
        category=self.fields['Category'].get(); name=f'{dt.date.today()} {title}'; path=docx(self.workspace,title,category,name)
        self.data['Documents'].append(dict(Id=uid(),Name=name,Path=path,Category=category,Updated=now()))
        for c in self.data['Categories']:
            if c['Name']==category: c['Links'].append(dict(Title=name,Url=path,Kind='file')); c['Updated']=now()
        self.persist(); self.refresh()
    def delete_doc(self):
        d=self.selected(self.docs,self.data['Documents'])
        if not d or not self.mb.askyesno('Trash',self.tr('将文档移到废纸篓？','Move document to Trash?')): return
        p=self.resolve(d)
        if p: self.trash(p)
        self.data['Documents'].remove(d)
        for c in self.data['Categories']:
            old=c['Links']; c['Links']=[l for l in old if l.get('Url')!=d.get('Path')]
            if c['Links']!=old: c['Updated']=now()
        self.persist(); self.refresh()
    def choose_local(self):
        f=self.fd.askdirectory()
        if f: self.config['localrefs']=f; atomic(self.configfile,self.config); self.refresh_local()
    def refresh_local(self):
        f=Path(self.config.get('localrefs','/nonexistent')); self.local_paths=sorted(x for x in f.iterdir() if x.is_file()) if f.is_dir() else []
        self.fill(self.local,[(p.name,str(p)) for p in self.local_paths])
    def copy_local(self):
        import shutil
        f=Path(self.config.get('localrefs','/nonexistent'))
        if not f.is_dir(): self.choose_local(); return
        for p in self.fd.askopenfilenames():
            dest=f/Path(p).name
            if dest.resolve()==Path(p).resolve(): continue
            if dest.exists() and not self.mb.askyesno('Replace',str(dest)): continue
            shutil.copy2(p,dest)
        self.refresh_local()
    def open_local(self):
        sel=self.local.selection()
        if sel and sys.platform=='darwin': subprocess.Popen(['open',str(self.local_paths[int(sel[0])])])
    def build_sync(self):
        p=self.tabs[4]; self.sync_fields={}
        for key,default in [('repo',DEFAULT_REPO),('branch','main'),('path','sync/research_log.json')]:
            self.ttk.Label(p,text=key).pack(anchor='w'); v=self.tk.StringVar(value=self.config.get(key,default)); self.sync_fields[key]=v; self.ttk.Entry(p,textvariable=v).pack(fill='x')
        self.ttk.Label(p,text=self.tr('个人 GitHub 令牌（保存在 macOS 钥匙串）','Your GitHub token (stored in macOS Keychain)')).pack(anchor='w',pady=(12,0))
        self.token=self.tk.StringVar(); self.ttk.Entry(p,textvariable=self.token,show='●').pack(fill='x')
        self.ttk.Label(p,text=self.tr('留空使用钥匙串内的令牌；浏览器登录不会自动授权此应用。','Leave blank to use Keychain; browser sign-in does not authenticate this app.')).pack(anchor='w')
        self.scope={k:self.tk.BooleanVar(value=k in self.config.get('scope',['entries','categories'])) for k in ['entries','categories']}
        bar=self.ttk.Frame(p); bar.pack(fill='x')
        for k,v in self.scope.items(): self.ttk.Checkbutton(bar,text=k,variable=v).pack(side='left',padx=6,pady=10)
        bar=self.ttk.Frame(p); bar.pack(fill='x'); self.button(bar,'验证并保存配置','Validate & Save',self.save_sync); self.button(bar,'从 GitHub 合并','Pull',lambda:self.sync(False)); self.button(bar,'一键同步','Sync All',lambda:self.sync(True)); self.button(bar,'改动记录','History',self.history)
        bar=self.ttk.Frame(p); bar.pack(fill='x'); self.button(bar,'上传参考资料','Upload References',lambda:self.upload(False)); self.button(bar,'上传个人研究','Upload My Research',lambda:self.upload(True))
        self.ttk.Label(p,text=self.tr('共享记录和类别；个人清单写入 team/<登录名>/。文件本身不上传。','Shared records/categories; personal metadata goes to team/<login>/. Files themselves are not uploaded.'),wraplength=860).pack(anchor='w',pady=16)
    def api(self):
        config={k:v.get().strip() for k,v in self.sync_fields.items()}
        if not self.keychain: self.keychain=Keychain()
        token=self.token.get().strip() or self.keychain.load()
        return GitHub(config,token), config
    def save_api(self,api,config):
        self.keychain.save(api.token); self.token.set(''); self.config.update(config); self.config['scope']=[k for k,v in self.scope.items() if v.get()]; atomic(self.configfile,self.config)
    def save_sync(self):
        api,c=self.api()
        self.run(lambda:api.request('GET','/repos/'+api.repo),lambda _: (self.save_api(api,c),self.status.set('Configuration saved')))
    def sync(self,upload):
        self.save_entry(); api,c=self.api(); scope=[k for k,v in self.scope.items() if v.get()]
        if not scope: raise ValueError('Select records or categories')
        snapshot=copy.deepcopy(self.data)
        def done(result):
            self.data,note=result; self.persist(); self.save_api(api,c); self.refresh(); self.load_entry(); self.status.set(note)
        self.run(lambda:api.sync(snapshot,scope,c.get('path') or 'sync/research_log.json',upload),done)
    def upload(self,research):
        self.save_entry(); api,c=self.api(); snapshot=copy.deepcopy(self.data); folder=self.config.get('personal','')
        self.run(lambda:api.personal(snapshot,folder,research),lambda note:(self.save_api(api,c),self.status.set(note)))
    def history(self):
        api,_=self.api()
        self.run(lambda:api.get('sync/history.json')[0],lambda rows:self.mb.showinfo('History','\n'.join(f"{x.get('time')} · {x.get('user')} · {x.get('scope')}" for x in (rows or [])[-30:]) or 'No history'))
    def build_team(self):
        p=self.tabs[1]; bar=self.ttk.Frame(p); bar.pack(fill='x'); self.button(bar,'选择个人文件夹','Choose Personal Folder',self.choose_personal); self.button(bar,'刷新成员','Refresh Members',self.team); self.button(bar,'查看成员研究','View Member Research',self.member_research); self.button(bar,'合并团队参考资料','Merge Team References',self.team_refs)
        self.personal_label=self.ttk.Label(p,text=self.config.get('personal','')); self.personal_label.pack(anchor='w',pady=12)
        self.members_tree=self.tree(p,('GitHub login',))
    def choose_personal(self):
        value=self.fd.askdirectory()
        if value: self.config['personal']=value; atomic(self.configfile,self.config); self.personal_label.configure(text=value)
    def team(self):
        api,_=self.api()
        def work():
            login=api.login(); profile,sha=api.get(f'team/{login}/profile.json')
            if profile is None:
                try: api.put(f'team/{login}/profile.json',dict(login=login,joined=now()),None)
                except urllib.error.HTTPError as e:
                    if e.code not in (409,422): raise
            return api.members()
        self.run(work,lambda names:self.fill(self.members_tree,[(x,) for x in names]))
    def member_research(self):
        sel=self.members_tree.selection()
        if not sel: return
        login=self.members_tree.item(sel[0],'values')[0]; api,_=self.api()
        self.run(lambda:api.get(f'team/{login}/research.json')[0],lambda rows:self.mb.showinfo(login,'\n\n'.join(str(x.get('Date',''))+' '+str(x.get('Title',''))+'\n'+str(x.get('Content',''))[:1000] for x in (rows or [])[:30]) or 'No personal research uploaded'))
    def team_refs(self):
        api,_=self.api()
        def work():
            refs=[]
            for m in api.members():
                rows,_=api.get(f'team/{m}/references.json')
                if isinstance(rows,list): refs.extend(x for x in rows if isinstance(x,dict) and x.get('Path'))
            return refs
        def done(rows):
            seen={x.get('Path') for x in self.data['Refs']}; added=0
            for r in rows:
                if r['Path'] not in seen: self.data['Refs'].append(r); seen.add(r['Path']); added+=1
            self.persist(); self.refresh(); self.status.set(f'Added {added} references (metadata only)')
        self.run(work,done)
    def build_help(self):
        text=self.tk.Text(self.tabs[5],wrap='word',padx=16,pady=16); text.pack(fill='both',expand=True)
        zh='首次启动选择工作文件夹；本机记录保存在用户 Library/Application Support/ResearchProgressRecorder，自动保留 .bak 备份。\n\n项目概览汇总记录与类别。参考资料可添加、打开、移到废纸篓；类别支持任务数、工时与附件路径/链接。\n\n本地进度支持研究日志自动保存、搜索和删除；删除记录会随团队同步传播。创建 Word 文档后可在本机编辑；文档和附件本身不会上传。\n\n团队同步：先被邀请加入私有数据仓库，再填自己的 GitHub 令牌。令牌只保存在 macOS 钥匙串。个人仓库协作者目前使用 classic PAT 的 repo 权限；令牌不要分享。\n\n一键同步按勾选合并记录/类别后上传，保留未勾选的远端数据；同时修改同一记录时较新时间优先，删除标记防止记录复活。请保持电脑时钟准确。\n\n团队研究：选定个人文件夹后，可将参考资料或研究/文档清单上传到 team/<GitHub用户名>/；可查看成员研究并合并参考资料。该目录分隔是应用行为，拥有仓库写权限的人仍能通过 GitHub 修改其他目录。\n\n跨平台本地路径可能无效，打开时重新定位文件。同步为手动触发，无后台服务。Mac 版尚未完成第二台真实设备的团队验收。'
        en='Choose a workspace on first launch. Local records are stored under Library/Application Support/ResearchProgressRecorder, with .bak recovery copies.\n\nReferences can be added, opened or moved to Trash. Categories support tasks, estimated hours and attachment paths/URLs.\n\nResearch Log supports auto-save, search and deletion. Entry deletions propagate through sync. Word documents are created locally. Document and attachment files are not uploaded.\n\nJoin the private data repository first. Enter your own GitHub token; it is stored only in macOS Keychain. Personal-repository collaborators currently need a classic PAT with repo scope. Never share tokens.\n\nSync All merges selected records/categories before upload, preserves unselected remote data and retries conflicts. Newer timestamps win on edits; deletion markers prevent resurrection. Keep device clocks accurate.\n\nChoose a personal folder, then upload reference metadata or research/document lists under team/<GitHub login>/. View member research and merge reference metadata on Team Research. Folder separation is enforced by the app, not a GitHub permission boundary. Repository writers can edit other folders through GitHub.\n\nPaths from other devices may need relocation. Sync is manual. A second real-device team acceptance test is still required.'
        text.insert('1.0',en if self.en else zh); text.configure(state='disabled')
    def refresh(self):
        if not hasattr(self,'ref_tree'): return
        self.fill(self.ref_tree,[(r.get('Title',''),r.get('Path','')) for r in self.data['Refs']])
        self.fill(self.cat_tree,[(c.get('Name',''),str(c.get('TaskDone',0))+'/'+str(c.get('TaskTotal',0)),c.get('Hours',0),len(c.get('Links',[]))) for c in self.data['Categories']])
        self.fill(self.docs,[(d.get('Name',''),d.get('Category',''),d.get('Path','')) for d in self.data['Documents']])
        self.entry_categories.configure(values=['']+[c.get('Name','') for c in self.data['Categories']]); self.refresh_entries(); self.refresh_local()
        total=sum(c.get('TaskTotal',0) for c in self.data['Categories']); done=sum(c.get('TaskDone',0) for c in self.data['Categories'])
        self.overview.configure(text=self.tr('研究记录','Research entries')+f": {len(self.data['Entries'])}\n\n"+self.tr('类别','Categories')+f": {len(self.data['Categories'])}\n\n"+self.tr('任务进度','Tasks')+f': {done}/{total}\n\n'+self.tr('最近更新','Recent updates')+'\n'+'\n'.join(e.get('Date','')+'  '+e.get('Title','') for e in sorted(self.data['Entries'],key=lambda e:stamp(e.get('Updated')),reverse=True)[:8]))

def main():
    import tkinter as tk
    root=tk.Tk(); root.withdraw()
    try:
        smoke='--smoke-test' in sys.argv
        if smoke:
            with tempfile.TemporaryDirectory() as tmp:
                client=Client(root,tmp,smoke=True); root.update(); client.en=True
                for w in root.winfo_children(): w.destroy()
                client.build(); root.update(); root.destroy(); print('macOS UI smoke test passed')
        else:
            Client(root); root.deiconify(); root.mainloop()
    except Exception as e:
        if '--smoke-test' in sys.argv: raise
        from tkinter import messagebox
        messagebox.showerror('Research Progress Recorder',str(e)+'\nOriginal data was preserved. Restore research_log.json.bak if needed.')

if __name__=='__main__': main()
