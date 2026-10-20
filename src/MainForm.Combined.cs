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
        /* ---------- 参考文献与资料（含研究类别） ---------- */

        private Panel BuildRefsAndCategoriesPage()
        {
            Panel page = new Panel();
            page.BackColor = Color.White;
            Panel content = new Panel();
            content.Dock = DockStyle.Fill;
            content.BackColor = Color.White;
            Panel refs = BuildRefPage();
            refs.Dock = DockStyle.Fill;
            Panel cats = BuildCategoryPage();
            cats.Dock = DockStyle.Fill;
            cats.Visible = false;
            content.Controls.Add(refs);
            content.Controls.Add(cats);
            Panel bar = new Panel();
            bar.Dock = DockStyle.Top;
            bar.Height = Ui.S(44);
            bar.BackColor = Color.White;
            AddSegBar(bar, new string[] { "参考资料", "研究类别" }, delegate(int sel)
            {
                refs.Visible = sel == 0;
                cats.Visible = sel == 1;
                if (sel == 0) RebuildRefs(); else RebuildCategories();
            });
            page.Controls.Add(content);
            page.Controls.Add(bar);
            return page;
        }

        /* ---------- 本地进度（研究日志 / 本地文档 / 本地资料） ---------- */

        private Panel[] localProgressTabs;
        private Button[] localProgressButtons;
        private void ShowResearchEntry(Entry entry)
        {
            SetPage(3);
            for (int i = 0; i < localProgressTabs.Length; i++) localProgressTabs[i].Visible = i == 0;
            SelectSeg(localProgressButtons, 0);
            txtSearch.Text = "搜索标题或内容"; txtSearch.ForeColor = Theme.TextFaint;
            current = entry; RefreshList();
        }

        private Panel BuildLocalProgressPage()
        {
            Panel page = new Panel();
            page.BackColor = Color.White;
            Panel content = new Panel();
            content.Dock = DockStyle.Fill;
            content.BackColor = Color.White;
            Panel log = BuildLogPage();
            log.Dock = DockStyle.Fill;
            Panel doc = BuildDocPage();
            doc.Dock = DockStyle.Fill;
            Panel local = BuildLocalRefsPanel();
            local.Dock = DockStyle.Fill;
            doc.Visible = false;
            local.Visible = false;
            content.Controls.Add(log);
            content.Controls.Add(doc);
            content.Controls.Add(local);
            Panel bar = new Panel();
            bar.Dock = DockStyle.Top;
            bar.Height = Ui.S(44);
            bar.BackColor = Color.White;
            localProgressTabs = new Panel[] { log, doc, local };
            localProgressButtons = AddSegBar(bar, new string[] { "研究日志", "本地文档", "本地资料" }, delegate(int sel)
            {
                log.Visible = sel == 0;
                doc.Visible = sel == 1;
                local.Visible = sel == 2;
                if (sel == 0) RefreshList();
                if (sel == 1) RefreshDocPage();
                if (sel == 2) RefreshLocalRefs();
            });
            page.Controls.Add(content);
            page.Controls.Add(bar);
            return page;
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
