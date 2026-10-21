"""0.2.0 file channels; no credentials on disk, no destructive replacement before success."""
from __future__ import annotations
import base64
import datetime as dt
import json
import os
from pathlib import Path
import re
import shutil
import sys
import urllib.error
import urllib.parse
import uuid

MAX_FILE = 50 * 1024 * 1024

def no_links(path):
    path = Path(os.path.abspath(path))
    # macOS owns these root aliases. Canonicalize only the exact, root-owned
    # system mapping; resolving an arbitrary user symlink would hide unsafe links.
    if sys.platform == 'darwin':
        for alias in ('var', 'tmp'):
            prefix = Path('/') / alias
            if path == prefix or prefix in path.parents:
                expected = Path('/private') / alias
                if prefix.is_symlink() and prefix.lstat().st_uid == 0 and prefix.parent / os.readlink(prefix) == expected:
                    path = expected / path.relative_to(prefix)
                break
    for p in (path, *path.parents):
        if p.is_symlink() or (hasattr(p, 'is_junction') and p.is_junction()):
            raise ValueError('Linked files/folders are not supported: ' + str(p))
    return path

def safe_name(name):
    if (not isinstance(name, str) or not name or name in ('.', '..') or name != name.strip().rstrip('.')
        or re.search(r'[\x00-\x1f/\\:*?"<>|]', name)
        or re.fullmatch(r'(?i:CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])', name.split('.')[0])):
        raise ValueError('Invalid filename: ' + str(name))
    return name

def child(parent, name):
    return no_links(Path(parent) / safe_name(name))

def member_folder(root, login):
    # GitHub logins cannot include '@'; reserve that prefix for platform-reserved names.
    if not re.fullmatch(r'[A-Za-z0-9-]{1,39}', login): raise ValueError('Invalid GitHub login')
    try: safe_name(login); name = login
    except ValueError: name = '@member-' + login
    if name.lower() == 'default': name = '@' + login
    return no_links(Path(root) / 'References' / name)

def upload_files(root):
    root = no_links(root)
    if not root.is_dir(): raise ValueError('Choose a progress folder first')
    files = []
    for directory, dirs, names in os.walk(root, followlinks=False):
        no_links(directory)
        for name in dirs: child(directory, name)
        for name in sorted(names):
            p = child(directory, name)
            if not p.is_file(): raise ValueError('Not a regular file: ' + str(p))
            if p.stat().st_size > MAX_FILE: raise ValueError('File exceeds 50 MiB: ' + str(p))
            files.append(p)
    return files

def replace_download(destination, download):
    destination = no_links(destination)
    destination.parent.mkdir(parents=True, exist_ok=True)
    stage = destination.with_name(destination.name + '.download-' + uuid.uuid4().hex)
    backup = destination.with_name(destination.name + '.backup-' + dt.datetime.now(dt.timezone.utc).strftime('%Y%m%d%H%M%S') + '-' + uuid.uuid4().hex[:6])
    stage.mkdir()
    moved = False
    try:
        download(stage)
        if not upload_files(stage): raise ValueError('Remote folder is empty; previous local copy preserved')
        if destination.exists(): destination.rename(backup); moved = True
        try: stage.rename(destination)
        except Exception:
            if moved: backup.rename(destination)
            raise
        return backup if moved else None
    finally:
        if stage.exists(): no_links(stage); shutil.rmtree(stage)

def merge_references(old, selected):
    if not isinstance(old, list) or any(not isinstance(x, dict) for x in old): raise ValueError('Invalid references manifest')
    result = {x.get('Path'): x for x in old if x.get('Path')}
    result.update({x['Path']: x for x in selected})
    return list(result.values())

