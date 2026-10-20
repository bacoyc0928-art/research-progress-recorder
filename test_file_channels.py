import base64
import json
from pathlib import Path
import tempfile
import unittest
import urllib.error
import urllib.parse
import file_channels as f

class Fake:
    repo='owner/research-data'; branch='main'
    def __init__(self): self.files={}; self.calls=[]; self.conflict=False; self.force_raw=set()
    def endpoint(self,path): return '/repos/'+self.repo+'/contents/'+urllib.parse.quote(path,safe='/')
    def login(self): return 'me'
    def get(self,path):
        if path not in self.files: return None,None
        return json.loads(self.files[path]), 'sha'
    def put(self,path,data,sha):
        if self.conflict: self.conflict=False; raise urllib.error.HTTPError('',409,'conflict',{},None)
        self.files[path]=json.dumps(data).encode()
    def request(self,method,endpoint,data=None,raw=False,binary=False):
        self.calls.append((method,endpoint))
        if '/contents/' not in endpoint: return {}
        path=urllib.parse.unquote(endpoint.split('/contents/',1)[1].split('?',1)[0])
        if method=='PUT':
            if self.conflict: self.conflict=False; raise urllib.error.HTTPError('',409,'conflict',{},None)
            self.files[path]=base64.b64decode(data['content']); return {}
        if path in self.files:
            if raw: return self.files[path] if binary else self.files[path].decode()
            return dict(sha='sha',encoding='none' if path in self.force_raw else 'base64',content=base64.b64encode(self.files[path]).decode())
        rows={}
        for file in self.files:
            if file.startswith(path+'/'):
                suffix=file[len(path)+1:]; name=suffix.split('/')[0]
                rows[name]=dict(name=name,type='dir' if '/' in suffix else 'file')
        if rows: return list(rows.values())
        raise urllib.error.HTTPError('',404,'missing',{},None)

