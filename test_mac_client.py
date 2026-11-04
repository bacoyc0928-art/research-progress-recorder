import copy
import importlib.util
from pathlib import Path
import tempfile
import unittest
import io
import ssl
from unittest.mock import patch
from types import SimpleNamespace
import urllib.error
import zipfile
import xml.etree.ElementTree as ET

spec=importlib.util.spec_from_file_location('recorder',Path(__file__).with_name('mac-client.py'))
m=importlib.util.module_from_spec(spec); spec.loader.exec_module(m)
OLD='2026-10-20T10:00:00.0000000Z'; NEW='2026-10-20T10:00:00.0000001Z'
def entry(ident,title='A',updated=OLD): return dict(Id=ident,Title=title,Updated=updated,Category='',Content='',Date='2026-10-20',Progress=0)
class SyncTests(unittest.TestCase):
    def test_frozen_update_probe_writes_result_without_console(self):
        with tempfile.TemporaryDirectory() as tmp:
            target=Path(tmp)/'result.json'
            context=SimpleNamespace(check_hostname=True,verify_mode=ssl.CERT_REQUIRED)
            release=dict(tag_name='v0.2.12',assets=[dict(name='SHA256SUMS.txt',browser_download_url='https://example.invalid/sums')])
            checksum=('a'*64+'  app.dmg').encode()
            with patch.object(m,'network_context',return_value=context), patch.object(m,'release_info',return_value=release), patch.object(m.urllib.request,'urlopen',return_value=io.BytesIO(checksum)), patch.object(m,'app_bundle_path',return_value=None):
                m.verify_update_runtime(target)
            result=m.json.loads(target.read_text())
            self.assertTrue(result['tls_verified']); self.assertTrue(result['checksum_download']); self.assertEqual(result['version'],m.VERSION)

    def test_updater_and_sync_use_verified_system_ca_context_on_macos(self):
        with patch.object(m.sys,'platform','darwin'), patch.object(m.Path,'is_file',return_value=True), patch.object(m.ssl,'create_default_context') as create:
            context=m.network_context()
            self.assertIs(context,create.return_value)
            context.load_verify_locations.assert_called_once_with(cafile='/etc/ssl/cert.pem')
        context=ssl.create_default_context()
        self.assertTrue(context.check_hostname); self.assertEqual(context.verify_mode,ssl.CERT_REQUIRED)

    def test_release_check_passes_verified_tls_context(self):
        context=object()
        with patch.object(m,'network_context',return_value=context), patch.object(m.urllib.request,'urlopen',return_value=io.BytesIO(b'{"tag_name":"v0.2.12"}')) as request:
            self.assertEqual(m.release_info()['tag_name'],'v0.2.12')
            self.assertIs(request.call_args.kwargs['context'],context)

    def test_download_and_checksum_both_pass_verified_tls_context(self):
        with tempfile.TemporaryDirectory() as folder:
            content=b'isolated fake DMG'; name='test-macOS-arm64.dmg'
            digest=m.hashlib.sha256(content).hexdigest()
            asset=dict(name=name,browser_download_url='https://example.invalid/app.dmg')
            release=dict(assets=[asset,dict(name='SHA256SUMS.txt',browser_download_url='https://example.invalid/sums')])
            context=object()
            with patch.object(m.tempfile,'mkdtemp',return_value=folder), patch.object(m,'network_context',return_value=context), patch.object(m.urllib.request,'urlopen',side_effect=[io.BytesIO(content),io.BytesIO((digest+'  '+name).encode())]) as request:
                self.assertEqual(Path(m.download_update(release,asset)).read_bytes(),content)
                self.assertTrue(all(call.kwargs['context'] is context for call in request.call_args_list))

    def test_tls_error_is_distinguished_from_app_signing_error(self):
        error=urllib.error.URLError(ssl.SSLCertVerificationError(1,'CERTIFICATE_VERIFY_FAILED'))
        self.assertIn('网络证书',m.update_error_message(error))
        self.assertIn('not an app-signing error',m.update_error_message(error,True))

    def test_rate_limit_error_is_explicit(self):
        error=urllib.error.HTTPError('',403,'rate limited',{},None)
        self.assertIn('请求次数限制',m.update_error_message(error))

    def test_adhoc_signing_is_not_developer_id(self):
        result=SimpleNamespace(returncode=0,stdout='',stderr='Signature=adhoc\nTeamIdentifier=not set\n')
        with patch.object(m.subprocess,'run',return_value=result): self.assertIsNone(m.application_team(Path('/Applications/App.app')))
        result.stderr='Authority=Developer ID Application: Example\nTeamIdentifier=ABCDEFGHIJ\n'
        with patch.object(m.subprocess,'run',return_value=result): self.assertEqual(m.application_team(Path('/Applications/App.app')),'ABCDEFGHIJ')

    def test_adhoc_update_opens_dmg_without_closing_or_replacing_app(self):
        with patch.object(m,'app_bundle_path',return_value=Path('/Applications/ResearchProgressRecorder.app')), patch.object(m.os,'access',return_value=True), patch.object(m,'application_team',return_value=None), patch.object(m.subprocess,'run') as open_dmg, patch.object(m.subprocess,'Popen') as spawn:
            result=m.install_update('/tmp/verified.dmg')
            self.assertFalse(result['automatic']); self.assertEqual(result['reason'],'unnotarized-build')
            open_dmg.assert_called_once_with(['/usr/bin/open','/tmp/verified.dmg'],check=True,timeout=15)
            spawn.assert_not_called()

    def test_dmg_translocation_and_unwritable_installs_choose_manual_mode(self):
        for app in ('/Volumes/Recorder/ResearchProgressRecorder.app','/private/var/folders/a/AppTranslocation/x/d/ResearchProgressRecorder.app'):
            self.assertEqual(m.update_install_plan(Path(app))['reason'],'disk-image')
        with patch.object(m.os,'access',return_value=False):
            self.assertEqual(m.update_install_plan(Path('/Applications/ResearchProgressRecorder.app'))['reason'],'not-writable')

    def test_developer_id_update_stages_helper_with_diagnostics(self):
        with tempfile.TemporaryDirectory() as tmp:
            with patch.object(m,'app_bundle_path',return_value=Path('/Applications/ResearchProgressRecorder.app')), patch.object(m.os,'access',return_value=True), patch.object(m,'application_team',return_value='ABCDEFGHIJ'), patch.object(m.subprocess,'Popen') as spawn:
                plan=m.install_update('/tmp/verified.dmg',tmp)
                self.assertTrue(plan['automatic']); self.assertEqual(len(spawn.call_args.args[0]),8)
                self.assertTrue(Path(spawn.call_args.args[0][1]).is_file())
                self.assertEqual(Path(plan['status']).parent,Path(tmp))
                self.assertIn('spctl --assess',m.UPDATE_SCRIPT)
                self.assertNotIn('xattr',m.UPDATE_SCRIPT)

    def test_help_history_preserves_windows_entries_when_appending_with_retry(self):
        writes=[]
        def get(path):
            return [dict(Time=OLD,User='WindowsUser',Action='更新使用说明',Detail='old revision')],str(len(writes))
        def put(path,rows,sha):
            writes.append((path,rows,sha))
            if len(writes)==1: raise urllib.error.HTTPError('',409,'conflict',{},None)
        m.append_activity_to(SimpleNamespace(get=get,put=put),'help/changelog.json','MacUser','更新使用说明','new revision',200)
        self.assertEqual(len(writes),2)
        self.assertEqual([row['user'] for row in writes[-1][1]],['WindowsUser','MacUser'])
        self.assertEqual(writes[-1][2],'1')

    def test_activity_accepts_windows_and_mac_fields(self):
        api=SimpleNamespace(get=lambda path:([
            dict(Time=NEW,User='WindowsUser',Action='登录',Detail='PIN'),
            dict(time=OLD,user='MacUser',action='Sign in',detail='token'),None], 'sha'))
        rows=m.read_activity(api,'logs/public/activity.json')
        self.assertEqual([r['user'] for r in rows],['WindowsUser','MacUser'])
        self.assertEqual(rows[0]['action'],'登录')

    def test_team_roster_handles_invites_case_and_mixed_activity(self):
        client=m.Client.__new__(m.Client); client.en=False; results=[]
        client.api=lambda:(SimpleNamespace(),{})
        client.channels=lambda api:SimpleNamespace(identity=lambda:'ME',members=lambda:['Mixed'],
            collaborators=lambda:['me','mixed','NewMember'],pending_invitations=lambda:['Invited'])
        client.run=lambda work,done:results.append(work())
        with patch.object(m,'read_activity',return_value=[
            dict(user='MIXED',time=OLD,action='登录'),
            dict(user='mixed',time='2026-10-21T00:00:00',action='upload')]):
            client.team()
        names,states=results[0]
        self.assertEqual(len(names),4)
        self.assertIn('已连接',states['Mixed'])
        self.assertIn('已连接 GitHub',states['ME'])
        self.assertEqual(states['Invited'],'待接受邀请')
        self.assertEqual(states['NewMember'],'已加入 · 未连接')

    def test_download_picker_marks_invited_and_unuploaded_and_skips_them(self):
        client=m.Client.__new__(m.Client); client.en=False; downloaded=[]; labels=[]
        api=SimpleNamespace(); client.api=lambda:(api,{})
        client.channels=lambda _:SimpleNamespace(identity=lambda:'me',members=lambda:['HasFiles'],
            collaborators=lambda:['me','HasFiles','NewMember'],pending_invitations=lambda:['Invited'],
            download_member=lambda member,dest:downloaded.append(member) or True)
        client.team_destination=lambda *a:Path('unused'); client.log_activity=lambda *a:None; client.finish=lambda *a:None
        def pick(title,hint,items,**options): labels.append(options['labels']); return items
        client.pick=pick; client.run=lambda work,done:done(work())
        client.pull_others()
        self.assertEqual(downloaded,['HasFiles'])
        self.assertIn('尚未上传',labels[0]['NewMember'])
        self.assertIn('待接受邀请',labels[0]['Invited'])

    def test_own_member_files_use_local_reference_copy(self):
        client=m.Client.__new__(m.Client); results=[]; api=SimpleNamespace()
        client.selected_member=lambda:'ME'; client.api=lambda:(api,{})
        client.reference_dir=Path('local/Reference')
        client.channels=lambda _:SimpleNamespace(identity=lambda:'me')
        client.run=lambda work,done:results.append(work())
        client.member_research()
        self.assertEqual(results,[client.reference_dir])

    def test_update_rejects_missing_checksum_before_download(self):
        with patch.object(m.urllib.request,'urlopen') as request:
            with self.assertRaisesRegex(ValueError,'SHA256SUMS'):
                m.download_update({'assets':[]},{'name':'app.dmg','browser_download_url':'https://example.invalid'})
            request.assert_not_called()

    def test_help_draft_survives_close_with_backup(self):
        with tempfile.TemporaryDirectory() as tmp:
            client=m.Client.__new__(m.Client); client.home=Path(tmp); client.busy=False; client.help_dirty=True
            target=client.home/'help_local.md'; target.write_text('old draft')
            client.help_text=SimpleNamespace(get=lambda *a:'new draft')
            cleaned=[]; closed=[]; client.clear_public_temp=lambda:cleaned.append(True)
            client.root=SimpleNamespace(destroy=lambda:closed.append(True))
            client.close()
            self.assertEqual(target.read_text(),'new draft')
            self.assertEqual(Path(str(target)+'.bak').read_text(),'old draft')
            self.assertEqual((cleaned,closed),([True],[True]))

    def test_help_reload_does_not_replace_new_edits(self):
        client=m.Client.__new__(m.Client); client.en=False; client.help_dirty=False
        client.help_text=SimpleNamespace(get=lambda *a:'old text')
        client.api=lambda:(SimpleNamespace(request=lambda *a,**k:'cloud text',endpoint=lambda p:p,branch='main'),{})
        pending=[]; client.run=lambda work,done:pending.append((work,done))
        client.remember=lambda *a:None; client.set_help_text=lambda *a:self.fail('new edit replaced')
        client.load_help(quiet=True)
        client.help_text=SimpleNamespace(get=lambda *a:'new edit while downloading')
        pending[0][1](pending[0][0]())

    def test_language_switch_preserves_session_and_editors_without_restarting(self):
        class Widget:
            def __init__(self,kind='TLabel',text='',children=()): self.kind=kind; self.text=text; self.children=children
            def winfo_class(self): return self.kind
            def winfo_children(self): return self.children
            def configure(self,**kw): self.text=kw.get('text',self.text)
            def cget(self,key): return self.text
        client=m.Client.__new__(m.Client); client.en=False; client.busy=False; client.config={}; client.configfile=Path('unused')
        label=Widget(text=client.tr('成员资料','Members')); editor=Widget('Text','unsaved draft')
        client.root=Widget('Tk',children=[label,editor]); client.tab_labels=[('成员资料','Members')]; client.tab_base=['成员资料']
        tab=['成员资料']
        client.book=SimpleNamespace(tab=lambda i,*a,**k:tab.__setitem__(0,k['text']) if k else tab[0])
        client.lang_btn=Widget('TButton','EN'); status=['就绪']; client.tr('就绪','Ready')
        client.status=SimpleNamespace(get=lambda:status[0],set=lambda v:status.__setitem__(0,v))
        client.update_login_ui=lambda:None; client.personal_label=Widget(); client.personal_display=lambda:'me  /Reference'
        client.members_tree=SimpleNamespace(get_children=lambda:[])
        client.session_login='me'; client.session_token='private-in-memory'
        with patch.object(m,'atomic'), patch.object(m.subprocess,'Popen') as spawn:
            client.toggle_language(); self.assertEqual(label.text,'Members'); self.assertEqual(tab[0],'Members')
            self.assertEqual(status[0],'Ready'); self.assertEqual(editor.text,'unsaved draft')
            self.assertEqual(client.session_login,'me'); self.assertEqual(client.session_token,'private-in-memory')
            client.toggle_language(); self.assertEqual(label.text,'成员资料'); spawn.assert_not_called()

    def test_badges_use_the_module_github_client(self):
        with tempfile.TemporaryDirectory() as tmp:
            client=m.Client.__new__(m.Client); client.session_token='offline'; client.session_login='me'
            client.mat_local=client.reference_dir=Path(tmp)
            with patch.object(m,'GitHub',return_value=object()) as github, patch.object(m,'list_posts',return_value=[]), patch.object(m,'read_ids',return_value=[]), patch.object(m,'read_activity',return_value=[]), patch.object(m,'FileChannels') as channels:
                channels.return_value.members.return_value=['me','colleague']
                self.assertEqual(set(client.news_state()),set(range(6)))
                github.assert_called_once()
    def test_sign_out_uses_normal_exit_cleanup(self):
        client=m.Client.__new__(m.Client); client.root=object(); client.mb=SimpleNamespace(askyesno=lambda *a,**k:True)
        client.session_login=client.login='me'; client.session_token='offline'; closed=[]
        client.close=lambda:closed.append(True)
        client.sign_out(); self.assertEqual(closed,[True]); self.assertEqual(client.session_token,'')
    def test_progress_picker_supports_external_files_and_custom_folder(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp); client=m.Client.__new__(m.Client); client.files_root=root/'SRCRec'; client.files_root.mkdir()
            client.session_token='offline'
            client.mat_local=client.files_root/'PersonalWorkspaceSRRec/Mat_Local'; client.mat_local.mkdir(parents=True)
            note=client.mat_local/'note.txt'; note.write_text('note')
            custom=root/'custom'; custom.mkdir(); extra=root/'extra.txt'; extra.write_text('extra')
            client.progress=SimpleNamespace(get=lambda:str(custom)); picked=[]; uploaded=[]
            def pick(title,hint,items,extra_files=False):
                picked.append(extra_files); return items+[str(extra)]
            client.pick=pick; api=SimpleNamespace(); client.api=lambda:(api,{})
            files=SimpleNamespace(identity=lambda:'me',upload_selected_progress=lambda folder,paths:uploaded.append((folder,paths)) or len(paths))
            client.channels=lambda _:files; client.log_activity=lambda *a:None; client.finish=lambda *a:None
            client.run=lambda work,done:done(work())
            client.upload_progress()
            self.assertEqual(picked,[True])
            self.assertEqual(uploaded[0][0].resolve(),client.mat_local.resolve())
            self.assertEqual([p.resolve() for p in uploaded[0][1]],[note.resolve(),extra.resolve()])

    def test_uploads_stop_before_picker_when_signed_out(self):
        client=m.Client.__new__(m.Client); client.require_login=lambda:False
        client.pick=lambda *a,**k:self.fail('Signed-out upload opened a file picker')
        client.upload_progress(); client.upload_references()

    def test_viewing_a_tab_persists_its_integer_key_signature(self):
        client=m.Client.__new__(m.Client); client.tab_base=['Team','Posts']; client.news_sig={1:'new-posts'}
        client.news_seen={'1':'old'}; client.config={}; client.configfile=Path('unused')
        client.book=SimpleNamespace(tab=lambda *a,**k:None)
        with patch.object(m,'atomic') as save:
            client.mark_news_seen(1)
            self.assertEqual(client.news_seen['1'],'new-posts'); save.assert_called_once()

    def test_local_badge_detects_an_edit_without_file_count_change(self):
        with tempfile.TemporaryDirectory() as tmp:
            client=m.Client.__new__(m.Client); client.session_token='offline'; client.session_login='me'
            client.mat_local=client.reference_dir=Path(tmp); note=Path(tmp)/'note.txt'; note.write_text('first')
            with patch.object(m,'GitHub',return_value=object()), patch.object(m,'list_posts',return_value=[]), patch.object(m,'read_ids',return_value=[]), patch.object(m,'read_activity',return_value=[]), patch.object(m,'FileChannels') as channels:
                channels.return_value.members.return_value=['me']
                before=client.news_state()[3]; note.write_text('edited content')
                self.assertNotEqual(before,client.news_state()[3])

    def test_021_upgrade_preserves_020_user_data(self):
        with tempfile.TemporaryDirectory() as tmp:
            home=Path(tmp)
            (home/'data_version.txt').write_text('0.2.0',encoding='utf-8')
            for name in ('config.json','research_log.json','workspace.json','sync_config.json'):
                (home/name).write_text('preserve-'+name,encoding='utf-8')
            default=home/'SRCRec'/'References'/'Default'; default.mkdir(parents=True)
            (default/'team-book.pdf').write_bytes(b'test')
            member=home/'SRCRec'/'References'/'alice'/'TeamConRef'; member.mkdir(parents=True)
            (member/'notes.txt').write_text('test',encoding='utf-8')
            (member.parent/'references.json').write_text('[]',encoding='utf-8')
            client=m.Client.__new__(m.Client); client.home=home
            client.clean_previous_data()
            self.assertEqual((home/'config.json').read_text(encoding='utf-8'),'preserve-config.json')
            self.assertEqual((home/'research_log.json').read_text(encoding='utf-8'),'preserve-research_log.json')
            self.assertTrue((default/'team-book.pdf').exists())
            self.assertTrue((member/'notes.txt').exists())
            self.assertTrue((member.parent/'references.json').exists())
            self.assertEqual((home/'data_version.txt').read_text(encoding='utf-8'),'0.2.0')
            self.assertFalse((home/'upgrade_cleanup.log').exists())

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

