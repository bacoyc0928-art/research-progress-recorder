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
            hint.Text = "每位成员使用自己的 GitHub 账号和访问密钥。进度记录、类别及相关材料链接会同步；本地文件和文档本身不会上传。";
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

            AddSLabel(card, "访问密钥（仓库所有者可用 fine-grained PAT；个人仓库协作者需 classic PAT）", 24, 160);
            txtSToken = new TextBox();
            txtSToken.Font = Theme.F(9.5f);
            txtSToken.UseSystemPasswordChar = true;
            txtSToken.SetBounds(24, 182, 620, 30);
            card.Controls.Add(txtSToken);

            Label sc = new Label();
            sc.Text = "同步内容：";
            sc.Font = Theme.F(8.6f);
            sc.ForeColor = Theme.TextMuted;
            sc.BackColor = Color.White;
            sc.SetBounds(24, 220, 80, 24);
            card.Controls.Add(sc);
            chkSyncEntries = Chk("记录", 100, 220);
            chkSyncCategories = Chk("类别", 170, 220);


            Button bSave = SButton("保存配置", 24, 254, 110);
            bSave.Click += delegate { ValidateAndSaveSyncConfig(); };
            Button bTest = SButton("测试连接", 144, 254, 110);
            bTest.Click += delegate { TestSyncConnection(); };
            Button bPull = SButton("从 GitHub 合并", 264, 254, 130);
            bPull.Click += delegate { PullSync(); };
            Button bPush = SButton("合并后上传", 404, 254, 130);
            bPush.Click += delegate { UploadSync(); };
            Button bAll = SButton("一键同步", 544, 254, 110);
            bAll.Click += delegate { SyncAll(); };
            bAll.BackColor = Theme.Accent;
            bAll.ForeColor = Color.White;

            Button bRefs = SButton("上传参考资料", 24, 294, 130);
            bRefs.Click += delegate { UploadRefsChannel(); };
            Button bMine = SButton("上传个人研究", 164, 294, 130);
            bMine.Click += delegate { UploadMyResearch(); };

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
                txtSLog.Text = "提示：仓库需为私有仓库；每人使用自己的令牌。个人仓库的协作者目前需使用 classic PAT（repo 权限）。";
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
            if (token == "********") token = SecretStore.Load(GitHubSync.TokenTarget);
            if (string.IsNullOrEmpty(token))
            {
                Msg.Show("请填写你自己的 GitHub 访问密钥。", "团队同步");
                return false;
            }
            return true;
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
            if (syncBusy) return;
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
            if (syncBusy) return;
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
            if (syncBusy) return;
            string owner, repo, path, branch, token;
            if (!ReadSyncForm(out owner, out repo, out path, out branch, out token)) return;
            syncBusy = true; syncCard.Enabled = false;
            try { await Task.Run(() => GitHubSync.Test(owner, repo, token)); SaveSyncConfig(); }
            catch (Exception ex) { Log(ex.Message); lblSStatus.Text = "Validation failed; previous saved token was preserved."; }
            finally { syncBusy = false; syncCard.Enabled = true; Lang.Apply(this); }
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

        private void UploadRefsChannel() { UploadPersonal(false); }
        private void UploadMyResearch() { UploadPersonal(true); }
        private async void UploadPersonal(bool research)
        {
            if (syncBusy) return;
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
                else files["references.json"] = ser.Serialize(store.Refs);
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
            SyncConfig cfg = GitHubSync.LoadConfig(appDir);
            string token = SecretStore.Load(GitHubSync.TokenTarget);
            if (string.IsNullOrEmpty(cfg.owner) || string.IsNullOrEmpty(token))
            {
                Msg.Show("请先在“团队同步”页配置仓库和访问密钥。", "团队同步");
                return;
            }
            try
            {
                List<string> members = await Task.Run(() => GitHubSync.ListDir(cfg.owner, cfg.repo, "team", cfg.branch, token));
                int added = 0;
                JavaScriptSerializer ser = SyncData.Serializer();
                foreach (string m in members)
                {
                    string sha;
                    string json = null;
                    try { json = await Task.Run(() => GitHubSync.GetFile(cfg.owner, cfg.repo, "team/" + m + "/references.json", cfg.branch, token, out sha)); }
                    catch { continue; }
                    if (string.IsNullOrEmpty(json)) continue;
                    List<RefItem> list = null;
                    try { list = ser.Deserialize<List<RefItem>>(json); } catch { continue; }
                    if (list == null) continue;
                    foreach (RefItem r in list)
                    {
                        if (r == null || string.IsNullOrEmpty(r.Path)) continue;
                        if (store.Refs.Find(x => x.Path == r.Path) == null) { store.Refs.Add(r); added++; }
                    }
                }
                Persist();
                RebuildRefs();
                Msg.Show("已合并团队成员参考资料，新增 " + added + " 条。", "团队信息");
            }
            catch (Exception ex)
            {
                Msg.Show("合并失败：" + ex.Message, "团队信息");
            }
        }

        private void Log(string text)
        {
            if (txtSLog == null) return;
            txtSLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + text + Environment.NewLine);
        }
    }
}