class FileChannelTests(unittest.TestCase):
    def setUp(self): self.tmp=tempfile.TemporaryDirectory(); self.root=Path(self.tmp.name); self.api=Fake(); self.files=f.FileChannels(self.api)
    def tearDown(self): self.tmp.cleanup()
    def test_names(self):
        for name in ('..','../x','a/b','a\\b','CON','x:stream','trailing.','trailing '):
            with self.subTest(name=name),self.assertRaises(ValueError): f.child(self.root,name)
        self.assertEqual(f.child(self.root,'实验.pdf').name,'实验.pdf')
    def test_reserved_member(self):
        self.assertNotEqual(f.member_folder(self.root,'Default'),self.root/'References'/'Default')
        self.assertNotEqual(f.member_folder(self.root,'Default'),f.member_folder(self.root,'member-Default'))
        self.assertNotEqual(f.member_folder(self.root,'CON'),f.member_folder(self.root,'member-CON'))
    def test_symlink(self):
        target=self.root/'target'; target.mkdir(); link=self.root/'link'
        try: link.symlink_to(target,target_is_directory=True)
        except OSError: self.skipTest('symlink creation unavailable on this Windows host')
        with self.assertRaises(ValueError): f.upload_files(link)
    def test_failed_download_preserves(self):
        dest=self.root/'dest'; dest.mkdir(); (dest/'old').write_text('old')
        def interrupted(stage): (stage/'partial').write_text('partial'); raise OSError('offline')
        with self.assertRaises(OSError): f.replace_download(dest,interrupted)
        self.assertEqual((dest/'old').read_text(),'old')
    def test_empty_download_preserves(self):
        dest=self.root/'dest'; dest.mkdir(); (dest/'old').write_text('old')
        with self.assertRaises(ValueError): f.replace_download(dest,lambda stage:None)
        self.assertTrue((dest/'old').exists())
    def test_successful_download_backup(self):
        dest=self.root/'dest'; dest.mkdir(); (dest/'old').write_text('old')
        backup=f.replace_download(dest,lambda stage:(stage/'new').write_text('new'))
        self.assertEqual((backup/'old').read_text(),'old'); self.assertEqual((dest/'new').read_text(),'new')
    def test_binary_raw(self):
        self.api.files['default/large.pdf']=b'\x00\xff\x80'; self.api.force_raw.add('default/large.pdf')
        self.assertEqual(self.files.get_bytes('default/large.pdf'),b'\x00\xff\x80')
    def test_nested_member_download(self):
        self.api.files['team/alice/progress/nested/result.docx']=b'\xffdata'
        self.api.files['team/alice/references/a.pdf']=b'pdf'
        dest=self.root/'alice'; self.assertTrue(self.files.download_member('alice',dest))
        self.assertEqual((dest/'progress/nested/result.docx').read_bytes(),b'\xffdata')
        self.assertFalse((dest/'profile.json').exists())
    def test_member_without_files(self):
        self.api.files['team/alice/profile.json']=b'{}'; dest=self.root/'alice'; dest.mkdir(); (dest/'old').write_text('old')
        self.assertFalse(self.files.download_member('alice',dest)); self.assertTrue((dest/'old').exists())
    def test_missing_default(self):
        dest=self.root/'default'; dest.mkdir(); (dest/'old').write_text('old')
        with self.assertRaises(urllib.error.HTTPError): self.files.default_library(dest)
        self.assertTrue((dest/'old').exists())
    def test_reference_download_preserves_progress(self):
        self.api.files['team/alice/references/new.pdf']=b'new'; dest=self.root/'alice'; (dest/'progress').mkdir(parents=True); (dest/'progress/old.docx').write_bytes(b'old')
        self.files.download_member('alice',dest,references_only=True)
        self.assertEqual((dest/'progress/old.docx').read_bytes(),b'old')
    def test_remote_path_traversal(self):
        self.api.request=lambda *a,**k:[dict(name='../escape',type='file')] if '/contents/' in a[1] else {}
        with self.assertRaises(ValueError): self.files.download_dir('default',self.root/'default')
        self.assertFalse((self.root/'escape').exists())
    def test_large_file_preflight(self):
        p=self.root/'large';
        with p.open('wb') as stream: stream.truncate(f.MAX_FILE+1)
        with self.assertRaises(ValueError): f.upload_files(self.root)
    def test_upload_dedicated_progress(self):
        progress=self.root/'progress'; (progress/'nested').mkdir(parents=True); (progress/'nested/note.docx').write_bytes(b'docx')
        self.assertEqual(self.files.upload_progress(progress,self.root),1)
        self.assertEqual(self.api.files['team/me/progress/nested/note.docx'],b'docx')
    def test_upload_refuses_root(self):
        with self.assertRaises(ValueError): self.files.upload_progress(self.root,self.root)
        self.assertFalse(self.api.calls)
    def test_binary_conflict_retry(self):
        self.api.conflict=True; self.files.upload_bytes('team/me/progress/a',b'\xff\x00')
        self.assertEqual(self.api.files['team/me/progress/a'],b'\xff\x00')
        self.assertEqual(len([x for x in self.api.calls if x[0]=='PUT']),2)
    def test_reference_manifest_keeps_previous(self):
        self.api.files['team/me/profile.json']=b'{}'
        self.api.files['team/me/references.json']=json.dumps([dict(Path='team/me/references/old.pdf')]).encode()
        file=self.root/'new.pdf'; file.write_bytes(b'pdf')
        rows=self.files.upload_references([file],self.root/'personal')
        self.assertEqual({x['Path'] for x in rows},{'team/me/references/old.pdf','team/me/references/new.pdf'})
    def test_reference_upload_same_source_target(self):
        folder=self.root/'personal'; (folder/'references').mkdir(parents=True); file=folder/'references/new.pdf'; file.write_bytes(b'pdf')
        self.files.upload_references([file],folder); self.assertEqual(file.read_bytes(),b'pdf')
    def test_duplicate_reference_names(self):
        a=self.root/'a'; b=self.root/'b'; a.mkdir(); b.mkdir(); (a/'same.pdf').write_bytes(b'a'); (b/'same.pdf').write_bytes(b'b')
        with self.assertRaises(ValueError): self.files.upload_references([a/'same.pdf',b/'same.pdf'],self.root/'personal')
        self.assertFalse(self.api.calls)
    def test_metadata_list_merge(self):
        rows=f.merge_references([dict(Path='old')],[dict(Path='new')]); self.assertEqual(len(rows),2)
        with self.assertRaises(ValueError): f.merge_references({'bad':True},[])
    def test_read_only_members(self):
        self.api.files['team/alice/profile.json']=b'{}'; self.assertEqual(self.files.members(),['alice'])
        self.assertFalse(any(method=='PUT' for method,_ in self.api.calls))

if __name__=='__main__': unittest.main()