class DialogLayoutTests(unittest.TestCase):
    SCREEN=(0,0,1440,900)
    def test_dialog_fits_primary_screen_with_menu_and_dock_margins(self):
        w,h,left,top=m.dialog_geometry(900,520,self.SCREEN,(100,80,1120,760))
        self.assertEqual((w,h),(900,520))
        self.assertGreaterEqual(left,0); self.assertLessEqual(left+w,1440)
        self.assertGreaterEqual(top,28); self.assertLessEqual(top+h,900-48)
    def test_dialog_is_clamped_on_small_primary_screen(self):
        w,h,left,top=m.dialog_geometry(900,540,(0,0,800,600),(0,0,800,600))
        self.assertEqual((w,h),(768,500))
        self.assertGreaterEqual(left,0); self.assertLessEqual(left+w,800)
        self.assertGreaterEqual(top,28); self.assertLessEqual(top+h,600-48)
    def test_dialog_stays_on_secondary_display_right_of_primary(self):
        parent=(1440+200,100,1120,760)
        w,h,left,top=m.dialog_geometry(850,520,self.SCREEN,parent)
        self.assertGreaterEqual(left,parent[0]); self.assertLessEqual(left+w,parent[0]+parent[2])
        self.assertGreaterEqual(top,parent[1]); self.assertLessEqual(top+h,parent[1]+parent[3])
    def test_dialog_stays_on_secondary_display_left_of_or_above_primary(self):
        for parent in [(-1300,50,1120,760),(200,-900,940,640)]:
            w,h,left,top=m.dialog_geometry(780,540,self.SCREEN,parent)
            self.assertGreaterEqual(left,parent[0]); self.assertLessEqual(left+w,parent[0]+parent[2])
            self.assertGreaterEqual(top,parent[1]); self.assertLessEqual(top+h,parent[1]+parent[3])
    def test_dialog_larger_than_secondary_window_is_shrunk_to_window(self):
        parent=(2000,0,600,400)
        w,h,left,top=m.dialog_geometry(900,520,self.SCREEN,parent)
        self.assertLessEqual(w,600); self.assertLessEqual(h,400)
        self.assertEqual(left,2000)

