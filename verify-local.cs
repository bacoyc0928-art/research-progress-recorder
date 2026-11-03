using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using SRLogbook;
public class Verify002
{
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern IntPtr GetThreadDpiAwarenessContext();
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool AreDpiAwarenessContextsEqual(IntPtr a, IntPtr b);
    static int count;
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); Console.WriteLine("PASS " + message); count++; }
    static Entry E(string id, string date, string title) { return new Entry { Id=id, Updated=date, Title=title, Date="2026-10-20", Category="", Status="进行中", Progress=0, Content="" }; }
    static void PickerCheck(MainForm owner, string root, bool browse)
    {
        Exception failure = null; bool inspected = false;
        var timer = new System.Windows.Forms.Timer { Interval = 150 };
        timer.Tick += delegate
        {
            Form dialog = null;
            foreach (Form candidate in Application.OpenForms) if (candidate != owner) dialog = candidate;
            if (dialog == null) return;
            timer.Stop();
            try
            {
                var area = Screen.FromControl(dialog).WorkingArea;
                Check(area.Contains(dialog.Bounds), "selection dialog fits current monitor including title bar");
                CheckedListBox list = null; Label hint = null; Panel bar = null;
                foreach (Control child in dialog.Controls)
                {
                    if (child is CheckedListBox) list = (CheckedListBox)child;
                    if (child is Label) hint = (Label)child;
                    if (child is Panel) bar = (Panel)child;
                }
                Check(list != null && hint != null && bar != null && list.Top >= hint.Bottom && list.Bottom <= bar.Top && list.Height > 40,
                    "selection hint and toolbar cannot cover the first or last list item");
                foreach (Control flow in bar.Controls) foreach (Control button in flow.Controls)
                    Check(flow.ClientRectangle.Contains(button.Bounds), "selection action stays inside toolbar: " + button.Text);
                using (var bitmap = new System.Drawing.Bitmap(dialog.Width,dialog.Height))
                { dialog.DrawToBitmap(bitmap,new System.Drawing.Rectangle(System.Drawing.Point.Empty,dialog.Size)); bitmap.Save(Path.Combine(root,"picker-"+(Lang.En?"en":"zh")+(browse?"-upload":"-download")+".png")); }
                inspected = true;
            }
            catch (Exception ex) { failure = ex; }
            finally { dialog.DialogResult = DialogResult.Cancel; dialog.Close(); }
        };
        timer.Start();
        var rows = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string,string>>();
        rows.Add(new System.Collections.Generic.KeyValuePair<string,string>("one","First member — sample file.txt"));
        rows.Add(new System.Collections.Generic.KeyValuePair<string,string>("two","Second member — no upload yet"));
        typeof(MainForm).GetMethod("PickItems",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(owner,new object[] {
            "下载他人内容 / Download team files",
            "勾选需要下载的内容。未勾选的文件不会被修改，原有文件保留备份。 / Check files to download. Unchecked files stay untouched; existing files keep backups.",rows,browse});
        timer.Dispose(); if (failure != null) throw failure;
        Check(inspected,"selection modal was inspected and closed");
    }
    [STAThread] public static void Main()
    {
        Ui.EnableHighDpi();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        string root = AppDomain.CurrentDomain.BaseDirectory; LocalTransferChecks.Run(root);
        string upgradeFixture = Path.Combine(root, "upgrade-fixture");
        Directory.CreateDirectory(upgradeFixture);
        File.WriteAllText(Path.Combine(upgradeFixture, "data_version.txt"), CleanUpgrade.Target);
        File.WriteAllText(Path.Combine(upgradeFixture, "sync_config.json"), "existing-user-config");
        Check(!CleanUpgrade.Run(upgradeFixture) && File.Exists(Path.Combine(upgradeFixture, "sync_config.json")),
            "0.2.0 cleanup marker preserves existing Windows user data on 0.2.1 upgrade");
        string older="2026-10-20T10:00:00.0000000Z", newer="2026-10-20T10:00:00.0000001Z";
        var a=new Store(); var b=new Store(); a.Entries.Add(E("a",older,"local")); b.Entries.Add(E("a",newer,"remote")); b.Entries.Add(E("b",older,"other"));
        SyncData.Merge(a,b,"entries"); Check(a.Entries.Count==2 && a.Entries[0].Title=="remote","merge newer records and preserve independent entries");
        a.DeletedEntries.Add(new DeletedCategory {Id="a",At=newer}); SyncData.Merge(a,b,"entries"); Check(!a.Entries.Exists(e=>e.Id=="a"),"deleted records do not resurrect");
        a.Categories.Add(new Category{Id="cat",Name="old",Updated=older}); a.Entries[0].Category="old";
        b.Categories.Add(new Category{Id="cat",Name="new",Updated=newer}); SyncData.Merge(a,b,"categories");
        Check(a.Categories[0].Name=="new" && a.Entries.Find(e=>e.Id=="b").Category=="new","category rename updates references");
        b.DeletedCategories.Add(new DeletedCategory{Id="cat",At=newer}); SyncData.Merge(a,b,"categories"); Check(a.Categories.Count==0,"category deletion wins equal timestamps");
        a.Refs.Add(new RefItem { Path="PRIVATE-PATH" }); a.ProjectRoot="PRIVATE-ROOT"; a.Documents.Add(new LocalDoc{Path="PRIVATE-DOC"});
        var remote=new Store(); remote.Entries.Add(E("untouched",older,"remote"));
        string payload=SyncData.Payload(a,remote,"categories"); var shared=SyncData.Read(payload);
        Check(shared.Entries[0].Id=="untouched","unselected channel preserved on upload");
        Check(!payload.Contains("PRIVATE-"),"shared payload excludes local private metadata");
        Check(SyncData.Compare("2026-10-20T11:00:00+01:00",older)==0,"timezone-aware comparison");
        Check(SyncData.Compare(newer,older)>0,"subsecond precision retained");
        bool rejected=false; try { SyncData.Read("{\"unexpected\":true}"); } catch { rejected=true; } Check(rejected,"invalid remote schema stops upload");
        Check(!SyncData.IsWithin(Path.Combine(root,"sibling"),Path.Combine(root,"sib")),"path protection uses directory boundary");
        string file=Path.Combine(root,"atomic.json"); SyncData.WriteAtomic(file,"{\"v\":1}"); SyncData.WriteAtomic(file,"{\"v\":2}");
        Check(File.ReadAllText(file+".bak").Contains(":1"),"atomic writes retain previous data");
        string doc=LocalDocs.CreateDocx(root,"a&b","","CON<> .");
        using(var z=ZipFile.OpenRead(doc)) { foreach(var e in z.Entries) using(var r=e.Open()) new XmlDocument().Load(r); } Check(true,"Word document is valid XML/OOXML");
        SyncData.WriteAtomic(Path.Combine(root,"workspace.json"),"{\"folder\":"+SyncData.Serializer().Serialize(root)+"}");
        SyncData.WriteAtomic(Path.Combine(root,"sync_config.json"),"{\"owner\":\"\",\"repo\":\"\",\"scope\":\"entries,categories\"}");
        File.WriteAllText(Path.Combine(root,"lang.txt"),"zh");
        var dpiContext = GetThreadDpiAwarenessContext();
        Console.WriteLine("DPI context: " + dpiContext.ToInt64());
        Check(AreDpiAwarenessContextsEqual(dpiContext, new IntPtr(-4)), "Windows UI runs in PerMonitorV2 DPI mode");
        using(var f=new MainForm())
        {
            f.Show(); Application.DoEvents();
            for(int i=0;i<6;i++) { f.SetPagePublic(i); Application.DoEvents(); }
            Lang.En=true; Lang.Apply(f); f.SetPagePublic(3); Application.DoEvents(); f.CaptureTo(Path.Combine(root,"windows-en.png"));
            Lang.En=false; Lang.Apply(f); f.SetPagePublic(0); Application.DoEvents(); f.CaptureTo(Path.Combine(root,"windows-zh.png"));
            f.SetSizePublic(1020, 680); Application.DoEvents();
            var fields = BindingFlags.NonPublic | BindingFlags.Instance;
            f.SetPagePublic(0); Application.DoEvents();
            var workspaceAction = (Button)typeof(MainForm).GetField("btnWorkspaceHeader", fields).GetValue(f);
            var openWorkspaceAction = (Button)typeof(MainForm).GetField("btnOpenWorkspaceHeader", fields).GetValue(f);
            Check(workspaceAction.Visible && openWorkspaceAction.Visible, "workspace actions remain available on Team Research");
            f.SetPagePublic(1); Application.DoEvents();
            Check(!workspaceAction.Visible && !openWorkspaceAction.Visible, "unrelated workspace actions are hidden on other pages");
            var pagePanels = (Panel[])typeof(MainForm).GetField("pages", fields).GetValue(f);
            var postCard = (Panel)typeof(MainForm).GetField("postCard", fields).GetValue(f);
            var postComposer = (TextBox)typeof(MainForm).GetField("postComposer", fields).GetValue(f);
            var postPublish = (Button)typeof(MainForm).GetField("postPublishButton", fields).GetValue(f);
            var postHistory = (Button)typeof(MainForm).GetField("postHistoryButton", fields).GetValue(f);
            Check(pagePanels[1].ClientRectangle.Contains(postCard.Bounds) && postCard.ClientRectangle.Contains(postComposer.Bounds) && postCard.ClientRectangle.Contains(postPublish.Bounds) &&
                postCard.ClientRectangle.Contains(postHistory.Bounds), "Quick Post controls fit at the minimum supported window size");
            f.SetPagePublic(5); Application.DoEvents();
            var toolbar = (FlowLayoutPanel)typeof(MainForm).GetField("helpToolbar", fields).GetValue(f);
            foreach (Control button in toolbar.Controls) Application.DoEvents();
            bool helpButtonsFit = toolbar.Controls.Count == 4;
            foreach (Control button in toolbar.Controls)
                helpButtonsFit = helpButtonsFit && toolbar.ClientRectangle.Contains(button.Bounds);
            Check(helpButtonsFit, "all four bilingual Help toolbar buttons fit without clipping");
            var updateButton = (Button)typeof(MainForm).GetField("updateButton", fields).GetValue(f);
            var sidebarPanel = (Panel)typeof(MainForm).GetField("sidebar", fields).GetValue(f);
            Check(updateButton != null && updateButton.Parent == sidebarPanel && sidebarPanel.ClientRectangle.Contains(updateButton.Bounds),
                "update button sits in the bottom-left sidebar");
            f.SetPagePublic(4); Application.DoEvents();
            var syncCard = (Panel)typeof(MainForm).GetField("syncCard", fields).GetValue(f);
            Check(pagePanels[4].ClientRectangle.Contains(syncCard.Bounds), "Team Sync card fits inside the page at minimum size");
            string[] syncFields = new string[] { "boundLabel", "syncHint", "lblSStatus", "txtSLog" };
            bool syncContentFits = true;
            foreach (string name in syncFields)
            {
                Control control = (Control)typeof(MainForm).GetField(name, fields).GetValue(f);
                syncContentFits = syncContentFits && syncCard.ClientRectangle.Contains(control.Bounds);
            }
            Check(syncContentFits, "Team Sync text and log fit inside the minimum-size card");
            Check(((Label)typeof(MainForm).GetField("boundLabel",fields).GetValue(f)).Text.Contains("bacoyc0928-art/research-data"), "Chinese bound-repository label retains repository name and branch");
            var account = (Control)typeof(MainForm).GetField("lblSyncAccount", fields).GetValue(f);
            bool actionsFit = true;
            foreach (Control child in syncCard.Controls) if (child is Button)
                actionsFit = actionsFit && syncCard.ClientRectangle.Contains(child.Bounds) && !account.Bounds.IntersectsWith(child.Bounds);
            Check(actionsFit && syncCard.ClientRectangle.Contains(account.Bounds), "sync account and all six actions fit without overlap at minimum size");
            f.CaptureTo(Path.Combine(root,"sync-minimum.png"));
            f.SetPagePublic(3); Application.DoEvents();
            var progressList = (ListView)typeof(MainForm).GetField("progList",fields).GetValue(f);
            Control localCard = progressList.Parent;
            bool localFits = true;
            foreach (Control child in localCard.Controls) localFits = localFits && localCard.ClientRectangle.Contains(child.Bounds);
            Check(localFits, "all three local workspace cards and choose/refresh buttons fit at minimum width");
            f.CaptureTo(Path.Combine(root,"local-minimum.png"));
            var help = (TextBox)typeof(MainForm).GetField("helpEditor",fields).GetValue(f);
            Check(help.ReadOnly, "help is read-only before login on a fresh launch");
            help.Text = "LOCAL DRAFT - must survive language switch";
            Lang.En = true; Lang.Apply(f); Lang.En = false; Lang.Apply(f);
            Check(help.Text == "LOCAL DRAFT - must survive language switch", "language switching preserves help drafts");
            Lang.En = true; Lang.Apply(f);
            Check(progressList.Columns[0].Text == "Name", "list headings follow English even before toggling handler runs");
            PickerCheck(f,root,false); PickerCheck(f,root,true);
            Lang.En = false; Lang.Apply(f); PickerCheck(f,root,true);
            typeof(MainForm).GetMethod("AddTeamCard",fields).Invoke(f,new object[]{"a-long-member-name", "Connected · last active 11-03 10:00",false,true});
            var teamFlow = (FlowLayoutPanel)typeof(MainForm).GetField("teamFlow",fields).GetValue(f);
            Control memberCard=teamFlow.Controls[teamFlow.Controls.Count-1];
            Label memberNote=null; Button view=null;
            foreach(Control child in memberCard.Controls) { if(child is Label && child.Top>Ui.S(25)) memberNote=(Label)child; if(child is Button) view=(Button)child; }
            Check(memberNote != null && view != null && memberNote.Right<=view.Left, "other-member state cannot overlap its open button");
            f.Close();
            Check(File.ReadAllText(Path.Combine(root,"help_local.md"))=="LOCAL DRAFT - must survive language switch", "help draft survives normal application exit");
            var sf=typeof(MainForm).GetField("store",BindingFlags.NonPublic|BindingFlags.Instance); var data=(Store)sf.GetValue(f);
            Check(data.Entries.Count==0 && data.Categories.Count==0,"fresh install has no personal seed data");
        }
        Check(true,"all six Windows pages and language switch render");
        Console.WriteLine("TOTAL " + count + " checks passed");
    }
}
