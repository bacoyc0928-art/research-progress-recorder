using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SRLogbook
{
    public partial class MainForm
    {
        private Panel postCard;
        private TextBox postComposer;
        private Button postPublishButton;
        private Button postHistoryButton;
        private FlowLayoutPanel postFlow;
        private Label postStatus;
        private Label postHint;

        private Panel BuildPostsPage()
        {
            Panel page = new Panel();
            page.BackColor = Color.White;

            Panel card = new Panel();
            card.SetBounds(0, 0, 900, 640);
            postCard = card;
            card.BackColor = Color.White;
            card.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen p = new Pen(Theme.CardBorder)) e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
            };
            page.Controls.Add(card);

            Label h = new Label();
            h.Text = "Quick Post";
            h.Font = Theme.FB(13f);
            h.ForeColor = Theme.Navy;
            h.BackColor = Color.White;
            h.SetBounds(Ui.S(24), Ui.S(16), Ui.S(300), Ui.S(28));
            card.Controls.Add(h);

            postHint = new Label();
            postHint.Text = "向团队发布消息；已阅内容可在历史记录查看。 / Team notes; read items move to History.";
            postHint.Font = Theme.F(8.6f);
            postHint.ForeColor = Theme.TextMuted;
            postHint.BackColor = Color.White;
            postHint.AutoEllipsis = true;
            postHint.SetBounds(Ui.S(24), Ui.S(46), Ui.S(840), Ui.S(20));
            card.Controls.Add(postHint);

            postComposer = new TextBox();
            postComposer.Multiline = true;
            postComposer.ScrollBars = ScrollBars.Vertical;
            postComposer.Font = Theme.F(9.5f);
            postComposer.SetBounds(Ui.S(24), Ui.S(74), Ui.S(620), Ui.S(84));
            card.Controls.Add(postComposer);

            postPublishButton = PostButton("发布", 660, 74, 100);
            postPublishButton.Click += delegate { PublishPost(); };
            card.Controls.Add(postPublishButton);

            postHistoryButton = PostButton("历史记录", 766, 74, 114);
            postHistoryButton.Click += delegate { ShowPostHistory(); };
            card.Controls.Add(postHistoryButton);

            postStatus = new Label();
            postStatus.Font = Theme.F(8.6f);
            postStatus.ForeColor = Theme.TextMuted;
            postStatus.BackColor = Color.White;
            postStatus.SetBounds(Ui.S(24), Ui.S(166), Ui.S(856), Ui.S(20));
            card.Controls.Add(postStatus);

            postFlow = new FlowLayoutPanel();
            postFlow.SetBounds(Ui.S(24), Ui.S(192), Ui.S(856), Ui.S(430));
            postFlow.AutoScroll = true;
            postFlow.FlowDirection = FlowDirection.TopDown;
            postFlow.WrapContents = false;
            postFlow.BackColor = Color.White;
            card.Controls.Add(postFlow);

            page.Resize += delegate { LayoutPostsPage(page); };
            return page;
        }

        private void LayoutPostsPage(Panel page)
        {
            if (postCard == null || postFlow == null) return;
            postCard.Width = Math.Min(Ui.S(920), Math.Max(Ui.S(400), page.ClientSize.Width - Ui.S(2)));
            postCard.Height = Math.Min(Ui.S(640), Math.Max(Ui.S(400), page.ClientSize.Height - Ui.S(2)));
            int margin = Ui.S(24), gap = Ui.S(8);
            int contentWidth = Math.Max(Ui.S(320), postCard.ClientSize.Width - margin * 2);
            int publishWidth = Ui.S(100), historyWidth = Ui.S(114);
            int composerWidth = Math.Max(Ui.S(120), contentWidth - publishWidth - historyWidth - gap * 2);
            postHint.Width = contentWidth;
            postComposer.SetBounds(margin, Ui.S(74), composerWidth, Ui.S(84));
            postPublishButton.SetBounds(margin + composerWidth + gap, Ui.S(74), publishWidth, Ui.S(34));
            postHistoryButton.SetBounds(margin + composerWidth + gap + publishWidth + gap, Ui.S(74), historyWidth, Ui.S(34));
            postStatus.Width = contentWidth;
            postFlow.Width = contentWidth;
            postFlow.Height = Math.Max(Ui.S(140), postCard.ClientSize.Height - postFlow.Top - Ui.S(10));
        }

        private Button PostButton(string text, int x, int y, int w)
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
            b.SetBounds(Ui.S(x), Ui.S(y), Ui.S(w), Ui.S(34));
            return b;
        }

        private static string PostPreview(string body)
        {
            if (string.IsNullOrEmpty(body)) return "(空)";
            string s = body.Replace("\r", " ").Replace("\n", " ").Trim();
            if (s.Length > 140) s = s.Substring(0, 140) + "…";
            return s;
        }

        private void AddPostCard(PostItem p)
        {
            Panel card = new Panel();
            card.SetBounds(0, 0, Math.Max(Ui.S(300), postFlow.ClientSize.Width - Ui.S(30)), Ui.S(92));
            card.Margin = new Padding(0, 0, 0, Ui.S(8));
            card.BackColor = Color.White;
            card.Cursor = Cursors.Hand;
            card.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen pen = new Pen(Theme.CardBorder)) e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };
            Label head = new Label();
            head.Text = p.Author + "  ·  " + p.Time;
            head.Font = Theme.FB(9.5f);
            head.ForeColor = Theme.Navy;
            head.BackColor = Color.White;
            head.SetBounds(Ui.S(14), Ui.S(8), Ui.S(520), Ui.S(20));
            card.Controls.Add(head);
            Label body = new Label();
            body.Text = PostPreview(p.Body);
            body.Font = Theme.F(8.8f);
            body.ForeColor = Theme.TextMuted;
            body.BackColor = Color.White;
            body.SetBounds(Ui.S(14), Ui.S(30), Math.Max(Ui.S(300), card.Width - Ui.S(200)), Ui.S(52));
            card.Controls.Add(body);
            Button read = PostButton("已阅", 0, 0, 70);
            read.SetBounds(card.Width - Ui.S(78), Ui.S(30), Ui.S(64), Ui.S(28));
            read.Click += delegate { MarkPostRead(p); };
            card.Controls.Add(read);
            if (string.Equals(p.Author, sessionLogin, StringComparison.OrdinalIgnoreCase))
            {
                Button del = PostButton("删除", 0, 0, 70);
                del.SetBounds(card.Width - Ui.S(78), Ui.S(62), Ui.S(64), Ui.S(24));
                del.ForeColor = Theme.Warm;
                del.Click += delegate { DeletePost(p); };
                card.Controls.Add(del);
            }
            EventHandler open = delegate { ShowPost(p); };
            card.Click += open; head.Click += open; body.Click += open;
            postFlow.Controls.Add(card);
        }

        private async void RefreshPostsPage()
        {
            if (postFlow == null) return;
            if (string.IsNullOrEmpty(sessionToken))
            {
                postFlow.Controls.Clear();
                postStatus.Text = "请先在左下角登录 GitHub 账号。";
                return;
            }
            postStatus.Text = "正在读取帖子…";
            try
            {
                SyncConfig c = GitHubSync.LoadConfig(appDir);
                string token = sessionToken, login = sessionLogin;
                List<PostItem> all = await Task.Run(() => Posts.List(c.owner, c.repo, c.branch, token));
                List<string> read = await Task.Run(() => Posts.ReadIds(c.owner, c.repo, c.branch, token, login));
                HashSet<string> rset = new HashSet<string>(read, StringComparer.OrdinalIgnoreCase);
                postFlow.SuspendLayout();
                postFlow.Controls.Clear();
                int shown = 0;
                foreach (PostItem p in all)
                {
                    if (rset.Contains(p.Id)) continue;
                    AddPostCard(p);
                    shown++;
                }
                postFlow.ResumeLayout();
                postStatus.Text = shown == 0
                    ? "暂无未读帖子；已阅的帖子在右上角“历史记录”里。"
                    : "共 " + shown + " 条未读帖子";
            }
            catch (Exception ex) { postStatus.Text = "读取失败：" + ex.Message; }
        }

        private async void PublishPost()
        {
            if (string.IsNullOrEmpty(sessionToken)) { Msg.Show("请先在左下角登录 GitHub 账号。", "Quick Post"); return; }
            string body = postComposer.Text.Trim();
            if (body.Length == 0) { Msg.Show("请先写点内容再发布。", "Quick Post"); return; }
            SyncConfig c = GitHubSync.LoadConfig(appDir);
            string token = sessionToken, login = sessionLogin;
            try
            {
                await Task.Run(() => Posts.Publish(c.owner, c.repo, c.branch, token, login, body));
                postComposer.Text = "";
                await LogActivityAsync("发布帖子 / Publish post", body.Length + " 字符");
                RefreshPostsPage();
                Msg.Show("已发布；其他成员登录后即可看到。", "Quick Post");
            }
            catch (Exception ex) { Msg.Show("发布失败：" + ex.Message, "Quick Post"); }
        }

        private void ShowPost(PostItem p)
        {
            using (Form dialog = new Form())
            {
                dialog.Text = p.Author + " · " + p.Time;
                dialog.Size = new Size(720, 560);
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.BackColor = Color.White;
                TextBox body = new TextBox();
                body.Dock = DockStyle.Fill;
                body.Multiline = true;
                body.ReadOnly = true;
                body.ScrollBars = ScrollBars.Vertical;
                body.WordWrap = true;
                body.BorderStyle = BorderStyle.None;
                body.Font = Theme.F(10f);
                body.Text = p.Body;
                Panel bottom = new Panel();
                bottom.Dock = DockStyle.Bottom;
                bottom.Height = 52;
                Button read = new Button();
                read.Text = "已阅";
                read.FlatStyle = FlatStyle.Flat;
                read.SetBounds(12, 10, 100, 32);
                read.Click += delegate { dialog.Close(); MarkPostRead(p); };
                bottom.Controls.Add(read);
                Button close = new Button();
                close.Text = "关闭";
                close.FlatStyle = FlatStyle.Flat;
                close.SetBounds(dialog.Width - 130, 10, 100, 32);
                close.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
                close.Click += delegate { dialog.Close(); };
                bottom.Controls.Add(close);
                dialog.Controls.Add(body);
                dialog.Controls.Add(bottom);
                Lang.Apply(dialog);
                dialog.ShowDialog(this);
            }
        }

        private async void MarkPostRead(PostItem p)
        {
            if (string.IsNullOrEmpty(sessionToken)) return;
            SyncConfig c = GitHubSync.LoadConfig(appDir);
            string token = sessionToken, login = sessionLogin;
            try
            {
                await Task.Run(() => Posts.MarkRead(c.owner, c.repo, c.branch, token, login, p.Id));
                RefreshPostsPage();
            }
            catch (Exception ex) { Msg.Show("标记已阅失败：" + ex.Message, "Quick Post"); }
        }

        private async void DeletePost(PostItem p)
        {
            if (!string.Equals(p.Author, sessionLogin, StringComparison.OrdinalIgnoreCase))
            { Msg.Show("只能删除自己发布的帖子。", "Quick Post"); return; }
            if (Msg.Show("确定删除这条帖子吗？删除后其他人也看不到了。", "Quick Post", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            SyncConfig c = GitHubSync.LoadConfig(appDir);
            string token = sessionToken;
            try
            {
                await Task.Run(() => Posts.Delete(c.owner, c.repo, c.branch, token, p.Id));
                await LogActivityAsync("删除帖子 / Delete post", p.Time);
                RefreshPostsPage();
            }
            catch (Exception ex) { Msg.Show("删除失败：" + ex.Message, "Quick Post"); }
        }

        private async void ShowPostHistory()
        {
            if (string.IsNullOrEmpty(sessionToken)) { Msg.Show("请先在左下角登录 GitHub 账号。", "历史记录"); return; }
            SyncConfig c = GitHubSync.LoadConfig(appDir);
            string token = sessionToken, login = sessionLogin;
            try
            {
                List<PostItem> all = await Task.Run(() => Posts.List(c.owner, c.repo, c.branch, token));
                List<string> read = await Task.Run(() => Posts.ReadIds(c.owner, c.repo, c.branch, token, login));
                HashSet<string> rset = new HashSet<string>(read, StringComparer.OrdinalIgnoreCase);
                List<PostItem> seen = new List<PostItem>();
                foreach (PostItem p in all) if (rset.Contains(p.Id)) seen.Add(p);
                if (seen.Count == 0) { Msg.Show("还没有已阅的帖子。", "历史记录"); return; }
                using (Form dialog = new Form())
                {
                    dialog.Text = "历史记录（已阅）";
                    dialog.Size = new Size(820, 520);
                    dialog.StartPosition = FormStartPosition.CenterParent;
                    dialog.BackColor = Color.White;
                    ListView list = new ListView();
                    list.Dock = DockStyle.Fill;
                    list.View = View.Details;
                    list.FullRowSelect = true;
                    list.Columns.Add("作者", 150);
                    list.Columns.Add("时间", 150);
                    list.Columns.Add("内容", 460);
                    foreach (PostItem p in seen)
                    {
                        ListViewItem it = new ListViewItem(p.Author);
                        it.SubItems.Add(p.Time);
                        it.SubItems.Add(PostPreview(p.Body));
                        it.Tag = p;
                        list.Items.Add(it);
                    }
                    list.DoubleClick += delegate
                    {
                        if (list.SelectedItems.Count > 0) ShowPost((PostItem)list.SelectedItems[0].Tag);
                    };
                    Button close = new Button();
                    close.Text = "关闭";
                    close.FlatStyle = FlatStyle.Flat;
                    Panel bottom = new Panel();
                    bottom.Dock = DockStyle.Bottom;
                    bottom.Height = 50;
                    close.SetBounds(12, 9, 100, 32);
                    close.Click += delegate { dialog.Close(); };
                    bottom.Controls.Add(close);
                    dialog.Controls.Add(list);
                    dialog.Controls.Add(bottom);
                    Lang.Apply(dialog);
                    dialog.ShowDialog(this);
                }
            }
            catch (Exception ex) { Msg.Show("读取历史记录失败：" + ex.Message, "历史记录"); }
        }
    }
}