class DialogBottomBarTkTests(unittest.TestCase):
    def setUp(self):
        try:
            import tkinter
            from tkinter import ttk
            self.root=tkinter.Tk(); self.root.withdraw()
        except Exception as error:
            self.skipTest('Tk unavailable: '+str(error))
        self.tk,self.ttk=tkinter,ttk
        ns=SimpleNamespace(ttk=ttk,root=self.root)
        ns.flow_bar=lambda win,**k:m.Client.flow_bar(ns,win,**k)
        self.client=ns
    def tearDown(self):
        if hasattr(self,'root'): self.root.destroy()
    def assert_button_visible(self,win,button):
        win.update()
        self.assertGreaterEqual(button.winfo_height(),button.winfo_reqheight()-2)
        self.assertLessEqual(button.winfo_rooty()+button.winfo_height(),win.winfo_rooty()+win.winfo_height()+1)
    def test_shrunken_dialog_keeps_close_button_after_tall_content(self):
        win=self.tk.Toplevel(self.root); win.geometry('360x220+40+40')
        self.ttk.Label(win,text='header',padding=8).pack(anchor='w')
        self.tk.Text(win,height=24).pack(fill='both',expand=True)
        button=self.ttk.Button(m.Client.bottom_bar(self.client,win),text='Close'); button.pack(pady=8)
        self.assert_button_visible(win,button)
    def test_flow_bar_variant_stays_at_bottom_below_list(self):
        win=self.tk.Toplevel(self.root); win.geometry('360x220+40+40')
        self.ttk.Label(win,text='hint').pack(fill='x')
        bar=m.Client.bottom_bar(self.client,win,flow=True,padx=12,pady=8)
        button=self.ttk.Button(bar,text='OK'); bar._flow_buttons.append(button)
        body=self.tk.Listbox(win,height=18); body.pack(fill='both',expand=True)
        win.update(); bar._reflow(); win.update()
        self.assertGreater(bar.winfo_y(),body.winfo_y())
        self.assert_button_visible(win,button)

