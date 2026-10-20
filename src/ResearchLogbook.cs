using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace SRLogbook
{
    public class Entry
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Date { get; set; }
        public string Category { get; set; }
        public string Status { get; set; }
        public int Progress { get; set; }
        public string Content { get; set; }
        public string Updated { get; set; }
    }

    public class RefItem
    {
        public string Title { get; set; }
        public string Path { get; set; }
        public string RelPath { get; set; }
        public string Kind { get; set; }
        public bool Personal { get; set; }
    }

    public class CatLink
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string Kind { get; set; }
    }

    public class Category
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Updated { get; set; }
        public int TaskTotal { get; set; }
        public int TaskDone { get; set; }
        public double Hours { get; set; }
        public List<CatLink> Links { get; set; }

        public Category()
        {
            Links = new List<CatLink>();
        }
    }

    public class LocalDoc
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string Category { get; set; }
        public string Updated { get; set; }
    }

    public class DeletedCategory
    {
        public string Id { get; set; }
        public string At { get; set; }
    }

    public class Store
    {
        public string ProjectRoot { get; set; }
        public List<Entry> Entries { get; set; }
        public List<RefItem> Refs { get; set; }
        public List<Category> Categories { get; set; }
        public List<LocalDoc> Documents { get; set; }
        public List<DeletedCategory> DeletedCategories { get; set; }
        public List<DeletedCategory> DeletedEntries { get; set; }
        public bool CategoriesInitialized { get; set; }
        public string DocProject { get; set; }

        public Store()
        {
            Entries = new List<Entry>();
            Refs = new List<RefItem>();
            Categories = new List<Category>();
            Documents = new List<LocalDoc>();
            DeletedCategories = new List<DeletedCategory>();
            DeletedEntries = new List<DeletedCategory>();
        }
    }

    public static class Ui
    {
        [DllImport("user32.dll")] private static extern bool SetProcessDpiAwarenessContext(IntPtr value);
        [DllImport("shcore.dll")] private static extern int SetProcessDpiAwareness(int value);
        public static void EnableHighDpi()
        {
            try { if (SetProcessDpiAwarenessContext(new IntPtr(-4))) return; } catch (EntryPointNotFoundException) { } catch (DllNotFoundException) { }
            try { SetProcessDpiAwareness(2); } catch (EntryPointNotFoundException) { } catch (DllNotFoundException) { }
        }
        public static float Scale = 1f;
        public static bool StaticBuild = false;
        public static void Init()
        {
            try
            {
                using (Graphics g = Graphics.FromHwnd(IntPtr.Zero)) { Scale = g.DpiX / 96f; }
            }
            catch { Scale = 1f; }
            if (Scale < 1f) Scale = 1f;
        }
        public static int S(int v) { return StaticBuild ? v : (int)Math.Round(v * Scale); }
    }

    public static class Theme
    {
        public static readonly Color Navy = Color.FromArgb(31, 42, 68);
        public static readonly Color SideActive = Color.FromArgb(43, 58, 88);
        public static readonly Color Accent = Color.FromArgb(45, 108, 223);
        public static readonly Color AccentSoft = Color.FromArgb(232, 239, 253);
        public static readonly Color Warm = Color.FromArgb(196, 122, 42);
        public static readonly Color Bg = Color.FromArgb(244, 246, 250);
        public static readonly Color CardBorder = Color.FromArgb(226, 231, 240);
        public static readonly Color TextMain = Color.FromArgb(31, 41, 55);
        public static readonly Color TextMuted = Color.FromArgb(94, 105, 122);
        public static readonly Color TextFaint = Color.FromArgb(132, 141, 158);
        public const string UiFont = "Microsoft YaHei UI";

        public static Font F(float size)
        {
            return new Font(UiFont, size, FontStyle.Regular);
        }

        public static Font FB(float size)
        {
            return new Font(UiFont, size, FontStyle.Bold);
        }
    }

    public class SideButton : Panel
    {
        private Label lbl;
        private Panel bar;
        public event EventHandler Clicked;

        public SideButton(string text, int y, int width)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            BackColor = Theme.Navy;
            Height = 46;
            Width = width;
            Location = new Point(0, y);
            Cursor = Cursors.Hand;

            bar = new Panel();
            bar.SetBounds(0, 12, 4, 22);
            bar.BackColor = Theme.Navy;
            Controls.Add(bar);

            lbl = new Label();
            lbl.Text = text;
            lbl.AutoSize = false;
            lbl.SetBounds(26, 0, width - 30, 46);
            lbl.TextAlign = ContentAlignment.MiddleLeft;
            lbl.ForeColor = Color.FromArgb(196, 208, 228);
            lbl.Font = Theme.F(10.5f);
            lbl.BackColor = Theme.Navy;
            lbl.Cursor = Cursors.Hand;
            Controls.Add(lbl);

            EventHandler h = delegate(object s, EventArgs e) { if (Clicked != null) Clicked(this, e); };
            lbl.Click += h;
            Click += h;
            lbl.MouseEnter += delegate { if (BackColor == Theme.Navy) { BackColor = Color.FromArgb(38, 51, 79); lbl.BackColor = Color.FromArgb(38, 51, 79); } };
            lbl.MouseLeave += delegate { if (BackColor == Color.FromArgb(38, 51, 79)) { BackColor = Theme.Navy; lbl.BackColor = Theme.Navy; } };
        }

        public void SetSelected(bool sel)
        {
            bar.BackColor = sel ? Theme.Accent : Theme.Navy;
            BackColor = sel ? Theme.SideActive : Theme.Navy;
            lbl.BackColor = BackColor;
            lbl.ForeColor = sel ? Color.White : Color.FromArgb(196, 208, 228);
            lbl.Font = sel ? Theme.FB(10.5f) : Theme.F(10.5f);
        }
    }

    /// <summary>
    /// 把鼠标滚轮事件转发给参考文献页的 FlowLayoutPanel：
    /// WinForms 的滚轮消息只会送到焦点控件，鼠标停在卡片上时页面不会滚动。
    /// </summary>
    public class WheelRouter : IMessageFilter
    {
        private readonly FlowLayoutPanel target;
        public WheelRouter(FlowLayoutPanel t) { target = t; }

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(Point p);

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg != 0x020A) return false;   // WM_MOUSEWHEEL
            if (target == null || target.IsDisposed) return false;
            Control c = Control.FromHandle(WindowFromPoint(Cursor.Position));
            if (c == null) return false;
            Control p = c;
            while (p != null && p != target) p = p.Parent;
            if (p == null) return false;

            int delta = (short)((long)m.WParam >> 16);
            int max = target.VerticalScroll.Maximum - target.VerticalScroll.LargeChange + 1;
            if (max < 0) max = 0;
            int y = -target.AutoScrollPosition.Y - delta;
            if (y < 0) y = 0;
            if (y > max) y = max;
            target.AutoScrollPosition = new Point(0, y);
            return true;
        }
    }

    public class RefCard : Panel
    {
        private Color borderColor = Theme.CardBorder;
        private ToolTip tip = new ToolTip();

        public RefCard(RefItem item, Action<RefItem> onOpen, Action<RefItem> onLocate, Action<RefItem> onRemove)
        {
            Width = Ui.S(430);
            Height = Ui.S(126);
            BackColor = Color.White;
            Margin = new Padding(0, 0, Ui.S(14), Ui.S(14));
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

            string kind = item.Kind == null ? "file" : item.Kind.ToLower();
            Color kc;
            string badge;
            if (kind == "folder") { kc = Theme.Warm; badge = "文件夹"; }
            else if (kind == "pdf") { kc = Color.FromArgb(190, 60, 55); badge = "PDF"; }
            else if (kind == "docx") { kc = Color.FromArgb(46, 84, 150); badge = "DOCX"; }
            else { kc = Color.FromArgb(80, 110, 140); badge = "文件"; }

            Panel strip = new Panel();
            strip.SetBounds(0, 0, Ui.S(5), Height);
            strip.BackColor = kc;
            Controls.Add(strip);

            Label lbBadge = new Label();
            lbBadge.Text = badge;
            lbBadge.Font = Theme.FB(8f);
            lbBadge.ForeColor = kc;
            lbBadge.BackColor = Color.White;
            lbBadge.SetBounds(Ui.S(18), Ui.S(12), Ui.S(90), Ui.S(16));
            Controls.Add(lbBadge);

            Label lbTitle = new Label();
            lbTitle.Text = item.Title;
            lbTitle.Font = Theme.FB(11f);
            lbTitle.ForeColor = Theme.Navy;
            lbTitle.BackColor = Color.White;
            lbTitle.AutoEllipsis = true;
            lbTitle.SetBounds(Ui.S(18), Ui.S(32), Width - Ui.S(36), Ui.S(24));
            Controls.Add(lbTitle);

            tip.SetToolTip(lbTitle, item.Title);
            tip.SetToolTip(lbBadge, item.Title + "\r\n" + item.Path);
            Label lbPath = new Label();
            lbPath.Text = item.Path;
            lbPath.Font = Theme.F(8.2f);
            lbPath.ForeColor = Theme.TextFaint;
            lbPath.BackColor = Color.White;
            lbPath.AutoEllipsis = true;
            lbPath.SetBounds(Ui.S(18), Ui.S(60), Width - Ui.S(36), Ui.S(18));
            tip.SetToolTip(lbPath, item.Path);
            Controls.Add(lbPath);

            Button bOpen = MakeFlat("打开", 66, 28, Theme.Accent, Color.White, true);
            bOpen.SetBounds(Ui.S(18), Ui.S(86), Ui.S(66), Ui.S(28));
            bOpen.Click += delegate { onOpen(item); };
            Controls.Add(bOpen);

            Button bLoc = MakeFlat("定位", 66, 28, Color.White, Theme.TextMain, false);
            bLoc.SetBounds(Ui.S(92), Ui.S(86), Ui.S(66), Ui.S(28));
            bLoc.Click += delegate { onLocate(item); };
            Controls.Add(bLoc);

            Button bDel = MakeFlat("移除", 58, 28, Color.White, Theme.TextFaint, false);
            bDel.SetBounds(Ui.S(166), Ui.S(86), Ui.S(58), Ui.S(28));
            bDel.Click += delegate { onRemove(item); };
            Controls.Add(bDel);
            if (item.Personal || (item.Kind != null && item.Kind == "default")) bDel.Visible = false;

            this.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen p = new Pen(borderColor))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
                }
            };
            this.MouseEnter += delegate { borderColor = Color.FromArgb(178, 198, 232); Invalidate(); };
            this.MouseLeave += delegate { borderColor = Theme.CardBorder; Invalidate(); };
            this.DoubleClick += delegate { onOpen(item); };
            lbTitle.DoubleClick += delegate { onOpen(item); };
            lbBadge.DoubleClick += delegate { onOpen(item); };
            lbPath.DoubleClick += delegate { onOpen(item); };
        }

        private Button MakeFlat(string text, int w, int h, Color back, Color fore, bool filled)
        {
            Button b = new Button();
            b.Text = text;
            b.Font = Theme.F(9.2f);
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = filled ? 0 : 1;
            b.FlatAppearance.BorderColor = Theme.CardBorder;
            b.BackColor = back;
            b.ForeColor = fore;
            b.Cursor = Cursors.Hand;
            b.TabStop = false;
            return b;
        }
    }

    public partial class MainForm : Form
    {
        private string appDir;
        private string dataPath;
        private string projectRoot;
        private string workspaceFolder;
        private Store store;
        public static bool StartupCancelled;

        private Panel sidebar;
        private Panel loginPanel;
        private Panel avatarPanel;
        private Label loginNameLabel;
        private Label loginHintLabel;
        private TextBox loginTokenBox;
        private Button loginButton;
        private Button logoutButton;
        private string sessionToken;
        private string sessionLogin;
        private Panel header;
        private Panel pagesHost;
        private Label lblHeaderTitle;
        private SideButton[] navs;
        private Panel[] pages;
        private int pageIndex;

        private ListBox lstEntries;
        private TextBox txtSearch;
        private TextBox txtTitle;
        private TextBox txtContent;
        private DateTimePicker dtDate;
        private ComboBox cbCat;
        private ComboBox cbStatus;
        private TrackBar tbProg;
        private Label lblProgress;
        private Label lblSaved;
        private Label lblCount;
        private Button btnDelete;
        private FlowLayoutPanel flow;
        private Panel p3;
        private Timer saveTimer;
        private Entry current;
        private bool loading;
        private Action layoutLog;
        private Panel refsTop;

        public MainForm()
        {
            Ui.Init();
            appDir = Path.GetDirectoryName(Application.ExecutablePath);
            Lang.Load(appDir);
            workspaceFolder = LoadWorkspace();
            if (!Directory.Exists(workspaceFolder))
            {
                workspaceFolder = AppPaths.Ensure(appDir);
                SaveWorkspace(workspaceFolder);
            }
            projectRoot = workspaceFolder;
            dataPath = Path.Combine(appDir, "research_log.json");
            store = LoadStore();
            RefItem defaultRef = store.Refs.Find(r => r.Kind == "default" || r.Title == "Default Reference Material");
            if (defaultRef == null) { defaultRef = new RefItem { Title = "Default Reference Material" }; store.Refs.Insert(0, defaultRef); }
            defaultRef.Kind = "default";
            defaultRef.Path = AppPaths.DefaultReferences(appDir);
            defaultRef.RelPath = "";

            this.Text = "SRPRecorder2027 · 0.1.1";
            this.ClientSize = new Size(1180, 730);
            this.MinimumSize = new Size(1020, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Theme.Bg;
            this.Font = Theme.F(10f);
            this.DoubleBuffered = true;

            saveTimer = new Timer();
            saveTimer.Interval = 700;
            saveTimer.Tick += delegate { saveTimer.Stop(); SaveCurrent(); };

            BuildHeader();
            BuildSidebar();
            UpdateLoginUi();
            Ui.StaticBuild = true;
            BuildPages();
            Ui.StaticBuild = false;
            pagesHost.BringToFront();
            SetPage(0);
            RefreshCategoryCombo();
            RefreshList();
            if (lstEntries != null && lstEntries.Items.Count > 0)
            {
                lstEntries.SelectedIndex = 0;
            }
            FormClosing += delegate(object sender, FormClosingEventArgs e) { if (syncBusy || teamLoading) { e.Cancel = true; return; } saveTimer.Stop(); SaveCurrent(); };
            if (Lang.En) Lang.Apply(this);
        }

        private string ComputeProjectRoot()
        {
            try
            {
                string up2 = Path.GetFullPath(Path.Combine(appDir, "..", ".."));
                if (Directory.Exists(up2) && Path.GetFileName(up2).IndexOf("Synchrotron", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return up2;
                }
            }
            catch { }
            return appDir;
        }

        private string WorkspacePath() { return Path.Combine(appDir, "workspace.json"); }

        private string LoadWorkspace()
        {
            try
            {
                if (File.Exists(WorkspacePath()))
                {
                    JavaScriptSerializer ser = new JavaScriptSerializer();
                    Dictionary<string, object> d = ser.Deserialize<Dictionary<string, object>>(File.ReadAllText(WorkspacePath(), Encoding.UTF8));
                    if (d != null && d.ContainsKey("folder"))
                    {
                        string f = Convert.ToString(d["folder"]);
                        if (!string.IsNullOrEmpty(f) && Directory.Exists(f)) return f;
                    }
                }
            }
            catch { }
            return "";
        }

        private void SaveWorkspace(string folder)
        {
            try
            {
                JavaScriptSerializer ser = new JavaScriptSerializer();
                Dictionary<string, object> d = new Dictionary<string, object>();
                d["folder"] = folder;
                File.WriteAllText(WorkspacePath(), ser.Serialize(d), new UTF8Encoding(false));
            }
            catch { }
        }

        private Store LoadStore()
        {
            JavaScriptSerializer ser = new JavaScriptSerializer();
            ser.MaxJsonLength = 20000000;
            try
            {
                if (File.Exists(dataPath))
                {
                    string json = File.ReadAllText(dataPath, Encoding.UTF8);
                    Store s = SyncData.Read(json);
                    if (s != null)
                    {
                        SyncData.Normalize(s);
                        foreach (Entry e in s.Entries)
                        {
                            if (string.IsNullOrEmpty(e.Id)) e.Id = Guid.NewGuid().ToString("N");
                            DateTime date;
                            e.Date = DateTime.TryParse(e.Date, out date) ? date.ToString("yyyy-MM-dd") : DateTime.Today.ToString("yyyy-MM-dd");
                        }
                        if (s.Categories.Count == 0 && !s.CategoriesInitialized) MigrateCategories(s);
                        s.CategoriesInitialized = true;
                        for (int i = s.Refs.Count - 1; i >= 0; i--)
                        {
                            for (int j = i - 1; j >= 0; j--)
                            {
                                if (!string.IsNullOrEmpty(s.Refs[i].Path) && s.Refs[i].Path == s.Refs[j].Path) { s.Refs.RemoveAt(i); break; }
                            }
                        }
                        bool hasDefault = false;
                        foreach (RefItem r0 in s.Refs) if (r0.Kind == "default") hasDefault = true;
                        if (!hasDefault)
                        {
                            string def = AppPaths.DefaultReferences(appDir);
                            if (!Directory.Exists(def)) def = Path.Combine(projectRoot, "同步辐射器件资料", "Default Reference Material");
                            RefItem found = null;
                            foreach (RefItem r1 in s.Refs)
                                if ((r1.Title != null && r1.Title.IndexOf("Default Reference Material", StringComparison.OrdinalIgnoreCase) >= 0) ||
                                    (r1.Path != null && r1.Path.EndsWith("Default Reference Material", StringComparison.OrdinalIgnoreCase))) found = r1;
                            if (found != null) found.Kind = "default";
                            else if (Directory.Exists(def)) s.Refs.Insert(0, new RefItem { Title = "Default Reference Material", Path = def, RelPath = "", Kind = "default" });
                        }
                        projectRoot = !string.IsNullOrEmpty(workspaceFolder) && Directory.Exists(workspaceFolder)
                            ? workspaceFolder
                            : ((s.ProjectRoot != null && Directory.Exists(s.ProjectRoot)) ? s.ProjectRoot : ComputeProjectRoot());
                        s.ProjectRoot = projectRoot;
                        return s;
                    }
                }
            }
            catch (Exception ex) { throw new InvalidDataException("Cannot read research_log.json. The original file was preserved; restore research_log.json.bak if needed.", ex); }
            Store ns = new Store();
            projectRoot = workspaceFolder;
            ns.ProjectRoot = projectRoot;
            ns.CategoriesInitialized = true;
            // Start empty on a new installation; no personal sample data.

            return ns;
        }

        private void MigrateCategories(Store s)
        {
            Dictionary<string, Category> map = new Dictionary<string, Category>();
            foreach (Entry e in s.Entries)
            {
                string name = string.IsNullOrEmpty(e.Category) ? "其他" : e.Category;
                if (!map.ContainsKey(name))
                {
                    Category c = new Category();
                    c.Id = Guid.NewGuid().ToString("N");
                    c.Name = name;
                    c.TaskTotal = 0; c.TaskDone = 0; c.Hours = 0;
                    map[name] = c;
                }
                map[name].TaskTotal++;
                if (e.Status == "已完成") map[name].TaskDone++;
            }
            foreach (Category c in map.Values) s.Categories.Add(c);
            AddDefaultCategory(s, "文献阅读", 8, 2, 20);
            AddDefaultCategory(s, "实验设计", 4, 0, 12);
            AddDefaultCategory(s, "数据分析", 4, 1, 10);
            AddDefaultCategory(s, "写作/汇报", 5, 2, 15);
            AddDefaultCategory(s, "组会/讨论", 6, 1, 6);
            AddDefaultCategory(s, "其他", 3, 0, 4);
            if (s.Categories.Count == 0)
            {
                s.Categories.Add(new Category { Id = Guid.NewGuid().ToString("N"), Name = "文献阅读", TaskTotal = 5, TaskDone = 0, Hours = 10 });
                s.Categories.Add(new Category { Id = Guid.NewGuid().ToString("N"), Name = "其他", TaskTotal = 3, TaskDone = 0, Hours = 4 });
            }
        }

        private void ApplyFixedCategories(Store s)
        {
            string user = Environment.UserName;
            string personal = "其他个人材料 -- " + user;
            for (int i = s.Categories.Count - 1; i >= 0; i--)
            {
                string n = s.Categories[i].Name ?? "";
                if (n.StartsWith("其他个人材料", StringComparison.Ordinal))
                {
                    s.Categories[i].Name = personal;
                    continue;
                }
                if (n != "文献" && n != "文献总结" && n != "个人总结" && n != "协作工作栏")
                    s.Categories.RemoveAt(i);
            }
            string[] base4 = new string[] { "文献", "文献总结", "个人总结", "协作工作栏" };
            foreach (string n in base4)
            {
                bool exists = false;
                foreach (Category c in s.Categories) if (c.Name == n) { exists = true; break; }
                if (!exists) s.Categories.Add(new Category { Id = Guid.NewGuid().ToString("N"), Name = n, Updated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ") });
            }
            bool hasPersonal = false;
            foreach (Category c in s.Categories) if (c.Name == personal) { hasPersonal = true; break; }
            if (!hasPersonal) s.Categories.Add(new Category { Id = Guid.NewGuid().ToString("N"), Name = personal, Updated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ") });
        }

        private void AddDefaultCategory(Store s, string name, int total, int done, double hours)
        {
            foreach (Category c in s.Categories) if (c.Name == name) return;
            s.Categories.Add(new Category { Id = Guid.NewGuid().ToString("N"), Name = name, TaskTotal = total, TaskDone = done, Hours = hours });
        }

        private void Seed(Store s)
        {
            string doc1 = Path.Combine(projectRoot, "An Introduction to Synchrotron Radiation Techniques and Applications (Philip Willmott) (z-library.sk, 1lib.sk, z-lib.sk).pdf");
            string doc2 = Path.Combine(projectRoot, "同步辐射装置与狭义相对论基础.docx");
            string doc3 = Path.Combine(projectRoot, "同步辐射器件资料", "旧版（器件详述）", "同步辐射光源核心器件_功能_原理与性能.docx");
            string doc4 = Path.Combine(projectRoot, "Introduction to General Relativity A Course for Undergraduate Students of Physics (Cosimo Bambi) (z-library.sk, 1lib.sk, z-lib.sk).pdf");

            s.Refs.Add(new RefItem { Title = "项目根目录（书籍与文档）", Path = projectRoot, RelPath = "", Kind = "folder" });
            if (File.Exists(doc1)) s.Refs.Add(new RefItem { Title = "An Introduction to Synchrotron Radiation", Path = doc1, RelPath = Path.GetFileName(doc1), Kind = "pdf" });
            if (File.Exists(doc2)) s.Refs.Add(new RefItem { Title = "同步辐射装置与狭义相对论基础", Path = doc2, RelPath = Path.GetFileName(doc2), Kind = "docx" });
            if (File.Exists(doc3)) s.Refs.Add(new RefItem { Title = "旧版：器件详述（高年级可读）", Path = doc3, RelPath = "同步辐射器件资料\\旧版（器件详述）\\同步辐射光源核心器件_功能_原理与性能.docx", Kind = "docx" });
            string doc5 = Path.Combine(projectRoot, "同步辐射器件资料", "高中生版", "01_同步辐射入门_中英对照.docx");
            string doc6 = Path.Combine(projectRoot, "同步辐射器件资料", "高中生版", "02_数学与物理补充_推导讲义.docx");
            if (File.Exists(doc5)) s.Refs.Add(new RefItem { Title = "高中生版：同步辐射入门（中英对照）", Path = doc5, RelPath = "同步辐射器件资料\\高中生版\\01_同步辐射入门_中英对照.docx", Kind = "docx" });
            if (File.Exists(doc6)) s.Refs.Add(new RefItem { Title = "高中生版：数学与物理补充（推导）", Path = doc6, RelPath = "同步辐射器件资料\\高中生版\\02_数学与物理补充_推导讲义.docx", Kind = "docx" });
            if (File.Exists(doc4)) s.Refs.Add(new RefItem { Title = "Introduction to General Relativity (Bambi)", Path = doc4, RelPath = Path.GetFileName(doc4), Kind = "pdf" });
            s.Refs.Add(new RefItem { Title = "本软件所在文件夹", Path = Path.Combine(projectRoot, "同步辐射器件资料"), RelPath = "同步辐射器件资料", Kind = "folder" });
            string litDir = Path.Combine(projectRoot, "同步辐射器件资料", "文献");
            if (Directory.Exists(litDir)) s.Refs.Add(new RefItem { Title = "文献（PDF 资料库）", Path = litDir, RelPath = "同步辐射器件资料\\文献", Kind = "folder" });

            Entry e = new Entry();
            e.Id = Guid.NewGuid().ToString("N");
            e.Title = "项目启动：器件资料整理与记录工具";
            e.Date = DateTime.Now.ToString("yyyy-MM-dd");
            e.Category = "写作/汇报";
            e.Status = "进行中";
            e.Progress = 35;
            e.Updated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
            e.Content = "已完成：\r\n1. 通读 Willmott《An Introduction to Synchrotron Radiation》中关于储存环、插入件与光束线的章节；\r\n2. 梳理同步辐射光源的主要器件，整理为《同步辐射光源核心器件：功能、物理原理与特殊性能》；\r\n3. 搭建本记录工具，把常用书籍与文档加入“参考文献与资料”，点击即可打开。\r\n\r\n下一步：\r\n· 补充波荡器与光束线光学方面的定量数据；\r\n· 为每条结论补上可核查的文献出处。";
            s.Entries.Add(e);

            if (s.Categories.Count == 0)
            {
                string lit = Path.Combine(projectRoot, "同步辐射器件资料", "文献", "文献目录.md");
                Category c1 = new Category { Id = Guid.NewGuid().ToString("N"), Name = "文献阅读", TaskTotal = 8, TaskDone = 2, Hours = 20 };
                if (File.Exists(lit)) c1.Links.Add(new CatLink { Title = "文献目录", Url = lit, Kind = "file" });
                s.Categories.Add(c1);
                s.Categories.Add(new Category { Id = Guid.NewGuid().ToString("N"), Name = "实验设计", TaskTotal = 4, TaskDone = 0, Hours = 12 });
                s.Categories.Add(new Category { Id = Guid.NewGuid().ToString("N"), Name = "数据分析", TaskTotal = 4, TaskDone = 1, Hours = 10 });
                s.Categories.Add(new Category { Id = Guid.NewGuid().ToString("N"), Name = "写作/汇报", TaskTotal = 5, TaskDone = 2, Hours = 15 });
                s.Categories.Add(new Category { Id = Guid.NewGuid().ToString("N"), Name = "组会/讨论", TaskTotal = 6, TaskDone = 1, Hours = 6 });
            }
        }

        private void Persist()
        {
            try
            {
                JavaScriptSerializer ser = new JavaScriptSerializer();
                ser.MaxJsonLength = 20000000;
                store.ProjectRoot = projectRoot;
                string json = ser.Serialize(store);
                SyncData.WriteAtomic(dataPath, json);
            }
            catch (Exception ex)
            {
                Msg.Show("保存失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BuildSidebar()
        {
            sidebar = new Panel();
            sidebar.Dock = DockStyle.Left;
            sidebar.Width = 220;
            sidebar.BackColor = Theme.Navy;
            Controls.Add(sidebar);

            Label brand = new Label();
            brand.Text = "SR";
            brand.Font = new Font("Times New Roman", 22f, FontStyle.Bold);
            brand.ForeColor = Color.FromArgb(120, 170, 230);
            brand.BackColor = Theme.Navy;
            brand.SetBounds(22, 22, 60, 36);
            sidebar.Controls.Add(brand);

            Label title = new Label();
            title.Text = "SRPRecorder2027";
            title.Font = Theme.FB(11f);
            title.ForeColor = Color.White;
            title.BackColor = Theme.Navy;
            title.SetBounds(22, 62, 190, 48);
            sidebar.Controls.Add(title);

            Panel divider = new Panel();
            divider.BackColor = Color.FromArgb(48, 62, 92);
            divider.SetBounds(22, 118, 188, 1);
            sidebar.Controls.Add(divider);

            navs = new SideButton[6];
            string[] texts = new string[] { "团队研究", "快速发行", "参考文献与资料", "本地进度", "团队同步", "使用说明" };
            for (int i = 0; i < texts.Length; i++)
            {
                int idx = i;
                navs[i] = new SideButton(texts[i], 136 + i * 46, 220);
                navs[i].Clicked += delegate { SetPage(idx); };
                sidebar.Controls.Add(navs[i]);
            }

            loginPanel = new Panel();
            loginPanel.BackColor = Theme.Navy;
            loginPanel.SetBounds(14, 0, 192, 122);
            sidebar.Controls.Add(loginPanel);

            avatarPanel = new Panel();
            avatarPanel.SetBounds(0, 0, 40, 40);
            avatarPanel.BackColor = Theme.Navy;
            avatarPanel.Paint += delegate(object s, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                string initial = "?";
                if (!string.IsNullOrEmpty(sessionLogin)) initial = sessionLogin.Substring(0, 1).ToUpperInvariant();
                using (SolidBrush b = new SolidBrush(string.IsNullOrEmpty(sessionLogin) ? Color.FromArgb(64, 78, 104) : Theme.Accent))
                    e.Graphics.FillEllipse(b, 0, 0, avatarPanel.Width - 1, avatarPanel.Height - 1);
                using (StringFormat sf = new StringFormat())
                {
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Center;
                    using (Font fnt = Theme.FB(13f))
                        e.Graphics.DrawString(initial, fnt, Brushes.White, new RectangleF(0, 0, avatarPanel.Width, avatarPanel.Height), sf);
                }
            };
            loginPanel.Controls.Add(avatarPanel);

            loginNameLabel = new Label();
            loginNameLabel.Font = Theme.FB(9.5f);
            loginNameLabel.ForeColor = Color.White;
            loginNameLabel.BackColor = Theme.Navy;
            loginNameLabel.SetBounds(50, 2, 142, 20);
            loginPanel.Controls.Add(loginNameLabel);

            loginHintLabel = new Label();
            loginHintLabel.Font = Theme.F(7.6f);
            loginHintLabel.ForeColor = Color.FromArgb(150, 166, 190);
            loginHintLabel.BackColor = Theme.Navy;
            loginHintLabel.SetBounds(50, 22, 142, 18);
            loginPanel.Controls.Add(loginHintLabel);

            loginTokenBox = new TextBox();
            loginTokenBox.Font = Theme.F(8.8f);
            loginTokenBox.UseSystemPasswordChar = true;
            loginTokenBox.SetBounds(0, 50, 192, 26);
            loginPanel.Controls.Add(loginTokenBox);

            loginButton = new Button();
            loginButton.Text = "登录";
            loginButton.Font = Theme.F(9f);
            loginButton.FlatStyle = FlatStyle.Flat;
            loginButton.FlatAppearance.BorderSize = 0;
            loginButton.BackColor = Theme.Accent;
            loginButton.ForeColor = Color.White;
            loginButton.Cursor = Cursors.Hand;
            loginButton.SetBounds(0, 82, 92, 28);
            loginButton.Click += delegate { SignIn(); };
            loginPanel.Controls.Add(loginButton);

            logoutButton = new Button();
            logoutButton.Text = "退出";
            logoutButton.Font = Theme.F(9f);
            logoutButton.FlatStyle = FlatStyle.Flat;
            logoutButton.FlatAppearance.BorderColor = Color.FromArgb(90, 106, 134);
            logoutButton.FlatAppearance.BorderSize = 1;
            logoutButton.BackColor = Color.FromArgb(48, 62, 92);
            logoutButton.ForeColor = Color.White;
            logoutButton.Cursor = Cursors.Hand;
            logoutButton.SetBounds(100, 82, 92, 28);
            logoutButton.Click += delegate { SignOut(); };
            loginPanel.Controls.Add(logoutButton);

            Label foot = new Label();
            foot.Text = "Synchrotron Radiation\r\nProject 2027";
            foot.Font = Theme.F(8f);
            foot.ForeColor = Color.FromArgb(120, 136, 164);
            foot.BackColor = Theme.Navy;
            foot.SetBounds(22, 620, 190, 42);
            foot.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            sidebar.Controls.Add(foot);

            sidebar.Resize += delegate
            {
                foot.Top = sidebar.Height - Ui.S(62);
                loginPanel.Top = Math.Max(320, foot.Top - loginPanel.Height - 10);
            };
        }

        private void UpdateLoginUi()
        {
            if (loginNameLabel == null) return;
            bool signed = !string.IsNullOrEmpty(sessionLogin);
            loginNameLabel.Text = signed ? sessionLogin : (Lang.En ? "Not signed in" : "未登录");
            loginHintLabel.Text = signed
                ? (Lang.En ? "Signed in with GitHub" : "已登录 GitHub")
                : (Lang.En ? "Enter token, then Sign In" : "输入密钥后登录");
            if (loginTokenBox != null) loginTokenBox.Visible = !signed;
            if (loginButton != null) loginButton.Visible = !signed;
            if (logoutButton != null) logoutButton.Visible = signed;
            if (avatarPanel != null) avatarPanel.Invalidate();
            UpdateSyncActionState();
        }

        private async void SignIn()
        {
            if (loginButton == null || !loginButton.Enabled) return;
            string token = CleanToken(loginTokenBox.Text);
            if (token.Length == 0)
            {
                Msg.Show(Lang.En ? "Paste your GitHub token first." : "请先粘贴你的 GitHub 访问密钥。", "登录");
                return;
            }
            loginButton.Enabled = false;
            loginHintLabel.Text = Lang.En ? "Checking…" : "正在验证…";
            try
            {
                string login = await Task.Run(() => GitHubSync.GetUserLogin(token));
                sessionToken = token;
                sessionLogin = login;
                loginTokenBox.Text = "";
                UpdateLoginUi();
                RefreshTeamPage();
                await LogActivityAsync("登录", "身份验证成功");
            }
            catch (Exception ex)
            {
                sessionToken = null;
                sessionLogin = null;
                UpdateLoginUi();
                Msg.Show((Lang.En ? "Sign-in failed: " : "登录失败：") + ex.Message, "登录", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally { loginButton.Enabled = true; Lang.Apply(this); }
        }

        private void SignOut()
        {
            sessionToken = null;
            sessionLogin = null;
            UpdateLoginUi();
            RefreshTeamPage();
        }

        private void BuildHeader()
        {
            header = new Panel();
            header.Dock = DockStyle.Top;
            header.Height = 88;
            header.BackColor = Color.White;
            header.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen p = new Pen(Theme.CardBorder))
                {
                    e.Graphics.DrawLine(p, 0, header.Height - 1, header.Width, header.Height - 1);
                }
            };
            Controls.Add(header);

            lblHeaderTitle = new Label();
            lblHeaderTitle.Text = "研究日志";
            lblHeaderTitle.Font = Theme.FB(17f);
            lblHeaderTitle.ForeColor = Theme.Navy;
            lblHeaderTitle.BackColor = Color.White;
            lblHeaderTitle.SetBounds(28, 16, 520, 30);
            header.Controls.Add(lblHeaderTitle);

            Button bLang = new Button();
            bLang.Text = Lang.En ? "中文" : "EN";
            bLang.Font = Theme.F(8.8f);
            bLang.FlatStyle = FlatStyle.Flat;
            bLang.FlatAppearance.BorderSize = 1;
            bLang.FlatAppearance.BorderColor = Theme.CardBorder;
            bLang.BackColor = Color.White;
            bLang.ForeColor = Theme.TextMain;
            bLang.Cursor = Cursors.Hand;
            bLang.SetBounds(header.Width - 240, 24, 56, 40);
            bLang.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            bLang.Click += delegate
            {
                Lang.En = !Lang.En;
                Lang.Save(appDir);
                Lang.ApplyAll();
                bLang.Text = Lang.En ? "中文" : "EN";
            };
            header.Controls.Add(bLang);

            Button bWs = new Button();
            bWs.Text = "更换工作文件夹";
            bWs.Font = Theme.F(8.8f);
            bWs.FlatStyle = FlatStyle.Flat;
            bWs.FlatAppearance.BorderSize = 1;
            bWs.FlatAppearance.BorderColor = Theme.CardBorder;
            bWs.BackColor = Color.White;
            bWs.ForeColor = Theme.TextMain;
            bWs.Cursor = Cursors.Hand;
            bWs.SetBounds(header.Width - 430, 24, 180, 40);
            bWs.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            bWs.Click += delegate { ChangeWorkspace(); };
            header.Controls.Add(bWs);

            Button bOpen = new Button();
            bOpen.Text = "打开工作文件夹";
            bOpen.Font = Theme.F(8.8f);
            bOpen.FlatStyle = FlatStyle.Flat;
            bOpen.FlatAppearance.BorderSize = 1;
            bOpen.FlatAppearance.BorderColor = Theme.CardBorder;
            bOpen.BackColor = Color.White;
            bOpen.ForeColor = Theme.TextMain;
            bOpen.Cursor = Cursors.Hand;
            bOpen.SetBounds(header.Width - 176, 24, 166, 40);
            bOpen.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            bOpen.Click += delegate { OpenPath(projectRoot); };
            header.Controls.Add(bOpen);
        }

        private void BuildPages()
        {
            pagesHost = new Panel();
            pagesHost.Dock = DockStyle.Fill;
            pagesHost.BackColor = Theme.Bg;
            pagesHost.Padding = new Padding(18);
            Controls.Add(pagesHost);

            pages = new Panel[6];
            pages[0] = BuildTeamPage();
            pages[1] = BuildPostsPage();
            pages[2] = BuildRefsAndCategoriesPage();
            pages[3] = BuildLocalProgressPage();
            pages[4] = BuildSyncPage();
            pages[5] = BuildHelpPage();
            for (int i = 0; i < pages.Length; i++)
            {
                pages[i].Dock = DockStyle.Fill;
                pages[i].BackColor = Color.White;
                pages[i].Visible = false;
                pagesHost.Controls.Add(pages[i]);
            }
        }

        private Panel BuildLogPage()
        {
            Panel page = new Panel();
            page.Padding = new Padding(0);

            Panel editor = new Panel();
            editor.Dock = DockStyle.Fill;
            editor.BackColor = Color.White;

            Panel left = new Panel();
            left.Dock = DockStyle.Left;
            left.Width = 316;
            left.BackColor = Color.FromArgb(250, 251, 253);
            left.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen p = new Pen(Theme.CardBorder))
                {
                    e.Graphics.DrawLine(p, left.Width - 1, 0, left.Width - 1, left.Height);
                }
            };

            page.Controls.Add(editor);
            page.Controls.Add(left);

            txtSearch = new TextBox();
            txtSearch.Font = Theme.F(9.2f);
            txtSearch.BorderStyle = BorderStyle.FixedSingle;
            txtSearch.SetBounds(16, 16, 282, 28);
            txtSearch.ForeColor = Theme.TextFaint;
            txtSearch.Text = "搜索标题或内容";
            txtSearch.GotFocus += delegate { if (txtSearch.Text == "搜索标题或内容") { txtSearch.Text = ""; txtSearch.ForeColor = Theme.TextMain; } };
            txtSearch.LostFocus += delegate { if (txtSearch.Text.Trim().Length == 0) { txtSearch.Text = "搜索标题或内容"; txtSearch.ForeColor = Theme.TextFaint; } };
            txtSearch.TextChanged += delegate { if (txtSearch.ForeColor != Theme.TextFaint) RefreshList(true); };
            left.Controls.Add(txtSearch);

            Button btnNew = new Button();
            btnNew.Text = "＋  新建记录";
            btnNew.Font = Theme.FB(9.2f);
            btnNew.FlatStyle = FlatStyle.Flat;
            btnNew.FlatAppearance.BorderSize = 0;
            btnNew.BackColor = Theme.Accent;
            btnNew.ForeColor = Color.White;
            btnNew.Cursor = Cursors.Hand;
            btnNew.SetBounds(16, 54, 282, 34);
            btnNew.Click += delegate { NewEntry(); };
            left.Controls.Add(btnNew);

            lstEntries = new ListBox();
            lstEntries.SetBounds(0, 102, 316, 400);
            lstEntries.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lstEntries.BorderStyle = BorderStyle.None;
            lstEntries.DrawMode = DrawMode.OwnerDrawFixed;
            lstEntries.ItemHeight = Ui.S(68);
            lstEntries.BackColor = Color.FromArgb(250, 251, 253);
            lstEntries.Font = Theme.F(9.5f);
            lstEntries.IntegralHeight = false;
            lstEntries.DrawItem += LstEntries_DrawItem;
            lstEntries.SelectedIndexChanged += delegate
            {
                if (loading) return;
                SaveCurrent();
                if (lstEntries.SelectedIndex >= 0 && lstEntries.SelectedIndex < lstEntries.Items.Count)
                {
                    LoadEntry(lstEntries.Items[lstEntries.SelectedIndex] as Entry);
                }
            };
            left.Controls.Add(lstEntries);

            lblCount = new Label();
            lblCount.Text = "";
            lblCount.Font = Theme.F(8.3f);
            lblCount.ForeColor = Theme.TextFaint;
            lblCount.BackColor = Color.FromArgb(250, 251, 253);
            lblCount.SetBounds(16, 0, 200, 18);
            lblCount.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblCount.Top = left.Height - 26;
            left.Controls.Add(lblCount);

            // editor contents
            txtTitle = new TextBox();
            txtTitle.Font = Theme.FB(16f);
            txtTitle.BorderStyle = BorderStyle.None;
            txtTitle.ForeColor = Theme.Navy;
            txtTitle.BackColor = Color.White;
            txtTitle.SetBounds(28, 18, 552, 38);
            txtTitle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtTitle.TextChanged += delegate { ScheduleSave(); if (lblSaved != null) lblSaved.Text = ""; UpdateSelectedTitle(); };
            editor.Controls.Add(txtTitle);

            Panel line = new Panel();
            line.BackColor = Color.FromArgb(226, 231, 240);
            line.SetBounds(28, 64, 552, 1);
            line.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            editor.Controls.Add(line);

            AddMetaLabel(editor, "日期", 28, 78);
            dtDate = new DateTimePicker();
            dtDate.Format = DateTimePickerFormat.Custom;
            dtDate.CustomFormat = "yyyy-MM-dd";
            dtDate.Font = Theme.F(9f);
            dtDate.SetBounds(28, 98, 118, 26);
            dtDate.ValueChanged += delegate { ScheduleSave(); };
            editor.Controls.Add(dtDate);

            AddMetaLabel(editor, "类别", 156, 78);
            cbCat = new ComboBox();
            cbCat.DropDownStyle = ComboBoxStyle.DropDownList;
            cbCat.Font = Theme.F(9f);

            cbCat.SetBounds(156, 98, 118, 26);
            cbCat.SelectedIndexChanged += delegate { ScheduleSave(); };
            editor.Controls.Add(cbCat);

            AddMetaLabel(editor, "状态", 282, 78);
            cbStatus = new ComboBox();
            cbStatus.DropDownStyle = ComboBoxStyle.DropDownList;
            cbStatus.Font = Theme.F(9f);
            cbStatus.Items.AddRange(new object[] { "进行中", "已完成", "暂停" });
            cbStatus.SetBounds(282, 98, 88, 26);
            cbStatus.SelectedIndexChanged += delegate { ScheduleSave(); };
            editor.Controls.Add(cbStatus);

            AddMetaLabel(editor, "进度", 380, 78);
            tbProg = new TrackBar();
            tbProg.Minimum = 0;
            tbProg.Maximum = 100;
            tbProg.TickStyle = TickStyle.None;
            tbProg.SetBounds(378, 96, 116, 34);
            tbProg.ValueChanged += delegate
            {
                lblProgress.Text = tbProg.Value + "%";
                ScheduleSave();
            };
            editor.Controls.Add(tbProg);

            lblProgress = new Label();
            lblProgress.Text = "0%";
            lblProgress.Font = Theme.FB(9f);
            lblProgress.ForeColor = Theme.Accent;
            lblProgress.SetBounds(500, 102, 46, 20);
            editor.Controls.Add(lblProgress);

            Label lc = new Label();
            lc.Text = "研究内容 / 备注";
            lc.Font = Theme.F(8.5f);
            lc.ForeColor = Theme.TextMuted;
            lc.SetBounds(28, 142, 200, 18);
            editor.Controls.Add(lc);

            txtContent = new TextBox();
            txtContent.Multiline = true;
            txtContent.ScrollBars = ScrollBars.Vertical;
            txtContent.BorderStyle = BorderStyle.FixedSingle;
            txtContent.BackColor = Color.White;
            txtContent.Font = Theme.F(10f);
            txtContent.SetBounds(28, 164, 552, 300);
            txtContent.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtContent.TextChanged += delegate { ScheduleSave(); if (lblSaved != null) lblSaved.Text = ""; };
            editor.Controls.Add(txtContent);

            Button btnSave = new Button();
            btnSave.Text = "保存";
            btnSave.Font = Theme.FB(9f);
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.FlatAppearance.BorderSize = 1;
            btnSave.FlatAppearance.BorderColor = Theme.Accent;
            btnSave.BackColor = Color.White;
            btnSave.ForeColor = Theme.Accent;
            btnSave.Cursor = Cursors.Hand;
            btnSave.SetBounds(28, 0, 82, 32);
            btnSave.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnSave.Top = 520;
            btnSave.Click += delegate { SaveCurrent(); };
            editor.Controls.Add(btnSave);

            btnDelete = new Button();
            btnDelete.Text = "删除此记录";
            btnDelete.Font = Theme.F(8.6f);
            btnDelete.FlatStyle = FlatStyle.Flat;
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.BackColor = Color.White;
            btnDelete.ForeColor = Theme.TextFaint;
            btnDelete.Cursor = Cursors.Hand;
            btnDelete.SetBounds(508, 520, 96, 32);
            btnDelete.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnDelete.Click += delegate { DeleteCurrent(); };
            editor.Controls.Add(btnDelete);

            lblSaved = new Label();
            lblSaved.Text = "自动保存";
            lblSaved.Font = Theme.F(8f);
            lblSaved.ForeColor = Theme.TextFaint;
            lblSaved.SetBounds(122, 528, 300, 18);
            lblSaved.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            editor.Controls.Add(lblSaved);

            layoutLog = delegate
            {
                btnSave.Top = page.Height - Ui.S(50);
                btnDelete.Top = page.Height - Ui.S(50);
                btnDelete.Left = Math.Max(Ui.S(400), editor.Width - Ui.S(116));
                lblSaved.Top = page.Height - Ui.S(42);
                txtTitle.Width = Math.Max(Ui.S(300), editor.Width - Ui.S(56));
                line.Width = Math.Max(Ui.S(300), editor.Width - Ui.S(56));
                txtContent.Width = Math.Max(Ui.S(300), editor.Width - Ui.S(56));
                txtContent.Height = Math.Max(Ui.S(120), page.Height - Ui.S(164) - Ui.S(66));
                lblCount.Top = left.Height - Ui.S(26);
            };
            page.Resize += delegate { if (uiReady) layoutLog(); };

            return page;
        }

        private void AddMetaLabel(Panel parent, string text, int x, int y)
        {
            Label l = new Label();
            l.Text = text;
            l.Font = Theme.F(8.8f);
            l.ForeColor = Theme.TextMuted;
            l.SetBounds(x, y, 90, 16);
            parent.Controls.Add(l);
        }

        private Panel BuildRefPage()
        {
            Panel page = new Panel();

            Panel top = new Panel();
            top.Dock = DockStyle.Top;
            top.Height = 76;
            top.BackColor = Color.White;
            page.Controls.Add(top);
            refsTop = top;

            Label t = new Label();
            t.Text = "点击卡片即可打开对应的书籍、文档或文件夹";
            t.Font = Theme.F(8.8f);
            t.ForeColor = Theme.TextMuted;
            t.SetBounds(2, 50, 520, 20);
            top.Controls.Add(t);

            Label h = new Label();
            h.Text = "资料库";
            h.Font = Theme.FB(12.5f);
            h.ForeColor = Theme.Navy;
            h.SetBounds(0, 14, 200, 26);
            top.Controls.Add(h);

            Button bHist = MakeTopButton("改动记录", 130);
            bHist.SetBounds(top.Width - 424, 22, 130, 32);
            bHist.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            bHist.Click += delegate { ShowChangeHistory(); };
            top.Controls.Add(bHist);

            Button bFile = MakeTopButton("添加文件", 120);
            bFile.SetBounds(top.Width - 274, 22, 120, 32);
            bFile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            bFile.Click += delegate { AddFileRef(); };
            top.Controls.Add(bFile);

            Button bFolder = MakeTopButton("添加文件夹", 120);
            bFolder.SetBounds(top.Width - 144, 22, 120, 32);
            bFolder.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            bFolder.Click += delegate { AddFolderRef(); };
            top.Controls.Add(bFolder);

            bFile.Visible = false;
            bFolder.Visible = false;
            t.Text = "参考资料为只读；个人资料请在“团队同步”页上传。";

            flow = new FlowLayoutPanel();
            Application.AddMessageFilter(new WheelRouter(flow));
            flow.Dock = DockStyle.None;
            flow.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            flow.SetBounds(0, 76, 100, 100);
            flow.AutoScroll = true;
            flow.BackColor = Color.White;
            flow.Padding = new Padding(0, 10, 0, 0);
            flow.WrapContents = true;
            page.Controls.Add(flow);

            page.Resize += delegate { if (uiReady) LayoutRefsPage(); };
            return page;
        }

        private Button MakeTopButton(string text, int w)
        {
            Button b = new Button();
            b.Text = text;
            b.Font = Theme.F(8.8f);
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = Theme.CardBorder;
            b.BackColor = Color.White;
            b.ForeColor = Theme.TextMain;
            b.Cursor = Cursors.Hand;
            return b;
        }

        private Panel BuildOverviewPage()
        {
            p3 = new Panel();
            p3.BackColor = Color.White;
            p3.AutoScroll = true;
            return p3;
        }

        private TextBox helpEditor;
        private bool helpDirty;

        private Panel BuildHelpPage()
        {
            Panel page = new Panel();
            page.BackColor = Color.White;

            Panel bar = new Panel();
            bar.Dock = DockStyle.Top;
            bar.Height = Ui.S(50);
            bar.BackColor = Color.White;

            Button upload = HelpButton("上传到 GitHub", 12);
            upload.Click += delegate { UploadHelp(); };
            Button reload = HelpButton("读取云端版本", 152);
            reload.Click += delegate { LoadHelpFromGitHub(true); };
            Button reset = HelpButton("恢复默认", 292);
            reset.Click += delegate { RestoreDefaultHelp(); };
            Button history = HelpButton("改动日志", 402);
            history.Click += delegate { ShowHelpChangelog(); };
            bar.Controls.Add(upload);
            bar.Controls.Add(reload);
            bar.Controls.Add(reset);
            bar.Controls.Add(history);

            helpEditor = new TextBox();
            helpEditor.Dock = DockStyle.Fill;
            helpEditor.BorderStyle = BorderStyle.None;
            helpEditor.BackColor = Color.White;
            helpEditor.Font = Theme.F(10f);
            helpEditor.Multiline = true;
            helpEditor.ReadOnly = false;
            helpEditor.ScrollBars = ScrollBars.Vertical;
            helpEditor.WordWrap = true;
            helpEditor.ForeColor = Theme.TextMain;
            helpEditor.Name = "helpEditor";
            string local = Path.Combine(appDir, "help_local.md");
            if (File.Exists(local))
            {
                try { helpEditor.Text = File.ReadAllText(local, Encoding.UTF8); }
                catch { helpEditor.Text = Lang.HelpText(); }
            }
            else helpEditor.Text = Lang.HelpText();
            helpEditor.TextChanged += delegate { helpDirty = true; };

            page.Controls.Add(helpEditor);
            page.Controls.Add(bar);
            return page;
        }

        private Button HelpButton(string text, int x)
        {
            Button b = new Button();
            b.Text = text;
            b.Font = Theme.F(8.8f);
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = Theme.CardBorder;
            b.BackColor = Color.White;
            b.ForeColor = Theme.TextMain;
            b.Cursor = Cursors.Hand;
            b.SetBounds(Ui.S(x), Ui.S(10), Ui.S(130), Ui.S(30));
            return b;
        }

        private async void UploadHelp()
        {
            if (string.IsNullOrEmpty(sessionToken)) { Msg.Show("请先在左下角登录 GitHub 账号。", "使用说明"); return; }
            string text = helpEditor.Text;
            if (string.IsNullOrWhiteSpace(text)) { Msg.Show("使用说明不能为空。", "使用说明"); return; }
            SyncConfig c = GitHubSync.LoadConfig(appDir);
            string token = sessionToken, login = sessionLogin;
            try
            {
                string sha = null;
                await Task.Run(() => GitHubSync.GetFile(c.owner, c.repo, "help/help.md", c.branch, token, out sha));
                await Task.Run(() => GitHubSync.PutFile(c.owner, c.repo, "help/help.md", c.branch, token, text, sha));
                await Task.Run(() => ActivityLog.AppendTo(c.owner, c.repo, c.branch, token, "help/changelog.json", login, "更新使用说明", text.Length + " 字符", 200));
                File.WriteAllText(Path.Combine(appDir, "help_local.md"), text, new UTF8Encoding(false));
                helpDirty = false;
                await LogActivityAsync("更新使用说明", text.Length + " 字符");
                Msg.Show("已上传；所有成员登录后都会读到这一版。", "使用说明");
            }
            catch (Exception ex) { Msg.Show("上传失败：" + ex.Message, "使用说明"); }
        }

        private async void LoadHelpFromGitHub(bool manual)
        {
            if (helpEditor == null) return;
            if (string.IsNullOrEmpty(sessionToken))
            {
                if (manual) Msg.Show("请先在左下角登录 GitHub 账号。", "使用说明");
                return;
            }
            SyncConfig c = GitHubSync.LoadConfig(appDir);
            string token = sessionToken;
            try
            {
                string sha = null;
                string text = await Task.Run(() => GitHubSync.GetFile(c.owner, c.repo, "help/help.md", c.branch, token, out sha));
                if (string.IsNullOrEmpty(text))
                {
                    if (manual) Msg.Show("云端还没有上传过使用说明。", "使用说明");
                    return;
                }
                helpEditor.Text = text;
                helpDirty = false;
                if (manual) Msg.Show("已读取云端使用说明。", "使用说明");
            }
            catch (Exception ex) { if (manual) Msg.Show("读取失败：" + ex.Message, "使用说明"); }
        }

        private void RestoreDefaultHelp()
        {
            if (Msg.Show("将编辑框恢复为软件自带的使用说明（上传前不影响云端）。继续？", "使用说明", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            helpEditor.Text = Lang.HelpText();
            helpDirty = false;
        }

        private async void ShowHelpChangelog()
        {
            if (string.IsNullOrEmpty(sessionToken)) { Msg.Show("请先在左下角登录 GitHub 账号。", "改动日志"); return; }
            SyncConfig c = GitHubSync.LoadConfig(appDir);
            string token = sessionToken;
            try
            {
                List<ActivityEntry> rows = await Task.Run(() => ActivityLog.Read(c.owner, c.repo, c.branch, token, "help/changelog.json"));
                if (rows.Count == 0) { Msg.Show("还没有改动日志。", "改动日志"); return; }
                StringBuilder sb = new StringBuilder();
                for (int i = rows.Count - 1; i >= 0; i--)
                    sb.Append(rows[i].Time).Append("  ·  ").Append(rows[i].User).Append("  ·  ").Append(rows[i].Detail).AppendLine();
                Msg.Show(sb.ToString(), "改动日志");
            }
            catch (Exception ex) { Msg.Show("读取改动日志失败：" + ex.Message, "改动日志"); }
        }



        private void SetPage(int idx)
        {
            if (idx < 0 || idx >= pages.Length) throw new ArgumentOutOfRangeException("idx");
            if (saveTimer != null && saveTimer.Enabled) { saveTimer.Stop(); SaveCurrent(); }
            if (idx == 5 && helpEditor != null && !helpDirty)
                LoadHelpFromGitHub(false);
            if (idx == 4 && !Directory.Exists(LoadProgressFolder()))
            {
                Msg.Show("请先在“本地进度”选择文件夹，否则无法进入团队同步。", "本地进度");
                idx = 3;
            }
            pageIndex = idx;
            for (int i = 0; i < pages.Length; i++)
            {
                pages[i].Visible = (i == idx);
                navs[i].SetSelected(i == idx);
            }
            string[] titles = new string[] { "团队研究", "快速发行", "参考文献与资料", "本地进度", "团队同步", "使用说明" };
            lblHeaderTitle.Text = titles[idx];
            if (idx == 0) RefreshTeamPage();
            if (idx == 1) RefreshPostsPage();
            if (idx == 2) RebuildRefs();
            if (idx == 3 && txtProgPath != null) txtProgPath.Text = LoadProgressFolder();
            if (idx == 4) RefreshSyncPage();
            Lang.Apply(this);
        }

        private void RefreshList()
        {
            RefreshList(false);
        }

        private void RefreshList(bool keepQuery)
        {
            if (lstEntries == null) return;
            if (saveTimer != null && saveTimer.Enabled) { saveTimer.Stop(); SaveCurrent(); }
            loading = true;
            string q = (txtSearch.Text == "搜索标题或内容") ? "" : txtSearch.Text.Trim().ToLower();
            string selId = current == null ? null : current.Id;
            lstEntries.BeginUpdate();
            lstEntries.Items.Clear();
            foreach (Entry e in store.Entries)
            {
                string hay = ((e.Title == null ? "" : e.Title) + " " + (e.Content == null ? "" : e.Content)).ToLower();
                if (q.Length == 0 || hay.IndexOf(q) >= 0)
                {
                    lstEntries.Items.Add(e);
                }
            }
            lstEntries.EndUpdate();
            if (lblCount != null) lblCount.Text = store.Entries.Count + " 条记录";
            int sel = -1;
            if (selId != null)
            {
                for (int i = 0; i < lstEntries.Items.Count; i++)
                {
                    Entry e = lstEntries.Items[i] as Entry;
                    if (e != null && e.Id == selId) { sel = i; break; }
                }
            }
            if (sel < 0 && lstEntries.Items.Count > 0) sel = 0;
            lstEntries.SelectedIndex = sel;
            loading = false;
            if (sel >= 0) LoadEntry(lstEntries.Items[sel] as Entry);
            else { current = null; loading = true; txtTitle.Clear(); txtContent.Clear(); loading = false; }
        }

        private void LstEntries_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            Entry en = lstEntries.Items[e.Index] as Entry;
            if (en == null) return;
            bool sel = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Rectangle r = new Rectangle(0, e.Bounds.Top, lstEntries.ClientSize.Width, e.Bounds.Height);
            int pad = Ui.S(18);
            using (SolidBrush bg = new SolidBrush(sel ? Theme.AccentSoft : Color.FromArgb(250, 251, 253)))
            {
                e.Graphics.FillRectangle(bg, r);
            }
            if (sel)
            {
                using (SolidBrush bar = new SolidBrush(Theme.Accent))
                {
                    e.Graphics.FillRectangle(bar, new Rectangle(0, r.Top, Ui.S(4), r.Height));
                }
            }
            string title = en.Title == null ? "（未命名）" : en.Title;
            if (title.Length > 22) title = title.Substring(0, 22) + "…";
            using (Font f1 = Theme.FB(10.3f))
            using (SolidBrush b1 = new SolidBrush(Theme.Navy))
            {
                e.Graphics.DrawString(title, f1, b1, new RectangleF(pad, r.Top + Ui.S(11), r.Width - Ui.S(30), Ui.S(24)));
            }
            string meta = (en.Date == null ? "" : en.Date) + "  ·  " + (en.Category == null ? "" : en.Category) +
                          "  ·  " + en.Progress + "%  ·  " + (en.Status == null ? "" : en.Status);
            using (Font f2 = Theme.F(8.3f))
            using (SolidBrush b2 = new SolidBrush(Theme.TextFaint))
            {
                e.Graphics.DrawString(meta, f2, b2, new RectangleF(pad, r.Top + Ui.S(37), r.Width - Ui.S(30), Ui.S(20)));
            }
            using (Pen p = new Pen(Color.FromArgb(238, 241, 247)))
            {
                e.Graphics.DrawLine(p, Ui.S(16), r.Bottom - 1, r.Width - Ui.S(16), r.Bottom - 1);
            }
        }

        private void UpdateSelectedTitle()
        {
            if (loading || current == null) return;
            lstEntries.Invalidate();
        }

        private void LoadEntry(Entry e)
        {
            if (e == null) return;
            loading = true;
            current = e;
            txtTitle.Text = e.Title == null ? "" : e.Title;
            txtContent.Text = e.Content == null ? "" : e.Content;
            try { dtDate.Value = DateTime.Parse(e.Date); } catch { dtDate.Value = DateTime.Now; }
            cbCat.SelectedItem = e.Category;
            if (cbCat.SelectedIndex < 0) cbCat.SelectedIndex = 0;
            cbStatus.SelectedItem = e.Status;
            if (cbStatus.SelectedIndex < 0) cbStatus.SelectedIndex = 0;
            tbProg.Value = Math.Max(0, Math.Min(100, e.Progress));
            lblProgress.Text = tbProg.Value + "%";
            lblSaved.Text = "上次保存：" + (e.Updated == null ? "—" : e.Updated);
            loading = false;
        }

        private void NewEntry()
        {
            SaveCurrent();
            txtSearch.Text = "搜索标题或内容"; txtSearch.ForeColor = Theme.TextFaint;
            Entry e = new Entry();
            e.Id = Guid.NewGuid().ToString("N");
            e.Title = "新记录";
            e.Date = DateTime.Now.ToString("yyyy-MM-dd");
            e.Category = "";
            e.Status = "进行中";
            e.Progress = 0;
            e.Updated = SyncData.Now();
            e.Content = "";
            store.Entries.Insert(0, e);
            Persist();
            current = e;
            RefreshList();
            if (lstEntries.Items.Count > 0) lstEntries.SelectedIndex = 0;
            txtTitle.Focus();
            txtTitle.SelectAll();
        }

        private void DeleteCurrent()
        {
            if (current == null) return;
            if (Msg.Show("确定删除这条记录吗？该操作不可撤销。", "删除记录", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            saveTimer.Stop();
            store.DeletedEntries.Add(new DeletedCategory { Id = current.Id, At = SyncData.Now() });
            store.Entries.Remove(current);
            current = null;
            Persist();
            RefreshList();
        }

        private void ScheduleSave()
        {
            if (loading) return;
            saveTimer.Stop();
            saveTimer.Start();
        }

        private void SaveCurrent()
        {
            if (loading || current == null) return;
            string title = txtTitle.Text.Trim().Length == 0 ? "（未命名）" : txtTitle.Text.Trim();
            string content = txtContent.Text;
            string date = dtDate.Value.ToString("yyyy-MM-dd");
            string category = cbCat.SelectedItem == null ? "" : cbCat.SelectedItem.ToString();
            string status = cbStatus.SelectedItem == null ? "进行中" : cbStatus.SelectedItem.ToString();
            int progress = tbProg.Value;
            if (current.Title == title && current.Content == content && current.Date == date &&
                current.Category == category && current.Status == status && current.Progress == progress) return;
            current.Title = title;
            current.Content = content;
            current.Date = date;
            current.Category = category;
            current.Status = status;
            current.Progress = progress;
            current.Updated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
            Persist();
            if (lblSaved != null) lblSaved.Text = "已保存 " + DateTime.Now.ToString("HH:mm:ss");
            lstEntries.Invalidate();
        }

        private void LayoutRefsPage()
        {
            if (flow == null || refsTop == null) return;
            Control p = flow.Parent;
            if (p == null) return;
            flow.SetBounds(0, refsTop.Height, p.ClientSize.Width, Math.Max(Ui.S(80), p.ClientSize.Height - refsTop.Height));
            RebuildRefs();
        }

        private void RebuildRefs()
        {
            if (flow == null || !uiReady) return;
            int keepY = -flow.AutoScrollPosition.Y;
            flow.SuspendLayout();
            flow.Controls.Clear();
            int cardW = Ui.S(430);
            int avail = flow.ClientSize.Width - Ui.S(2);
            if (avail < Ui.S(880)) cardW = Math.Max(Ui.S(300), avail - Ui.S(20));
            foreach (RefItem r in store.Refs)
            {
                RefCard card = new RefCard(r, OpenRef, LocateRef, RemoveRef);
                card.Width = cardW;
                flow.Controls.Add(card);
            }
            flow.ResumeLayout();
            if (keepY > 0)
            {
                int max = flow.VerticalScroll.Maximum - flow.VerticalScroll.LargeChange + 1;
                flow.AutoScrollPosition = new Point(0, Math.Min(keepY, Math.Max(0, max)));
            }
        }

        private string Resolve(RefItem r)
        {
            if (r.Path != null && (File.Exists(r.Path) || Directory.Exists(r.Path))) return r.Path;
            if (!string.IsNullOrEmpty(r.RelPath))
            {
                string p = Path.Combine(projectRoot, r.RelPath);
                if (File.Exists(p) || Directory.Exists(p)) return p;
            }
            return r.Path;
        }

        private void OpenRef(RefItem r)
        {
            string p = Resolve(r);
            if (p == null || (!File.Exists(p) && !Directory.Exists(p)))
            {
                Msg.Show("找不到该文件：\r\n" + r.Path + "\r\n\r\n它可能已被移动或删除。可以移除这张卡片后重新添加。", "文件不存在", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            OpenPath(p);
        }

        private void OpenPath(string p)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = p, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Msg.Show("无法打开：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void LocateRef(RefItem r)
        {
            string p = Resolve(r);
            try
            {
                if (Directory.Exists(p))
                {
                    Process.Start("explorer.exe", "\"" + p + "\"");
                }
                else if (File.Exists(p))
                {
                    Process.Start("explorer.exe", "/select,\"" + p + "\"");
                }
                else
                {
                    Msg.Show("找不到该文件。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch { }
        }

        private void RemoveRef(RefItem r)
        {
            if (r.Kind == "default" || r.Personal)
            {
                Msg.Show("参考资料为只读；个人资料请在“团队同步”页重新上传。", "参考资料");
                return;
            }
            string p = Resolve(r);
            if (Msg.Show("删除“" + r.Title + "”？\r\n这会将该文件或文件夹移入回收站。", "删除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            if (!string.IsNullOrEmpty(p) && (SyncData.IsWithin(projectRoot, p) || SyncData.IsWithin(appDir, p)))
            {
                Msg.Show("不能删除工作文件夹本身。", "提示");
                return;
            }
            try
            {
                if (File.Exists(p)) Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(p, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                else if (Directory.Exists(p))
                {
                    if ((File.GetAttributes(p) & FileAttributes.ReparsePoint) != 0) throw new IOException("Cannot delete a linked folder.");
                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(p, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                }
            }
            catch (Exception ex)
            {
                Msg.Show("删除失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            store.Refs.Remove(r);
            Persist();
            RebuildRefs();
        }

        private void AddFileRef()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Multiselect = true;
            dlg.Title = "选择要加入资料库的文件";
            dlg.Filter = "常用资料|*.pdf;*.docx;*.doc;*.pptx;*.xlsx;*.txt;*.md|所有文件|*.*";
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            foreach (string f in dlg.FileNames)
            {
                string ext = Path.GetExtension(f).ToLower();
                string kind = "file";
                if (ext == ".pdf") kind = "pdf";
                else if (ext == ".docx" || ext == ".doc") kind = "docx";
                string rel = "";
                try
                {
                    if (SyncData.IsWithin(f, projectRoot))
                    {
                        rel = f.Substring(projectRoot.Length).TrimStart('\\');
                    }
                }
                catch { }
                store.Refs.Add(new RefItem { Title = Path.GetFileNameWithoutExtension(f), Path = f, RelPath = rel, Kind = kind });
            }
            Persist();
            RebuildRefs();
        }

        private void AddFolderRef()
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.Description = "选择要加入资料库的文件夹";
            if (!string.IsNullOrEmpty(projectRoot)) dlg.SelectedPath = projectRoot;
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            string f = dlg.SelectedPath;
            string rel = "";
            try
            {
                if (SyncData.IsWithin(f, projectRoot)) rel = f.Substring(projectRoot.Length).TrimStart('\\');
            }
            catch { }
            store.Refs.Add(new RefItem { Title = new DirectoryInfo(f).Name, Path = f, RelPath = rel, Kind = "folder" });
            Persist();
            RebuildRefs();
        }

        private void RefreshOverview()
        {
            if (p3 == null) return;
            p3.SuspendLayout();
            p3.Controls.Clear();

            int total = store.Entries.Count;
            int sum = 0;
            foreach (Entry e in store.Entries) sum += e.Progress;
            int avg = total == 0 ? 0 : sum / total;

            AddStatCard(p3, 0, "记录总数", total.ToString(), "条", Theme.Accent);
            AddStatCard(p3, 1, "平均进度", avg.ToString(), "%", Theme.Warm);
            AddStatCard(p3, 2, "资料条目", store.Refs.Count.ToString(), "项", Color.FromArgb(46, 130, 110));

            Label lc = new Label();
            lc.Text = "研究类别分布";
            lc.Font = Theme.FB(11f);
            lc.ForeColor = Theme.Navy;
            lc.SetBounds(Ui.S(4), Ui.S(124), Ui.S(300), Ui.S(24));
            p3.Controls.Add(lc);

            string[] cats = new string[] { "文献阅读", "实验设计", "数据分析", "写作/汇报", "组会/讨论", "其他" };
            int y = Ui.S(160);
            foreach (string c in cats)
            {
                int n = 0;
                foreach (Entry e in store.Entries) if (e.Category == c) n++;
                int w = total == 0 ? 0 : (int)(240.0 * n / total);
                Label ln = new Label();
                ln.Text = c;
                ln.Font = Theme.F(9f);
                ln.ForeColor = Theme.TextMain;
                ln.SetBounds(Ui.S(4), y, Ui.S(96), Ui.S(20));
                p3.Controls.Add(ln);
                Panel track = new Panel();
                track.SetBounds(Ui.S(104), y + Ui.S(6), Ui.S(240), Ui.S(9));
                track.BackColor = Color.FromArgb(237, 241, 248);
                p3.Controls.Add(track);
                Panel fill = new Panel();
                fill.SetBounds(0, 0, w, Ui.S(9));
                fill.BackColor = Theme.Accent;
                track.Controls.Add(fill);
                Label lv = new Label();
                lv.Text = n.ToString();
                lv.Font = Theme.F(8.8f);
                lv.ForeColor = Theme.TextMuted;
                lv.SetBounds(Ui.S(352), y, Ui.S(40), Ui.S(20));
                p3.Controls.Add(lv);
                y += Ui.S(36);
            }

            Label lr = new Label();
            lr.Text = "最近更新";
            lr.Font = Theme.FB(11f);
            lr.ForeColor = Theme.Navy;
            lr.SetBounds(Ui.S(440), Ui.S(124), Ui.S(300), Ui.S(24));
            p3.Controls.Add(lr);

            List<Entry> sorted = new List<Entry>(store.Entries);
            sorted.Sort(delegate(Entry a, Entry b) { return string.Compare(b.Updated, a.Updated); });
            int yy = Ui.S(160);
            int shown = 0;
            foreach (Entry e in sorted)
            {
                if (shown >= 6) break;
                Label lt = new Label();
                string t = e.Title;
                if (t.Length > 26) t = t.Substring(0, 26) + "…";
                lt.Text = t;
                lt.Font = Theme.F(9.2f);
                lt.ForeColor = Theme.TextMain;
                lt.AutoEllipsis = true;
                lt.SetBounds(Ui.S(440), yy, Ui.S(330), Ui.S(20));
                p3.Controls.Add(lt);
                Label ld = new Label();
                ld.Text = (e.Updated == null ? "" : e.Updated) + "  ·  " + e.Progress + "%";
                ld.Font = Theme.F(7.8f);
                ld.ForeColor = Theme.TextFaint;
                ld.SetBounds(Ui.S(440), yy + Ui.S(20), Ui.S(330), Ui.S(16));
                p3.Controls.Add(ld);
                yy += Ui.S(50);
                shown++;
            }
            if (shown == 0)
            {
                Label le = new Label();
                le.Text = "还没有记录。切换到“研究日志”开始第一条。";
                le.Font = Theme.F(9f);
                le.ForeColor = Theme.TextFaint;
                le.SetBounds(Ui.S(440), Ui.S(160), Ui.S(380), Ui.S(20));
                p3.Controls.Add(le);
            }
            p3.ResumeLayout();
        }

        private void AddStatCard(Panel host, int idx, string caption, string value, string unit, Color accent)
        {
            Panel card = new Panel();
            card.SetBounds(Ui.S(4) + idx * Ui.S(292), Ui.S(8), Ui.S(276), Ui.S(96));
            card.BackColor = Color.FromArgb(250, 251, 253);
            string val = value;
            string unitText = unit;
            string cap = caption;
            card.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen p = new Pen(Theme.CardBorder)) e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
                using (SolidBrush b = new SolidBrush(accent)) e.Graphics.FillRectangle(b, 0, 0, 4, card.Height);
                using (Font fv = new Font("Times New Roman", 26f, FontStyle.Bold))
                using (Font fu = Theme.F(9f))
                using (Font fc = Theme.F(8.8f))
                using (SolidBrush bv = new SolidBrush(accent))
                using (SolidBrush bu = new SolidBrush(Theme.TextMuted))
                {
                    e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    e.Graphics.DrawString(val, fv, bv, new PointF(Ui.S(18), Ui.S(8)));
                    float w = e.Graphics.MeasureString(val, fv).Width;
                    e.Graphics.DrawString(unitText, fu, bu, new PointF(Ui.S(22) + w, Ui.S(34)));
                    e.Graphics.DrawString(cap, fc, bu, new PointF(Ui.S(20), Ui.S(64)));
                }
            };
            host.Controls.Add(card);
        }

        private bool uiReady = false;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!uiReady) try
            {
                this.AutoScaleDimensions = new SizeF(96F, 96F);
                this.AutoScaleMode = AutoScaleMode.Dpi;
                this.PerformAutoScale();
            }
            catch { }
            uiReady = true;
            if (layoutLog != null) layoutLog();
            LayoutRefsPage();
            RebuildRefs();
            RebuildCategories();
            RefreshOverview2();
        }

        protected override void WndProc(ref Message m)
        {
            bool dpiChanged = m.Msg == 0x02E0;
            if (dpiChanged) Ui.Scale = Math.Max(1f, (m.WParam.ToInt64() & 0xffff) / 96f);
            base.WndProc(ref m);
            if (dpiChanged && uiReady)
            {
                if (layoutLog != null) layoutLog();
                LayoutRefsPage(); RebuildRefs(); RebuildCategories(); RefreshOverview2();
                Invalidate(true);
            }
        }

        public void SetPagePublic(int idx) { SetPage(idx); }

        public void SetSizePublic(int w, int h) { this.ClientSize = new Size(w, h); }

        public void DumpLayout(string path)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("client=" + ClientSize.Width + "x" + ClientSize.Height);
            if (flow != null)
            {
                sb.AppendLine("flow.client=" + flow.ClientSize.Width + "x" + flow.ClientSize.Height);
                sb.AppendLine("flow.display=" + flow.DisplayRectangle.Width + "x" + flow.DisplayRectangle.Height);
                sb.AppendLine("scroll.min=" + flow.VerticalScroll.Minimum + " max=" + flow.VerticalScroll.Maximum +
                              " large=" + flow.VerticalScroll.LargeChange + " value=" + flow.VerticalScroll.Value +
                              " visible=" + flow.VerticalScroll.Visible);
                sb.AppendLine("autoscrollpos=" + flow.AutoScrollPosition.X + "," + flow.AutoScrollPosition.Y);
                sb.AppendLine("cards=" + flow.Controls.Count);
                foreach (Control c in flow.Controls)
                {
                    sb.AppendLine("  card " + c.Bounds.X + "," + c.Bounds.Y + " " + c.Width + "x" + c.Height);
                }
            }
            if (pages != null && pages.Length > 1)
            {
                sb.AppendLine("page1=" + pages[1].Bounds);
                foreach (Control c in pages[1].Controls)
                {
                    sb.AppendLine("  P1 " + c.GetType().Name + " " + c.Bounds + " vis=" + c.Visible);
                    foreach (Control d in c.Controls)
                    {
                        sb.AppendLine("    " + d.GetType().Name + " '" + d.Text + "' " + d.Bounds + " vis=" + d.Visible);
                    }
                }
            }
            if (pages != null && pages.Length > 0)
            {
                sb.AppendLine("page0=" + pages[0].Bounds);
                foreach (Control c in pages[0].Controls)
                {
                    sb.AppendLine("  P0 " + c.GetType().Name + " " + c.Bounds);
                    if (c is Panel)
                    {
                        foreach (Control d in c.Controls)
                        {
                            sb.AppendLine("    " + d.GetType().Name + " '" + d.Text + "' " + d.Bounds);
                        }
                    }
                }
            }
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        public void CaptureTo(string path)
        {
            Bitmap bmp = new Bitmap(this.Width, this.Height);
            this.DrawToBitmap(bmp, new Rectangle(0, 0, this.Width, this.Height));
            bmp.Save(path, ImageFormat.Png);
            bmp.Dispose();
        }
    }

    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Ui.EnableHighDpi();
            Application.EnableVisualStyles();
            bool firstInstance;
            using (System.Threading.Mutex instance = new System.Threading.Mutex(true, @"Local\SRLogbook_ResearchProgressRecorder", out firstInstance))
            {
                if (!firstInstance)
                {
                    Msg.Show("SRPRecorder2027 已经打开。请切换到现有窗口，避免同时修改研究数据。", "SRPRecorder2027", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            Application.SetCompatibleTextRenderingDefault(false);
            MainForm f;
            try
            {
                f = new MainForm();
            }
            catch (Exception ex)
            {
                try { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_error.txt"), ex.ToString()); } catch { }
                return;
            }
            if (MainForm.StartupCancelled) return;
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--page") f.SetPagePublic(int.Parse(args[i + 1]));
            }
            if (args.Length >= 2 && args[0] == "--dump")
            {
                f.SetPagePublic(1);
                f.Shown += delegate
                {
                    Timer td = new Timer();
                    td.Interval = 1200;
                    td.Tick += delegate
                    {
                        td.Stop();
                        try { f.DumpLayout(args[1]); } catch (Exception ex) { File.WriteAllText(args[1] + ".err.txt", ex.ToString()); }
                        f.Close();
                    };
                    td.Start();
                };
            }
            if (args.Length >= 3 && args[0] == "--shot")
            {
                int page = int.Parse(args[1]);
                string outPath = args[2];
                if (args.Length >= 5) f.SetSizePublic(int.Parse(args[3]), int.Parse(args[4]));
                f.SetPagePublic(page);
                f.Shown += delegate
                {
                    Timer t = new Timer();
                    t.Interval = 1400;
                    t.Tick += delegate
                    {
                        t.Stop();
                        try { f.CaptureTo(outPath); } catch (Exception ex) { File.WriteAllText(outPath + ".err.txt", ex.ToString()); }
                        f.Close();
                    };
                    t.Start();
                };
            }
            Application.Run(f);
            }
        }
    }
}