class FileChannels:
    def __init__(self, api): self.api = api; self._login = None
    def identity(self):
        if self._login is None:
            self.api.request('GET', '/repos/' + self.api.repo)
            self.api.request('GET', '/repos/' + self.api.repo + '/branches/' + urllib.parse.quote(self.api.branch, safe=''))
            self._login = self.api.login()
            self.api.login_cached = self._login
            member_folder(Path('.'), self._login)
        return self._login
    def listing(self, path, missing_ok=False):
        self.identity()
        try: rows = self.api.request('GET', self.api.endpoint(path) + '?ref=' + urllib.parse.quote(self.api.branch, safe=''))
        except urllib.error.HTTPError as error:
            if missing_ok and error.code == 404: return []
            raise
        if not isinstance(rows, list): raise ValueError('GitHub did not return a directory: ' + path)
        return rows
    def get_bytes(self, path):
        endpoint = self.api.endpoint(path) + '?ref=' + urllib.parse.quote(self.api.branch, safe='')
        data = self.api.request('GET', endpoint)
        if not isinstance(data, dict) or not data.get('sha'): raise ValueError('GitHub did not return a readable file')
        if data.get('encoding') == 'none': return self.api.request('GET', endpoint, raw=True, binary=True)
        if data.get('encoding') != 'base64': raise ValueError('Unsupported GitHub encoding')
        return base64.b64decode(data['content'])
    def download_dir(self, remote, destination):
        rows = self.listing(remote)
        destination = no_links(destination); destination.mkdir(parents=True, exist_ok=True)
        for row in rows:
            path = child(destination, row['name'])
            if row['type'] == 'dir': self.download_dir(remote + '/' + row['name'], path)
            elif row['type'] == 'file':
                path.write_bytes(self.get_bytes(remote + '/' + row['name']))
            else: raise ValueError('Remote links are not supported')
    def members(self): return [x['name'] for x in self.listing('team', True) if x.get('type') == 'dir']
    def download_member(self, member, destination, references_only=False):
        member_folder(Path('.'), member)
        remote = 'team/' + member
        dirs = {x['name'] for x in self.listing(remote, True) if x.get('type') == 'dir'}
        selected = [x for x in ('references',) if x in dirs] if references_only else [x for x in ('references', 'progress') if x in dirs]
        if not selected: return False
        if references_only:
            replace_download(Path(destination) / 'references', lambda stage: self.download_dir(remote + '/references', stage))
        else:
            def work(stage):
                for name in selected: self.download_dir(remote + '/' + name, stage / name)
            replace_download(destination, work)
        return True
    def default_library(self, destination):
        replace_download(destination, lambda stage: self.download_dir('default', stage))
    def ensure_profile(self):
        login = self.identity(); path = 'team/' + login + '/profile.json'
        profile, sha = self.api.get(path)
        if profile is None:
            try: self.api.put(path, {'login': login, 'joined': dt.datetime.now(dt.timezone.utc).isoformat()}, None)
            except urllib.error.HTTPError as error:
                if error.code not in (409, 422): raise
        return login
    def upload_bytes(self, path, content):
        if len(content) > MAX_FILE: raise ValueError('File exceeds 50 MiB')
        for attempt in range(3):
            try: data = self.api.request('GET', self.api.endpoint(path) + '?ref=' + urllib.parse.quote(self.api.branch, safe='')); sha = data['sha']
            except urllib.error.HTTPError as error:
                if error.code != 404: raise
                sha = None
            body = {'message': 'upload: SRPRecorder2027 0.2.0', 'branch': self.api.branch, 'content': base64.b64encode(content).decode()}
            if sha: body['sha'] = sha
            try: self.api.request('PUT', self.api.endpoint(path), body); return
            except urllib.error.HTTPError as error:
                if attempt == 2 or error.code not in (409,422): raise
    def remote_references(self):
        login = self.identity()
        rows = self.listing('team/' + login + '/references', True)
        return login, {row['name']: row['sha'] for row in rows if row.get('type') == 'file' and row.get('sha')}
    def remove_references(self, names, personal_folder=None):
        login = self.identity(); names = [safe_name(n) for n in names]; count = 0
        for name in names:
            path = 'team/' + login + '/references/' + name
            data = self.api.request('GET', self.api.endpoint(path) + '?ref=' + urllib.parse.quote(self.api.branch, safe=''))
            body = {'message': 'remove: SRPRecorder2027 0.2.0', 'branch': self.api.branch, 'sha': data['sha']}
            self.api.request('DELETE', self.api.endpoint(path), body)
            if personal_folder:
                local = Path(personal_folder) / 'references' / name
                try:
                    if local.is_file(): local.unlink()
                except OSError:
                    pass
            count += 1
        manifest = 'team/' + login + '/references.json'
        for attempt in range(3):
            old, sha = self.api.get(manifest)
            if old is None: return count
            gone = {n.casefold() for n in names}
            rows = [x for x in old if not (x and (str(x.get('RelPath') or '').casefold() in gone or Path(str(x.get('Path') or '')).name.casefold() in gone))]
            try: self.api.put(manifest, rows, sha); return count
            except urllib.error.HTTPError as error:
                if attempt == 2 or error.code not in (409,422): raise
        return count
    def upload_progress(self, folder, protected_root):
        folder = no_links(folder); protected_root = no_links(protected_root)
        if folder == protected_root or folder in protected_root.parents: raise ValueError('Choose a dedicated progress folder, not the application data root or its parents')
        files = upload_files(folder)
        if not files: return 0
        login = self.ensure_profile()
        for file in files: self.upload_bytes('team/' + login + '/progress/' + file.relative_to(folder).as_posix(), file.read_bytes())
        return len(files)
    def upload_references(self, files, personal_folder):
        files = [no_links(x) for x in files]; names = set()
        for file in files:
            name = safe_name(file.name)
            if name.casefold() in names: raise ValueError('Selected files have duplicate names; rename them first')
            names.add(name.casefold())
            if file.stat().st_size > MAX_FILE: raise ValueError('File exceeds 50 MiB: ' + str(file))
        if not files: return []
        login = self.ensure_profile(); copies = no_links(Path(personal_folder) / 'references'); copies.mkdir(parents=True, exist_ok=True)
        selected = []
        for file in files:
            target = child(copies, file.name)
            if file != target: shutil.copy2(file, target)
            remote = 'team/' + login + '/references/' + file.name
            self.upload_bytes(remote, file.read_bytes())
            selected.append(dict(Title=file.name, Path=remote, RelPath=file.name, Kind='file', Personal=True))
        manifest = 'team/' + login + '/references.json'
        for attempt in range(3):
            old, sha = self.api.get(manifest)
            rows = merge_references(old if old is not None else [], selected)
            try: self.api.put(manifest, rows, sha); return rows
            except urllib.error.HTTPError as error:
                if attempt == 2 or error.code not in (409,422): raise
