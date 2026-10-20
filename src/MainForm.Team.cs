using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace SRLogbook
{
    public partial class MainForm
    {
        private FlowLayoutPanel teamFlow;
        private Label teamStatus;
        private LinkLabel lblPersonal;
        private Label lblPersonalPath;
        private string personalFolder;

        private Panel BuildTeamPage()
        {
            Panel page = new Panel();
            page.BackColor = Color.White;

            Panel top = new Panel();
            top.Dock = DockStyle.Top;
            top.Height = Ui.S(84);
            top.BackColor = Color.White;

            Label h = new Label();
            h.Text = "团队研究";
            h.Font = Theme.FB(13f);
            h.ForeColor = Theme.Navy;
            h.BackColor = Color.White;
            h.SetBounds(Ui.S(2), Ui.S(12), Ui.S(300), Ui.S(28));
            top.Controls.Add(h);

            Label sub = new Label();
            sub.Text = "每位成员加入仓库后，程序用其 GitHub 账号自动建立个人档案文件夹（team/<用户名>/）。";
            sub.Font = Theme.F(8.8f);
            sub.ForeColor = Theme.TextMuted;
            sub.BackColor = Color.White;
            sub.SetBounds(Ui.S(2), Ui.S(44), Ui.S(820), Ui.S(20));
            top.Controls.Add(sub);

            Button b = new Button();
            b.Text = "刷新";
            b.Font = Theme.F(8.8f);
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = Theme.CardBorder;
            b.BackColor = Color.White;
            b.ForeColor = Theme.TextMain;
            b.Cursor = Cursors.Hand;
            b.SetBounds(Ui.S(860), Ui.S(18), Ui.S(80), Ui.S(30));
            b.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            b.Click += delegate { RefreshTeamPage(); };
            top.Controls.Add(b);

            Button bMerge = new Button();
            bMerge.Text = "合并团队参考资料";
            bMerge.Font = Theme.F(8.8f);
            bMerge.FlatStyle = FlatStyle.Flat;
            bMerge.FlatAppearance.BorderSize = 1;
            bMerge.FlatAppearance.BorderColor = Theme.CardBorder;
            bMerge.BackColor = Color.White;
            bMerge.ForeColor = Theme.TextMain;
            bMerge.Cursor = Cursors.Hand;
            bMerge.SetBounds(Ui.S(670), Ui.S(18), Ui.S(180), Ui.S(30));
            bMerge.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            bMerge.Click += delegate { MergeTeamRefs(); };
            top.Controls.Add(bMerge);

            teamStatus = new Label();
            teamStatus.Text = "";
            teamStatus.Font = Theme.F(8.6f);
            teamStatus.ForeColor = Theme.TextMuted;
            teamStatus.BackColor = Color.White;
            teamStatus.SetBounds(Ui.S(2), Ui.S(64), Ui.S(820), Ui.S(18));
            top.Controls.Add(teamStatus);

            Panel pp = new Panel();
            pp.Dock = DockStyle.Top;
            pp.Height = Ui.S(76);
            pp.BackColor = Color.White;

            Label ph = new Label();
            ph.Text = "个人研究";
            ph.Font = Theme.FB(10.5f);
            ph.ForeColor = Theme.Navy;
            ph.BackColor = Color.White;
            ph.SetBounds(Ui.S(2), Ui.S(8), Ui.S(120), Ui.S(22));
            pp.Controls.Add(ph);

            lblPersonal = new LinkLabel();
            lblPersonal.Text = "（未登录）";
            lblPersonal.Font = Theme.FB(10f);
            lblPersonal.LinkColor = Theme.Accent;
            lblPersonal.BackColor = Color.White;
            lblPersonal.SetBounds(Ui.S(110), Ui.S(8), Ui.S(420), Ui.S(22));
            lblPersonal.LinkClicked += delegate { OpenPersonalFolder(); };
            pp.Controls.Add(lblPersonal);

            lblPersonalPath = new Label();
            lblPersonalPath.Text = "未选择个人文件夹：个人研究上传将失败。";
            lblPersonalPath.Font = Theme.F(8.6f);
            lblPersonalPath.ForeColor = Theme.TextMuted;
            lblPersonalPath.BackColor = Color.White;
            lblPersonalPath.SetBounds(Ui.S(2), Ui.S(36), Ui.S(600), Ui.S(18));
            pp.Controls.Add(lblPersonalPath);
            personalFolder = LoadPersonalFolder();
            UpdatePersonalLabels();

            Button bChoose = new Button();
            bChoose.Text = "选择个人文件夹";
            bChoose.Font = Theme.F(8.8f);
            bChoose.FlatStyle = FlatStyle.Flat;
            bChoose.FlatAppearance.BorderSize = 1;
            bChoose.FlatAppearance.BorderColor = Theme.CardBorder;
            bChoose.BackColor = Color.White;
            bChoose.ForeColor = Theme.TextMain;
            bChoose.Cursor = Cursors.Hand;
            bChoose.SetBounds(Ui.S(610), Ui.S(30), Ui.S(130), Ui.S(30));
            bChoose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            bChoose.Click += delegate { ChoosePersonalFolder(); };
            pp.Controls.Add(bChoose);

            Button bOpenP = new Button();
            bOpenP.Text = "打开个人文件夹";
            bOpenP.Font = Theme.F(8.8f);
            bOpenP.FlatStyle = FlatStyle.Flat;
            bOpenP.FlatAppearance.BorderSize = 1;
            bOpenP.FlatAppearance.BorderColor = Theme.CardBorder;
            bOpenP.BackColor = Color.White;
            bOpenP.ForeColor = Theme.TextMain;
            bOpenP.Cursor = Cursors.Hand;
            bOpenP.SetBounds(Ui.S(750), Ui.S(30), Ui.S(130), Ui.S(30));
            bOpenP.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            bOpenP.Click += delegate { OpenPersonalFolder(); };
            pp.Controls.Add(bOpenP);

            teamFlow = new FlowLayoutPanel();
            teamFlow.Dock = DockStyle.Fill;
            teamFlow.FlowDirection = FlowDirection.TopDown;
            teamFlow.WrapContents = false;
            teamFlow.AutoScroll = true;
            teamFlow.BackColor = Color.White;
            teamFlow.Padding = new Padding(0, 8, 0, 0);

            page.Controls.Add(teamFlow);
            page.Controls.Add(pp);
            page.Controls.Add(top);
            return page;
        }

        private string LoadPersonalFolder()
        {
            try
            {
                string p = Path.Combine(appDir, "personal.json");
                if (File.Exists(p))
                {
                    JavaScriptSerializer ser = new JavaScriptSerializer();
                    Dictionary<string, object> d = ser.Deserialize<Dictionary<string, object>>(File.ReadAllText(p, System.Text.Encoding.UTF8));
                    if (d != null && d.ContainsKey("folder")) return Convert.ToString(d["folder"]);
                }
            }
            catch { }
            return "";
        }

        private void SavePersonalFolder(string f)
        {
            try
            {
                JavaScriptSerializer ser = new JavaScriptSerializer();
                Dictionary<string, object> d = new Dictionary<string, object>();
                d["folder"] = f;
                File.WriteAllText(Path.Combine(appDir, "personal.json"), ser.Serialize(d), new System.Text.UTF8Encoding(false));
            }
            catch { }
        }

        private void UpdatePersonalLabels()
        {
            if (lblPersonalPath == null) return;
            if (Directory.Exists(personalFolder))
            {
                lblPersonalPath.Text = "个人文件夹：" + personalFolder;
                lblPersonalPath.ForeColor = Theme.TextMuted;
            }
            else
            {
                lblPersonalPath.Text = "未选择个人文件夹：个人研究上传将失败。";
                lblPersonalPath.ForeColor = Color.FromArgb(179, 69, 63);
            }
        }

        private void ChoosePersonalFolder()
        {
            using (FolderBrowserDialog d = new FolderBrowserDialog())
            {
                d.Description = "选择个人研究文件夹（存放你上传到 GitHub 的个人资料）";
                if (Directory.Exists(personalFolder)) d.SelectedPath = personalFolder;
                if (d.ShowDialog(this) != DialogResult.OK) return;
                personalFolder = d.SelectedPath;
                try { Directory.CreateDirectory(personalFolder); } catch { }
                SavePersonalFolder(personalFolder);
                UpdatePersonalLabels();
            }
        }

        private void OpenPersonalFolder()
        {
            if (!Directory.Exists(personalFolder)) { ChoosePersonalFolder(); if (!Directory.Exists(personalFolder)) return; }
            OpenPath(personalFolder);
        }

        private async void RefreshTeamPage()
        {
            if (teamFlow == null) return;
            if (teamLoading) return;
            SyncConfig cfg = GitHubSync.LoadConfig(appDir);
            string token = SecretStore.Load(GitHubSync.TokenTarget);
            if (string.IsNullOrEmpty(cfg.owner) || string.IsNullOrEmpty(token))
            {
                teamStatus.Text = "请先在“团队同步”页配置仓库和访问密钥。";
                return;
            }
            teamStatus.Text = "正在读取团队成员…";
            teamLoading = true;
            Cursor = Cursors.WaitCursor;
            try
            {
                string login = await Task.Run(() => GitHubSync.GetUserLogin(token));
                lblPersonal.Text = login;
                personalFolder = LoadPersonalFolder();
                UpdatePersonalLabels();
                bool created = await Task.Run(() => GitHubSync.EnsureProfile(cfg.owner, cfg.repo, cfg.branch, token, login));
                List<string> members = await Task.Run(() => GitHubSync.ListDir(cfg.owner, cfg.repo, "team", cfg.branch, token));
                teamFlow.SuspendLayout();
                teamFlow.Controls.Clear();
                AddTeamCard(login + "（我）", created ? "已创建个人档案文件夹" : "个人档案已存在", true);
                foreach (string m in members)
                {
                    if (m == login) continue;
                    AddTeamCard(m, "团队成员", false);
                }
                teamFlow.ResumeLayout();
                teamStatus.Text = "共 " + members.Count + " 位成员" + (created ? "；已为 " + login + " 创建 team/" + login + "/" : "");
            }
            catch (Exception ex)
            {
                teamStatus.Text = "读取失败：" + ex.Message;
            }
            finally { teamLoading = false; Cursor = Cursors.Default; Lang.Apply(this); }
        }

        private bool teamLoading;
        private async void ShowMemberResearch(string login)
        {
            SyncConfig cfg = GitHubSync.LoadConfig(appDir);
            string token = SecretStore.Load(GitHubSync.TokenTarget);
            try
            {
                string sha;
                string json = await Task.Run(() => GitHubSync.GetFile(cfg.owner, cfg.repo, "team/" + login + "/research.json", cfg.branch, token, out sha));
                var entries = string.IsNullOrEmpty(json) ? new List<Entry>() : SyncData.Serializer().Deserialize<List<Entry>>(json);
                var text = new System.Text.StringBuilder();
                foreach (var e in entries) if (e != null) text.AppendLine(e.Date + "  " + e.Title).AppendLine(e.Content).AppendLine();
                using (Form dialog = new Form())
                {
                    dialog.Text = login; dialog.Size = new Size(700, 560); dialog.StartPosition = FormStartPosition.CenterParent;
                    TextBox view = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, Dock = DockStyle.Fill, Text = text.Length == 0 ? "No personal research uploaded." : text.ToString() };
                    dialog.Controls.Add(view); dialog.ShowDialog(this);
                }
            }
            catch (Exception ex) { Msg.Show(ex.Message, "Team Research"); }
        }

        private void AddTeamCard(string name, string note, bool me)
        {
            Panel card = new Panel();
            card.SetBounds(0, 0, Math.Max(Ui.S(600), teamFlow.ClientSize.Width - Ui.S(30)), Ui.S(64));
            card.Margin = new Padding(0, 0, 0, Ui.S(8));
            card.BackColor = Color.White;
            card.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen p = new Pen(Theme.CardBorder)) e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
                using (SolidBrush b = new SolidBrush(me ? Theme.Accent : Color.FromArgb(200, 210, 225))) e.Graphics.FillRectangle(b, 0, 0, Ui.S(4), card.Height);
            };
            Label n = new Label();
            n.Text = name;
            n.Font = Theme.FB(10.5f);
            n.ForeColor = Theme.Navy;
            n.BackColor = Color.White;
            n.SetBounds(Ui.S(18), Ui.S(10), Ui.S(400), Ui.S(22));
            card.Controls.Add(n);
            Label d = new Label();
            d.Text = note;
            d.Font = Theme.F(8.6f);
            d.ForeColor = Theme.TextMuted;
            d.BackColor = Color.White;
            d.SetBounds(Ui.S(18), Ui.S(34), Ui.S(500), Ui.S(18));
            card.Controls.Add(d);
            Button view = new Button(); view.Text = Lang.En ? "View Research" : "查看研究";
            view.SetBounds(card.Width - 150, 16, 130, 30);
            string login = name.Replace("（我）", "");
            view.Click += delegate { ShowMemberResearch(login); };
            card.Controls.Add(view);
            teamFlow.Controls.Add(card);
        }
    }
}
