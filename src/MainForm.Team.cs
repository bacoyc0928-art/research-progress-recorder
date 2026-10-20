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
            sub.Text = "下载成员内容到 References/<用户名>；上传时会建立本人的 team/<用户名>/ 档案。";
            sub.Font = Theme.F(8.8f);
            sub.ForeColor = Theme.TextMuted;
            sub.BackColor = Color.White;
            sub.SetBounds(Ui.S(2), Ui.S(44), Ui.S(820), Ui.S(20));
            top.Controls.Add(sub);

            FlowLayoutPanel topRight = new FlowLayoutPanel();
            topRight.Dock = DockStyle.Right;
            topRight.Width = Ui.S(470);
            topRight.FlowDirection = FlowDirection.RightToLeft;
            topRight.WrapContents = false;
            topRight.BackColor = Color.White;
            topRight.Padding = new Padding(0, Ui.S(16), Ui.S(10), 0);
            Button b = TeamBtn("刷新");
            b.Click += delegate { RefreshTeamPage(); };
            Button bMerge = TeamBtn("下载并合并团队参考资料");
            bMerge.Click += delegate { MergeTeamRefs(); };
            topRight.Controls.Add(b);
            topRight.Controls.Add(bMerge);
            top.Controls.Add(topRight);

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
            lblPersonal.SetBounds(Ui.S(132), Ui.S(8), Ui.S(600), Ui.S(22));
            lblPersonal.AutoEllipsis = false;
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

            FlowLayoutPanel ppRight = new FlowLayoutPanel();
            ppRight.Dock = DockStyle.Right;
            ppRight.Width = Ui.S(330);
            ppRight.FlowDirection = FlowDirection.RightToLeft;
            ppRight.WrapContents = false;
            ppRight.BackColor = Color.White;
            ppRight.Padding = new Padding(0, Ui.S(28), Ui.S(10), 0);
            Button bOpenP = TeamBtn("打开个人文件夹");
            bOpenP.Click += delegate { OpenPersonalFolder(); };
            Button bChoose = TeamBtn("选择个人文件夹");
            bChoose.Click += delegate { ChoosePersonalFolder(); };
            ppRight.Controls.Add(bOpenP);
            ppRight.Controls.Add(bChoose);
            pp.Controls.Add(ppRight);

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
            Action layoutTeam = delegate
            {
                b.Anchor = bMerge.Anchor = bChoose.Anchor = bOpenP.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                b.SetBounds(Math.Max(Ui.S(310), top.ClientSize.Width - Ui.S(94)), Ui.S(12), Ui.S(84), Ui.S(30));
                bMerge.SetBounds(Math.Max(Ui.S(110), top.ClientSize.Width - Ui.S(304)), Ui.S(12), Ui.S(200), Ui.S(30));
                sub.Width = teamStatus.Width = Math.Max(Ui.S(200), top.ClientSize.Width - Ui.S(12));
                lblPersonal.Width = Math.Max(Ui.S(200), pp.ClientSize.Width - Ui.S(150));
                lblPersonalPath.Width = Math.Max(Ui.S(150), pp.ClientSize.Width - Ui.S(330));
                bChoose.SetBounds(Math.Max(Ui.S(170), pp.ClientSize.Width - Ui.S(318)), Ui.S(30), Ui.S(150), Ui.S(30));
                bOpenP.SetBounds(Math.Max(Ui.S(330), pp.ClientSize.Width - Ui.S(158)), Ui.S(30), Ui.S(150), Ui.S(30));
            };
            top.Resize += delegate { layoutTeam(); };
            pp.Resize += delegate { layoutTeam(); };
            layoutTeam();
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
                // Opening or refreshing this page only reads. Upload actions create the profile when needed.
                bool created = false;
                List<string> members = await Task.Run(() => GitHubSync.ListDir(cfg.owner, cfg.repo, "team", cfg.branch, token));
                // Roster shows invited collaborators even before their first upload; failures are ignored on purpose.
                List<string> roster = new List<string>();
                try { roster = await Task.Run(() => GitHubSync.ListCollaborators(cfg.owner, cfg.repo, cfg.branch, token)); }
                catch { roster = new List<string>(); }
                teamFlow.SuspendLayout();
                teamFlow.Controls.Clear();
                AddTeamCard(login + "（我）", "已连接 GitHub 账号", true);
                foreach (string m in members)
                {
                    if (m == login) continue;
                    AddTeamCard(m, "项目成员", false);
                    roster.RemoveAll(x => string.Equals(x, m, StringComparison.OrdinalIgnoreCase));
                }
                int pending = 0;
                foreach (string m in roster)
                {
                    if (string.Equals(m, login, StringComparison.OrdinalIgnoreCase)) continue;
                    AddTeamCard(m, "项目成员", false);
                    pending++;
                }
                teamFlow.ResumeLayout();
                teamStatus.Text = "共 " + (members.Count + pending) + " 位项目成员" + (created ? "；已为 " + login + " 创建 team/" + login + "/" : "");
            }
            catch (Exception ex)
            {
                teamStatus.Text = "读取失败：" + ex.Message;
            }
            finally { teamLoading = false; Cursor = Cursors.Default; Lang.Apply(this); }
        }

        private bool teamLoading;

        private string MemberFolder(string login)
        {
            return AppPaths.MemberReferences(appDir, login);
        }

        private string TeamConRefRoot(string myLogin)
        {
            return Path.Combine(AppPaths.MemberReferences(appDir, myLogin), "TeamConRef");
        }

        private void ShowMemberFiles(string login, string dir)
        {
            using (Form dialog = new Form())
            {
                dialog.Text = login + " - 团队内容";
                dialog.Size = new Size(760, 520);
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.BackColor = Color.White;
                ListView list = new ListView();
                list.Dock = DockStyle.Fill;
                list.View = View.Details;
                list.FullRowSelect = true;
                list.Columns.Add("文件", 320);
                list.Columns.Add("目录", 300);
                string[] files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
                foreach (string f in files)
                {
                    string rel = f.Substring(dir.Length).TrimStart('\\');
                    ListViewItem it = new ListViewItem(Path.GetFileName(f));
                    it.SubItems.Add(Path.GetDirectoryName(rel));
                    it.Tag = f;
                    list.Items.Add(it);
                }
                list.DoubleClick += delegate
                {
                    if (list.SelectedItems.Count > 0) OpenPath(Convert.ToString(list.SelectedItems[0].Tag));
                };
                Panel bottom = new Panel();
                bottom.Dock = DockStyle.Bottom;
                bottom.Height = 48;
                Button open = new Button();
                open.Text = "打开文件";
                open.FlatStyle = FlatStyle.Flat;
                open.SetBounds(12, 10, 100, 30);
                open.Click += delegate
                {
                    if (list.SelectedItems.Count > 0) OpenPath(Convert.ToString(list.SelectedItems[0].Tag));
                };
                Button openDir = new Button();
                openDir.Text = "打开文件夹";
                openDir.FlatStyle = FlatStyle.Flat;
                openDir.SetBounds(122, 10, 110, 30);
                openDir.Click += delegate { OpenPath(dir); };
                Button close = new Button();
                close.Text = "关闭";
                close.FlatStyle = FlatStyle.Flat;
                close.SetBounds(660, 10, 80, 30);
                close.Click += delegate { dialog.Close(); };
                bottom.Controls.Add(open);
                bottom.Controls.Add(openDir);
                bottom.Controls.Add(close);
                dialog.Controls.Add(list);
                dialog.Controls.Add(bottom);
                Lang.Apply(dialog); dialog.ShowDialog(this);
            }
        }

        private async void ShowMemberResearch(string login)
        {
            if (syncBusy || teamLoading) return;
            SyncConfig cfg = GitHubSync.LoadConfig(appDir);
            string token = SecretStore.Load(GitHubSync.TokenTarget);
            if (string.IsNullOrEmpty(cfg.owner) || string.IsNullOrEmpty(token)) { Msg.Show("请先在“团队同步”页配置仓库和访问密钥。", "团队研究"); return; }
            syncBusy = true; Cursor = Cursors.WaitCursor;
            try
            {
                string me = await Task.Run(() => GitHubSync.GetUserLogin(token));
                if (string.Equals(login, me, StringComparison.OrdinalIgnoreCase))
                {
                    // Your own uploads are already on this machine; re-downloading them only adds a long staging path.
                    string personal = LoadPersonalFolder();
                    string refs = string.IsNullOrEmpty(personal) ? null : Path.Combine(personal, "references");
                    if (refs != null && Directory.Exists(refs) && Directory.GetFiles(refs, "*", SearchOption.AllDirectories).Length > 0)
                    { ShowMemberFiles(login + "（我）", refs); return; }
                    Msg.Show(Lang.En
                        ? "These are your own files. Uploaded copies live in the personal folder's references subfolder; use Remove My Uploaded References to manage the team area."
                        : "这是你自己的账号。你上传的副本在个人文件夹的 references 子目录；团队区的文件请用“撤除我上传的参考资料”管理。", "团队研究");
                    if (!string.IsNullOrEmpty(personal) && Directory.Exists(personal)) OpenPath(personal);
                    return;
                }
                string dir = FileTransfers.Child(TeamConRefRoot(me), login);
                bool downloaded = await Task.Run(() => GitHubSync.DownloadMemberFiles(cfg.owner, cfg.repo, login, cfg.branch, token, dir));
                if (!downloaded) { Msg.Show("该成员尚未上传文件；已有本地副本保留。", "团队研究"); return; }
                ShowMemberFiles(login, dir);
            }
            catch (Exception ex) { Msg.Show("打开成员内容失败：" + ex.Message, "团队研究"); }
            finally { syncBusy = false; Cursor = Cursors.Default; }
        }

        private Button TeamBtn(string text)
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
            b.AutoSize = true;
            b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            b.Padding = new Padding(Ui.S(10), Ui.S(3), Ui.S(10), Ui.S(3));
            b.MinimumSize = new Size(Ui.S(70), Ui.S(30));
            b.Margin = new Padding(Ui.S(4), 0, 0, 0);
            return b;
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
            Button view = new Button(); view.Text = Lang.En ? "Open Folder" : "打开文件夹";
            view.SetBounds(card.Width - Ui.S(150), Ui.S(16), Ui.S(130), Ui.S(32));
            string login = name.Replace("（我）", "");
            view.Click += delegate { ShowMemberResearch(login); };
            card.Controls.Add(view);
            if (me)
            {
                int labelWidth = Math.Max(Ui.S(160), card.Width - Ui.S(150) - Ui.S(160) - Ui.S(24));
                n.Width = labelWidth; d.Width = labelWidth;
                Button remove = TeamBtn(Lang.En ? "Remove My References" : "撤除参考资料");
                remove.AutoSize = false;
                remove.SetBounds(card.Width - Ui.S(150) - Ui.S(160), Ui.S(16), Ui.S(150), Ui.S(32));
                remove.Click += delegate { RemoveMyUploadedRefs(); };
                card.Controls.Add(remove);
            }
            teamFlow.Controls.Add(card);
        }
    }
}
