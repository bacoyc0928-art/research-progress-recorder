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

        private Panel BuildSyncPage()
        {
            Panel page = new Panel();
            page.BackColor = Color.White;

            Panel card = new Panel();
            card.SetBounds(0, 0, 860, 470);
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

            Button bSave = SButton("保存配置", 24, 232, 110);
            bSave.Click += delegate { SaveSyncConfig(); };
            Button bTest = SButton("测试连接", 144, 232, 110);
            bTest.Click += delegate { TestSyncConnection(); };
            Button bPull = SButton("从 GitHub 合并", 264, 232, 130);
            bPull.Click += delegate { PullSync(); };
            Button bPush = SButton("合并后上传", 404, 232, 130);
            bPush.Click += delegate { UploadSync(); };
            Button bAll = SButton("一键同步", 544, 232, 110);
            bAll.Click += delegate { SyncAll(); };
            bAll.BackColor = Theme.Accent;
            bAll.ForeColor = Color.White;

            lblSStatus = new Label();
            lblSStatus.Font = Theme.F(8.6f);
            lblSStatus.ForeColor = Theme.TextMuted;
            lblSStatus.BackColor = Color.White;
            lblSStatus.SetBounds(24, 284, 800, 40);
            card.Controls.Add(lblSStatus);

            Label logLabel = new Label();
            logLabel.Text = "同步日志";
            logLabel.Font = Theme.FB(10.5f);
            logLabel.ForeColor = Theme.Navy;
            logLabel.BackColor = Color.White;
            logLabel.SetBounds(24, 330, 200, 24);
            card.Controls.Add(logLabel);

            txtSLog = new TextBox();
            txtSLog.Multiline = true;
            txtSLog.ReadOnly = true;
            txtSLog.ScrollBars = ScrollBars.Vertical;
            txtSLog.Font = Theme.F(8.6f);
            txtSLog.BackColor = Color.White;
            txtSLog.SetBounds(24, 358, 800, 92);
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

        private void RefreshSyncPage()
        {
            if (txtSRepo == null) return;
            SyncConfig c = GitHubSync.LoadConfig(appDir);
            txtSRepo.Text = string.IsNullOrEmpty(c.owner) ? "" : (c.owner + "/" + c.repo);
            txtSPath.Text = string.IsNullOrEmpty(c.path) ? "sync/research_log.json" : c.path;
            txtSBranch.Text = string.IsNullOrEmpty(c.branch) ? "main" : c.branch;
            txtSToken.Text = GitHubSync.HasToken() ? "********" : "";
            if (string.IsNullOrEmpty(txtSLog.Text))
                txtSLog.Text = "提示：仓库需为私有仓库；每人使用自己的令牌。个人仓库的协作者目前需使用 classic PAT（repo 权限）。";
        }

        private bool ReadSyncForm(out string owner, out string repo, out string path, out string branch, out string token)
        {
            owner = repo = path = branch = token = "";
            string full = txtSRepo.Text.Trim();
            int slash = full.IndexOf('/');
            if (slash <= 0 || slash == full.Length - 1)
            {
                MessageBox.Show("仓库请填写为 owner/repo，例如 yourname/research-data。", "团队同步");
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
                MessageBox.Show("请填写 GitHub 访问密钥（fine-grained PAT）。", "团队同步");
                return false;
            }
            return true;
        }

        private void SaveSyncConfig()
        {
            string owner, repo, path, branch, token;
            if (!ReadSyncForm(out owner, out repo, out path, out branch, out token)) return;
            SyncConfig c = new SyncConfig { owner = owner, repo = repo, path = path, branch = branch };
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
            SaveSyncConfig();
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
                int n = MergeStore(remote);
                Log("合并完成：远程记录已并入，新增/更新 " + n + " 条。");
                lblSStatus.Text = "合并完成，新增/更新 " + n + " 条。";
            }
            catch (Exception ex)
            {
                Log("拉取失败：" + ex.Message);
                lblSStatus.Text = "拉取失败，详见日志。";
            }
            finally { syncBusy = false; }
        }

        private async void SyncAll()
        {
            if (syncBusy) return;
            string owner, repo, path, branch, token;
            if (!ReadSyncForm(out owner, out repo, out path, out branch, out token)) return;
            syncBusy = true;
            SaveSyncConfig();
            lblSStatus.Text = "正在同步…";
            try
            {
                if (saveTimer.Enabled) { saveTimer.Stop(); SaveCurrent(); }
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    string sha = null;
                    string remote = await Task.Run(() => GitHubSync.GetFile(owner, repo, path, branch, token, out sha));
                    if (!string.IsNullOrEmpty(remote)) MergeStore(remote);
                    string json = SerializeSyncStore();
                    try
                    {
                        await Task.Run(() => GitHubSync.PutFile(owner, repo, path, branch, token, json,
                            string.IsNullOrEmpty(sha) ? null : sha));
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
            finally { syncBusy = false; }
        }

        private string SerializeSyncStore()
        {
            // 不上传个人资料库和文档文件；类别里的相关材料链接是团队数据的一部分。
            Store shared = new Store();
            shared.Entries = store.Entries;
            shared.CategoriesInitialized = true;
            shared.DeletedCategories = store.DeletedCategories;
            foreach (Category c in store.Categories)
            {
                if (c == null) continue;
                shared.Categories.Add(new Category
                {
                    Id = c.Id, Name = c.Name, Updated = c.Updated,
                    TaskTotal = c.TaskTotal, TaskDone = c.TaskDone, Hours = c.Hours,
                    Links = c.Links == null ? new List<CatLink>() : c.Links
                });
            }
            JavaScriptSerializer ser = new JavaScriptSerializer();
            ser.MaxJsonLength = 20000000;
            return ser.Serialize(shared);
        }

        private static int CompareUpdated(string a, string b)
        {
            DateTimeOffset da, db;
            bool aOk = DateTimeOffset.TryParse(a, out da);
            bool bOk = DateTimeOffset.TryParse(b, out db);
            if (aOk && bOk) return da.CompareTo(db);
            if (aOk) return 1;
            if (bOk) return -1;
            return string.CompareOrdinal(a ?? "", b ?? "");
        }

        private int MergeStore(string remoteJson)
        {
            Store remote;
            try { remote = new JavaScriptSerializer().Deserialize<Store>(remoteJson); }
            catch (Exception ex) { throw new InvalidDataException("仓库中的进度 JSON 无法读取，已停止上传以保护远端数据。", ex); }
            if (remote == null) throw new InvalidDataException("仓库中的进度 JSON 为空，已停止上传以保护远端数据。");
            int changed = 0;
            bool currentChanged = false;

            if (remote.Entries != null)
            {
                foreach (Entry r in remote.Entries)
                {
                    if (r == null || string.IsNullOrEmpty(r.Id)) continue;
                    Entry l = store.Entries.Find(x => x.Id == r.Id);
                    if (l == null)
                    {
                        store.Entries.Add(r);
                        changed++;
                    }
                    else if (CompareUpdated(r.Updated, l.Updated) > 0)
                    {
                        l.Title = r.Title; l.Date = r.Date; l.Category = r.Category;
                        l.Status = r.Status; l.Progress = r.Progress; l.Content = r.Content; l.Updated = r.Updated;
                        if (current != null && current.Id == l.Id) currentChanged = true;
                        changed++;
                    }
                }
            }
            if (remote.DeletedCategories != null)
            {
                foreach (DeletedCategory d in remote.DeletedCategories)
                {
                    if (d == null || string.IsNullOrEmpty(d.Id)) continue;
                    DeletedCategory localDel = store.DeletedCategories.Find(x => x.Id == d.Id);
                    if (localDel == null)
                    {
                        store.DeletedCategories.Add(d);
                        changed++;
                    }
                    else if (CompareUpdated(d.At, localDel.At) > 0)
                    {
                        localDel.At = d.At;
                        changed++;
                    }
                }
            }
            for (int i = store.Categories.Count - 1; i >= 0; i--)
            {
                Category c = store.Categories[i];
                DeletedCategory d = store.DeletedCategories.Find(x => x.Id == c.Id);
                if (d != null && (string.IsNullOrEmpty(c.Updated) || CompareUpdated(d.At, c.Updated) > 0))
                {
                    foreach (Entry e in store.Entries) if (e.Category == c.Name) e.Category = "";
                    foreach (LocalDoc doc in store.Documents) if (doc.Category == c.Name) doc.Category = "";
                    store.Categories.RemoveAt(i);
                    changed++;
                }
            }
            if (remote.Categories != null)
            {
                foreach (Category r in remote.Categories)
                {
                    if (r == null || string.IsNullOrEmpty(r.Id)) continue;
                    DeletedCategory d = store.DeletedCategories.Find(x => x.Id == r.Id);
                    if (d != null && (string.IsNullOrEmpty(r.Updated) || CompareUpdated(d.At, r.Updated) > 0)) continue;
                    Category l = store.Categories.Find(x => x.Id == r.Id);
                    if (l == null) l = store.Categories.Find(x => x.Name == r.Name);
                    if (l == null)
                    {
                        Category add = new Category
                        {
                            Id = r.Id, Name = r.Name, Updated = r.Updated,
                            TaskTotal = r.TaskTotal, TaskDone = r.TaskDone, Hours = r.Hours
                        };
                        if (r.Links != null) add.Links = r.Links;
                        store.Categories.Add(add);
                        changed++;
                    }
                    else
                    {
                        if (l.Id != r.Id) { l.Id = r.Id; changed++; }
                        if (CompareUpdated(r.Updated, l.Updated) > 0 ||
                            (string.IsNullOrEmpty(r.Updated) && string.IsNullOrEmpty(l.Updated)))
                        {
                            l.Name = r.Name; l.TaskTotal = r.TaskTotal; l.TaskDone = r.TaskDone;
                            l.Hours = r.Hours; l.Updated = r.Updated;
                            if (r.Links != null) l.Links = r.Links;
                            changed++;
                        }
                    }
                }
            }
            if (changed > 0)
            {
                Persist();
                RefreshCategoryCombo();
                if (currentChanged) LoadEntry(current);
                RefreshList();
                RebuildCategories();
                RefreshOverview2();
                RefreshDocPage();
            }
            return changed;
        }

        private void Log(string text)
        {
            if (txtSLog == null) return;
            txtSLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + text + Environment.NewLine);
        }
    }
}
