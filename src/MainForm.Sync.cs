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
        private Label boundLabel;
        private Button btnActivity;

        private Panel BuildSyncPage()
        {
            Panel page = new Panel();
            page.BackColor = Color.White;

            Panel card = new Panel();
            card.SetBounds(0, 0, 900, 590);
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

            boundLabel = new Label();
            boundLabel.Font = Theme.F(8.6f);
            boundLabel.ForeColor = Theme.TextMuted;
            boundLabel.BackColor = Color.White;
            boundLabel.SetBounds(24, 48, 850, 40);
            card.Controls.Add(boundLabel);

            Label tokenHint = new Label();
            tokenHint.Text = "登录后在团队同步页操作即可；仓库已绑定，无需填写任何配置。";
            tokenHint.Font = Theme.F(8.2f);
            tokenHint.ForeColor = Theme.Warm;
            tokenHint.BackColor = Color.White;
            tokenHint.SetBounds(24, 88, 860, 18);
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


            btnOthers = SButton("从 GitHub 下载他人内容", 24, 120, 300);
            btnOthers.Click += delegate { PullOthersContent(); };
            btnDefault = SButton("从 GitHub 下载默认资料库", 334, 120, 330);
            btnDefault.Click += delegate { PullDefaultLibrary(); };
            btnUpRefs = SButton("上传参考资料", 24, 160, 240);
            btnUpRefs.Click += delegate { UploadRefsAction(); };
            btnUpProgress = SButton("上传本地进度", 274, 160, 240);
            btnUpProgress.Click += delegate { UploadProgressFolder(); };
            btnRemoveRefs = SButton("撤除我上传的参考资料", 24, 200, 330);
            btnRemoveRefs.Click += delegate { RemoveMyUploadedRefs(); };
            btnActivity = SButton("查看活动日志", 364, 200, 220);
            btnActivity.Click += delegate { ShowActivityLogs(); };

            UpdateSyncActionState();

            lblSStatus = new Label();
            lblSStatus.Font = Theme.F(8.6f);
            lblSStatus.ForeColor = Theme.TextMuted;
            lblSStatus.BackColor = Color.White;
            lblSStatus.SetBounds(24, 244, 850, 40);
            card.Controls.Add(lblSStatus);

            Label logLabel = new Label();
            logLabel.Text = "同步日志";
            logLabel.Font = Theme.FB(10.5f);
            logLabel.ForeColor = Theme.Navy;
            logLabel.BackColor = Color.White;
            logLabel.SetBounds(24, 292, 260, 24);
            card.Controls.Add(logLabel);

            txtSLog = new TextBox();
            txtSLog.Multiline = true;
            txtSLog.ReadOnly = true;
            txtSLog.ScrollBars = ScrollBars.Vertical;
            txtSLog.Font = Theme.F(8.6f);
            txtSLog.BackColor = Color.White;
            txtSLog.SetBounds(24, 320, 850, 220);
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
            if (txtSLog == null) return;
            SyncConfig c = GitHubSync.LoadConfig(appDir);
            if (boundLabel != null)
                boundLabel.Text = "已绑定仓库 / Bound repository: "
                    + (string.IsNullOrEmpty(c.owner) ? "bacoyc0928-art" : c.owner) + "/"
                    + (string.IsNullOrEmpty(c.repo) ? "research-data" : c.repo) + " · "
                    + (string.IsNullOrEmpty(c.branch) ? "main" : c.branch)
                    + "。请在左下角登录后使用下列操作 / Sign in at the bottom-left to use these actions.";
            if (string.IsNullOrEmpty(txtSLog.Text))
                txtSLog.Text = "提示：请先在左下角用本人的 GitHub 密钥登录 / Sign in with your own token first.";
        }

        private bool ReadSyncForm(out string owner, out string repo, out string path, out string branch, out string token)
        {
            owner = repo = path = branch = token = "";
            if (string.IsNullOrEmpty(sessionToken))
            {
                Msg.Show("请先在左下角登录 GitHub 账号。", "团队同步");
                return false;
            }
            SyncConfig c = GitHubSync.LoadConfig(appDir);
            owner = string.IsNullOrEmpty(c.owner) ? "bacoyc0928-art" : c.owner;
            repo = string.IsNullOrEmpty(c.repo) ? "research-data" : c.repo;
            path = string.IsNullOrEmpty(c.path) ? "sync/research_log.json" : c.path;
            branch = string.IsNullOrEmpty(c.branch) ? "main" : c.branch;
            token = sessionToken;
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

        // Sign-in moved to the sidebar login panel; per-operation repository configuration is no longer required.
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
            string token = sessionToken;
            if (string.IsNullOrEmpty(token))
            {
                Msg.Show("请先在左下角登录 GitHub 账号。", "改动记录");
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
            string token = sessionToken;
            if (string.IsNullOrEmpty(token)) { Msg.Show("请先在左下角登录 GitHub 账号。", "团队同步"); return; }
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
            if (!Directory.Exists(folder)) { Msg.Show("请先在“本地进度”选择文件夹 / Pick a progress folder first.", "本地进度"); return; }
            if (SyncData.IsWithin(appDir, folder)) { Msg.Show("请选择专用进度文件夹，不能上传程序目录或其上级目录 / Choose a dedicated folder, not the program folder.", "本地进度"); return; }
            syncBusy = true; syncCard.Enabled = false;
            lblSStatus.Text = "正在列出可上传文件… / Listing files…";
            try
            {
                List<string> files = FileTransfers.UploadFiles(folder);
                if (files.Count == 0) { lblSStatus.Text = "本地进度文件夹为空，未上传 / Folder is empty."; return; }
                List<string> picked = PickItems("上传本地进度 / Upload local progress",
                    "勾选要上传的文件；未勾选的文件不会上传。 / Check the files to upload; unchecked files are not sent.",
                    Rows(files, f => f.Substring(folder.Length).TrimStart('\\', '/') + "  ·  " + Math.Max(1, new FileInfo(f).Length / 1024) + " KB"), false);
                if (picked.Count == 0) { lblSStatus.Text = "已取消 / Cancelled."; return; }
                string login = await Task.Run(() => GitHubSync.GetUserLogin(token));
                await Task.Run(() => GitHubSync.EnsureProfile(owner, repo, branch, token, login));
                int n = 0;
                foreach (string file in picked)
                {
                    string rel = file.Substring(folder.Length).TrimStart('\\', '/').Replace('\\', '/');
                    byte[] bytes = File.ReadAllBytes(file);
                    await Task.Run(() => GitHubSync.UploadFileBytes(owner, repo, "team/" + login + "/progress/" + rel, branch, token, bytes));
                    n++;
                }
                SaveSyncConfig();
                Log("已上传本地进度 " + n + " 个文件；其他远端文件保留，不自动删除 / Uploaded; remote older files kept.");
                lblSStatus.Text = "本地进度上传完成 / Progress uploaded: " + n + " 个文件 / files";
                await LogActivityAsync("上传本地进度 / Upload local progress", n + " 个文件 / files");
            }
            catch (Exception ex) { Log("本地进度上传失败（可能已上传部分文件）/ Upload failed: " + ex.Message); lblSStatus.Text = "上传失败，详见日志 / Upload failed; see log."; }
            finally { syncBusy = false; syncCard.Enabled = true; UpdateSyncActionState(); Lang.Apply(this); }
        }

        private void UpdateSyncActionState()
        {
            bool has = !string.IsNullOrEmpty(sessionToken);
            if (btnOthers != null) btnOthers.Enabled = has;
            if (btnDefault != null) btnDefault.Enabled = has;
            if (btnUpRefs != null) btnUpRefs.Enabled = has;
            if (btnUpProgress != null) btnUpProgress.Enabled = has;
            if (btnRemoveRefs != null) btnRemoveRefs.Enabled = has;
            if (btnActivity != null) btnActivity.Enabled = has;
        }

        /// <summary>通用“先预览、再勾选”对话框：列出可下载或可上传的条目，只处理勾选项。</summary>
        private List<string> PickItems(string title, string hint, IList<KeyValuePair<string, string>> items, bool allowBrowse)
        {
            List<string> picked = new List<string>();
            using (Form dialog = new Form())
            {
                dialog.Text = Lang.T(title);
                dialog.Size = new Size(780, 580);
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.BackColor = Color.White;
                Label tip = new Label();
                tip.Text = Lang.T(hint);
                tip.Dock = DockStyle.Top; tip.Height = 56; tip.Padding = new Padding(14, 10, 14, 0);
                tip.Font = Theme.F(8.8f); tip.ForeColor = Theme.TextMuted; tip.BackColor = Color.White;
                CheckedListBox list = new CheckedListBox();
                list.Dock = DockStyle.Fill; list.CheckOnClick = true; list.IntegralHeight = false;
                list.Font = Theme.F(9f);
                foreach (KeyValuePair<string, string> it in items) list.Items.Add(it.Value, true);
                Panel bottom = new Panel(); bottom.Dock = DockStyle.Bottom; bottom.Height = 56; bottom.BackColor = Color.White;
                int x = 14;
                Button all = new Button(); all.Text = "全选 / Select All"; all.FlatStyle = FlatStyle.Flat; all.SetBounds(x, 12, 128, 30); x += 136;
                Button none = new Button(); none.Text = "全不选 / Select None"; none.FlatStyle = FlatStyle.Flat; none.SetBounds(x, 12, 148, 30); x += 156;
                all.Click += delegate { for (int i = 0; i < list.Items.Count; i++) list.SetItemChecked(i, true); };
                none.Click += delegate { for (int i = 0; i < list.Items.Count; i++) list.SetItemChecked(i, false); };
                bottom.Controls.Add(all); bottom.Controls.Add(none);
                if (allowBrowse)
                {
                    Button browse = new Button(); browse.Text = "添加文件 / Add files"; browse.FlatStyle = FlatStyle.Flat; browse.SetBounds(x, 12, 150, 30);
                    browse.Click += delegate
                    {
                        using (OpenFileDialog od = new OpenFileDialog())
                        {
                            od.Multiselect = true; od.Filter = "所有文件 / All files|*.*";
                            if (od.ShowDialog(dialog) != DialogResult.OK) return;
                            foreach (string f in od.FileNames)
                            {
                                bool exists = false;
                                foreach (KeyValuePair<string, string> it in items) if (string.Equals(it.Key, f, StringComparison.OrdinalIgnoreCase)) exists = true;
                                if (exists) continue;
                                FileInfo info = new FileInfo(f);
                                items.Add(new KeyValuePair<string, string>(f, info.Name + "  ·  " + Math.Max(1, info.Length / 1024) + " KB"));
                                list.Items.Add(items[items.Count - 1].Value, true);
                            }
                        }
                    };
                    bottom.Controls.Add(browse);
                }
                Button ok = new Button(); ok.Text = "确定 / OK"; ok.FlatStyle = FlatStyle.Flat; ok.SetBounds(bottom.Width - 236, 12, 104, 30);
                Button cancel = new Button(); cancel.Text = "取消 / Cancel"; cancel.FlatStyle = FlatStyle.Flat; cancel.SetBounds(bottom.Width - 124, 12, 110, 30);
                ok.Anchor = AnchorStyles.Top | AnchorStyles.Right; cancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                ok.Click += delegate { dialog.DialogResult = DialogResult.OK; dialog.Close(); };
                cancel.Click += delegate { dialog.Close(); };
                bottom.Controls.Add(ok); bottom.Controls.Add(cancel);
                dialog.Controls.Add(tip);
                dialog.Controls.Add(bottom);
                dialog.Controls.Add(list);
                dialog.AcceptButton = ok; dialog.CancelButton = cancel;
                if (dialog.ShowDialog(this) != DialogResult.OK) return picked;
                for (int i = 0; i < list.Items.Count && i < items.Count; i++)
                    if (list.GetItemChecked(i)) picked.Add(items[i].Key);
            }
            return picked;
        }

        private static List<KeyValuePair<string, string>> Rows(IEnumerable<string> values, Func<string, string> format)
        {
            List<KeyValuePair<string, string>> rows = new List<KeyValuePair<string, string>>();
            foreach (string v in values) rows.Add(new KeyValuePair<string, string>(v, format(v)));
            return rows;
        }

        private async void PullOthersContent()
        {
            if (syncBusy || teamLoading) return;
            string owner, repo, path, branch, token;
            if (!ReadSyncForm(out owner, out repo, out path, out branch, out token)) return;
            syncBusy = true; syncCard.Enabled = false;
            lblSStatus.Text = "正在读取团队成员… / Reading team members…";
            try
            {
                string login = await Task.Run(() => GitHubSync.GetUserLogin(token));
                List<string> members = await Task.Run(() => GitHubSync.ListDir(owner, repo, "team", branch, token));
                members.RemoveAll(m => m.Equals(login, StringComparison.OrdinalIgnoreCase));
                members.Sort(StringComparer.OrdinalIgnoreCase);
                if (members.Count == 0) { lblSStatus.Text = "没有其他成员内容 / No other member content."; return; }
                Dictionary<string, string> display = new Dictionary<string, string>();
                foreach (string member in members)
                {
                    int refCount = 0; bool hasProgress = false;
                    try
                    {
                        refCount = (await Task.Run(() => GitHubSync.ListFileShas(owner, repo, "team/" + member + "/references", branch, token))).Count;
                        hasProgress = (await Task.Run(() => GitHubSync.ListDir(owner, repo, "team/" + member, branch, token))).Contains("progress");
                    }
                    catch { }
                    display[member] = member + "  ·  references: " + refCount + (hasProgress ? "  ·  progress ✓" : "  ·  progress —");
                }
                List<string> picked = PickItems("下载他人内容 / Download team files",
                    "勾选要下载的成员；只下载勾选的成员，未勾选的不动。 / Check the members to download; everything else stays untouched.",
                    Rows(members, m => display[m]), false);
                if (picked.Count == 0) { lblSStatus.Text = "已取消 / Cancelled."; return; }
                int n = 0;
                foreach (string member in picked)
                {
                    string dest = FileTransfers.Child(TeamConRefRoot(login), member);
                    if (await Task.Run(() => GitHubSync.DownloadMemberFiles(owner, repo, member, branch, token, dest))) n++;
                }
                SaveSyncConfig();
                lblSStatus.Text = n == 0 ? "没有其他成员内容 / No other member content." : "已下载 / Downloaded: " + n;
                if (n > 0) await LogActivityAsync("下载他人内容 / Download team files", n + " 位成员 / members");
                string baseDir = Path.Combine(AppPaths.MemberReferences(appDir, login), "TeamConRef");
                Log(lblSStatus.Text + " " + baseDir);
                if (n > 0) OpenPath(baseDir);
            }
            catch (Exception ex) { Log("下载他人内容失败 / Download failed: " + ex.Message); lblSStatus.Text = "下载失败，原有内容保留，详见日志 / Download failed; existing files kept."; }
            finally { syncBusy = false; syncCard.Enabled = true; UpdateSyncActionState(); Lang.Apply(this); }
        }

        private async void PullDefaultLibrary()
        {
            if (syncBusy || teamLoading) return;
            string owner, repo, path, branch, token;
            if (!ReadSyncForm(out owner, out repo, out path, out branch, out token)) return;
            string local = AppPaths.DefaultReferences(appDir);
            syncBusy = true; syncCard.Enabled = false;
            lblSStatus.Text = "正在读取默认资料库… / Reading default library…";
            try
            {
                Dictionary<string, string> remote = await Task.Run(() => GitHubSync.ListFileShas(owner, repo, "default", branch, token));
                if (remote.Count == 0) { lblSStatus.Text = "远端默认资料库为空 / The remote default library is empty."; return; }
                List<string> names = new List<string>(remote.Keys);
                names.Sort(StringComparer.OrdinalIgnoreCase);
                List<string> picked = PickItems("下载默认资料库 / Download default library",
                    "勾选要下载的文件；未勾选的文件不会被改动，被覆盖的旧文件会保留 .bak 备份。 / Check files to download; unchecked files stay untouched, overwritten ones keep a .bak copy.",
                    Rows(names, n => n + (File.Exists(FileTransfers.Child(local, n)) ? "  ·  已存在 / exists" : "  ·  新增 / new")), false);
                if (picked.Count == 0) { lblSStatus.Text = "已取消 / Cancelled."; return; }
                Directory.CreateDirectory(local);
                int n2 = 0, updated = 0;
                foreach (string name in picked)
                {
                    string sha = null;
                    byte[] bytes = await Task.Run(() => GitHubSync.GetFileBytes(owner, repo, "default/" + name, branch, token, out sha));
                    if (bytes == null) continue;
                    string dest = FileTransfers.Child(local, name);
                    if (File.Exists(dest)) { File.Copy(dest, dest + ".bak-" + DateTime.Now.ToString("yyyyMMddHHmmss"), true); updated++; }
                    string temp = dest + ".tmp-" + Guid.NewGuid().ToString("N").Substring(0, 8);
                    File.WriteAllBytes(temp, bytes);
                    if (File.Exists(dest)) File.Delete(dest);
                    File.Move(temp, dest);
                    n2++;
                }
                SaveSyncConfig();
                lblSStatus.Text = "默认资料库已更新 / Default library updated: " + n2 + (updated > 0 ? "（备份 / backups: " + updated + "）" : "");
                await LogActivityAsync("下载默认资料库 / Download default library", n2 + " 个文件 / files");
                Log(lblSStatus.Text); RebuildRefs();
            }
            catch (Exception ex) { Log("下载默认资料库失败 / Default library failed: " + ex.Message); lblSStatus.Text = "下载失败，原有默认资料库保留 / Failed; existing library kept."; }
            finally { syncBusy = false; syncCard.Enabled = true; UpdateSyncActionState(); Lang.Apply(this); }
        }

        private async void UploadRefsAction()
        {
            if (syncBusy || teamLoading) return;
            string owner, repo, path, branch, token;
            if (!ReadSyncForm(out owner, out repo, out path, out branch, out token)) return;
            string folder = LoadPersonalFolder();
            if (!Directory.Exists(folder)) { Msg.Show("请先在“团队研究”页选择个人文件夹；未选择时上传会失败 / Pick your personal folder on Team Research first.", "团队研究"); return; }
            List<string> candidates = new List<string>();
            string previewDir = Path.Combine(folder, "references");
            if (Directory.Exists(previewDir))
                foreach (string f in Directory.GetFiles(previewDir)) candidates.Add(f);
            candidates.Sort(StringComparer.OrdinalIgnoreCase);
            List<string> picked = PickItems("上传参考资料 / Upload references",
                "勾选要上传的文件；也可以用“添加文件”从其他位置挑选。未勾选的不会上传。 / Check the files to upload, or add more from elsewhere; unchecked files are not sent.",
                Rows(candidates, f => Path.GetFileName(f) + "  ·  " + Math.Max(1, new FileInfo(f).Length / 1024) + " KB"), true);
            if (picked.Count == 0) { lblSStatus.Text = "已取消 / Cancelled."; return; }
            string[] files = picked.ToArray();
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
                Log("已上传 / Uploaded " + files.Length + " 个参考文件 / reference file(s).");
                lblSStatus.Text = "参考资料上传完成 / References uploaded.";
                await LogActivityAsync("上传参考资料 / Upload references", files.Length + " 个文件 / files");
            }
            catch (Exception ex) { Log("参考资料上传失败（可能已上传部分文件）/ Upload failed: " + ex.Message); lblSStatus.Text = "上传失败，详见日志 / Upload failed; see log."; }
            finally { syncBusy = false; syncCard.Enabled = true; UpdateSyncActionState(); Lang.Apply(this); }
        }

        private async void RemoveMyUploadedRefs()
        {
            if (syncBusy || teamLoading) return;
            string owner, repo, path, branch, token;
            if (!ReadSyncForm(out owner, out repo, out path, out branch, out token)) return;
            syncBusy = true; syncCard.Enabled = false;
            lblSStatus.Text = "正在读取你上传的参考资料… / Reading your uploaded references…";
            try
            {
                string login = await Task.Run(() => GitHubSync.GetUserLogin(token));
                string remoteDir = "team/" + login + "/references";
                Dictionary<string, string> remote = await Task.Run(() => GitHubSync.ListFileShas(owner, repo, remoteDir, branch, token));
                if (remote.Count == 0)
                {
                    lblSStatus.Text = "团队区里没有你上传的参考资料 / You have not uploaded any references.";
                    Msg.Show("团队区里没有你上传的参考资料 / There are no uploaded references under your account.", "撤除参考资料");
                    return;
                }
                List<string> names = new List<string>(remote.Keys);
                names.Sort(StringComparer.OrdinalIgnoreCase);
                List<string> selected = PickItems("撤除我上传的参考资料 / Remove my uploaded references",
                    "勾选要从团队区删除的文件；只删除勾选项，其他人将无法再下载它们。 / Check the files to delete; only checked ones are removed.",
                    Rows(names, n => n), false);
                if (selected.Count == 0) { lblSStatus.Text = "已取消 / Cancelled."; return; }
                string ask = "将从团队区永久删除你上传的 " + selected.Count + " 个参考资料（其他人将无法再下载）。继续？ / Permanently remove " + selected.Count + " uploaded file(s)?";
                if (Msg.Show(ask, "撤除参考资料", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                { lblSStatus.Text = "已取消 / Cancelled."; return; }
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
                    Log("已撤除 / Removed: " + remotePath);
                }
                await UpdateReferencesManifest(owner, repo, branch, token, login, selected);
                Persist(); RebuildRefs();
                lblSStatus.Text = "已撤除 / Removed: " + removed;
                await LogActivityAsync("撤除参考资料 / Remove references", removed + " 个文件 / files: " + string.Join("、", selected.ToArray()));
                Msg.Show(lblSStatus.Text, "撤除参考资料");
            }
            catch (Exception ex) { Log("撤除参考资料失败 / Remove failed: " + ex.Message); lblSStatus.Text = "撤除失败，详见日志 / Remove failed; see log."; }
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

        private async void ShowActivityLogs()
        {
            if (string.IsNullOrEmpty(sessionToken)) { Msg.Show("请先在左下角登录 GitHub 账号。", "活动日志"); return; }
            SyncConfig c = GitHubSync.LoadConfig(appDir);
            string login = sessionLogin, token = sessionToken;
            Cursor = Cursors.WaitCursor;
            try
            {
                List<ActivityEntry> pub = await Task.Run(() => ActivityLog.Read(c.owner, c.repo, c.branch, token, ActivityLog.PublicPath));
                List<ActivityEntry> mine = await Task.Run(() => ActivityLog.Read(c.owner, c.repo, c.branch, token, ActivityLog.PrivatePath(login)));
                ShowActivityDialog(pub, mine);
            }
            catch (Exception ex) { Msg.Show("读取活动日志失败：" + ex.Message, "活动日志"); }
            finally { Cursor = Cursors.Default; }
        }

        private void ShowActivityDialog(List<ActivityEntry> pub, List<ActivityEntry> mine)
        {
            using (Form dialog = new Form())
            {
                dialog.Text = "活动日志";
                dialog.Size = new Size(900, 560);
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.BackColor = Color.White;
                ListView list = new ListView();
                list.Dock = DockStyle.Fill;
                list.View = View.Details;
                list.FullRowSelect = true;
                list.Columns.Add("时间", 190);
                list.Columns.Add("用户", 150);
                list.Columns.Add("动作", 130);
                list.Columns.Add("详情", 380);
                List<ActivityEntry> all = new List<ActivityEntry>();
                all.AddRange(pub);
                all.AddRange(mine);
                all.Reverse();
                foreach (ActivityEntry e in all)
                {
                    ListViewItem it = new ListViewItem(e.Time ?? "");
                    it.SubItems.Add(e.User ?? "");
                    it.SubItems.Add(e.Action ?? "");
                    it.SubItems.Add(e.Detail ?? "");
                    list.Items.Add(it);
                }
                Panel bottom = new Panel();
                bottom.Dock = DockStyle.Bottom;
                bottom.Height = 48;
                Button close = new Button();
                close.Text = "关闭";
                close.FlatStyle = FlatStyle.Flat;
                close.SetBounds(12, 10, 100, 30);
                close.Click += delegate { dialog.Close(); };
                bottom.Controls.Add(close);
                Label note = new Label();
                note.Text = "公开日志 " + pub.Count + " 条 · 我的日志 " + mine.Count + " 条";
                note.SetBounds(130, 14, 600, 22);
                note.ForeColor = Theme.TextMuted;
                bottom.Controls.Add(note);
                dialog.Controls.Add(list);
                dialog.Controls.Add(bottom);
                Lang.Apply(dialog);
                dialog.ShowDialog(this);
            }
        }

        private async Task LogActivityAsync(string action, string detail, bool toPublic = true)
        {
            if (string.IsNullOrEmpty(sessionToken) || string.IsNullOrEmpty(sessionLogin)) return;
            SyncConfig c = GitHubSync.LoadConfig(appDir);
            string login = sessionLogin, token = sessionToken;
            try { await Task.Run(() => ActivityLog.Append(c.owner, c.repo, c.branch, token, login, action, detail, toPublic)); }
            catch (Exception ex) { Log("活动日志写入失败 / Activity log failed: " + ex.Message); }
            RefreshNews();
        }

        private void Log(string text)
        {
            if (txtSLog == null) return;
            txtSLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + text + Environment.NewLine);
        }
    }
}
