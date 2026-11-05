using System;
using System.Collections.Generic;
using System.Drawing;
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
    /// <summary>通用布局审计：同一容器内可见控件不得重叠、不得超出父容器，单行标签的文字高度不得被裁掉。</summary>
    static List<string> LayoutIssues(Control top, string where)
    {
        var issues = new List<string>();
        Action<Control> walk = null;
        walk = delegate(Control parent)
        {
            var kids = new List<Control>();
            foreach (Control c in parent.Controls) if (c.Visible && c.Width > 0 && c.Height > 0) kids.Add(c);
            foreach (Control c in kids)
            {
                bool leaf = c is Label || c is Button || c is TextBox || c is CheckBox || c is ListView;
                if (leaf && !(parent is FlowLayoutPanel) && !(parent is ScrollableControl && ((ScrollableControl)parent).AutoScroll))
                {
                    Rectangle area = parent.ClientRectangle;
                    if (!area.Contains(c.Bounds)) issues.Add(where + ": '" + Name(c) + "' exceeds " + Name(parent) + " " + c.Bounds + " / " + area);
                }
                Label l = c as Label;
                if (l != null && !l.AutoSize && !string.IsNullOrEmpty(l.Text) && l.Text.IndexOf('\n') < 0)
                {
                    int need = TextRenderer.MeasureText(l.Text, l.Font).Height;
                    if (need > l.ClientSize.Height + 2) issues.Add(where + ": label '" + Name(l) + "' text height " + need + " > " + l.ClientSize.Height);
                }
            }
            if (!(parent is FlowLayoutPanel))
                for (int i = 0; i < kids.Count; i++)
                    for (int j = i + 1; j < kids.Count; j++)
                    {
                        Control a = kids[i], b = kids[j];
                        bool la = a is Label || a is Button || a is TextBox || a is CheckBox, lb = b is Label || b is Button || b is TextBox || b is CheckBox;
                        // 叶子控件与叶子控件、或叶子控件与兄弟容器（列表/卡片流）重叠都会遮挡文字。
                        if ((la || lb) && a.Bounds.IntersectsWith(b.Bounds))
                            issues.Add(where + ": '" + Name(a) + "' overlaps '" + Name(b) + "' " + a.Bounds + " " + b.Bounds);
                    }
            foreach (Control c in kids) if (c.Controls.Count > 0) walk(c);
        };
        walk(top);
        return issues;
    }
    static string Name(Control c)
    {
        string t = (c.Text ?? "").Replace("\r", " ").Replace("\n", " ");
        if (t.Length > 24) t = t.Substring(0, 24);
        return c.GetType().Name + ":" + (string.IsNullOrEmpty(c.Name) ? t : c.Name);
    }
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
            bool helpButtonsFit = toolbar.Controls.Count == 3;   // 改动日志已随日志同步一起取消
            foreach (Control button in toolbar.Controls)
                helpButtonsFit = helpButtonsFit && toolbar.ClientRectangle.Contains(button.Bounds);
            Check(helpButtonsFit, "all three Help toolbar buttons fit without clipping (no change-log button)");
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
            var helpButtons = (Button[])typeof(MainForm).GetField("helpToolbarButtons",fields).GetValue(f);
            bool signedOutLocked = !postPublish.Enabled && !postHistory.Enabled && postComposer.ReadOnly;
            foreach (Button button in helpButtons) signedOutLocked = signedOutLocked && !button.Enabled;
            foreach (Control child in syncCard.Controls) if (child is Button && child.Visible) signedOutLocked = signedOutLocked && !child.Enabled;
            Check(signedOutLocked, "signed-out state visibly disables Quick Post, Help toolbar and all sync actions");
            Check(syncCard.Controls.Count > 0 && postPublish.BackColor != Color.White, "disabled buttons use the grey disabled look");
            typeof(MainForm).GetField("sessionToken",fields).SetValue(f,"offline-test-not-a-credential");
            typeof(MainForm).GetField("sessionLogin",fields).SetValue(f,"offline-tester");
            typeof(MainForm).GetMethod("UpdateLoginUi",fields).Invoke(f,null); Application.DoEvents();
            bool signedInOpen = postPublish.Enabled && postHistory.Enabled && !postComposer.ReadOnly && !help.ReadOnly && postPublish.BackColor == Theme.Accent && postHistory.BackColor == Color.White;
            foreach (Button button in helpButtons) signedInOpen = signedInOpen && button.Enabled;
            foreach (Control child in syncCard.Controls) if (child is Button && child.Visible) signedInOpen = signedInOpen && child.Enabled;
            Check(signedInOpen, "signing in re-enables Quick Post, Help toolbar and sync actions with normal look");
            var groupTitles = (Label[])typeof(MainForm).GetField("syncGroupTitles",fields).GetValue(f);
            var upRefs = (Button)typeof(MainForm).GetField("btnUpRefs",fields).GetValue(f);
            var others = (Button)typeof(MainForm).GetField("btnOthers",fields).GetValue(f);
            f.SetPagePublic(4); Application.DoEvents();
            Check(groupTitles.Length == 3 && groupTitles[0].Bottom <= others.Top && groupTitles[1].Bottom <= upRefs.Top && upRefs.BackColor == Theme.Accent && others.BackColor == Color.White,
                "Progress Sync groups actions under three captions and highlights uploads as primary");
            var groups = (Button[][])typeof(MainForm).GetField("syncGroups",fields).GetValue(f);
            var publicUp = (Button)typeof(MainForm).GetField("btnPublicUp",fields).GetValue(f);
            var publicDown = (Button)typeof(MainForm).GetField("btnPublicDown",fields).GetValue(f);
            Check(Array.IndexOf(groups[0], publicDown) >= 0 && Array.IndexOf(groups[1], publicUp) >= 0 && groups[2].Length == 1 && publicUp.BackColor == Color.White && publicUp.Visible,
                "Share group offers Upload to Public Library (secondary style) and Get group offers Download Public Library; no activity-log button");
            f.SetPagePublic(2); Application.DoEvents();
            var refsList = (ListBox)typeof(MainForm).GetField("refsFolders",fields).GetValue(f);
            var refsFilesView = (ListView)typeof(MainForm).GetField("refsFiles",fields).GetValue(f);
            Check(refsList.Items.Count >= 3 && refsList.Dock == DockStyle.Left && refsFilesView.Parent != null && refsList.Right <= refsFilesView.Parent.Left + 1,
                "References lists folders vertically on the left with the file list on the right");
            var signedInIssues = new List<string>();
            for (int page = 0; page < 6; page++) { f.SetPagePublic(page); Application.DoEvents(); signedInIssues.AddRange(LayoutIssues(f, "signed-in page" + page)); }
            foreach (string issue in signedInIssues) Console.WriteLine("LAYOUT " + issue);
            Check(signedInIssues.Count == 0, "signed-in pages have no overlapping or clipped controls");
            typeof(MainForm).GetField("sessionToken",fields).SetValue(f,null);
            typeof(MainForm).GetField("sessionLogin",fields).SetValue(f,null);
            typeof(MainForm).GetMethod("UpdateLoginUi",fields).Invoke(f,null); Application.DoEvents();
            f.SetPagePublic(3); Application.DoEvents();
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
            var layoutIssues = new List<string>();
            foreach (bool en in new[] { false, true })
            {
                Lang.En = en; Lang.Apply(f);
                foreach (int[] size in new[] { new[] { 1280, 800 }, new[] { 1020, 680 } })
                {
                    f.SetSizePublic(size[0], size[1]); Application.DoEvents();
                    for (int page = 0; page < 6; page++)
                    {
                        f.SetPagePublic(page); Application.DoEvents();
                        layoutIssues.AddRange(LayoutIssues(f, (en ? "en" : "zh") + " " + size[0] + " page" + page));
                    }
                }
            }
            Lang.En = false; Lang.Apply(f);
            var distinct = new List<string>(new HashSet<string>(layoutIssues)); distinct.Sort();
            foreach (string issue in distinct) Console.WriteLine("LAYOUT " + issue);
            Check(distinct.Count == 0 || Environment.GetEnvironmentVariable("SRPREC_LAYOUT_REPORT_ONLY") == "1", "no overlapping, clipped or out-of-card controls on six pages, both languages, default and minimum size (" + distinct.Count + " issues)");
            f.Close();
            Check(File.ReadAllText(Path.Combine(root,"help_local.md"))=="LOCAL DRAFT - must survive language switch", "help draft survives normal application exit");
            var sf=typeof(MainForm).GetField("store",BindingFlags.NonPublic|BindingFlags.Instance); var data=(Store)sf.GetValue(f);
            Check(data.Entries.Count==0 && data.Categories.Count==0,"fresh install has no personal seed data");
        }
        Check(true,"all six Windows pages and language switch render");
        Console.WriteLine("TOTAL " + count + " checks passed");
    }
}