class SignedOutStateTkTests(unittest.TestCase):
    def test_signed_out_disables_posts_help_and_sync_then_sign_in_restores(self):
        try:
            import tkinter
            root=tkinter.Tk(); root.withdraw()
        except Exception as error:
            self.skipTest('Tk unavailable: '+str(error))
        try:
            with tempfile.TemporaryDirectory() as home, patch.object(m,'remembered_blob',return_value=None), patch.object(m.GitHub,'request',side_effect=AssertionError('offline')):
                client=m.Client(root,home,smoke=True); client.run=lambda *a:None; client.load_help=lambda *a,**k:None
                root.update()
                actions=client.sync_actions+client.login_actions
                self.assertEqual(len(client.help_actions),4)
                self.assertTrue(all(str(b.cget('state'))=='disabled' for b in actions))
                self.assertEqual(str(client.post_text.cget('state')),'disabled')
                self.assertEqual(str(client.help_text.cget('state')),'disabled')
                client.session_login=client.login='offline-tester'; client.session_token='offline-not-a-credential'
                client.update_login_ui(); root.update()
                self.assertTrue(all(str(b.cget('state'))=='normal' for b in actions))
                self.assertEqual(str(client.post_text.cget('state')),'normal')
                client.toggle_language(); root.update()
                self.assertTrue(all(str(b.cget('state'))=='normal' for b in actions))
        finally:
            root.destroy()

if __name__=='__main__': unittest.main()
