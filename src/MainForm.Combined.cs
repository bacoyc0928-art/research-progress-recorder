using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace SRLogbook
{
    public partial class MainForm
    {
        /* ---------- 本地进度页的文件夹内容列表 ---------- */
        private ListView progList;

        /* ---------- 参考文献与资料（仅参考资料） ---------- */

        private Panel BuildRefsAndCategoriesPage()
        {
            Panel page = new Panel();
            page.BackColor = Color.White;
            Panel refs = BuildRefPage();
            refs.Dock = DockStyle.Fill;
            page.Controls.Add(refs);
            return page;
        }

        /* ---------- 本地进度（仅：选文件夹 / 新建 docx / 打开文件夹） ---------- */

        private TextBox txtProgPath;

        private void ShowResearchEntry(Entry entry) { SetPage(3); }

        private string ProgressConfigPath() { return Path.Combine(appDir, "progress.json"); }

        private string LoadProgressFolder()
        {
            try
            {
                if (File.Exists(ProgressConfigPath()))
                {
                    JavaScriptSerializer ser = new JavaScriptSerializer();
                    Dictionary<string, object> d = ser.Deserialize<Dictionary<string, object>>(File.ReadAllText(ProgressConfigPath(), System.Text.Encoding.UTF8));
                    if (d != null && d.ContainsKey("folder")) return Convert.ToString(d["folder"]);
                }
            }
            catch { }
            return AppPaths.PersonalProgress(appDir);
        }

        private void SaveProgressFolder(string f)
        {
            try
            {
                JavaScriptSerializer ser = new JavaScriptSerializer();
                Dictionary<string, object> d = new Dictionary<string, object>();
                d["folder"] = f;
                SyncData.WriteAtomic(ProgressConfigPath(), ser.Serialize(d));
            }
            catch { }
        }

        private Panel BuildLocalProgressPage()
        {
            Panel page = new Panel();
            page.BackColor = Color.White;
            Panel card = new Panel();
            card.SetBounds(0, 0, 880, 570);
            card.BackColor = Color.White;
            card.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen p = new Pen(Theme.CardBorder)) e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
            };
            page.Controls.Add(card);

            Label h = new Label();
            h.Text = "本地进度文件夹";
            h.Font = Theme.FB(13f);
            h.ForeColor = Theme.Navy;
            h.BackColor = Color.White;
            h.SetBounds(Ui.S(24), Ui.S(18), Ui.S(400), Ui.S(28));
            card.Controls.Add(h);

            Label hint = new Label();
            hint.Text = "所有本地进度内容都存放在这个文件夹；未选择时无法进入“团队同步”。";
            hint.Font = Theme.F(8.8f);
            hint.ForeColor = Theme.TextMuted;
            hint.BackColor = Color.White;
            hint.SetBounds(Ui.S(24), Ui.S(52), Ui.S(800), Ui.S(24));
            card.Controls.Add(hint);

            txtProgPath = new TextBox();
            txtProgPath.ReadOnly = true;
            txtProgPath.Font = Theme.F(9.5f);
            txtProgPath.SetBounds(Ui.S(24), Ui.S(96), Ui.S(620), Ui.S(30));
            card.Controls.Add(txtProgPath);

            Button bChoose = new Button();
            bChoose.Text = "选择文件夹";
            bChoose.Font = Theme.F(9f);
            bChoose.FlatStyle = FlatStyle.Flat;
            bChoose.FlatAppearance.BorderSize = 0;
            bChoose.BackColor = Theme.Accent;
            bChoose.ForeColor = Color.White;
            bChoose.Cursor = Cursors.Hand;
            bChoose.SetBounds(Ui.S(660), Ui.S(96), Ui.S(160), Ui.S(30));
            bChoose.Click += delegate { ChooseProgressFolder(); };
            card.Controls.Add(bChoose);

            Button bDoc = new Button();
            bDoc.Text = "新建文档";
            bDoc.Font = Theme.F(9f);
            bDoc.FlatStyle = FlatStyle.Flat;
            bDoc.FlatAppearance.BorderSize = 1;
            bDoc.FlatAppearance.BorderColor = Theme.CardBorder;
            bDoc.BackColor = Color.White;
            bDoc.ForeColor = Theme.TextMain;
            bDoc.Cursor = Cursors.Hand;
            bDoc.SetBounds(Ui.S(24), Ui.S(150), Ui.S(220), Ui.S(40));
            bDoc.Click += delegate { CreateProgressDoc(); };
            card.Controls.Add(bDoc);

            Button bOpen = new Button();
            bOpen.Text = "打开本地资料文件夹";
            bOpen.Font = Theme.F(9f);
            bOpen.FlatStyle = FlatStyle.Flat;
            bOpen.FlatAppearance.BorderSize = 1;
            bOpen.FlatAppearance.BorderColor = Theme.CardBorder;
            bOpen.BackColor = Color.White;
            bOpen.ForeColor = Theme.TextMain;
            bOpen.Cursor = Cursors.Hand;
            bOpen.SetBounds(Ui.S(258), Ui.S(150), Ui.S(260), Ui.S(40));
            bOpen.Click += delegate { OpenProgressFolder(); };
            card.Controls.Add(bOpen);

            Label fh = new Label();
            fh.Text = "文件夹内容 / Files";
            fh.Font = Theme.FB(10.5f);
            fh.ForeColor = Theme.Navy;
            fh.BackColor = Color.White;
            fh.SetBounds(Ui.S(24), Ui.S(204), Ui.S(320), Ui.S(24));
            card.Controls.Add(fh);

            Button bRefresh = new Button();
            bRefresh.Text = "刷新";
            bRefresh.Font = Theme.F(8.8f);
            bRefresh.FlatStyle = FlatStyle.Flat;
            bRefresh.FlatAppearance.BorderSize = 1;
            bRefresh.FlatAppearance.BorderColor = Theme.CardBorder;
            bRefresh.BackColor = Color.White;
            bRefresh.ForeColor = Theme.TextMain;
            bRefresh.Cursor = Cursors.Hand;
            bRefresh.SetBounds(Ui.S(744), Ui.S(200), Ui.S(112), Ui.S(30));
            bRefresh.Click += delegate { RefreshProgressFiles(); };
            card.Controls.Add(bRefresh);

            progList = new ListView();
            progList.View = View.Details;
            progList.FullRowSelect = true;
            progList.GridLines = false;
            progList.HideSelection = false;
            progList.Font = Theme.F(9f);
            progList.BackColor = Color.White;
            progList.Columns.Add("名称 / Name", Ui.S(380), HorizontalAlignment.Left);
            progList.Columns.Add("大小 / Size", Ui.S(150), HorizontalAlignment.Left);
            progList.Columns.Add("修改 / Modified", Ui.S(230), HorizontalAlignment.Left);
            progList.SetBounds(Ui.S(24), Ui.S(234), Ui.S(832), Ui.S(300));
            progList.DoubleClick += delegate { OpenProgressListItem(); };
            card.Controls.Add(progList);

            page.Resize += delegate
            {
                card.Width = Math.Min(Ui.S(880), page.ClientSize.Width - Ui.S(2));
                if (progList != null) progList.Width = Math.Max(Ui.S(300), card.Width - Ui.S(48));
            };
            return page;
        }

        /// <summary>列出本地进度文件夹里的文件与子文件夹（用户要求：选定文件夹后在软件内直接看到内容）。</summary>
        private void RefreshProgressFiles()
        {
            if (progList == null) return;
            string folder = LoadProgressFolder();
            progList.BeginUpdate();
            progList.Items.Clear();
            if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
            {
                try
                {
                    foreach (string dir in Directory.GetDirectories(folder))
                    {
                        DirectoryInfo di = new DirectoryInfo(dir);
                        ListViewItem item = new ListViewItem(di.Name + "\\");
                        item.SubItems.Add("<文件夹 / folder>");
                        item.SubItems.Add(di.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
                        item.Tag = dir;
                        item.ForeColor = Theme.Navy;
                        progList.Items.Add(item);
                    }
                    foreach (string file in Directory.GetFiles(folder))
                    {
                        FileInfo fi = new FileInfo(file);
                        ListViewItem item = new ListViewItem(fi.Name);
                        item.SubItems.Add(Math.Max(1, fi.Length / 1024) + " KB");
                        item.SubItems.Add(fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
                        item.Tag = file;
                        progList.Items.Add(item);
                    }
                }
                catch (Exception ex) { Log("读取进度文件夹失败 / Failed to list folder: " + ex.Message); }
            }
            progList.EndUpdate();
        }

        private void OpenProgressListItem()
        {
            if (progList == null || progList.SelectedItems.Count == 0) return;
            string path = progList.SelectedItems[0].Tag as string;
            if (string.IsNullOrEmpty(path)) return;
            if (Directory.Exists(path)) { OpenPath(path); return; }
            if (File.Exists(path)) { OpenPath(path); return; }
            Msg.Show("文件不存在，可能已被移动或删除 / File not found.", "本地进度");
        }

        private void ChooseProgressFolder()
        {
            if (!RequireLogin("本地进度")) return;
            using (FolderBrowserDialog d = new FolderBrowserDialog())
            {
                d.Description = "选择本地进度文件夹（所有内容都存放在这里）";
                string cur = LoadProgressFolder();
                if (Directory.Exists(cur)) d.SelectedPath = cur;
                if (d.ShowDialog(this) != DialogResult.OK) return;
                try { Directory.CreateDirectory(d.SelectedPath); } catch { }
                SaveProgressFolder(d.SelectedPath);
                txtProgPath.Text = d.SelectedPath;
                RefreshProgressFiles();
            }
        }

        private void CreateProgressDoc()
        {
            if (!RequireLogin("本地进度")) return;
            string folder = LoadProgressFolder();
            if (!Directory.Exists(folder)) { ChooseProgressFolder(); folder = LoadProgressFolder(); }
            if (!Directory.Exists(folder)) return;
            string project = Prompt.Show(this, "新建文档", "项目名称", "本地进度");
            if (string.IsNullOrEmpty(project)) return;
            string title = DateTime.Now.ToString("yyyy-MM-dd") + " " + project;
            try
            {
                string path = LocalDocs.CreateDocxIn(folder, project, title);
                RefreshProgressFiles();
                Msg.Show("文档已创建：" + path, "本地进度");
                OpenPath(path);
            }
            catch (Exception ex) { Msg.Show("创建失败：" + ex.Message, "本地进度"); }
        }

        private void OpenProgressFolder()
        {
            string folder = LoadProgressFolder();
            if (!Directory.Exists(folder)) { ChooseProgressFolder(); folder = LoadProgressFolder(); }
            if (Directory.Exists(folder)) OpenPath(folder);
        }

        /* ---------- 本地资料（选定文件夹作为参考） ---------- */

        private string localRefsFolder;
        private TextBox txtLocalRefsPath;
        private ListBox lstLocalRefs;

        private string LocalRefsPath() { return Path.Combine(appDir, "localrefs.json"); }

        private string LoadLocalRefsFolder()
        {
            try
            {
                if (File.Exists(LocalRefsPath()))
                {
                    JavaScriptSerializer ser = new JavaScriptSerializer();
                    Dictionary<string, object> d = ser.Deserialize<Dictionary<string, object>>(File.ReadAllText(LocalRefsPath(), System.Text.Encoding.UTF8));
                    if (d != null && d.ContainsKey("folder")) return Convert.ToString(d["folder"]);
                }
            }
            catch { }
            return "";
        }

        private void SaveLocalRefsFolder(string f)
        {
            try
            {
                JavaScriptSerializer ser = new JavaScriptSerializer();
                Dictionary<string, object> d = new Dictionary<string, object>();
                d["folder"] = f;
                File.WriteAllText(LocalRefsPath(), ser.Serialize(d), new System.Text.UTF8Encoding(false));
            }
            catch { }
        }

        private Panel BuildLocalRefsPanel()
        {
            Panel p = new Panel();
            p.BackColor = Color.White;
            Panel top = new Panel();
            top.Dock = DockStyle.Top;
            top.Height = Ui.S(60);
            top.BackColor = Color.White;
            Label t = new Label();
            t.Text = "本地资料：选定一个文件夹作为参考，程序读取其中所有文件；未选定则为空。";
            t.Font = Theme.F(8.8f);
            t.ForeColor = Theme.TextMuted;
            t.SetBounds(Ui.S(2), Ui.S(8), Ui.S(820), Ui.S(20));
            top.Controls.Add(t);
            txtLocalRefsPath = new TextBox();
            txtLocalRefsPath.ReadOnly = true;
            txtLocalRefsPath.Font = Theme.F(9f);
            txtLocalRefsPath.SetBounds(Ui.S(2), Ui.S(32), Ui.S(560), Ui.S(26));
            top.Controls.Add(txtLocalRefsPath);
            Button bs = SegBtn("选择文件夹");
            bs.SetBounds(Ui.S(570), Ui.S(32), Ui.S(110), Ui.S(26));
            bs.Click += delegate { ChooseLocalRefsFolder(); };
            top.Controls.Add(bs);
            Button br = SegBtn("补录文件");
            br.SetBounds(Ui.S(688), Ui.S(32), Ui.S(110), Ui.S(26));
            br.Click += delegate { AddLocalRefsFile(); };
            top.Controls.Add(br);
            p.Controls.Add(top);

            lstLocalRefs = new ListBox();
            lstLocalRefs.Dock = DockStyle.Fill;
            lstLocalRefs.Font = Theme.F(9.5f);
            lstLocalRefs.BorderStyle = BorderStyle.FixedSingle;
            lstLocalRefs.IntegralHeight = false;
            lstLocalRefs.DoubleClick += delegate { OpenSelectedLocalRef(); };
            p.Controls.Add(lstLocalRefs);
            return p;
        }

        private void RefreshLocalRefs()
        {
            if (lstLocalRefs == null) return;
            localRefsFolder = LoadLocalRefsFolder();
            txtLocalRefsPath.Text = localRefsFolder;
            lstLocalRefs.BeginUpdate();
            lstLocalRefs.Items.Clear();
            if (Directory.Exists(localRefsFolder))
            {
                foreach (string f in Directory.GetFiles(localRefsFolder))
                    lstLocalRefs.Items.Add(Path.GetFileName(f));
            }
            lstLocalRefs.EndUpdate();
        }

        private void ChooseLocalRefsFolder()
        {
            using (FolderBrowserDialog d = new FolderBrowserDialog())
            {
                d.Description = "选择本地参考资料文件夹";
                if (Directory.Exists(localRefsFolder)) d.SelectedPath = localRefsFolder;
                if (d.ShowDialog(this) != DialogResult.OK) return;
                localRefsFolder = d.SelectedPath;
                SaveLocalRefsFolder(localRefsFolder);
                RefreshLocalRefs();
            }
        }

        private void AddLocalRefsFile()
        {
            if (!Directory.Exists(localRefsFolder))
            {
                ChooseLocalRefsFolder();
                if (!Directory.Exists(localRefsFolder)) return;
            }
            using (OpenFileDialog d = new OpenFileDialog())
            {
                d.Filter = "所有文件|*.*";
                d.Multiselect = true;
                if (d.ShowDialog(this) != DialogResult.OK) return;
                foreach (string f in d.FileNames)
                {
                    string dest = Path.Combine(localRefsFolder, Path.GetFileName(f));
                    if (string.Equals(Path.GetFullPath(f), Path.GetFullPath(dest), StringComparison.OrdinalIgnoreCase)) continue;
                    if (File.Exists(dest) && Msg.Show("Replace existing file: " + dest + "?", "Confirm", MessageBoxButtons.YesNo) != DialogResult.Yes) continue;
                    try { File.Copy(f, dest, true); }
                    catch (Exception ex) { Msg.Show(ex.Message, "Error"); }
                }
                RefreshLocalRefs();
            }
        }

        private void OpenSelectedLocalRef()
        {
            if (lstLocalRefs == null || lstLocalRefs.SelectedIndex < 0 || !Directory.Exists(localRefsFolder)) return;
            string f = Path.Combine(localRefsFolder, Convert.ToString(lstLocalRefs.SelectedItem));
            if (File.Exists(f)) OpenPath(f);
        }

        /* ---------- 分段按钮工具 ---------- */

        private Button[] AddSegBar(Panel bar, string[] labels, Action<int> onSel)
        {
            Button[] btns = new Button[labels.Length];
            int x = Ui.S(2);
            for (int i = 0; i < labels.Length; i++)
            {
                int idx = i;
                Button b = new Button();
                b.Text = labels[i];
                b.Font = Theme.F(9f);
                b.FlatStyle = FlatStyle.Flat;
                b.FlatAppearance.BorderSize = 1;
                b.FlatAppearance.BorderColor = Theme.CardBorder;
                b.BackColor = Color.White;
                b.ForeColor = Theme.TextMain;
                b.Cursor = Cursors.Hand;
                b.SetBounds(x, Ui.S(6), Ui.S(110), Ui.S(30));
                b.Click += delegate
                {
                    SelectSeg(btns, idx);
                    onSel(idx);
                };
                bar.Controls.Add(b);
                btns[i] = b;
                x += Ui.S(116);
            }
            SelectSeg(btns, 0);
            return btns;
        }

        private void SelectSeg(Button[] btns, int sel)
        {
            for (int j = 0; j < btns.Length; j++)
            {
                btns[j].BackColor = j == sel ? Color.FromArgb(235, 241, 252) : Color.White;
                btns[j].ForeColor = j == sel ? Theme.Accent : Theme.TextMain;
            }
        }

        private Button SegBtn(string text)
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
    }
}
