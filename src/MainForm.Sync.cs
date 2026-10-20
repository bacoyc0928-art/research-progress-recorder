using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace SRLogbook
{
    public partial class MainForm
    {
        private TextBox txtSRepo;
        private TextBox txtSPath;
        private TextBox txtSBranch;
        private TextBox txtSToken;
        private TextBox txtSLog;
        private Label lblSStatus;
        private Panel syncCard;
        private bool syncBusy;
        private CheckBox chkSyncEntries;
        private CheckBox chkSyncCategories;
        
        
        private string syncScope = "entries,categories";
        private Button btnOthers;
        private Button btnDefault;
        private Button btnUpRefs;
        private Button btnUpProgress;
        private Button btnRemoveRefs;

        private Panel BuildSyncPage()
        {
            Panel page = new Panel();
            page.BackColor = Color.White;

            Panel card = new Panel();
            card.SetBounds(0, 0, 860, 520);
            syncCard = card;
            card.BackColor = Color.White;
            card.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen p = new Pen(Theme.CardBorder)) e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
            };
            page.Controls.Add(card);

            Label h = new Label();
            h.Text = "团队同步（GitHub 仓库）";
            h.Font = Theme.FB(13f);
            h.ForeColor = Theme.Navy;
            h.BackColor = Color.White;
            h.SetBounds(24, 16, 500, 28);
            card.Controls.Add(h);

            Label hint = new Label();
            hint.Text = "每位成员使用自己的 GitHub 账号和访问密钥。可下载成员文件或默认资料库，并上传本人选择的参考文件、本地进度文件夹；每个文件最大 50 MiB。";
            hint.Font = Theme.F(8.6f);
            hint.ForeColor = Theme.TextMuted;
            hint.BackColor = Color.White;
            hint.SetBounds(24, 48, 800, 40);
            card.Controls.Add(hint);

            AddSLabel(card, "仓库（owner/repo）", 24, 96);
            txtSRepo = new TextBox();
            txtSRepo.Font = Theme.F(9.5f);
            txtSRepo.SetBounds(24, 118, 360, 30);
            card.Controls.Add(txtSRepo);

            AddSLabel(card, "文件路径", 404, 96);
            txtSPath = new TextBox();
            txtSPath.Font = Theme.F(9.5f);
            txtSPath.SetBounds(404, 118, 300, 30);
            card.Controls.Add(txtSPath);

            AddSLabel(card, "分支", 724, 96);
            txtSBranch = new TextBox();
            txtSBranch.Font = Theme.F(9.5f);
            txtSBranch.SetBounds(724, 118, 110, 30);
            card.Controls.Add(txtSBranch);

            AddSLabel(card, "访问密钥（每个成员使用自己的令牌）", 24, 160);
            txtSToken = new TextBox();
            txtSToken.Font = Theme.F(9.5f);
            txtSToken.UseSystemPasswordChar = true;
            txtSToken.SetBounds(24, 182, 620, 30);
            card.Controls.Add(txtSToken);
            Button saveConfig = SButton("验证并保存配置", 654, 182, 180);
            saveConfig.Click += delegate { ValidateAndSaveSyncConfig(); };

            Label tokenHint = new Label();
            tokenHint.Text = "外部协作者请用 classic PAT 并勾选 repo；fine-grained 令牌无法访问他人账号的私有仓库。";
            tokenHint.Font = Theme.F(8.2f);
            tokenHint.ForeColor = Theme.Warm;
            tokenHint.BackColor = Color.White;
            tokenHint.SetBounds(24, 214, 800, 18);
            card.Controls.Add(tokenHint);

            Label sc = new Label();
            sc.Text = "同步内容：";
            sc.Font = Theme.F(8.6f);
            sc.ForeColor = Theme.TextMuted;
            sc.BackColor = Color.White;
            sc.SetBounds(24, 220, 80, 24);
            sc.Visible = false;
            chkSyncEntries = Chk("记录", 100, 220);
            chkSyncCategories = Chk("类别", 170, 220);
            chkSyncEntries.Visible = chkSyncCategories.Visible = false;


            btnOthers = SButton("从 GitHub 下载他人内容", 24, 254, 220);
            btnOthers.Click += delegate { PullOthersContent(); };
            btnDefault = SButton("从 GitHub 下载默认资料库", 254, 254, 230);
            btnDefault.Click += delegate { PullDefaultLibrary(); };
            btnUpRefs = SButton("上传参考资料", 24, 294, 180);
            btnUpRefs.Click += delegate { UploadRefsAction(); };
            btnUpProgress = SButton("上传本地进度", 214, 294, 180);
            btnUpProgress.Click += delegate { UploadProgressFolder(); };
            btnRemoveRefs = SButton("撤除我上传的参考资料", 414, 294, 230);
            btnRemoveRefs.Click += delegate { RemoveMyUploadedRefs(); };

            txtSToken.TextChanged += delegate { UpdateSyncActionState(); };
            UpdateSyncActionState();

            lblSStatus = new Label();
            lblSStatus.Font = Theme.F(8.6f);
            lblSStatus.ForeColor = Theme.TextMuted;
            lblSStatus.BackColor = Color.White;
            lblSStatus.SetBounds(24, 334, 800, 40);
            card.Controls.Add(lblSStatus);

            Label logLabel = new Label();
            logLabel.Text = "同步日志";
            logLabel.Font = Theme.FB(10.5f);
            logLabel.ForeColor = Theme.Navy;
            logLabel.BackColor = Color.White;
            logLabel.SetBounds(24, 380, 200, 24);
            card.Controls.Add(logLabel);

            txtSLog = new TextBox();
            txtSLog.Multiline = true;
            txtSLog.ReadOnly = true;
            txtSLog.ScrollBars = ScrollBars.Vertical;
            txtSLog.Font = Theme.F(8.6f);
            txtSLog.BackColor = Color.White;
            txtSLog.SetBounds(24, 408, 800, 92);
            card.Controls.Add(txtSLog);

            page.Resize += delegate
            {
                card.Width = Math.Min(Ui.S(880), page.ClientSize.Width - Ui.S(2));
                txtSLog.Width = Math.Max(Ui.S(300), card.Width - Ui.S(48));
            };
            return page;
        }

        private void AddSLabel(Control parent, string text, int x, int y)
        {
            Label l = new Label();
            l.Text = text;
            l.Font = Theme.F(8.4f);
            l.ForeColor = Theme.TextMuted;
            l.BackColor = Color.White;
            l.SetBounds(x, y, 680, 18);
            parent.Controls.Add(l);
        }

        private Button SButton(string text, int x, int y, int w)
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
            b.SetBounds(Ui.S(x), Ui.S(y), Ui.S(w), Ui.S(32));
            syncCard.Controls.Add(b);
            return b;
        }

        private CheckBox Chk(string text, int x, int y)
        {
            CheckBox c = new CheckBox();
            c.Text = text;
            c.Font = Theme.F(8.6f);
            c.ForeColor = Theme.TextMain;
            c.BackColor = Color.White;
            c.AutoSize = true;
            c.SetBounds(Ui.S(x), Ui.S(y), Ui.S(70), Ui.S(24));
            c.Checked = true;
            syncCard.Controls.Add(c);
            return c;
        }

        private string ScopeString()
        {
            List<string> s = new List<string>();
            if (chkSyncEntries != null && chkSyncEntries.Checked) s.Add("entries");
            if (chkSyncCategories != null && chkSyncCategories.Checked) s.Add("categories");

            return s.Count == 0 ? "none" : string.Join(",", s.ToArray());
        }

        private void ApplyScope(string scope)
        {
            if (chkSyncEntries == null) return;
            chkSyncEntries.Checked = chkSyncCategories.Checked = false;
            if (string.IsNullOrEmpty(scope) || scope == "all") { chkSyncEntries.Checked = chkSyncCategories.Checked = true; return; }
            foreach (string p in scope.Split(','))
            {
                string q = p.Trim();
                if (q == "entries") chkSyncEntries.Checked = true;
                else if (q == "categories") chkSyncCategories.Checked = true;

            }
        }

        private bool Sc(string scope, string part)
        {
            return string.IsNullOrEmpty(scope) || scope == "all" || ("," + scope + ",").IndexOf("," + part + ",", StringComparison.Ordinal) >= 0;
        }

        private void RefreshSyncPage()
        {
            if (txtSRepo == null) return;
            SyncConfig c = GitHubSync.LoadConfig(appDir);
            txtSRepo.Text = string.IsNullOrEmpty(c.owner) ? "" : (c.owner + "/" + c.repo);
            txtSPath.Text = string.IsNullOrEmpty(c.path) ? "sync/research_log.json" : c.path;
            txtSBranch.Text = string.IsNullOrEmpty(c.branch) ? "main" : c.branch;
            txtSToken.Text = GitHubSync.HasToken() ? "********" : "";
            ApplyScope(c.scope);
            if (string.IsNullOrEmpty(txtSLog.Text))
                txtSLog.Text = Lang.En ? "Use your own token for the private data repository. Personal-repository collaborators need a classic PAT with repo scope." : "提示：数据仓库保持私有；每人使用自己的令牌。个人仓库协作者使用 classic PAT（repo 权限）。";
        }

        private bool ReadSyncForm(out string owner, out string repo, out string path, out string branch, out string token)
        {
            owner = repo = path = branch = token = "";
            string full = txtSRepo.Text.Trim();
            int slash = full.IndexOf('/');
            if (slash <= 0 || slash == full.Length - 1 || full.LastIndexOf('/') != slash)
            {
                Msg.Show("仓库请填写为 owner/repo，例如 yourname/research-data。", "团队同步");
                return false;
            }
            owner = full.Substring(0, slash).Trim();
            repo = full.Substring(slash + 1).Trim();
            path = txtSPath.Text.Trim();
            if (path.Length == 0) path = "sync/research_log.json";
            branch = txtSBranch.Text.Trim();
            if (branch.Length == 0) branch = "main";
            token = txtSToken.Text.Trim();
            if (token == "********") token = "";
            token = CleanToken(token);
            if (token.Length == 0) token = CleanToken(SecretStore.Load(GitHubSync.TokenTarget));
            if (string.IsNullOrEmpty(token))
            {
                Msg.Show("请填写你自己的 GitHub 访问密钥。", "团队同步");
                return false;
            }
            return true;
        }

        private static string CleanToken(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            StringBuilder sb = new StringBuilder(raw.Length);
            foreach (char c in raw) if (!char.IsWhiteSpace(c)) sb.Append(c);
            return sb.ToString();
        }

        private void SaveSyncConfig()
        {
            string owner, repo, path, branch, token;
            if (!ReadSyncForm(out owner, out repo, out path, out branch, out token)) return;
            SyncConfig c = new SyncConfig { owner = owner, repo = repo, path = path, branch = branch, scope = ScopeString() };
            syncScope = ScopeString();
            GitHubSync.SaveConfig(appDir, c);
            GitHubSync.SetToken(token);
            txtSToken.Text = "********";
            lblSStatus.Text = "配置已保存；密钥存放在 Windows 凭据管理器。";
        }

        private async void TestSyncConnection()
        {
            string owner, repo, path, branch, token;
            if (!ReadSyncForm(out owner, out repo, out path, out branch, out token)) return;
            lblSStatus.Text = "正在连接 GitHub…";
            try
            {
                string full = await Task.Run(() => GitHubSync.Test(owner, repo, token));
                Log("连接成功：" + full);
                lblSStatus.Text = "连接成功：" + full;
            }
            catch (Exception ex)
            {
                Log("连接失败：" + ex.Message);
                lblSStatus.Text = "连接失败，详见日志。";
            }
        }

        private void UploadSync()
        {
            // 上传前必须合并远端；直接覆盖会丢失其他成员的新记录。
            SyncAll();
        }

        private async void PullSync()
        {
            if (syncBusy || teamLoading) return;
            string owner, repo, path, branch, token;
            if (!ReadSyncForm(out owner, out repo, out path, out branch, out token)) return;
            syncBusy = true;
            syncCard.Enabled = false;
            syncScope = ScopeString();
            lblSStatus.Text = "正在从 GitHub 读取…";
            try
            {
                if (saveTimer.Enabled) { saveTimer.Stop(); SaveCurrent(); }
                string sha;
                string remote = await Task.Run(() => GitHubSync.GetFile(owner, repo, path, branch, token, out sha));
                if (string.IsNullOrEmpty(remote))
                {
                    Log("远程文件不存在：" + path + "（可先点“上传到 GitHub”创建）");
                    lblSStatus.Text = "远程文件不存在。";
                    return;
                }
                SavePending();
                int n = MergeStore(remote);
                SaveSyncConfig();
                Log("合并完成：远程记录已并入，新增/更新 " + n + " 条。");
                lblSStatus.Text = "合并完成，新增/更新 " + n + " 条。";
            }
            catch (Exception ex)
            {
                Log("拉取失败：" + ex.Message);
                lblSStatus.Text = "拉取失败，详见日志。";
            }
            finally { syncBusy = false; syncCard.Enabled = true; Lang.Apply(this); }
        }

        private async void SyncAll()
        {
            if (syncBusy || teamLoading) return;
            string owner, repo, path, branch, token;
            if (!ReadSyncForm(out owner, out repo, out path, out branch, out token)) return;
            syncBusy = true;
            syncCard.Enabled = false;
            syncScope = ScopeString();
            if (syncScope == "none") { syncBusy = false; syncCard.Enabled = true; Msg.Show("Select records or categories to sync.", "Team Sync"); return; }
            lblSStatus.Text = "正在同步…";
            try
            {
                if (saveTimer.Enabled) { saveTimer.Stop(); SaveCurrent(); }
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    string sha = null;
                    string remote = await Task.Run(() => GitHubSync.GetFile(owner, repo, path, branch, token, out sha));
                    SavePending();
                    if (!string.IsNullOrEmpty(remote)) MergeStore(remote);
                    string json = SyncData.Payload(store, string.IsNullOrEmpty(remote) ? null : SyncData.Read(remote), syncScope);
                    try
                    {
                        await Task.Run(() => GitHubSync.PutFile(owner, repo, path, branch, token, json,
                            string.IsNullOrEmpty(sha) ? null : sha));
                        SaveSyncConfig();
                        try {
                            string login = await Task.Run(() => GitHubSync.GetUserLogin(token));
                            await Task.Run(() => GitHubSync.AppendHistory(owner, repo, branch, token, login, syncScope));
                        } catch (Exception historyError) { Log("Data uploaded; history update failed: " + historyError.Message); }
                        Log("同步完成：" + DateTime.Now.ToString("HH:mm:ss"));
                        lblSStatus.Text = "同步完成。";
                        return;
                    }
                    catch (Exception ex)
                    {
                        if (attempt < 2 && (ex.Message.IndexOf("409", StringComparison.Ordinal) >= 0 ||
                            ex.Message.IndexOf("422", StringComparison.Ordinal) >= 0))
                        {
                            Log("远端已变化，重新合并后重试…");
                            continue;
                        }
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("同步失败：" + ex.Message);
                lblSStatus.Text = "同步失败，详见日志。";
            }
            finally { syncBusy = false; syncCard.Enabled = true; Lang.Apply(this); }
        }

        private void SavePending()
        {
            if (saveTimer.Enabled) saveTimer.Stop();
            SaveCurrent();
        }

        private async void ValidateAndSaveSyncConfig()
        {
            if (syncBusy || teamLoading) return;
            string owner, repo, path, branch, token;
            if (!ReadSyncForm(out owner, out repo, out path, out branch, out token)) return;
            syncBusy = true; syncCard.Enabled = false;
            lblSStatus.Text = Lang.En ? "Checking your GitHub token…" : "正在验证你的 GitHub 令牌…";
            try
            {
                // Stage 1 is identity only. A valid token is stored even when the repository is not reachable yet,
                // so a wrong repository setting no longer makes a member look like "not signed in".
                string login;
                try { login = await Task.Run(() => GitHubSync.GetUserLogin(token)); }
                catch (Exception ex) { ReportSyncFailure(ex, false, "", owner, repo); return; }

                try { SaveSyncConfig(); }
                catch (Exception ex)
                {
                    Log((Lang.En ? "Token verified but settings could not be saved: " : "令牌有效，但配置无法保存：") + ex.Message);
                    lblSStatus.Text = Lang.En ? "Token is valid, but the configuration could not be saved." : "令牌有效，但配置无法保存。";
                    MessageBox.Show((Lang.En ? "Signed in as " : "已登录账号 ") + login
                        + (Lang.En ? ", but the settings folder is not writable. Move the program to a writable folder and try again." : "，但程序目录不可写。请把软件放到可写文件夹后重试。"),
                        Lang.En ? "Team Sync" : "团队同步", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                Log((Lang.En ? "Signed in: " : "已登录：") + login);
                lblSStatus.Text = (Lang.En ? "Signed in as " : "已登录 ") + login + (Lang.En ? "; checking repository access…" : "；正在检查仓库权限…");

                // Stage 2 is repository access, which is a separate problem from the account itself.
                string full;
                try { full = await Task.Run(() => GitHubSync.Test(owner, repo, token)); }
                catch (Exception ex) { ReportSyncFailure(ex, true, login, owner, repo); return; }

                Log((Lang.En ? "Repository reachable: " : "仓库可访问：") + full);
                lblSStatus.Text = (Lang.En ? "Signed in as " : "已登录 ") + login + (Lang.En ? "; configuration saved." : "；配置已保存，密钥存放在 Windows 凭据管理器。");
                Msg.Show((Lang.En ? "Signed in to GitHub as " : "已登录 GitHub 账号 ") + login
                    + (Lang.En ? " and this token can reach " : "，该令牌可以访问 ") + full + "。", "团队同步");
            }
            finally { syncBusy = false; syncCard.Enabled = true; UpdateSyncActionState(); Lang.Apply(this); }
        }

        private void ReportSyncFailure(Exception ex, bool repoStage, string login, string owner, string repo)
        {
            GitHubApiException api = ex as GitHubApiException;
            string prefix = repoStage && !string.IsNullOrEmpty(login)
                ? (Lang.En ? "Signed in as " + login + ", but the repository is not reachable.\n\n" : "账号 " + login + " 已登录，但访问仓库失败。\n\n")
                : "";
            string msg;
            if (api == null)
            {
                msg = prefix + (Lang.En ? "Cannot reach GitHub. Check the network or proxy.\n\n" : "无法连接 GitHub，请检查网络或代理。\n\n") + ex.Message;
            }
            else if (api.StatusCode == 401)
            {
                msg = prefix + (Lang.En
                    ? "GitHub rejected the token (HTTP 401): it is invalid, expired or revoked.\n\nCreate a new token: Settings → Developer settings → Personal access tokens → Tokens (classic), tick the repo scope, then paste it here."
                    : "GitHub 拒绝了该令牌（HTTP 401）：令牌无效、已过期或被撤销。\n\n请到 GitHub → Settings → Developer settings → Personal access tokens → Tokens (classic) 重新生成，勾选 repo 权限，再粘贴到这里。");
            }
            else if (api.StatusCode == 403)
            {
                bool rate = api.Message.IndexOf("rate limit", StringComparison.OrdinalIgnoreCase) >= 0;
                msg = prefix + (rate
                    ? (Lang.En ? "GitHub API rate limit reached (HTTP 403). Wait about an hour, then try again." : "GitHub API 调用次数已达上限（HTTP 403），请约一小时后重试。")
                    : (Lang.En ? "GitHub refused access (HTTP 403): the token most likely lacks write access to this repository.\n\nA classic PAT needs the repo scope; a fine-grained PAT needs Contents: Read and write."
                               : "GitHub 拒绝访问（HTTP 403）：令牌很可能没有该仓库的写入权限。\n\nclassic PAT 请勾选 repo；fine-grained 令牌请授予 Contents 读写。"));
            }
            else if (api.StatusCode == 404)
            {
                msg = prefix + (Lang.En
                    ? "GitHub returned 404 for " + owner + "/" + repo + ".\n\nA private repository answers 404 whenever the token cannot see it. Check in order:\n1. Is the collaborator invitation accepted? Open the repository in a browser while signed in as yourself.\n2. Is the token a fine-grained token? GitHub does not support fine-grained tokens for a repository owned by another account, so external collaborators must use a classic PAT with the repo scope.\n3. Does the token still have the repo scope, and is it unexpired?\n4. Is the repository exactly " + owner + "/" + repo + "? The program repository research-progress-recorder is public and is not the data repository."
                    : "GitHub 返回 404，找不到 " + owner + "/" + repo + "。\n\n私有仓库只要令牌看不到它就一律返回 404。请依次检查：\n1. 协作者邀请是否已接受？用本人账号在浏览器里能打开该仓库才算生效。\n2. 是不是用了 fine-grained 令牌？GitHub 不支持用 fine-grained 令牌访问他人账号的私有仓库，外部协作者必须用 classic PAT 并勾选 repo。\n3. 令牌是否仍有 repo 权限、是否已过期？\n4. 仓库名是否完全等于 " + owner + "/" + repo + "？公开的程序仓库是 research-progress-recorder，不是数据仓库。");
            }
            else
            {
                msg = prefix + (Lang.En ? "GitHub error HTTP " : "GitHub 返回 HTTP ") + api.StatusCode + "。\n\n" + ex.Message;
            }
            Log((Lang.En ? "Sign-in problem: " : "登录问题：") + msg.Replace("\n", " "));
            lblSStatus.Text = repoStage
                ? (Lang.En ? "Account signed in, but the repository is not reachable." : "账号已登录，但无法访问仓库。")
                : (Lang.En ? "Sign-in failed; see the message and log below." : "登录失败，详见提示框与下方日志。");
            MessageBox.Show(msg, Lang.En ? "Team Sync" : "团队同步", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private string SerializeSyncStore() { return SyncData.Payload(store, null, ScopeString()); }
        private static int CompareUpdated(string a, string b) { return SyncData.Compare(a, b); }
        private int MergeStore(string json)
        {
            string id = current == null ? null : current.Id;
            int n = SyncData.Merge(store, SyncData.Read(json), syncScope);
            if (n > 0)
            {
                current = store.Entries.Find(e => e.Id == id);
                Persist(); RefreshCategoryCombo(); RefreshList(); RebuildCategories();
                RebuildRefs(); RefreshOverview2(); RefreshDocPage();
            }
            return n;
        }

        public async void ShowChangeHistory()
        {
            SyncConfig c = GitHubSync.LoadConfig(appDir);
            string token = SecretStore.Load(GitHubSync.TokenTarget);
            if (string.IsNullOrEmpty(c.owner) || string.IsNullOrEmpty(token))
            {
                Msg.Show("请先在“团队同步”页填写仓库并保存配置。", "改动记录");
                return;
            }
            try
            {
                string sha;
                string hist = await Task.Run(() => GitHubSync.GetHistory(c.owner, c.repo, c.branch, token, out sha));
                if (string.IsNullOrEmpty(hist)) { Msg.Show("暂无改动记录。", "改动记录"); return; }
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                JavaScriptSerializer ser = SyncData.Serializer();
                object[] arr = ser.Deserialize<object[]>(hist);
                if (arr != null)
                {
                    foreach (object o in arr)
                    {
                        Dictionary<string, object> d = o as Dictionary<string, object>;
                        if (d == null) continue;
                        sb.Append(Convert.ToString(d.ContainsKey("time") ? d["time"] : "")).Append("  ·  ")
                          .Append(Convert.ToString(d.ContainsKey("user") ? d["user"] : "")).Append("  ·  ")
                          .Append(Convert.ToString(d.ContainsKey("scope") ? d["scope"] : "")).Append(Environment.NewLine);
                    }
                }
                Msg.Show(sb.Length == 0 ? "暂无改动记录。" : sb.ToString(), "改动记录");
            }
            catch (Exception ex)
            {
                Msg.Show("读取改动记录失败：" + ex.Message, "改动记录");
            }
        }

        private void AddPersonalRefs()
        {
            using (OpenFileDialog d = new OpenFileDialog())
            {
                d.Filter = "所有文件|*.*";
                d.Multiselect = true;
                if (d.ShowDialog(this) != DialogResult.OK) return;
                int added = 0;
                foreach (string f in d.FileNames)
                {
                    if (store.Refs.Find(x => x.Path == f) == null)
                    {
                        store.Refs.Add(new RefItem { Title = Path.GetFileName(f), Path = f, RelPath = "", Kind = "file", Personal = true });
                        added++;
                    }
                }
                Persist();
                RebuildRefs();
                Msg.Show("已添加 " + added + " 条个人参考资料。", "团队同步");
            }
        }

        private void UploadRefsChannel() { UploadPersonal(false); }
        private void UploadMyResearch() { UploadPersonal(true); }
        private async void UploadPersonal(bool research)
        {
            if (syncBusy || teamLoading) return;
            string owner, repo, path, branch, token;
            if (!ReadSyncForm(out owner, out repo, out path, out branch, out token)) return;
            string folder = LoadPersonalFolder();
            if (!Directory.Exists(folder)) { Msg.Show("请先在“团队研究”页选择个人文件夹；未选择时上传会失败。", "团队研究"); return; }
            syncBusy = true; syncCard.Enabled = false;
            try
            {
                string login = await Task.Run(() => GitHubSync.GetUserLogin(token));
                SavePending();
                var files = new Dictionary<string, string>();
                var ser = SyncData.Serializer();
                if (research) { files["research.json"] = ser.Serialize(store.Entries); files["documents.json"] = ser.Serialize(store.Documents); }
                else
                {
                    List<RefItem> mine = store.Refs.FindAll(x => x.Personal);
                    if (mine.Count == 0)
                    {
                        Msg.Show("没有个人参考资料可上传。请先在“参考文献与资料”页添加个人资料。", "团队同步");
                        lblSStatus.Text = "没有个人参考资料。";
                        return;
                    }
                    files["references.json"] = ser.Serialize(mine);
                }
                // Write the local export first, so an unwritable folder cannot report a successful upload.
                foreach (var f in files) SyncData.WriteAtomic(Path.Combine(folder, f.Key), f.Value);
                await Task.Run(() => GitHubSync.EnsureProfile(owner, repo, branch, token, login));
                foreach (var f in files)
                {
                    string remotePath = "team/" + login + "/" + f.Key;
                    string sha = null;
                    await Task.Run(() => GitHubSync.GetFile(owner, repo, remotePath, branch, token, out sha));
                    await Task.Run(() => GitHubSync.PutFile(owner, repo, remotePath, branch, token, f.Value, sha));
                    Log("Uploaded " + remotePath);
                }
                SaveSyncConfig();
                lblSStatus.Text = research ? "个人研究上传完成。" : "参考资料上传完成。";
            }
            catch (Exception ex) { Log("Upload failed: " + ex.Message); lblSStatus.Text = "上传失败，详见日志。"; }
            finally { syncBusy = false; syncCard.Enabled = true; Lang.Apply(this); }
        }

        private async void MergeTeamRefs()
        {
            if (syncBusy || teamLoading) return;
            SyncConfig cfg = GitHubSync.LoadConfig(appDir);
            string token = SecretStore.Load(GitHubSync.TokenTarget);
            if (string.IsNullOrEmpty(cfg.owner) || string.IsNullOrEmpty(token)) { Msg.Show("请先在“团队同步”页配置仓库和访问密钥。", "团队同步"); return; }
            syncBusy = true; Cursor = Cursors.WaitCursor;
            try
            {
                string me = await Task.Run(() => GitHubSync.GetUserLogin(token));
                List<string> members = await Task.Run(() => GitHubSync.ListDir(cfg.owner, cfg.repo, "team", cfg.branch, token));
                int added = 0;
                foreach (string member in members)
                {
                    List<string> folders = await Task.Run(() => GitHubSync.ListDir(cfg.owner, cfg.repo, "team/" + member, cfg.branch, token));
                    if (!folders.Contains("references")) continue;
                    string dest = FileTransfers.Child(FileTransfers.Child(TeamConRefRoot(me), member), "references");
                    await Task.Run(() => FileTransfers.ReplaceFromDownload(dest, stage => GitHubSync.DownloadDir(cfg.owner, cfg.repo, "team/" + member + "/references", cfg.branch, token, stage)));
                    foreach (string file in FileTransfers.UploadFiles(dest))
                        if (!store.Refs.Exists(x => x.Path == file)) { store.Refs.Add(new RefItem { Title = Path.GetFileName(file), Path = file, Kind = "file", Personal = true }); added++; }
                }
                Persist(); RebuildRefs(); Msg.Show("团队参考资料已下载，新增 " + added + " 条。", "团队研究");
            }
            catch (Exception ex) { Msg.Show("合并失败；旧副本保留：" + ex.Message, "团队研究"); }
            finally { syncBusy = false; Cursor = Cursors.Default; }
        }

        private async void UploadProgressFolder()
        {
            if (syncBusy || teamLoading) return;
            string owner, repo, path, branch, token;
            if (!ReadSyncForm(out owner, out repo, out path, out branch, out token)) return;
            string folder = Path.GetFullPath(LoadProgressFolder()).TrimEnd(Path.DirectorySeparatorChar);
            if (!Directory.Exists(folder)) { Msg.Show("请先在“本地进度”选择文件夹。", "本地进度"); return; }
            if (SyncData.IsWithin(appDir, folder)) { Msg.Show("请选择专用进度文件夹，不能上传程序目录或其上级目录。", "本地进度"); return; }
            syncBusy = true; syncCard.Enabled = false;
            lblSStatus.Text = "正在上传本地进度…";
            try
            {
                List<string> files = FileTransfers.UploadFiles(folder);
                if (files.Count == 0) { lblSStatus.Text = "本地进度文件夹为空，未上传。"; return; }
                string login = await Task.Run(() => GitHubSync.GetUserLogin(token));
                await Task.Run(() => GitHubSync.EnsureProfile(owner, repo, branch, token, login));
                int n = 0;
                foreach (string file in files)
                {
                    string rel = file.Substring(folder.Length).TrimStart('\\', '/').Replace('\\', '/');
                    byte[] bytes = File.ReadAllBytes(file);
                    await Task.Run(() => GitHubSync.UploadFileBytes(owner, repo, "team/" + login + "/progress/" + rel, branch, token, bytes));
                    n++;
                }
                SaveSyncConfig();
                Log("已上传本地进度 " + n + " 个文件；其他远端文件保留，不自动删除。");
                lblSStatus.Text = "本地进度上传完成（" + n + " 个文件）。";
            }
            catch (Exception ex) { Log("本地进度上传失败（可能已上传部分文件）：" + ex.Message); lblSStatus.Text = "上传失败，详见日志。"; }
            finally { syncBusy = false; syncCard.Enabled = true; UpdateSyncActionState(); Lang.Apply(this); }
        }

        private void UpdateSyncActionState()
        {
            bool has = (txtSToken != null && txtSToken.Text.Trim().Length > 0) || GitHubSync.HasToken();
            if (btnOthers != null) btnOthers.Enabled = has;
            if (btnDefault != null) btnDefault.Enabled = has;
            if (btnUpRefs != null) btnUpRefs.Enabled = has;
            if (btnUpProgress != null) btnUpProgress.Enabled = has;
            if (btnRemoveRefs != null) btnRemoveRefs.Enabled = has;
        }

        private async void PullOthersContent()
        {
            if (syncBusy || teamLoading) return;
            string owner, repo, path, branch, token;
            if (!ReadSyncForm(out owner, out repo, out path, out branch, out token)) return;
            syncBusy = true; syncCard.Enabled = false;
            lblSStatus.Text = "正在下载他人内容…";
            try
            {
                string login = await Task.Run(() => GitHubSync.GetUserLogin(token));
                List<string> members = await Task.Run(() => GitHubSync.ListDir(owner, repo, "team", branch, token));
                int n = 0;
                foreach (string member in members)
                {
                    if (member.Equals(login, StringComparison.OrdinalIgnoreCase)) continue;
                    string dest = FileTransfers.Child(TeamConRefRoot(login), member);
                    bool downloaded = await Task.Run(() => GitHubSync.DownloadMemberFiles(owner, repo, member, branch, token, dest));
                    if (downloaded) n++;
                }
                SaveSyncConfig();
                lblSStatus.Text = n == 0 ? "没有其他成员内容。" : "已下载 " + n + " 位成员内容。";
                string baseDir = Path.Combine(AppPaths.MemberReferences(appDir, login), "TeamConRef");
                Log(lblSStatus.Text + " " + baseDir);
                if (n > 0) OpenPath(baseDir);
            }
            catch (Exception ex) { Log("下载他人内容失败：" + ex.Message); lblSStatus.Text = "下载失败，原有内容保留，详见日志。"; }
            finally { syncBusy = false; syncCard.Enabled = true; UpdateSyncActionState(); Lang.Apply(this); }
        }

        private async void PullDefaultLibrary()
        {
            if (syncBusy || teamLoading) return;
            string owner, repo, path, branch, token;
            if (!ReadSyncForm(out owner, out repo, out path, out branch, out token)) return;
            string local = AppPaths.DefaultReferences(appDir);
            if (Msg.Show("将先下载并检查默认资料库，成功后替换本地副本并保留旧副本备份。继续？", "默认资料库", MessageBoxButtons.YesNo, MessageBoxIcon.Information) != DialogResult.Yes) return;
            syncBusy = true; syncCard.Enabled = false;
            lblSStatus.Text = "正在下载默认资料库…";
            try
            {
                string backup = await Task.Run(() => FileTransfers.ReplaceFromDownload(local,
                    stage => GitHubSync.DownloadDir(owner, repo, "default", branch, token, stage)));
                int count = Directory.GetFiles(local, "*", SearchOption.AllDirectories).Length;
                SaveSyncConfig();
                if (backup != null) Log("旧默认资料库备份：" + backup);
                lblSStatus.Text = "默认资料库已更新：" + count + " 个文件。";
                Log(lblSStatus.Text); RebuildRefs();
            }
            catch (Exception ex) { Log("下载默认资料库失败：" + ex.Message); lblSStatus.Text = "下载失败，原有默认资料库保留。"; }
            finally { syncBusy = false; syncCard.Enabled = true; UpdateSyncActionState(); Lang.Apply(this); }
        }

        private async void UploadRefsAction()
        {
            if (syncBusy || teamLoading) return;
            string owner, repo, path, branch, token;
            if (!ReadSyncForm(out owner, out repo, out path, out branch, out token)) return;
            string folder = LoadPersonalFolder();
            if (!Directory.Exists(folder)) { Msg.Show("请先在“团队研究”页选择个人文件夹；未选择时上传会失败。", "团队研究"); return; }
            string[] files;
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "所有文件|*.*"; dialog.Multiselect = true;
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                files = dialog.FileNames;
            }
            syncBusy = true; syncCard.Enabled = false;
            lblSStatus.Text = "正在上传参考资料…";
            try
            {
                HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string file in files)
                {
                    FileTransfers.NoLinks(file);
                    if (new FileInfo(file).Length > FileTransfers.MaxUploadBytes) throw new IOException("File exceeds 50 MiB: " + file);
                    if (!names.Add(Path.GetFileName(file))) throw new IOException("Selected files have duplicate names; rename them first.");
                }
                string login = await Task.Run(() => GitHubSync.GetUserLogin(token));
                await Task.Run(() => GitHubSync.EnsureProfile(owner, repo, branch, token, login));
                string refDir = Path.Combine(folder, "references");
                FileTransfers.NoLinks(refDir); Directory.CreateDirectory(refDir);
                List<RefItem> selected = new List<RefItem>();
                foreach (string file in files)
                {
                    string name = Path.GetFileName(file), copy = FileTransfers.Child(refDir, name);
                    if (!Path.GetFullPath(file).Equals(copy, StringComparison.OrdinalIgnoreCase)) File.Copy(file, copy, true);
                    string remote = "team/" + login + "/references/" + name;
                    byte[] bytes = File.ReadAllBytes(file);
                    await Task.Run(() => GitHubSync.UploadFileBytes(owner, repo, remote, branch, token, bytes));
                    selected.Add(new RefItem { Title = name, Path = remote, RelPath = name, Kind = "file", Personal = true });
                }
                string metaPath = "team/" + login + "/references.json";
                List<RefItem> merged = null;
                for (int attempt = 0; ; attempt++)
                {
                    string sha = null;
                    string old = await Task.Run(() => GitHubSync.GetFile(owner, repo, metaPath, branch, token, out sha));
                    merged = string.IsNullOrEmpty(old) ? new List<RefItem>() : SyncData.Serializer().Deserialize<List<RefItem>>(old);
                    if (merged == null) throw new InvalidDataException("Invalid references manifest.");
                    foreach (RefItem item in selected) { merged.RemoveAll(x => x != null && x.Path == item.Path); merged.Add(item); }
                    string json = SyncData.Serializer().Serialize(merged);
                    try { await Task.Run(() => GitHubSync.PutFile(owner, repo, metaPath, branch, token, json, sha)); break; }
                    catch (GitHubApiException ex) { if (attempt >= 2 || (ex.StatusCode != 409 && ex.StatusCode != 422)) throw; }
                }
                SyncData.WriteAtomic(Path.Combine(folder, "references.json"), SyncData.Serializer().Serialize(merged));
                foreach (RefItem item in selected)
                {
                    string local = FileTransfers.Child(refDir, item.RelPath);
                    if (!store.Refs.Exists(x => x.Path == local)) store.Refs.Add(new RefItem { Title = item.Title, Path = local, Kind = "file", Personal = true });
                }
                Persist(); RebuildRefs(); SaveSyncConfig();
                Log("已上传 " + files.Length + " 个参考文件，保留此前参考资料清单。");
                lblSStatus.Text = "参考资料上传完成。";
            }
            catch (Exception ex) { Log("参考资料上传失败（可能已上传部分文件）：" + ex.Message); lblSStatus.Text = "上传失败，详见日志。"; }
            finally { syncBusy = false; syncCard.Enabled = true; UpdateSyncActionState(); Lang.Apply(this); }
        }

        private async void RemoveMyUploadedRefs()
        {
            if (syncBusy || teamLoading) return;
            string owner, repo, path, branch, token;
            if (!ReadSyncForm(out owner, out repo, out path, out branch, out token)) return;
            syncBusy = true; syncCard.Enabled = false;
            lblSStatus.Text = Lang.En ? "Reading your uploaded references…" : "正在读取你上传的参考资料…";
            try
            {
                string login = await Task.Run(() => GitHubSync.GetUserLogin(token));
                string remoteDir = "team/" + login + "/references";
                Dictionary<string, string> remote = await Task.Run(() => GitHubSync.ListFileShas(owner, repo, remoteDir, branch, token));
                if (remote.Count == 0)
                {
                    lblSStatus.Text = Lang.En ? "You have not uploaded any references." : "团队区里没有你上传的参考资料。";
                    Msg.Show(Lang.En ? "There are no uploaded references under your account." : "团队区里没有你上传的参考资料。", "撤除参考资料");
                    return;
                }
                List<string> names = new List<string>(remote.Keys);
                names.Sort(StringComparer.OrdinalIgnoreCase);
                List<string> selected = ChooseFilesToRemove(names);
                if (selected.Count == 0) { lblSStatus.Text = Lang.En ? "Cancelled." : "已取消。"; return; }
                string ask = Lang.En
                    ? "Permanently remove " + selected.Count + " uploaded reference file(s) from the team area? Other members will no longer be able to download them."
                    : "将从团队区永久删除你上传的 " + selected.Count + " 个参考资料（其他人将无法再下载）。继续？";
                if (Msg.Show(ask, "撤除参考资料", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                { lblSStatus.Text = Lang.En ? "Cancelled." : "已取消。"; return; }
                string personal = LoadPersonalFolder();
                int removed = 0;
                foreach (string name in selected)
                {
                    string remotePath = remoteDir + "/" + name;
                    string sha = remote[name];
                    await Task.Run(() => GitHubSync.DeleteFile(owner, repo, remotePath, branch, token, sha));
                    if (Directory.Exists(personal))
                    {
                        string local = FileTransfers.Child(FileTransfers.Child(personal, "references"), name);
                        if (File.Exists(local))
                        {
                            try { File.Delete(local); } catch { }
                            store.Refs.RemoveAll(x => x != null && string.Equals(x.Path, local, StringComparison.OrdinalIgnoreCase));
                        }
                    }
                    removed++;
                    Log((Lang.En ? "Removed: " : "已撤除：") + remotePath);
                }
                await UpdateReferencesManifest(owner, repo, branch, token, login, selected);
                Persist(); RebuildRefs();
                lblSStatus.Text = (Lang.En ? "Removed " : "已撤除 ") + removed + (Lang.En ? " uploaded reference(s)." : " 个参考资料。");
                Msg.Show(lblSStatus.Text, "撤除参考资料");
            }
            catch (Exception ex) { Log((Lang.En ? "Remove failed: " : "撤除参考资料失败：") + ex.Message); lblSStatus.Text = Lang.En ? "Remove failed; see the log." : "撤除失败，详见日志。"; }
            finally { syncBusy = false; syncCard.Enabled = true; UpdateSyncActionState(); Lang.Apply(this); }
        }

        private async Task UpdateReferencesManifest(string owner, string repo, string branch, string token, string login, List<string> removedNames)
        {
            HashSet<string> gone = new HashSet<string>(removedNames, StringComparer.OrdinalIgnoreCase);
            string metaPath = "team/" + login + "/references.json";
            for (int attempt = 0; ; attempt++)
            {
                string sha = null;
                string old = await Task.Run(() => GitHubSync.GetFile(owner, repo, metaPath, branch, token, out sha));
                if (string.IsNullOrEmpty(old)) return;
                List<RefItem> merged = SyncData.Serializer().Deserialize<List<RefItem>>(old);
                if (merged == null) return;
                merged.RemoveAll(x => x != null &&
                    ((!string.IsNullOrEmpty(x.RelPath) && gone.Contains(x.RelPath)) ||
                     (!string.IsNullOrEmpty(x.Path) && gone.Contains(Path.GetFileName(x.Path)))));
                string json = SyncData.Serializer().Serialize(merged);
                try { await Task.Run(() => GitHubSync.PutFile(owner, repo, metaPath, branch, token, json, sha)); return; }
                catch (GitHubApiException ex) { if (attempt >= 2 || (ex.StatusCode != 409 && ex.StatusCode != 422)) throw; }
            }
        }

        private List<string> ChooseFilesToRemove(List<string> names)
        {
            List<string> picked = new List<string>();
            using (Form dialog = new Form())
            {
                dialog.Text = "撤除我上传的参考资料";
                dialog.Size = new Size(620, 460);
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.BackColor = Color.White;
                CheckedListBox list = new CheckedListBox();
                list.Dock = DockStyle.Fill;
                list.CheckOnClick = true;
                list.IntegralHeight = false;
                list.Font = Theme.F(9f);
                foreach (string n in names) list.Items.Add(n, true);
                Panel bottom = new Panel();
                bottom.Dock = DockStyle.Bottom;
                bottom.Height = 48;
                Button ok = new Button();
                ok.Text = "撤除所选";
                ok.FlatStyle = FlatStyle.Flat;
                ok.SetBounds(330, 10, 120, 30);
                ok.Click += delegate { dialog.DialogResult = DialogResult.OK; dialog.Close(); };
                Button cancel = new Button();
                cancel.Text = "取消";
                cancel.FlatStyle = FlatStyle.Flat;
                cancel.SetBounds(460, 10, 100, 30);
                cancel.Click += delegate { dialog.Close(); };
                bottom.Controls.Add(ok);
                bottom.Controls.Add(cancel);
                dialog.Controls.Add(list);
                dialog.Controls.Add(bottom);
                dialog.AcceptButton = ok;
                dialog.CancelButton = cancel;
                Lang.Apply(dialog);
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    foreach (object o in list.CheckedItems) picked.Add(Convert.ToString(o));
            }
            return picked;
        }

        private void Log(string text)
        {
            if (txtSLog == null) return;
            txtSLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + text + Environment.NewLine);
        }
    }
}
