"""Render the real Tk client on macOS, offline, with clearly synthetic preview data."""
import argparse
import importlib.util
import json
import os
from pathlib import Path
import platform
import subprocess
import sys
import tempfile
import time

def main():
    p=argparse.ArgumentParser(); p.add_argument('--output',default='ui-captures'); p.add_argument('--label',default='baseline'); p.add_argument('--strict',action='store_true'); a=p.parse_args()
    if sys.platform!='darwin': raise RuntimeError('This capture must run on real macOS')
    out=Path(a.output).resolve(); out.mkdir(parents=True,exist_ok=True)
    repo=Path.cwd(); sys.path.insert(0,str(repo))
    spec=importlib.util.spec_from_file_location('mac_client',repo/'mac-client.py')
    app=importlib.util.module_from_spec(spec); spec.loader.exec_module(app)
    app.remembered_blob=lambda:None
    def offline(*args,**kwargs): raise RuntimeError('Network is disabled in the UI preview')
    app.urllib.request.urlopen=offline
    import tkinter as tk
    import Quartz
    from PIL import Image
    manifest={'label':a.label,'version':app.VERSION,'macOS':platform.mac_ver()[0],'architecture':platform.machine(),'python':platform.python_version(),'data':'Synthetic offline preview; no token or team data','captures':[]}
    with tempfile.TemporaryDirectory(prefix='srprec-ui-') as home:
        root=tk.Tk(); root.withdraw(); client=app.Client(root,home,smoke=True)
        manifest['tk']=str(root.tk.call('info','patchlevel'))
        root.deiconify(); root.lift(); root.attributes('-topmost',True)
        def pump():
            for _ in range(6): root.update(); time.sleep(.1)
        def capture(name):
            pump()
            windows=Quartz.CGWindowListCopyWindowInfo(Quartz.kCGWindowListOptionOnScreenOnly,Quartz.kCGNullWindowID)
            choices=[w for w in windows if int(w.get('kCGWindowOwnerPID',0))==os.getpid() and w.get('kCGWindowBounds',{}).get('Width',0)>500 and w.get('kCGWindowBounds',{}).get('Height',0)>300]
            if not choices: raise RuntimeError('The macOS WindowServer did not expose the client window')
            w=max(choices,key=lambda w:w['kCGWindowBounds']['Width']*w['kCGWindowBounds']['Height'])
            target=out/(name+'.png')
            subprocess.run(['/usr/sbin/screencapture','-x','-o','-l',str(w['kCGWindowNumber']),str(target)],check=True)
            with Image.open(target) as img:
                if img.width<500 or img.height<300: raise RuntimeError('Capture did not contain the application window')
                pixels=[*img.convert('RGB').getextrema()]
                if all(lo==hi for lo,hi in pixels): raise RuntimeError('Blank screenshot')
                size=[img.width,img.height]
            clipped=[]
            def inspect(widget):
                if widget.winfo_ismapped() and widget.winfo_class() in ('TButton','TCheckbutton','TEntry','TLabel'):
                    left=widget.winfo_rootx(); top=widget.winfo_rooty(); right=left+widget.winfo_width(); bottom=top+widget.winfo_height()
                    parent=widget.master
                    while parent is not None:
                        left=max(left,parent.winfo_rootx()); top=max(top,parent.winfo_rooty())
                        right=min(right,parent.winfo_rootx()+parent.winfo_width()); bottom=min(bottom,parent.winfo_rooty()+parent.winfo_height())
                        parent=parent.master
                    if widget.winfo_reqwidth()>right-left+6 or widget.winfo_reqheight()>bottom-top+3:
                        try: text=str(widget.cget('text'))
                        except Exception: text=''
                        clipped.append({'class':widget.winfo_class(),'text':text,'requested':[widget.winfo_reqwidth(),widget.winfo_reqheight()],'visible':[right-left,bottom-top]})
                for child in widget.winfo_children(): inspect(child)
            inspect(root)
            manifest['captures'].append({'file':target.name,'pixels':size,'geometry':root.geometry(),'clipped_controls':clipped})
        assert str(client.help_text.cget('state'))=='disabled', 'Help must be read-only while signed out'
        root.geometry('1120x760+30+40'); pump(); capture('00-signed-out-1120')
        root.geometry('940x640+30+40'); pump(); capture('00-signed-out-940')
        client.session_login=client.login='demo-researcher'; client.session_token='offline-preview-not-a-credential'
        client.update_login_ui(); client.personal_label.configure(text=client.personal_display())
        assert str(client.help_text.cget('state'))=='normal', 'Help must be editable after sign-in'
        (client.reference_dir/'Example reference.txt').write_text('Synthetic sample reference.',encoding='utf-8')
        (client.default/'Default library overview.txt').write_text('Synthetic team default library.',encoding='utf-8')
        (client.mat_local/'Research progress.txt').write_text('Synthetic progress example.',encoding='utf-8')
        for member in ('demo-researcher','sample-colleague'): client.members_tree.insert('','end',values=(member,))
        client.refresh_references(); client.refresh_progress_files()
        client.post_status.configure(text='示例帖子 / Sample post (offline)')
        client.add_post_card({'id':'preview','author':'sample-colleague','time':'2026-11-02 12:00','body':'示例进度：完成数据整理，下一步核对计算结果。 / Example progress: data preparation is complete; review the calculations next.'},client.login)
        names=['team','quick-post','references','progress','sync','help']
        for width,height in ((1120,760),(940,640)):
            root.geometry(f'{width}x{height}+30+40')
            for index,name in enumerate(names):
                client.book.select(index)
                client.status.set('离线界面预览 / Offline UI preview · sample data')
                capture(f'{index+1:02d}-{name}-{width}')
        root.destroy()
    (out/'capture-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps(manifest,ensure_ascii=False))
    issues=[(c['file'],w['text']) for c in manifest['captures'] for w in c['clipped_controls'] if w['class'] in ('TButton','TCheckbutton')]
    if a.strict and issues: raise RuntimeError('Clipped actions: '+str(issues))

if __name__=='__main__': main()
