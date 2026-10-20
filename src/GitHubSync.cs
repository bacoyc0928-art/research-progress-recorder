using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace SRLogbook
{
    public class SyncConfig
    {
        public string owner { get; set; }
        public string repo { get; set; }
        public string path { get; set; }
        public string branch { get; set; }
    }

    /// <summary>
    /// 用 GitHub 仓库中的一个 JSON 文件作为团队共享数据。
    /// 访问凭据是 GitHub fine-grained PAT（只授予该仓库 Contents 读写），
    /// 保存在 Windows 凭据管理器，不写入项目文件。
    /// </summary>
    public static class GitHubSync
    {
        private const string API = "https://api.github.com";
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
                    JavaScriptSerializer ser = new JavaScriptSerializer();
                    SyncConfig c = ser.Deserialize<SyncConfig>(File.ReadAllText(ConfigPath(appDir), Encoding.UTF8));
                    if (c != null) return c;
                }
            }
            catch { }
            return new SyncConfig { owner = "", repo = "", path = "sync/research_log.json", branch = "main" };
        }

        public static void SaveConfig(string appDir, SyncConfig c)
        {
            JavaScriptSerializer ser = new JavaScriptSerializer();
            File.WriteAllText(ConfigPath(appDir), ser.Serialize(c), new UTF8Encoding(false));
        }

        public static bool HasToken() { return !string.IsNullOrEmpty(SecretStore.Load(TokenTarget)); }

        public static void SetToken(string token) { SecretStore.Save(TokenTarget, token); }

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
                if (ex.Message.IndexOf("404", StringComparison.Ordinal) >= 0) return null;
                throw;
            }
            Dictionary<string, object> d = Parse(json);
            if (!d.ContainsKey("content")) return null;
            sha = d.ContainsKey("sha") ? Convert.ToString(d["sha"]) : null;
            string b64 = Convert.ToString(d["content"]).Replace("\n", "").Replace("\r", "");
            return Encoding.UTF8.GetString(Convert.FromBase64String(b64));
        }

        public static void PutFile(string owner, string repo, string path, string branch, string token, string content, string sha)
        {
            Dictionary<string, object> body = new Dictionary<string, object>();
            body["message"] = "sync: research logbook " + DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            body["content"] = Convert.ToBase64String(new UTF8Encoding(false).GetBytes(content));
            body["branch"] = branch;
            if (!string.IsNullOrEmpty(sha)) body["sha"] = sha;
            JavaScriptSerializer ser = new JavaScriptSerializer();
            Request("PUT", API + "/repos/" + Uri.EscapeDataString(owner) + "/" + Uri.EscapeDataString(repo)
                + "/contents/" + EncodePath(path), token, ser.Serialize(body));
        }

        private static string EncodePath(string path)
        {
            string[] parts = path.Replace("\\", "/").Split('/');
            for (int i = 0; i < parts.Length; i++) parts[i] = Uri.EscapeDataString(parts[i]);
            return string.Join("/", parts);
        }

        private static string Request(string method, string url, string token, string body)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = method;
            req.UserAgent = "SRLogbook";
            req.Accept = "application/vnd.github+json";
            if (!string.IsNullOrEmpty(token)) req.Headers.Add("Authorization", "token " + token);
            req.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
            req.Timeout = 20000;
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
                throw new Exception("GitHub 返回 " + (ex.Response == null ? ex.Message : ((HttpWebResponse)ex.Response).StatusCode.ToString()) + "：" + detail);
            }
        }

        private static Dictionary<string, object> Parse(string json)
        {
            JavaScriptSerializer ser = new JavaScriptSerializer();
            Dictionary<string, object> d = ser.Deserialize<Dictionary<string, object>>(json);
            return d == null ? new Dictionary<string, object>() : d;
        }
    }
}
