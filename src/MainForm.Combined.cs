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
        /* ---------- 本地记录页的文件夹内容列表 ---------- */
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

        /* ---------- 本地记录（仅：选文件夹 / 新建 docx / 打开文件夹） ---------- */

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
            card.SetBounds(0, 0, 880, 604);
            card.BackColor = Color.White;
            card.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen p = new Pen(Theme.CardBorder)) e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
            };
            page.Controls.Add(card);

            Label h = new Label();
            h.Text = "本地记录文件夹";
            h.Font = Theme.FB(13f);
            h.ForeColor = Theme.Navy;
            h.BackColor = Color.White;
            h.SetBounds(Ui.S(24), Ui.S(16), Ui.S(400), Ui.S(28));
            card.Controls.Add(h);

            Label hint = new Label();
            hint.Text = "新文档与列表使用 Mat_Local；所选文件夹是上传时的备用来源。 / Documents and list use Mat_Local; the selected folder is an upload fallback.";
            hint.Font = Theme.F(8.8f);
            hint.ForeColor = Theme.TextMuted;
            hint.BackColor = Color.White;
            hint.SetBounds(Ui.S(24), Ui.S(48), Ui.S(800), Ui.S(22));
            card.Controls.Add(hint);

            txtProgPath = new TextBox();
            txtProgPath.ReadOnly = true;
            txtProgPath.Font = Theme.F(9.5f);
            txtProgPath.SetBounds(Ui.S(24), Ui.S(84), Ui.S(620), Ui.S(30));
            card.Controls.Add(txtProgPath);

            Button bChoose = new Button();
            bChoose.Text = "选择文件夹 / Choose";
            bChoose.Font = Theme.F(9f);
            bChoose.FlatStyle = FlatStyle.Flat;
            bChoose.FlatAppearance.BorderSize = 0;
            bChoose.BackColor = Theme.Accent;
            bChoose.ForeColor = Color.White;
            bChoose.Cursor = Cursors.Hand;
            bChoose.SetBounds(Ui.S(660), Ui.S(84), Ui.S(160), Ui.S(30));
            bChoose.Click += delegate { ChooseProgressFolder(); };
            card.Controls.Add(bChoose);

            Button bDoc = new Button();
            bDoc.Text = "新建文档 / New Document";
            bDoc.Font = Theme.F(9f);
            bDoc.FlatStyle = FlatStyle.Flat;
            bDoc.FlatAppearance.BorderSize = 1;
            bDoc.FlatAppearance.BorderColor = Theme.CardBorder;
            bDoc.BackColor = Color.White;
            bDoc.ForeColor = Theme.TextMain;
            bDoc.Cursor = Cursors.Hand;
            bDoc.SetBounds(Ui.S(24), Ui.S(128), Ui.S(220), Ui.S(40));
            bDoc.Click += delegate { CreateProgressDoc(); };
            card.Controls.Add(bDoc);

            Button bOpen = new Button();
            bOpen.Text = "打开工作资料 / Open Working Files";
            bOpen.Font = Theme.F(9f);
            bOpen.FlatStyle = FlatStyle.Flat;
            bOpen.FlatAppearance.BorderSize = 1;
            bOpen.FlatAppearance.BorderColor = Theme.CardBorder;
            bOpen.BackColor = Color.White;
            bOpen.ForeColor = Theme.TextMain;
            bOpen.Cursor = Cursors.Hand;
            bOpen.SetBounds(Ui.S(258), Ui.S(128), Ui.S(280), Ui.S(40));
            bOpen.Click += delegate { OpenProgressFolder(); };
            card.Controls.Add(bOpen);

            Label note = new Label();
            note.Text = "新建文档保存在 Mat_Local（工作资料文件夹） / New documents are saved to Mat_Local.";
            note.Font = Theme.F(8.2f);
            note.ForeColor = Theme.Accent;
            note.BackColor = Color.White;
            note.SetBounds(Ui.S(24), Ui.S(172), Ui.S(800), Ui.S(20));
            card.Controls.Add(note);

            AddWorkspaceCard(card, 24, "工作资料 / Working files", "Mat_Local",
                "上传本地记录的默认来源 / default source for progress upload", AppPaths.MatLocal(appDir));
            AddWorkspaceCard(card, 306, "个人参考资料 / My references", "Reference",
                "上传参考资料的默认来源 / default source for references", AppPaths.ReferenceDir(appDir));
            AddWorkspaceCard(card, 588, "他人内容（临时） / Others (temp)", "Public_Temp",
                "下载他人内容，退出即清空 / team downloads, cleared on exit", AppPaths.PublicTemp(appDir));

            Label fh = new Label();
            fh.Text = "工作资料文件 / Working files";
            fh.Font = Theme.FB(10.5f);
            fh.ForeColor = Theme.Navy;
            fh.BackColor = Color.White;
            fh.SetBounds(Ui.S(24), Ui.S(316), Ui.S(320), Ui.S(24));
            card.Controls.Add(fh);

            Button bRefresh = new Button();
            bRefresh.Text = "刷新 / Refresh";
            bRefresh.Font = Theme.F(8.8f);
            bRefresh.FlatStyle = FlatStyle.Flat;
            bRefresh.FlatAppearance.BorderSize = 1;
            bRefresh.FlatAppearance.BorderColor = Theme.CardBorder;
            bRefresh.BackColor = Color.White;
            bRefresh.ForeColor = Theme.TextMain;
            bRefresh.Cursor = Cursors.Hand;
            bRefresh.SetBounds(Ui.S(744), Ui.S(312), Ui.S(112), Ui.S(30));
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
            progList.SetBounds(Ui.S(24), Ui.S(346), Ui.S(832), Ui.S(236));
            progList.DoubleClick += delegate { OpenProgressListItem(); };
            card.Controls.Add(progList);

            page.AutoScroll = true;
            Action layout = delegate
            {
                card.Width = Math.Min(Ui.S(880), Math.Max(Ui.S(320), page.ClientSize.Width - Ui.S(20)));
                card.Height = Math.Max(Ui.S(460), page.ClientSize.Height - Ui.S(2));
                int contentWidth = Math.Max(Ui.S(260), card.Width - Ui.S(48));
                hint.Width = note.Width = contentWidth;
                hint.AutoEllipsis = true; note.AutoEllipsis = true;
                bChoose.Left = card.Width - Ui.S(24) - bChoose.Width;
                txtProgPath.Width = Math.Max(Ui.S(60), bChoose.Left - txtProgPath.Left - Ui.S(12));
                bDoc.Width = Math.Min(Ui.S(220), (contentWidth - Ui.S(14)) / 2);
                bOpen.Left = bDoc.Right + Ui.S(14); bOpen.Width = contentWidth - bDoc.Width - Ui.S(14);
                int index = 0, gap = Ui.S(10), tileWidth = (contentWidth - gap * 2) / 3;
                foreach (Control child in card.Controls)
                {
                    if (!(child is Panel)) continue;
                    child.SetBounds(Ui.S(24) + index * (tileWidth + gap), Ui.S(196), tileWidth, Ui.S(104)); index++;
                    foreach (Control label in child.Controls) label.Width = Math.Max(1, tileWidth - Ui.S(28));
                }
                bRefresh.Left = card.Width - Ui.S(24) - bRefresh.Width;
                progList.Width = contentWidth;
                progList.Height = Math.Max(Ui.S(80), card.Height - progList.Top - Ui.S(20));
                progList.Columns[0].Width = Math.Max(Ui.S(100), contentWidth / 2);
                progList.Columns[1].Width = contentWidth / 6;
                progList.Columns[2].Width = Math.Max(Ui.S(100), contentWidth - progList.Columns[0].Width - progList.Columns[1].Width - Ui.S(8));
            };
            page.Resize += delegate { layout(); };
            page.VisibleChanged += delegate { if (page.Visible) layout(); };
            return page;
        }

        /// <summary>三个固定文件夹以“功能卡片”呈现，原始文件夹名作为副标题。</summary>
        private void AddWorkspaceCard(Panel parent, int x, string title, string rawName, string desc, string path)
        {
            Panel p = new Panel();
            p.SetBounds(Ui.S(x), Ui.S(196), Ui.S(270), Ui.S(104));
            p.BackColor = Color.FromArgb(248, 250, 253);
            p.Cursor = Cursors.Hand;
            p.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen pen = new Pen(Theme.CardBorder)) e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
                using (SolidBrush b = new SolidBrush(Theme.Accent)) e.Graphics.FillRectangle(b, 0, 0, Ui.S(4), p.Height);
            };
            Label t = new Label();
            t.Text = title;
            t.Font = Theme.FB(9.2f);
            t.ForeColor = Theme.Navy;
            t.BackColor = p.BackColor;
            t.SetBounds(Ui.S(14), Ui.S(10), Ui.S(244), Ui.S(20));
            p.Controls.Add(t);
            Label r = new Label();
            r.Text = rawName;
            r.Font = Theme.F(7.8f);
            r.ForeColor = Theme.TextMuted;
            r.BackColor = p.BackColor;
            r.SetBounds(Ui.S(14), Ui.S(32), Ui.S(244), Ui.S(16));
            p.Controls.Add(r);
            Label d = new Label();
            d.Text = desc;
            d.Font = Theme.F(7.8f);
            d.ForeColor = Theme.TextMuted;
            d.BackColor = p.BackColor;
            d.SetBounds(Ui.S(14), Ui.S(52), Ui.S(244), Ui.S(42));
            p.Controls.Add(d);
            EventHandler open = delegate { OpenPath(path); };
            p.Click += open; t.Click += open; r.Click += open; d.Click += open;
            parent.Controls.Add(p);
        }

        /// <summary>列出本地记录文件夹里的文件与子文件夹（用户要求：选定文件夹后在软件内直接看到内容）。</summary>
        private void RefreshProgressFiles()
        {
            if (progList == null) return;
            string folder = AppPaths.MatLocal(appDir);
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
            Msg.Show("文件不存在，可能已被移动或删除 / File not found.", "本地记录");
        }

        private void ChooseProgressFolder()
        {
            if (!RequireLogin("本地记录")) return;
            using (FolderBrowserDialog d = new FolderBrowserDialog())
            {
                d.Description = "选择本地记录文件夹（所有内容都存放在这里）";
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
            if (!RequireLogin("本地记录")) return;
            string folder = AppPaths.MatLocal(appDir);
            Directory.CreateDirectory(folder);
            string project = Prompt.Show(this, "新建文档", "项目名称", "本地记录");
            if (string.IsNullOrEmpty(project)) return;
            string title = DateTime.Now.ToString("yyyy-MM-dd") + " " + project;
            try
            {
                string path = LocalDocs.CreateDocxIn(folder, project, title);
                RefreshProgressFiles();
                Msg.Show("文档已创建：" + path, "本地记录");
                OpenPath(path);
            }
            catch (Exception ex) { Msg.Show("创建失败：" + ex.Message, "本地记录"); }
        }

        private void OpenProgressFolder()
        {
            string folder = AppPaths.MatLocal(appDir);
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
