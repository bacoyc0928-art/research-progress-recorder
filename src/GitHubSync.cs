using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace SRLogbook
{
    public class GitHubApiException : Exception
    {
        public int StatusCode { get; private set; }
        public GitHubApiException(int code, string message) : base("GitHub HTTP " + code + ": " + message) { StatusCode = code; }
    }
    public class SyncConfig
    {
        public string owner { get; set; }
        public string repo { get; set; }
        public string path { get; set; }
        public string branch { get; set; }
        public string scope { get; set; }
    }

    /// <summary>
    /// 用 GitHub 仓库中的一个 JSON 文件作为团队共享数据。
    /// 访问凭据是 GitHub fine-grained PAT（只授予该仓库 Contents 读写），
    /// 保存在 Windows 凭据管理器，不写入项目文件。
    /// </summary>
    public static class GitHubSync
    {
        private const string API = "https://api.github.com";
        internal static Func<string, string, string, string, string, string> RequestForTests;
        internal static Func<string, string, byte[]> BytesForTests;
        public const string TokenTarget = "SRLogbook/github_token";

        static GitHubSync()
        {
            try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; } catch { }
        }

        public static string ConfigPath(string appDir) { return Path.Combine(appDir, "sync_config.json"); }

        public static SyncConfig LoadConfig(string appDir)
        {
            try
            {
                if (File.Exists(ConfigPath(appDir)))
                {
                    JavaScriptSerializer ser = SyncData.Serializer();
                    SyncConfig c = ser.Deserialize<SyncConfig>(File.ReadAllText(ConfigPath(appDir), Encoding.UTF8));
                    if (c != null) return c;
                }
            }
            catch { }
            return new SyncConfig { owner = "bacoyc0928-art", repo = "research-data", path = "sync/research_log.json", branch = "main", scope = "entries,categories" };
        }

        public static void SaveConfig(string appDir, SyncConfig c)
        {
            JavaScriptSerializer ser = SyncData.Serializer();
            SyncData.WriteAtomic(ConfigPath(appDir), ser.Serialize(c));
        }

        public static bool HasToken() { return !string.IsNullOrEmpty(SecretStore.Load(TokenTarget)); }

        private static void CheckBranchAccess(string owner, string repo, string branch, string token)
        {
            Request("GET", API + "/repos/" + Uri.EscapeDataString(owner) + "/" + Uri.EscapeDataString(repo)
                + "/branches/" + Uri.EscapeDataString(branch), token, null);
        }

        public static void SetToken(string token) { if (!SecretStore.Save(TokenTarget, token)) throw new IOException("Cannot save token to Windows Credential Manager."); }

        public static void ClearToken() { SecretStore.Delete(TokenTarget); }

        public static string Test(string owner, string repo, string token)
        {
            string json = Request("GET", API + "/repos/" + Uri.EscapeDataString(owner) + "/" + Uri.EscapeDataString(repo), token, null);
            Dictionary<string, object> d = Parse(json);
            return d.ContainsKey("full_name") ? Convert.ToString(d["full_name"]) : (owner + "/" + repo);
        }

        public static string GetFile(string owner, string repo, string path, string branch, string token, out string sha)
        {
            sha = null;
            string url = API + "/repos/" + Uri.EscapeDataString(owner) + "/" + Uri.EscapeDataString(repo)
                + "/contents/" + EncodePath(path) + "?ref=" + Uri.EscapeDataString(branch);
            string json;
            try { json = Request("GET", url, token, null); }
            catch (Exception ex)
            {
                if (ex is GitHubApiException && ((GitHubApiException)ex).StatusCode == 404) return null;
                throw;
            }
            Dictionary<string, object> d = Parse(json);
            sha = d.ContainsKey("sha") ? Convert.ToString(d["sha"]) : null;
            if (d.ContainsKey("encoding") && Convert.ToString(d["encoding"]) == "none")
                return Request("GET", url, token, null, "application/vnd.github.raw+json");
            if (!d.ContainsKey("content") || string.IsNullOrEmpty(sha)) throw new InvalidDataException("GitHub did not return a readable file.");
            string b64 = Convert.ToString(d["content"]).Replace("\n", "").Replace("\r", "");
            return Encoding.UTF8.GetString(Convert.FromBase64String(b64));
        }

        public static string GetUserLogin(string token)
        {
            string json = Request("GET", API + "/user", token, null);
            Dictionary<string, object> d = Parse(json);
            return d.ContainsKey("login") ? Convert.ToString(d["login"]) : "";
        }

        public static List<string> ListDir(string owner, string repo, string path, string branch, string token)
        {
            List<string> names = new List<string>();
            string url = API + "/repos/" + Uri.EscapeDataString(owner) + "/" + Uri.EscapeDataString(repo)
                + "/contents/" + EncodePath(path) + "?ref=" + Uri.EscapeDataString(branch);
            string json;
            try { json = Request("GET", url, token, null); }
            catch (Exception ex)
            {
                if (ex is GitHubApiException && ((GitHubApiException)ex).StatusCode == 404) { CheckBranchAccess(owner, repo, branch, token); return names; }
                throw;
            }
            JavaScriptSerializer ser = SyncData.Serializer();
            object[] arr = ser.Deserialize<object[]>(json);
            if (arr != null)
            {
                foreach (object o in arr)
                {
                    Dictionary<string, object> d = o as Dictionary<string, object>;
                    if (d == null) continue;
                    if (d.ContainsKey("type") && Convert.ToString(d["type"]) == "dir")
                        names.Add(Convert.ToString(d["name"]));
                }
            }
            return names;
        }

        public static List<string> ListCollaborators(string owner, string repo, string branch, string token)
        {
            List<string> logins = new List<string>();
            string url = API + "/repos/" + Uri.EscapeDataString(owner) + "/" + Uri.EscapeDataString(repo)
                + "/collaborators?per_page=100";
            JavaScriptSerializer ser = SyncData.Serializer();
            object[] arr = ser.Deserialize<object[]>(Request("GET", url, token, null));
            if (arr != null)
            {
                foreach (object o in arr)
                {
                    Dictionary<string, object> d = o as Dictionary<string, object>;
                    if (d != null && d.ContainsKey("login")) logins.Add(Convert.ToString(d["login"]));
                }
            }
            return logins;
        }

        public static bool EnsureProfile(string owner, string repo, string branch, string token, string login)
        {
            string sha = null;
            string path = "team/" + login + "/profile.json";
            GetFile(owner, repo, path, branch, token, out sha);
            if (!string.IsNullOrEmpty(sha)) return false;
            string json = "{\"login\":\"" + login + "\",\"joined\":\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "\"}";
            try { PutFile(owner, repo, path, branch, token, json, null); }
            catch (GitHubApiException ex) { if (ex.StatusCode != 409 && ex.StatusCode != 422) throw; GetFile(owner, repo, path, branch, token, out sha); if (string.IsNullOrEmpty(sha)) throw; return false; }
            return true;
        }

        public static string GetHistory(string owner, string repo, string branch, string token, out string sha)
        {
            sha = null;
            try
            {
                return GetFile(owner, repo, "sync/history.json", branch, token, out sha);
            }
            catch (Exception ex)
            {
                if (ex is GitHubApiException && ((GitHubApiException)ex).StatusCode == 404) return null;
                throw;
            }
        }

        public static void AppendHistory(string owner, string repo, string branch, string token, string user, string scope)
        {
            for (int i = 0; ; i++)
            {
                try { AppendHistoryOnce(owner, repo, branch, token, user, scope); return; }
                catch (GitHubApiException ex) { if (i >= 2 || (ex.StatusCode != 409 && ex.StatusCode != 422)) throw; }
            }
        }

        private static void AppendHistoryOnce(string owner, string repo, string branch, string token, string user, string scope)
        {
            string sha;
            string existing = GetHistory(owner, repo, branch, token, out sha);
            List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
            if (!string.IsNullOrEmpty(existing))
            {
                JavaScriptSerializer ser = SyncData.Serializer();
                object[] arr = ser.Deserialize<object[]>(existing);
                if (arr != null)
                {
                    foreach (object o in arr)
                    {
                        Dictionary<string, object> d = o as Dictionary<string, object>;
                        if (d != null) list.Add(d);
                    }
                }
            }
            Dictionary<string, object> e = new Dictionary<string, object>();
            e["time"] = SyncData.Now();
            e["user"] = user;
            e["scope"] = scope;
            list.Add(e);
            if (list.Count > 500) list.RemoveRange(0, list.Count - 500);
            JavaScriptSerializer s2 = SyncData.Serializer();
            PutFile(owner, repo, "sync/history.json", branch, token, s2.Serialize(list), sha);
        }

        public static void PutFile(string owner, string repo, string path, string branch, string token, string content, string sha)
        {
            Dictionary<string, object> body = new Dictionary<string, object>();
            body["message"] = "sync: research logbook " + DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            body["content"] = Convert.ToBase64String(new UTF8Encoding(false).GetBytes(content));
            body["branch"] = branch;
            if (!string.IsNullOrEmpty(sha)) body["sha"] = sha;
            JavaScriptSerializer ser = SyncData.Serializer();
            Request("PUT", API + "/repos/" + Uri.EscapeDataString(owner) + "/" + Uri.EscapeDataString(repo)
                + "/contents/" + EncodePath(path), token, ser.Serialize(body));
        }

        public static byte[] GetFileBytes(string owner, string repo, string path, string branch, string token, out string sha)
        {
            sha = null;
            string url = API + "/repos/" + Uri.EscapeDataString(owner) + "/" + Uri.EscapeDataString(repo)
                + "/contents/" + EncodePath(path) + "?ref=" + Uri.EscapeDataString(branch);
            string json;
            try { json = Request("GET", url, token, null); }
            catch (GitHubApiException ex) { if (ex.StatusCode == 404) return null; throw; }
            Dictionary<string, object> d = Parse(json);
            sha = d.ContainsKey("sha") ? Convert.ToString(d["sha"]) : null;
            if (string.IsNullOrEmpty(sha)) throw new InvalidDataException("GitHub did not return a file SHA.");
            if (d.ContainsKey("encoding") && Convert.ToString(d["encoding"]) == "none")
                return RequestBytes(url, token);
            if (!d.ContainsKey("content")) throw new InvalidDataException("GitHub did not return file contents.");
            string b64 = Convert.ToString(d["content"]).Replace("\n", "").Replace("\r", "");
            return Convert.FromBase64String(b64);
        }

        public static void PutFileBytes(string owner, string repo, string path, string branch, string token, byte[] content, string sha)
        {
            if (content == null || content.LongLength > FileTransfers.MaxUploadBytes) throw new IOException("File exceeds 50 MiB.");
            Dictionary<string, object> body = new Dictionary<string, object>();
            body["message"] = "upload: " + Path.GetFileName(path) + " " + DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            body["content"] = Convert.ToBase64String(content);
            body["branch"] = branch;
            if (!string.IsNullOrEmpty(sha)) body["sha"] = sha;
            JavaScriptSerializer ser = SyncData.Serializer();
            ser.MaxJsonLength = Math.Max(ser.MaxJsonLength, content.Length * 2 + 4096);
            Request("PUT", API + "/repos/" + Uri.EscapeDataString(owner) + "/" + Uri.EscapeDataString(repo)
                + "/contents/" + EncodePath(path), token, ser.Serialize(body));
        }

        public static void UploadFileBytes(string owner, string repo, string path, string branch, string token, byte[] content)
        {
            for (int attempt = 0; ; attempt++)
            {
                string sha = null;
                string url = API + "/repos/" + Uri.EscapeDataString(owner) + "/" + Uri.EscapeDataString(repo)
                    + "/contents/" + EncodePath(path) + "?ref=" + Uri.EscapeDataString(branch);
                try { Dictionary<string, object> info = Parse(Request("GET", url, token, null)); sha = Convert.ToString(info["sha"]); }
                catch (GitHubApiException ex) { if (ex.StatusCode != 404) throw; }
                try { PutFileBytes(owner, repo, path, branch, token, content, sha); return; }
                catch (GitHubApiException ex) { if (attempt >= 2 || (ex.StatusCode != 409 && ex.StatusCode != 422)) throw; }
            }
        }

        private static byte[] RequestBytes(string url, string token)
        {
            if (BytesForTests != null) return BytesForTests(url, token);
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.UserAgent = "SRPRecorder2027";
            req.Accept = "application/vnd.github.raw+json";
            req.Headers.Add("Authorization", "token " + token);
            req.Timeout = 120000;
            req.ReadWriteTimeout = 120000;
            using (WebResponse response = req.GetResponse())
            using (Stream input = response.GetResponseStream())
            using (MemoryStream output = new MemoryStream())
            {
                input.CopyTo(output);
                return output.ToArray();
            }
        }

        public static List<string> ListFiles(string owner, string repo, string path, string branch, string token)
        {
            List<string> names = new List<string>();
            string url = API + "/repos/" + Uri.EscapeDataString(owner) + "/" + Uri.EscapeDataString(repo)
                + "/contents/" + EncodePath(path) + "?ref=" + Uri.EscapeDataString(branch);
            string json;
            try { json = Request("GET", url, token, null); }
            catch (GitHubApiException ex) { if (ex.StatusCode == 404) { CheckBranchAccess(owner, repo, branch, token); return names; } throw; }
            JavaScriptSerializer ser = SyncData.Serializer();
            object[] arr = ser.Deserialize<object[]>(json);
            if (arr != null)
            {
                foreach (object o in arr)
                {
                    Dictionary<string, object> d = o as Dictionary<string, object>;
                    if (d == null) continue;
                    if (d.ContainsKey("type") && Convert.ToString(d["type"]) == "file")
                        names.Add(Convert.ToString(d["name"]));
                }
            }
            return names;
        }

        public static void DownloadDir(string owner, string repo, string dir, string branch, string token, string dest)
        {
            FileTransfers.NoLinks(dest);
            string url = API + "/repos/" + Uri.EscapeDataString(owner) + "/" + Uri.EscapeDataString(repo)
                + "/contents/" + EncodePath(dir) + "?ref=" + Uri.EscapeDataString(branch);
            object[] entries = SyncData.Serializer().Deserialize<object[]>(Request("GET", url, token, null));
            if (entries == null) throw new InvalidDataException("GitHub did not return a directory.");
            Directory.CreateDirectory(dest);
            foreach (object entry in entries)
            {
                Dictionary<string, object> item = entry as Dictionary<string, object>;
                if (item == null || !item.ContainsKey("name") || !item.ContainsKey("type")) throw new InvalidDataException("Invalid remote directory entry.");
                string name = Convert.ToString(item["name"]), kind = Convert.ToString(item["type"]);
                string child = FileTransfers.Child(dest, name);
                if (kind == "dir") { DownloadDir(owner, repo, dir + "/" + name, branch, token, child); continue; }
                if (kind != "file") throw new InvalidDataException("Remote links are not supported: " + name);
                string sha;
                byte[] bytes = GetFileBytes(owner, repo, dir + "/" + name, branch, token, out sha);
                if (bytes == null) throw new IOException("Remote file disappeared during download: " + name);
                File.WriteAllBytes(child, bytes);
            }
        }

        public static bool DownloadMemberFiles(string owner, string repo, string member, string branch, string token, string destination)
        {
            string remote = "team/" + member;
            List<string> dirs = ListDir(owner, repo, remote, branch, token);
            if (!dirs.Contains("references") && !dirs.Contains("progress")) return false;
            FileTransfers.ReplaceFromDownload(destination, stage => {
                foreach (string name in new[] { "references", "progress" })
                    if (dirs.Contains(name)) DownloadDir(owner, repo, remote + "/" + name, branch, token, FileTransfers.Child(stage, name));
            });
            return true;
        }

        private static string EncodePath(string path)
        {
            string[] parts = path.Replace("\\", "/").Split('/');
            for (int i = 0; i < parts.Length; i++) parts[i] = Uri.EscapeDataString(parts[i]);
            return string.Join("/", parts);
        }

        private static string Request(string method, string url, string token, string body, string accept = "application/vnd.github+json")
        {
            if (RequestForTests != null) return RequestForTests(method, url, token, body, accept);
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = method;
            req.UserAgent = "SRLogbook";
            req.Accept = accept;
            if (!string.IsNullOrEmpty(token)) req.Headers.Add("Authorization", "token " + token);
            req.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
            req.Timeout = 120000;
            req.ReadWriteTimeout = 120000;
            if (body != null)
            {
                req.ContentType = "application/json";
                byte[] data = Encoding.UTF8.GetBytes(body);
                req.ContentLength = data.Length;
                using (Stream s = req.GetRequestStream()) s.Write(data, 0, data.Length);
            }
            try
            {
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                    return sr.ReadToEnd();
            }
            catch (WebException ex)
            {
                string detail = "";
                if (ex.Response != null)
                {
                    using (StreamReader sr = new StreamReader(ex.Response.GetResponseStream(), Encoding.UTF8))
                        detail = sr.ReadToEnd();
                }
                throw new GitHubApiException(ex.Response == null ? 0 : (int)((HttpWebResponse)ex.Response).StatusCode, detail.Length == 0 ? ex.Message : detail);
            }
        }

        private static Dictionary<string, object> Parse(string json)
        {
            JavaScriptSerializer ser = SyncData.Serializer();
            Dictionary<string, object> d = ser.Deserialize<Dictionary<string, object>>(json);
            return d == null ? new Dictionary<string, object>() : d;
        }
    }
}
