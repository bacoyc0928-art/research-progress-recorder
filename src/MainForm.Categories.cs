using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SRLogbook
{
    public partial class MainForm
    {
        private FlowLayoutPanel catsFlow;
        private Panel catsTop;
        private Button btnAddCat;

        /* ---------------- 研究类别 ---------------- */

        private Panel BuildCategoryPage()
        {
            Panel page = new Panel();
            page.BackColor = Color.White;

            Panel top = new Panel();
            top.Dock = DockStyle.Top;
            top.Height = 78;
            top.BackColor = Color.White;

            Label h = new Label();
            h.Text = "研究类别";
            h.Font = Theme.FB(12.5f);
            h.ForeColor = Theme.Navy;
            h.BackColor = Color.White;
            h.SetBounds(2, 14, 260, 26);
            top.Controls.Add(h);

            Label hint = new Label();
            hint.Text = "类别可自行添加、编辑、删除，并可设置任务数量、预计时间与相关材料。";
            hint.Font = Theme.F(8.8f);
            hint.ForeColor = Theme.TextMuted;
            hint.BackColor = Color.White;
            hint.SetBounds(2, 46, 700, 22);
            top.Controls.Add(hint);

            Button add = new Button();
            add.Text = "＋ 添加研究类别";
            add.Font = Theme.FB(9.2f);
            add.FlatStyle = FlatStyle.Flat;
            add.FlatAppearance.BorderSize = 0;
            add.BackColor = Theme.Accent;
            add.ForeColor = Color.White;
            add.Cursor = Cursors.Hand;
            add.SetBounds(0, 18, 190, 38);
            add.Click += delegate { EditCategory(null); };
            top.Controls.Add(add);
            catsTop = top;
            btnAddCat = add;

            Button clear = new Button();
            clear.Text = "删除全部类别";
            clear.Font = Theme.F(8.8f);
            clear.FlatStyle = FlatStyle.Flat;
            clear.FlatAppearance.BorderSize = 1;
            clear.FlatAppearance.BorderColor = Color.FromArgb(238, 215, 213);
            clear.BackColor = Color.White;
            clear.ForeColor = Color.FromArgb(179, 69, 63);
            clear.Cursor = Cursors.Hand;
            clear.SetBounds(Ui.S(200), Ui.S(22), Ui.S(150), Ui.S(34));
            clear.Click += delegate { ClearAllCategories(); };
            top.Controls.Add(clear);

            catsFlow = new FlowLayoutPanel();
            catsFlow.Dock = DockStyle.Fill;
            catsFlow.FlowDirection = FlowDirection.TopDown;
            catsFlow.WrapContents = false;
            catsFlow.AutoScroll = true;
            catsFlow.BackColor = Color.White;
            catsFlow.Padding = new Padding(0, 8, 0, 0);

            page.Controls.Add(catsFlow);
            page.Controls.Add(top);
            page.Resize += delegate { if (uiReady) RebuildCategories(); };
            return page;
        }

        private void RebuildCategories()
        {
            if (catsFlow == null || !uiReady) return;
            int keepY = -catsFlow.AutoScrollPosition.Y;
            catsFlow.SuspendLayout();
            catsFlow.Controls.Clear();
            int w = Math.Max(Ui.S(640), catsFlow.ClientSize.Width - Ui.S(30));
            foreach (Category c in store.Categories)
            {
                catsFlow.Controls.Add(BuildCategoryCard(c, w));
            }
            catsFlow.ResumeLayout();
            if (keepY > 0) catsFlow.AutoScrollPosition = new Point(0, keepY);
            if (btnAddCat != null && catsTop != null)
                btnAddCat.Left = Math.Max(Ui.S(0), catsTop.Width - btnAddCat.Width - Ui.S(22));
            if (Lang.En) Lang.Apply(catsFlow);
        }

        private void OpenLinkSafe(Category c, CatLink l)
        {
            string p = l.Url;
            if (l.Kind == "url" || (p != null && (p.StartsWith("http://") || p.StartsWith("https://"))))
            {
                OpenPath(p);
                return;
            }
            if (File.Exists(p) || Directory.Exists(p))
            {
                OpenPath(p);
                return;
            }
            if (Msg.Show("找不到文件：\n" + p + "\n\n是否重新定位？（其他人同步过来的路径在你的电脑上可能不存在）", "文件不存在", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            OpenFileDialog d = new OpenFileDialog();
            d.Filter = "所有文件|*.*";
            if (d.ShowDialog(this) != DialogResult.OK) return;
            l.Url = d.FileName;
            l.Title = Path.GetFileName(d.FileName);
            l.Kind = "file";
            Persist();
            RebuildCategories();
        }

        private Panel BuildCategoryCard(Category c, int w)
        {
            Panel card = new Panel();
            card.SetBounds(0, 0, w, Ui.S(136));
            card.Margin = new Padding(0, 0, 0, Ui.S(12));
            card.BackColor = Color.White;
            card.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen p = new Pen(Theme.CardBorder)) e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
                using (SolidBrush b = new SolidBrush(Theme.Accent)) e.Graphics.FillRectangle(b, 0, 0, Ui.S(4), card.Height);
            };

            Label name = new Label();
            name.Text = c.Name;
            name.Font = Theme.FB(11.5f);
            name.ForeColor = Theme.Navy;
            name.BackColor = Color.White;
            name.SetBounds(Ui.S(18), Ui.S(12), w - Ui.S(40), Ui.S(26));
            card.Controls.Add(name);

            double pct = c.TaskTotal > 0 ? Math.Min(100.0, 100.0 * c.TaskDone / c.TaskTotal) : 0;
            Label meta = new Label();
            meta.Text = "已完成 " + c.TaskDone + " / " + c.TaskTotal + " 项任务　·　预计 " + c.Hours.ToString("0.#") + " 小时　·　进度 " + Math.Round(pct) + "%";
            meta.Font = Theme.F(9f);
            meta.ForeColor = Theme.TextMuted;
            meta.BackColor = Color.White;
            meta.SetBounds(Ui.S(18), Ui.S(42), w - Ui.S(40), Ui.S(22));
            card.Controls.Add(meta);

            Panel track = new Panel();
            track.SetBounds(Ui.S(18), Ui.S(70), w - Ui.S(36), Ui.S(9));
            track.BackColor = Color.FromArgb(237, 241, 248);
            card.Controls.Add(track);
            Panel fill = new Panel();
            fill.SetBounds(0, 0, (int)((w - Ui.S(36)) * pct / 100.0), Ui.S(9));
            fill.BackColor = Theme.Accent;
            track.Controls.Add(fill);

            FlowLayoutPanel links = new FlowLayoutPanel();
            links.SetBounds(Ui.S(18), Ui.S(88), w - Ui.S(200), Ui.S(36));
            links.BackColor = Color.White;
            links.WrapContents = false;
            links.AutoScroll = true;
            int shown = 0;
            foreach (CatLink l in c.Links)
            {
                if (shown >= 6) break;
                string url = l.Url;
                Button b = new Button();
                b.Text = l.Title;
                b.Font = Theme.F(8.6f);
                b.FlatStyle = FlatStyle.Flat;
                b.FlatAppearance.BorderSize = 1;
                b.FlatAppearance.BorderColor = Theme.CardBorder;
                b.BackColor = Color.White;
                b.ForeColor = Theme.TextMain;
                b.Cursor = Cursors.Hand;
                b.AutoSize = true;
                b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                b.MaximumSize = new Size(Ui.S(220), Ui.S(28));
                b.MinimumSize = new Size(Ui.S(60), Ui.S(28));
                b.Padding = new Padding(Ui.S(6), 0, Ui.S(6), 0);
                CatLink linkRef = l;
                b.Click += delegate { OpenLinkSafe(c, linkRef); };
                links.Controls.Add(b);
                shown++;
            }
            if (c.Links.Count > 6)
            {
                Label more = new Label();
                more.Text = "+" + (c.Links.Count - 6);
                more.Font = Theme.F(8.4f);
                more.ForeColor = Theme.TextMuted;
                more.BackColor = Color.White;
                more.AutoSize = true;
                links.Controls.Add(more);
            }
            card.Controls.Add(links);

            Button del = new Button();
            del.Text = "删除";
            del.Font = Theme.F(8.8f);
            del.FlatStyle = FlatStyle.Flat;
            del.FlatAppearance.BorderSize = 1;
            del.FlatAppearance.BorderColor = Color.FromArgb(238, 215, 213);
            del.BackColor = Color.White;
            del.ForeColor = Color.FromArgb(179, 69, 63);
            del.Cursor = Cursors.Hand;
            del.SetBounds(w - Ui.S(98), Ui.S(12), Ui.S(72), Ui.S(30));
            del.Click += delegate { DeleteCategory(c); };
            card.Controls.Add(del);

            Button edit = new Button();
            edit.Text = "编辑";
            edit.Font = Theme.F(8.8f);
            edit.FlatStyle = FlatStyle.Flat;
            edit.FlatAppearance.BorderSize = 1;
            edit.FlatAppearance.BorderColor = Theme.CardBorder;
            edit.BackColor = Color.White;
            edit.ForeColor = Theme.TextMain;
            edit.Cursor = Cursors.Hand;
            edit.SetBounds(w - Ui.S(178), Ui.S(12), Ui.S(72), Ui.S(30));
            edit.Click += delegate { EditCategory(c); };
            card.Controls.Add(edit);

            return card;
        }

        private void EditCategory(Category src)
        {
            using (CategoryForm f = new CategoryForm(src))
            {
                if (f.ShowDialog(this) != DialogResult.OK) return;
                if (f.DeleteRequested) { if (src != null) DeleteCategory(src); return; }
                if (f.Result == null) return;
                if (store.Categories.Exists(c => c.Id != f.Result.Id && string.Equals(c.Name, f.Result.Name, StringComparison.OrdinalIgnoreCase)))
                { Msg.Show("Category name already exists.", "Category"); return; }
                if (src == null)
                {
                    f.Result.Updated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
                    store.Categories.Add(f.Result);
                }
                else
                {
                    string oldName = src.Name;
                    src.Name = f.Result.Name;
                    src.TaskTotal = f.Result.TaskTotal;
                    src.TaskDone = f.Result.TaskDone;
                    src.Hours = f.Result.Hours;
                    src.Links = f.Result.Links;
                    src.Updated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
                    if (oldName != src.Name)
                    {
                        foreach (Entry e in store.Entries) if (e.Category == oldName) { e.Category = src.Name; e.Updated = SyncData.Now(); }
                        foreach (LocalDoc d in store.Documents) if (d.Category == oldName) { d.Category = src.Name; d.Updated = SyncData.Now(); }
                    }
                    if (current != null) LoadEntry(current);
                }
                Persist();
                RebuildCategories();
                RefreshCategoryCombo();
                RefreshOverview2();
            }
        }

        private void DeleteCategory(Category c)
        {
            if (Msg.Show("删除类别“" + c.Name + "”？该类别下的记录不会被删除，会变为未归类。", "删除类别", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            foreach (Entry e in store.Entries) if (e.Category == c.Name) { e.Category = ""; e.Updated = SyncData.Now(); }
            foreach (LocalDoc d in store.Documents) if (d.Category == c.Name) d.Category = "";
            store.Categories.Remove(c);
            store.DeletedCategories.Add(new DeletedCategory { Id = c.Id, At = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ") });
            Persist();
            RebuildCategories();
            RefreshCategoryCombo();
            if (current != null) LoadEntry(current);
            RefreshOverview2();
        }

        private void ClearAllCategories()
        {
            if (store.Categories.Count == 0)
            {
                Msg.Show("当前没有类别。", "提示");
                return;
            }
            if (Msg.Show("删除全部 " + store.Categories.Count + " 个类别？记录会变为未归类，且不可恢复。", "删除全部类别", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            foreach (Category c in store.Categories)
            {
                foreach (Entry e in store.Entries) if (e.Category == c.Name) { e.Category = ""; e.Updated = SyncData.Now(); }
                foreach (LocalDoc d in store.Documents) if (d.Category == c.Name) d.Category = "";
                store.DeletedCategories.Add(new DeletedCategory { Id = c.Id, At = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ") });
            }
            store.Categories.Clear();
            Persist();
            RebuildCategories();
            RefreshCategoryCombo();
            if (current != null) LoadEntry(current);
            RefreshOverview2();
        }

        private void RefreshCategoryCombo()
        {
            if (cbCat == null) return;
            bool wasLoading = loading; loading = true;
            string cur = cbCat.SelectedItem as string;
            if (cur == null && current != null) cur = current.Category;
            cbCat.BeginUpdate();
            cbCat.Items.Clear();
            cbCat.Items.Add("");
            foreach (Category c in store.Categories) cbCat.Items.Add(c.Name);
            cbCat.EndUpdate();
            if (cur != null && cbCat.Items.Contains(cur)) cbCat.SelectedItem = cur;
            else cbCat.SelectedIndex = 0;
            loading = wasLoading;
        }

        /* ---------------- 概览 ---------------- */

        private void RefreshOverview2()
        {
            if (p3 == null) return;
            p3.SuspendLayout();
            p3.Controls.Clear();

            int totalTasks = 0, doneTasks = 0;
            double hours = 0;
            foreach (Category c in store.Categories)
            {
                totalTasks += c.TaskTotal;
                doneTasks += c.TaskDone;
                hours += c.Hours;
            }
            int overall = totalTasks > 0 ? (int)Math.Round(100.0 * doneTasks / totalTasks) : 0;

            AddStatCard(p3, 0, "总体进度", overall.ToString(), "%", Theme.Accent);
            AddStatCard(p3, 1, "任务完成", doneTasks + "/" + totalTasks, "项", Theme.Warm);
            AddStatCard(p3, 2, "预计总工时", hours.ToString("0.#"), "h", Color.FromArgb(46, 130, 110));

            Label lc = new Label();
            lc.Text = "各类别进度";
            lc.Font = Theme.FB(11f);
            lc.ForeColor = Theme.Navy;
            lc.SetBounds(Ui.S(4), Ui.S(124), Ui.S(120), Ui.S(24));
            p3.Controls.Add(lc);

            Button bAddCat = new Button();
            bAddCat.Text = "＋ 添加类别";
            bAddCat.Font = Theme.F(8.6f);
            bAddCat.FlatStyle = FlatStyle.Flat;
            bAddCat.FlatAppearance.BorderSize = 1;
            bAddCat.FlatAppearance.BorderColor = Theme.CardBorder;
            bAddCat.BackColor = Color.White;
            bAddCat.ForeColor = Theme.TextMain;
            bAddCat.Cursor = Cursors.Hand;
            bAddCat.SetBounds(Ui.S(130), Ui.S(120), Ui.S(100), Ui.S(26));
            bAddCat.Visible = true;
            p3.Controls.Add(bAddCat);

            int y = Ui.S(160);
            foreach (Category c in store.Categories)
            {
                int pct = c.TaskTotal > 0 ? (int)Math.Round(100.0 * c.TaskDone / c.TaskTotal) : 0;
                Label ln = new Label();
                ln.Text = c.Name;
                ln.Font = Theme.F(9f);
                ln.ForeColor = Theme.TextMain;
                ln.SetBounds(Ui.S(4), y, Ui.S(110), Ui.S(20));
                ln.Cursor = Cursors.Hand;
                Category cap = c;
                ln.Click += delegate { EditCategory(cap); };
                p3.Controls.Add(ln);

                Panel track = new Panel();
                track.SetBounds(Ui.S(120), y + Ui.S(6), Ui.S(220), Ui.S(9));
                track.BackColor = Color.FromArgb(237, 241, 248);
                p3.Controls.Add(track);
                Panel fill = new Panel();
                fill.SetBounds(0, 0, (int)(Ui.S(220) * pct / 100.0), Ui.S(9));
                fill.BackColor = Theme.Accent;
                track.Controls.Add(fill);

                Label lv = new Label();
                lv.Text = c.TaskDone + "/" + c.TaskTotal + " · " + pct + "%";
                lv.Font = Theme.F(8.4f);
                lv.ForeColor = Theme.TextMuted;
                lv.SetBounds(Ui.S(350), y, Ui.S(150), Ui.S(20));
                p3.Controls.Add(lv);
                y += Ui.S(34);
            }
            if (store.Categories.Count == 0)
            {
                Label le = new Label();
                le.Text = "还没有类别。到“研究类别”页添加。";
                le.Font = Theme.F(9f);
                le.ForeColor = Theme.TextFaint;
                le.SetBounds(Ui.S(4), y, Ui.S(400), Ui.S(20));
                p3.Controls.Add(le);
            }

            Label lr = new Label();
            lr.Text = "最近更新";
            lr.Font = Theme.FB(11f);
            lr.ForeColor = Theme.Navy;
            lr.SetBounds(Ui.S(540), Ui.S(124), Ui.S(300), Ui.S(24));
            p3.Controls.Add(lr);

            List<Entry> sorted = new List<Entry>(store.Entries);
            sorted.Sort(delegate(Entry a, Entry b) { return SyncData.Compare(b.Updated, a.Updated); });
            int yy = Ui.S(160);
            int shown = 0;
            foreach (Entry e in sorted)
            {
                if (shown >= 6) break;
                Label lt = new Label();
                string t = e.Title;
                if (t != null && t.Length > 24) t = t.Substring(0, 24) + "…";
                lt.Text = t;
                lt.Font = Theme.F(9.2f);
                lt.ForeColor = Theme.TextMain;
                lt.AutoEllipsis = true;
                lt.SetBounds(Ui.S(540), yy, Ui.S(340), Ui.S(20));
                lt.Cursor = Cursors.Hand;
                lt.Click += delegate { ShowResearchEntry(e); };
                p3.Controls.Add(lt);
                Label ld = new Label();
                ld.Text = (e.Updated == null ? "" : e.Updated) + "  ·  " + (e.Category == null ? "" : e.Category) + "  ·  " + e.Progress + "%";
                ld.Font = Theme.F(7.9f);
                ld.ForeColor = Theme.TextFaint;
                ld.SetBounds(Ui.S(540), yy + Ui.S(20), Ui.S(340), Ui.S(16));
                p3.Controls.Add(ld);
                yy += Ui.S(50);
                shown++;
            }
            p3.ResumeLayout();
            if (Lang.En) Lang.Apply(p3);
        }


        /* ---------------- 本地文档 ---------------- */

        private TextBox txtDProject;
        private ComboBox cbDCat;
        private TextBox txtDTitle;
        private Label lblDStatus;
        private ListBox lstDocs;
        private Panel docListPanel;

        private Panel BuildDocPage()
        {
            Panel page = new Panel();
            page.BackColor = Color.White;
            page.AutoScroll = true;

            Panel card = new Panel();
            card.SetBounds(0, 0, 860, 470);
            card.BackColor = Color.White;
            card.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen p = new Pen(Theme.CardBorder)) e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
            };
            page.Controls.Add(card);

            Label h = new Label();
            h.Text = "本地进度";
            h.Font = Theme.FB(13f);
            h.ForeColor = Theme.Navy;
            h.BackColor = Color.White;
            h.SetBounds(24, 18, 400, 28);
            card.Controls.Add(h);

            Label hint = new Label();
            hint.Text = "文档保存在工作文件夹内的“本地文档”文件夹，命名为“日期 + 项目名称”，并归入所选研究类别。";
            hint.Font = Theme.F(8.8f);
            hint.ForeColor = Theme.TextMuted;
            hint.BackColor = Color.White;
            hint.SetBounds(24, 52, 800, 40);
            card.Controls.Add(hint);

            AddDLabel(card, "项目名称", 24, 104);
            txtDProject = new TextBox();
            txtDProject.Font = Theme.F(10f);
            txtDProject.SetBounds(24, 126, 360, 30);
            card.Controls.Add(txtDProject);

            AddDLabel(card, "归属类别", 404, 104);
            cbDCat = new ComboBox();
            cbDCat.DropDownStyle = ComboBoxStyle.DropDownList;
            cbDCat.Font = Theme.F(9.5f);
            cbDCat.SetBounds(404, 126, 240, 30);
            card.Controls.Add(cbDCat);

            AddDLabel(card, "文档标题（可修改）", 24, 174);
            txtDTitle = new TextBox();
            txtDTitle.Font = Theme.F(9.5f);
            txtDTitle.SetBounds(24, 196, 620, 30);
            card.Controls.Add(txtDTitle);

            Button create = new Button();
            create.Text = "新建文档";
            create.Font = Theme.FB(9.5f);
            create.FlatStyle = FlatStyle.Flat;
            create.FlatAppearance.BorderSize = 0;
            create.BackColor = Theme.Accent;
            create.ForeColor = Color.White;
            create.Cursor = Cursors.Hand;
            create.SetBounds(24, 246, 180, 40);
            create.Click += delegate { CreateLocalDoc(); };
            card.Controls.Add(create);

            Button openDir = new Button();
            openDir.Text = "打开文档文件夹";
            openDir.Font = Theme.F(9f);
            openDir.FlatStyle = FlatStyle.Flat;
            openDir.FlatAppearance.BorderSize = 1;
            openDir.FlatAppearance.BorderColor = Theme.CardBorder;
            openDir.BackColor = Color.White;
            openDir.ForeColor = Theme.TextMain;
            openDir.Cursor = Cursors.Hand;
            openDir.SetBounds(216, 246, 160, 40);
            openDir.Click += delegate { OpenPath(LocalDocs.Folder(projectRoot)); };
            card.Controls.Add(openDir);

            lblDStatus = new Label();
            lblDStatus.Text = "";
            lblDStatus.Font = Theme.F(8.8f);
            lblDStatus.ForeColor = Theme.TextMuted;
            lblDStatus.BackColor = Color.White;
            lblDStatus.SetBounds(24, 304, 800, 60);
            card.Controls.Add(lblDStatus);

            Panel listCard = new Panel();
            listCard.SetBounds(0, 500, 860, 340);
            listCard.BackColor = Color.White;
            listCard.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen p = new Pen(Theme.CardBorder)) e.Graphics.DrawRectangle(p, 0, 0, listCard.Width - 1, listCard.Height - 1);
            };
            page.Controls.Add(listCard);
            docListPanel = listCard;

            Label dh = new Label();
            dh.Text = "文档清单";
            dh.Font = Theme.FB(12f);
            dh.ForeColor = Theme.Navy;
            dh.BackColor = Color.White;
            dh.SetBounds(24, 16, 300, 26);
            listCard.Controls.Add(dh);

            Label dsub = new Label();
            dsub.Text = "文档可打开或归入类别；移除会将磁盘文件移入回收站。";
            dsub.Font = Theme.F(8.4f);
            dsub.ForeColor = Theme.TextMuted;
            dsub.BackColor = Color.White;
            dsub.SetBounds(24, 44, 800, 20);
            listCard.Controls.Add(dsub);

            lstDocs = new ListBox();
            lstDocs.SetBounds(24, 70, 812, 170);
            lstDocs.Font = Theme.F(9f);
            lstDocs.BorderStyle = BorderStyle.FixedSingle;
            lstDocs.IntegralHeight = false;
            lstDocs.DoubleClick += delegate { OpenSelectedDoc(); };
            listCard.Controls.Add(lstDocs);

            Button bOpen = DButton("打开", 24, 256, 80);
            bOpen.Click += delegate { OpenSelectedDoc(); };
            Button bAssign = DButton("归入所选类别", 114, 256, 130);
            bAssign.Click += delegate { AssignSelectedDoc(); };
            Button bRemove = DButton("移除记录", 254, 256, 100);
            bRemove.Click += delegate { RemoveSelectedDoc(); };
            Button bRefresh = DButton("刷新", 364, 256, 80);
            bRefresh.Click += delegate { RefreshDocList(); };

            page.Resize += delegate
            {
                card.Width = Math.Min(Ui.S(880), page.ClientSize.Width - Ui.S(2));
                listCard.Width = card.Width;
                lstDocs.Width = Math.Max(Ui.S(300), listCard.Width - Ui.S(48));
            };
            return page;
        }

        private void AddDLabel(Control parent, string text, int x, int y)
        {
            Label l = new Label();
            l.Text = text;
            l.Font = Theme.F(8.4f);
            l.ForeColor = Theme.TextMuted;
            l.BackColor = Color.White;
            l.SetBounds(x, y, 200, 18);
            parent.Controls.Add(l);
        }

        private Button DButton(string text, int x, int y, int w)
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
            b.SetBounds(x, y, w, 32);
            docListPanel.Controls.Add(b);
            return b;
        }

        private void RefreshDocPage()
        {
            if (txtDProject == null) return;
            if (string.IsNullOrEmpty(store.DocProject)) store.DocProject = "Untitled";
            txtDProject.Text = store.DocProject;
            cbDCat.BeginUpdate();
            cbDCat.Items.Clear();
            cbDCat.Items.Add("");
            foreach (Category c in store.Categories) cbDCat.Items.Add(c.Name);
            cbDCat.EndUpdate();
            if (cbDCat.Items.Count > 0) cbDCat.SelectedIndex = 0;
            UpdateDocTitle();
            RefreshDocList();
            lblDStatus.Text = "文档保存位置：" + LocalDocs.Folder(projectRoot);
        }

        private void UpdateDocTitle()
        {
            if (txtDTitle == null) return;
            string project = txtDProject.Text.Trim();
            if (project.Length == 0) project = "Untitled";
            txtDTitle.Text = DateTime.Now.ToString("yyyy-MM-dd") + " " + project;
        }

        private void RefreshDocList()
        {
            if (lstDocs == null) return;
            lstDocs.BeginUpdate();
            lstDocs.Items.Clear();
            foreach (LocalDoc d in store.Documents)
            {
                string cat = string.IsNullOrEmpty(d.Category) ? "未分类" : d.Category;
                lstDocs.Items.Add(cat + "  |  " + d.Name + "  |  " + (d.Updated ?? ""));
            }
            lstDocs.EndUpdate();
        }

        private void OpenSelectedDoc()
        {
            if (lstDocs == null || lstDocs.SelectedIndex < 0 || lstDocs.SelectedIndex >= store.Documents.Count) return;
            OpenPath(store.Documents[lstDocs.SelectedIndex].Path);
        }

        private void AssignSelectedDoc()
        {
            if (lstDocs == null || lstDocs.SelectedIndex < 0 || lstDocs.SelectedIndex >= store.Documents.Count) return;
            string catName = cbDCat.SelectedItem == null ? "" : cbDCat.SelectedItem.ToString();
            if (catName.Length == 0) return;
            LocalDoc d = store.Documents[lstDocs.SelectedIndex];
            foreach (Category old in store.Categories)
            {
                if (old.Links.RemoveAll(l => l.Url == d.Path) > 0) old.Updated = SyncData.Now();
            }
            d.Category = catName; d.Updated = SyncData.Now();
            foreach (Category c in store.Categories)
            {
                if (c.Name != catName) continue;
                bool exists = false;
                foreach (CatLink l in c.Links) if (l.Url == d.Path) exists = true;
                if (!exists) { c.Links.Add(new CatLink { Title = d.Name, Url = d.Path, Kind = "file" }); c.Updated = SyncData.Now(); }
                break;
            }
            Persist();
            RebuildCategories();
            RefreshDocList();
            lblDStatus.Text = "已把“" + d.Name + "”归入类别“" + catName + "”。";
        }

        private void RemoveSelectedDoc()
        {
            if (lstDocs == null || lstDocs.SelectedIndex < 0 || lstDocs.SelectedIndex >= store.Documents.Count) return;
            LocalDoc d = store.Documents[lstDocs.SelectedIndex];
            if (Msg.Show("删除“" + d.Name + "”？\r\n这会将该文档移入回收站。", "删除文档", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                if (!string.IsNullOrEmpty(d.Path) && File.Exists(d.Path)) Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(d.Path, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
            }
            catch (Exception ex)
            {
                Msg.Show("删除失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            foreach (Category c in store.Categories) if (c.Links.RemoveAll(l => l.Url == d.Path) > 0) c.Updated = SyncData.Now();
            store.Documents.RemoveAt(lstDocs.SelectedIndex);
            RebuildCategories();
            Persist();
            RefreshDocList();
        }

        private void CreateLocalDoc()
        {
            string project = txtDProject.Text.Trim();
            if (project.Length == 0) project = "Untitled";
            string title = txtDTitle.Text.Trim();
            if (title.Length == 0) title = DateTime.Now.ToString("yyyy-MM-dd") + " " + project;
            string catName = cbDCat.SelectedItem == null ? "" : cbDCat.SelectedItem.ToString();
            try
            {
                string path = LocalDocs.CreateDocx(projectRoot, project, catName, title);
                store.DocProject = project;
                store.Documents.Add(new LocalDoc
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Name = title,
                    Path = path,
                    Category = catName,
                    Updated = SyncData.Now()
                });
                foreach (Category c in store.Categories)
                {
                    if (c.Name == catName)
                    {
                        c.Links.Add(new CatLink { Title = title, Url = path, Kind = "file" });
                        c.Updated = SyncData.Now();
                        break;
                    }
                }
                Persist();
                RebuildCategories();
                RefreshDocList();
                lblDStatus.Text = "已创建：" + path;
                if (Msg.Show("文档已创建，是否现在打开？", "本地进度", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    OpenPath(path);
            }
            catch (Exception ex)
            {
                lblDStatus.Text = "创建失败：" + ex.Message;
                Msg.Show("创建失败：" + ex.Message, "本地进度", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    /* ---------------- 类别编辑对话框 ---------------- */

    public class CategoryForm : Form
    {
        public Category Result;
        public bool DeleteRequested;
        private TextBox txtName;
        private NumericUpDown numTotal, numDone, numHours;
        private ListBox lstLinks;
        private List<CatLink> links;
        private string id;

        public CategoryForm(Category src)
        {
            this.Text = src == null ? "添加研究类别" : "编辑研究类别";
            this.ClientSize = new Size(Ui.S(600), Ui.S(560));
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false; this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.Font = Theme.F(9.5f);

            id = src == null ? Guid.NewGuid().ToString("N") : src.Id;
            bool isNew = src == null;
            links = new List<CatLink>();
            if (src != null) foreach (CatLink l in src.Links) links.Add(l);

            AddLabel("类别名称", 24, 20);
            txtName = new TextBox();
            txtName.SetBounds(Ui.S(24), Ui.S(42), Ui.S(540), Ui.S(30));
            txtName.Font = Theme.F(10.5f);
            txtName.Text = src == null ? "" : src.Name;
            Controls.Add(txtName);

            AddLabel("任务总数", 24, 88);
            numTotal = Num(24, 110, src == null ? 5 : src.TaskTotal, 999, 0);
            AddLabel("已完成", 220, 88);
            numDone = Num(220, 110, src == null ? 0 : src.TaskDone, 999, 0);
            AddLabel("预计时间（小时）", 416, 88);
            numHours = Num(416, 110, src == null ? 10 : (decimal)src.Hours, 9999, 1);

            AddLabel("相关材料（任意类型文件、文件夹或链接；也可直接把文件拖进列表）", 24, 158);
            lstLinks = new ListBox();
            lstLinks.SetBounds(Ui.S(24), Ui.S(182), Ui.S(540), Ui.S(160));
            lstLinks.Font = Theme.F(9f);
            lstLinks.BorderStyle = BorderStyle.FixedSingle;
            lstLinks.AllowDrop = true;
            lstLinks.DoubleClick += delegate { OpenSelectedLink(); };
            lstLinks.DragEnter += delegate(object s, DragEventArgs e)
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
            };
            lstLinks.DragDrop += delegate(object s, DragEventArgs e)
            {
                string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files == null) return;
                foreach (string f in files)
                {
                    if (Directory.Exists(f))
                        links.Add(new CatLink { Title = new DirectoryInfo(f).Name, Url = f, Kind = "folder" });
                    else if (File.Exists(f))
                        links.Add(new CatLink { Title = Path.GetFileName(f), Url = f, Kind = "file" });
                }
                RefreshLinks();
            };
            Controls.Add(lstLinks);
            RefreshLinks();

            Button bf = SmallBtn("添加文件", 24, 352, 104);
            bf.Click += delegate { AddFileLink(); };
            Button bd = SmallBtn("添加文件夹", 136, 352, 110);
            bd.Click += delegate { AddFolderLink(); };
            Button bl = SmallBtn("添加链接", 254, 352, 100);
            bl.Click += delegate { AddUrlLink(); };
            Button bo = SmallBtn("打开", 444, 352, 80);
            bo.Click += delegate { OpenSelectedLink(); };
            Button br = SmallBtn("移除", 362, 352, 80);
            br.Click += delegate
            {
                if (lstLinks.SelectedIndex >= 0) { links.RemoveAt(lstLinks.SelectedIndex); RefreshLinks(); }
            };

            if (!isNew)
            {
                Button delBtn = new Button();
                delBtn.Text = "删除此类别";
                delBtn.Font = Theme.F(8.8f);
                delBtn.FlatStyle = FlatStyle.Flat;
                delBtn.FlatAppearance.BorderSize = 1;
                delBtn.FlatAppearance.BorderColor = Color.FromArgb(238, 215, 213);
                delBtn.BackColor = Color.White;
                delBtn.ForeColor = Color.FromArgb(179, 69, 63);
                delBtn.Cursor = Cursors.Hand;
                delBtn.SetBounds(Ui.S(20), Ui.S(496), Ui.S(120), Ui.S(38));
                delBtn.Click += delegate { DeleteRequested = true; DialogResult = DialogResult.OK; Close(); };
                Controls.Add(delBtn);
            }

            Button ok = new Button();
            ok.Text = "保存";
            ok.Font = Theme.FB(9.5f);
            ok.FlatStyle = FlatStyle.Flat;
            ok.FlatAppearance.BorderSize = 0;
            ok.BackColor = Theme.Accent;
            ok.ForeColor = Color.White;
            ok.SetBounds(Ui.S(340), Ui.S(496), Ui.S(110), Ui.S(38));
            ok.Click += delegate { Save(); };
            Controls.Add(ok);

            Button cancel = new Button();
            cancel.Text = "取消";
            cancel.Font = Theme.F(9.5f);
            cancel.FlatStyle = FlatStyle.Flat;
            cancel.FlatAppearance.BorderSize = 1;
            cancel.FlatAppearance.BorderColor = Theme.CardBorder;
            cancel.BackColor = Color.White;
            cancel.SetBounds(Ui.S(458), Ui.S(496), Ui.S(100), Ui.S(38));
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(cancel);
            if (Lang.En) Lang.Apply(this);
        }

        private void AddLabel(string text, int x, int y)
        {
            Label l = new Label();
            l.Text = text;
            l.Font = Theme.F(8.5f);
            l.ForeColor = Theme.TextMuted;
            l.SetBounds(Ui.S(x), Ui.S(y), Ui.S(320), Ui.S(18));
            Controls.Add(l);
        }

        private NumericUpDown Num(int x, int y, decimal val, int max, int dec)
        {
            NumericUpDown n = new NumericUpDown();
            n.SetBounds(Ui.S(x), Ui.S(y), Ui.S(160), Ui.S(30));
            n.Minimum = 0; n.Maximum = max; n.DecimalPlaces = dec; n.Increment = dec > 0 ? 0.5m : 1m;
            n.Value = Math.Min(val, max);
            n.Font = Theme.F(9.5f);
            Controls.Add(n);
            return n;
        }

        private Button SmallBtn(string text, int x, int y, int w)
        {
            Button b = new Button();
            b.Text = text;
            b.Font = Theme.F(8.8f);
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = Theme.CardBorder;
            b.BackColor = Color.White;
            b.ForeColor = Theme.TextMain;
            b.SetBounds(Ui.S(x), Ui.S(y), Ui.S(w), Ui.S(32));
            Controls.Add(b);
            return b;
        }

        private void RefreshLinks()
        {
            lstLinks.BeginUpdate();
            lstLinks.Items.Clear();
            foreach (CatLink l in links) lstLinks.Items.Add(l.Title + "  —  " + l.Url);
            lstLinks.EndUpdate();
        }

        private void AddFileLink()
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Filter = "所有文件|*.*";
            if (d.ShowDialog(this) != DialogResult.OK) return;
            links.Add(new CatLink { Title = Path.GetFileName(d.FileName), Url = d.FileName, Kind = "file" });
            RefreshLinks();
        }

        private void AddFolderLink()
        {
            FolderBrowserDialog d = new FolderBrowserDialog();
            if (d.ShowDialog(this) != DialogResult.OK) return;
            links.Add(new CatLink { Title = new DirectoryInfo(d.SelectedPath).Name, Url = d.SelectedPath, Kind = "folder" });
            RefreshLinks();
        }

        private void OpenSelectedLink()
        {
            if (lstLinks.SelectedIndex < 0 || lstLinks.SelectedIndex >= links.Count) return;
            CatLink l = links[lstLinks.SelectedIndex];
            if (l.Kind == "url" || (l.Url != null && (l.Url.StartsWith("http://") || l.Url.StartsWith("https://"))))
            {
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = l.Url, UseShellExecute = true }); }
                catch (Exception ex) { Msg.Show("无法打开链接：" + ex.Message, "提示"); }
                return;
            }
            if (File.Exists(l.Url) || Directory.Exists(l.Url))
            {
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = l.Url, UseShellExecute = true }); }
                catch (Exception ex) { Msg.Show("无法打开文件：" + ex.Message, "提示"); }
                return;
            }
            if (Msg.Show("找不到文件：\n" + l.Url + "\n\n是否重新定位？", "文件不存在", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            OpenFileDialog d = new OpenFileDialog();
            d.Filter = "所有文件|*.*";
            if (d.ShowDialog(this) != DialogResult.OK) return;
            l.Url = d.FileName;
            l.Title = Path.GetFileName(d.FileName);
            l.Kind = "file";
            RefreshLinks();
        }

        private void AddUrlLink()
        {
            string url = Prompt.Show(this, "添加链接", "链接地址（https://…）", "https://");
            if (string.IsNullOrEmpty(url) || url == "https://") return;
            links.Add(new CatLink { Title = url.Length > 40 ? url.Substring(0, 40) + "…" : url, Url = url, Kind = "url" });
            RefreshLinks();
        }

        private void Save()
        {
            string name = txtName.Text.Trim();
            if (name.Length == 0) { Msg.Show("请填写类别名称。", "提示"); return; }
            Category c = new Category();
            c.Id = id;
            c.Name = name;
            c.TaskTotal = (int)numTotal.Value;
            c.TaskDone = Math.Min((int)numDone.Value, c.TaskTotal);
            c.Hours = (double)numHours.Value;
            c.Links = links;
            Result = c;
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    public static class Prompt
    {
        public static string Show(IWin32Window owner, string title, string label, string def)
        {
            using (Form f = new Form())
            {
                f.Text = title;
                f.ClientSize = new Size(Ui.S(520), Ui.S(190));
                f.StartPosition = FormStartPosition.CenterParent;
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.MaximizeBox = false; f.MinimizeBox = false;
                f.BackColor = Color.White;
                f.Font = Theme.F(9.5f);

                Label l = new Label();
                l.Text = label;
                l.SetBounds(Ui.S(20), Ui.S(16), Ui.S(460), Ui.S(20));
                f.Controls.Add(l);

                TextBox t = new TextBox();
                t.SetBounds(Ui.S(20), Ui.S(42), Ui.S(470), Ui.S(30));
                t.Text = def;
                f.Controls.Add(t);

                Button ok = new Button();
                ok.Text = "确定";
                ok.SetBounds(Ui.S(300), Ui.S(96), Ui.S(90), Ui.S(34));
                ok.FlatStyle = FlatStyle.Flat;
                ok.BackColor = Theme.Accent; ok.ForeColor = Color.White;
                ok.FlatAppearance.BorderSize = 0;
                ok.DialogResult = DialogResult.OK;
                f.Controls.Add(ok);

                Button no = new Button();
                no.Text = "取消";
                no.SetBounds(Ui.S(400), Ui.S(96), Ui.S(90), Ui.S(34));
                no.FlatStyle = FlatStyle.Flat;
                no.FlatAppearance.BorderSize = 1;
                no.FlatAppearance.BorderColor = Theme.CardBorder;
                no.BackColor = Color.White;
                no.DialogResult = DialogResult.Cancel;
                f.Controls.Add(no);

                f.AcceptButton = ok; f.CancelButton = no;
                if (f.ShowDialog(owner) != DialogResult.OK) return null;
                return t.Text.Trim();
            }
        }
    }
}
