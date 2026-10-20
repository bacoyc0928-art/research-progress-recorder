import copy
import importlib.util
from pathlib import Path
import tempfile
import unittest
import urllib.error
import zipfile
import xml.etree.ElementTree as ET

spec=importlib.util.spec_from_file_location('recorder',Path(__file__).with_name('mac-client.py'))
m=importlib.util.module_from_spec(spec); spec.loader.exec_module(m)
OLD='2026-10-20T10:00:00.0000000Z'; NEW='2026-10-20T10:00:00.0000001Z'
def entry(ident,title='A',updated=OLD): return dict(Id=ident,Title=title,Updated=updated,Category='',Content='',Date='2026-10-20',Progress=0)
class SyncTests(unittest.TestCase):
    def test_merge_and_subseconds(self):
        a=m.empty(); b=m.empty(); a['Entries']=[entry('a')]; b['Entries']=[entry('a','B',NEW),entry('b')]
        out=m.merge(a,b,['entries']); self.assertEqual([e['Title'] for e in out['Entries']],['B','A']); self.assertEqual(a['Entries'][0]['Title'],'A')
    def test_timezone(self): self.assertEqual(m.stamp(OLD),m.stamp('2026-10-20T11:00:00+01:00'))
    def test_deletion(self):
        a=m.empty(); b=m.empty(); a['DeletedEntries']=[dict(Id='a',At=NEW)]; b['Entries']=[entry('a')]
        self.assertEqual(m.merge(a,b,['entries'])['Entries'],[])
    def test_category_rename(self):
        a=m.empty(); b=m.empty(); a['Categories']=[dict(Id='c',Name='old',Updated=OLD)]; a['Entries']=[entry('e')]; a['Entries'][0]['Category']='old'
        b['Categories']=[dict(Id='c',Name='new',Updated=NEW)]
        self.assertEqual(m.merge(a,b,['categories'])['Entries'][0]['Category'],'new')
    def test_category_delete(self):
        a=m.empty(); b=m.empty(); a['Categories']=[dict(Id='c',Name='old',Updated=NEW)]; b['DeletedCategories']=[dict(Id='c',At=NEW)]
        self.assertEqual(m.merge(a,b,['categories'])['Categories'],[])
    def test_scope(self):
        a=m.empty(); b=m.empty(); a['Entries']=[entry('local')]; b['Entries']=[entry('remote')]
        out=m.payload(a,b,['categories']); self.assertEqual(out['Entries'][0]['Id'],'remote')
    def test_no_private_payload(self):
        a=m.empty(); a['ProjectRoot']='PRIVATE'; a['Refs']=[dict(Path='PRIVATE')]; a['Documents']=[dict(Path='PRIVATE')]
        self.assertNotIn('PRIVATE',str(m.payload(a,None,['entries','categories'])))
    def test_invalid_json(self):
        with self.assertRaises(ValueError): m.normalize({'garbage':[]})
    def test_atomic_backup(self):
        with tempfile.TemporaryDirectory() as tmp:
            p=Path(tmp)/'data.json'; m.atomic(p,{'v':1}); m.atomic(p,{'v':2}); self.assertIn('1',p.with_suffix('.json.bak').read_text())
    def test_corrupt_original_preserved(self):
        with tempfile.TemporaryDirectory() as tmp:
            p=Path(tmp)/'data.json'; p.write_text('{broken')
            with self.assertRaises(ValueError): m.atomic(p,{'v':2})
            self.assertEqual(p.read_text(),'{broken')
    def test_docx(self):
        with tempfile.TemporaryDirectory() as tmp:
            p=m.docx(tmp,'A&B','C','../../title<>&')
            self.assertTrue(m.within(p,tmp))
            with zipfile.ZipFile(p) as z:
                for name in z.namelist(): ET.fromstring(z.read(name))
            self.assertNotEqual(p,m.docx(tmp,'A&B','C','../../title<>&'))
    def test_path_boundary(self): self.assertFalse(m.within('/a/b-other','/a/b'))
    def test_conflict_retry(self):
        class Fake(m.GitHub):
            calls=0
            def get(self,path):
                data=m.empty(); data['Entries']=[entry('remote'+str(self.calls))]; return data,str(self.calls)
            def put(self,path,data,sha):
                self.calls+=1
                if self.calls==1: raise urllib.error.HTTPError('',409,'conflict',{},None)
                self.upload=data
            def history(self,*args): pass
            def login(self): return 'person'
        api=Fake({'repo':'a/b'},'test'); a=m.empty(); a['Entries']=[entry('local')]
        out,_=api.sync(a,['entries'],'sync/research_log.json'); self.assertEqual(api.calls,2); self.assertEqual({x['Id'] for x in out['Entries']},{'remote1','local'})
    def test_auth_error_does_not_upload(self):
        class Fake(m.GitHub):
            def get(self,p): raise urllib.error.HTTPError('',401,'bad token',{},None)
            def put(self,*a): self.fail('must not upload')
        with self.assertRaises(urllib.error.HTTPError): Fake({'repo':'a/b'},'x').sync(m.empty(),['entries'],'sync/research_log.json')
    def test_missing_personal_folder(self):
        with tempfile.TemporaryDirectory() as tmp:
            with self.assertRaises(ValueError): m.GitHub({'repo':'a/b'},'x').personal(m.empty(),str(Path(tmp)/'missing'),True)

if __name__=='__main__': unittest.main()
