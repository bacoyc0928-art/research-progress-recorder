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
        def shoot(name,pick_window,min_size):
            windows=Quartz.CGWindowListCopyWindowInfo(Quartz.kCGWindowListOptionOnScreenOnly,Quartz.kCGNullWindowID)
            choices=[w for w in windows if int(w.get('kCGWindowOwnerPID',0))==os.getpid() and pick_window(w.get('kCGWindowBounds',{}))]
            if not choices: raise RuntimeError('The macOS WindowServer did not expose the window for '+name)
            w=choices[0] if min_size[0]<500 else max(choices,key=lambda w:w['kCGWindowBounds']['Width']*w['kCGWindowBounds']['Height'])
            target=out/(name+'.png')
            subprocess.run(['/usr/sbin/screencapture','-x','-o','-l',str(w['kCGWindowNumber']),str(target)],check=True)
            with Image.open(target) as img:
                if img.width<min_size[0] or img.height<min_size[1]: raise RuntimeError('Capture did not contain the expected window: '+name)
                pixels=[*img.convert('RGB').getextrema()]
                if all(lo==hi for lo,hi in pixels): raise RuntimeError('Blank screenshot')
                return target,[img.width,img.height]
        def clipped_controls(start):
            clipped=[]
            def inspect(widget):
                if widget in client.tabs and str(widget)!=str(client.book.select()): return
                if widget.winfo_ismapped() and widget.winfo_class() in ('TButton','TCheckbutton','TEntry','TLabel'):
                    left=widget.winfo_rootx(); top=widget.winfo_rooty(); right=left+widget.winfo_width(); bottom=top+widget.winfo_height()
                    parent=widget.master; toplevel=widget.winfo_toplevel()
                    while parent is not None:
                        left=max(left,parent.winfo_rootx()); top=max(top,parent.winfo_rooty())
                        right=min(right,parent.winfo_rootx()+parent.winfo_width()); bottom=min(bottom,parent.winfo_rooty()+parent.winfo_height())
                        if parent is toplevel: break
                        parent=parent.master
                    if widget.winfo_reqwidth()>right-left+6 or widget.winfo_reqheight()>bottom-top+3:
                        try: text=str(widget.cget('text'))
                        except Exception: text=''
                        clipped.append({'class':widget.winfo_class(),'text':text,'requested':[widget.winfo_reqwidth(),widget.winfo_reqheight()],'visible':[right-left,bottom-top]})
                for child in widget.winfo_children():
                    if isinstance(child,tk.Toplevel) and start is root: continue
                    inspect(child)
            inspect(start)
            return clipped
        def capture(name):
            pump()
            target,size=shoot(name,lambda b:b.get('Width',0)>500 and b.get('Height',0)>300,(500,300))
            manifest['captures'].append({'file':target.name,'pixels':size,'geometry':root.geometry(),'clipped_controls':clipped_controls(root)})
        def capture_dialog(name,win):
            # 弹窗默认尺寸 + 用户可拖到的最小尺寸；两者都检查按钮是否被裁切或挤掉。
            for suffix,size in (('default',None),('min',win.minsize())):
                if size: win.geometry(f'{size[0]}x{size[1]}')
                pump()
                w,h=win.winfo_width(),win.winfo_height()
                target,pixels=shoot(f'{name}-{suffix}',lambda b:abs(b.get('Width',0)-w)<=6 and h-2<=b.get('Height',0)<=h+60,(min(200,w),min(150,h)))
                buttons=[]
                def collect(widget):
                    if widget.winfo_class()=='TButton': buttons.append(widget)
                    for child in widget.winfo_children(): collect(child)
                collect(win)
                hidden=[str(b.cget('text')) for b in buttons if not b.winfo_ismapped() or b.winfo_height()<b.winfo_reqheight()-2]
                manifest['captures'].append({'file':target.name,'pixels':pixels,'geometry':win.geometry(),'dialog':True,'hidden_buttons':hidden,'clipped_controls':clipped_controls(win)})
        def capture_modal(name,open_dialog):
            before=set(root.winfo_children()); errors=[]
            def later():
                try:
                    win=[w for w in root.winfo_children() if isinstance(w,tk.Toplevel) and w not in before][-1]
                    try: capture_dialog(name,win)
                    finally: win.destroy()
                except Exception as error: errors.append(error)
            root.after(400,later); open_dialog()
            if errors: raise errors[0]
        def capture_dialogs(prefix):
            post={'id':'preview','author':'sample-colleague','time':'2026-11-02 12:00','body':'示例帖子全文 / Sample full post text.\n'*30}
            before=set(root.winfo_children()); client.view_post(post,client.login)
            win=[w for w in root.winfo_children() if isinstance(w,tk.Toplevel) and w not in before][-1]
            capture_dialog(prefix+'dialog-post',win); win.grab_release(); win.destroy()
            before=set(root.winfo_children()); client.show_member_files('sample-colleague',client.mat_local)
            win=[w for w in root.winfo_children() if isinstance(w,tk.Toplevel) and w not in before][-1]
            capture_dialog(prefix+'dialog-member-files',win); win.destroy()
            before=set(root.winfo_children()); client.choose_references_to_remove(None,None,client.login,[f'Example reference {i}.pdf' for i in range(20)])
            win=[w for w in root.winfo_children() if isinstance(w,tk.Toplevel) and w not in before][-1]
            capture_dialog(prefix+'dialog-remove-references',win); win.grab_release(); win.destroy()
            capture_modal(prefix+'dialog-pick',lambda:client.pick('示例选择 / Sample picker','离线示例：勾选要处理的条目。 / Offline sample: choose items to process.',[f'sample-member-{i}' for i in range(25)],extra_files=True))
        assert str(client.help_text.cget('state'))=='disabled', 'Help must be read-only while signed out'
        assert all(str(b.cget('state'))=='disabled' for b in client.sync_actions), 'Sync actions must be disabled while signed out'
        root.geometry('1120x760+30+40'); pump(); capture('00-signed-out-1120')
        root.geometry('940x640+30+40'); pump(); capture('00-signed-out-940')
        client.session_login=client.login='demo-researcher'; client.session_token='offline-preview-not-a-credential'
        client.update_login_ui(); client.personal_label.configure(text=client.personal_display())
        assert str(client.help_text.cget('state'))=='normal', 'Help must be editable after sign-in'
        assert all(str(b.cget('state'))=='normal' for b in client.sync_actions), 'Sync actions must be enabled after sign-in'
        (client.reference_dir/'Example reference.txt').write_text('Synthetic sample reference.',encoding='utf-8')
        (client.default/'Default library overview.txt').write_text('Synthetic team default library.',encoding='utf-8')
        (client.mat_local/'Research progress.txt').write_text('Synthetic progress example.',encoding='utf-8')
        for member in ('demo-researcher','sample-colleague'): client.members_tree.insert('','end',values=(member,'已连接 · 示例 / Connected · sample'))
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
        root.geometry('1120x760+30+40'); pump(); capture_dialogs('')
        client.toggle_language(); pump()
        for width,height in ((1120,760),(940,640)):
            root.geometry(f'{width}x{height}+30+40')
            for index,name in enumerate(names):
                client.book.select(index); client.status.set('Offline UI preview · sample data')
                capture(f'en-{index+1:02d}-{name}-{width}')
        root.geometry('1120x760+30+40'); pump(); capture_dialogs('en-')
        root.destroy()
    (out/'capture-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps(manifest,ensure_ascii=False))
    issues=[(c['file'],w['text']) for c in manifest['captures'] for w in c['clipped_controls'] if w['class'] in ('TButton','TCheckbutton')]
    issues+=[(c['file'],text) for c in manifest['captures'] for text in c.get('hidden_buttons',[])]
    if a.strict and issues: raise RuntimeError('Clipped actions: '+str(issues))

if __name__=='__main__': main()
