using System;
using System.Collections.Generic;
using System.IO;

namespace SRLogbook
{
    public class PostItem
    {
        public string Id { get; set; }
        public string Author { get; set; }
        public string Time { get; set; }
        public string Body { get; set; }
    }

    /// <summary>快速发行：帖子以 posts/&lt;时间-账号-随机&gt;.txt 存放，已阅状态存 posts/read/&lt;账号&gt;.json。</summary>
    public static class Posts
    {
        public const string Dir = "posts";

        public static string ReadPath(string login) { return Dir + "/read/" + login + ".json"; }

        public static PostItem ParseId(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length < 20) return null;
            string ts = id.Substring(0, 14);
            for (int i = 0; i < ts.Length; i++) if (!char.IsDigit(ts[i])) return null;
            if (id.Length < 16 || id[14] != '-') return null;
            string rest = id.Substring(15);
            int dash = rest.LastIndexOf('-');
            if (dash <= 0) return null;
            PostItem p = new PostItem();
            p.Id = id;
            p.Author = rest.Substring(0, dash);
            p.Time = ts.Substring(0, 4) + "-" + ts.Substring(4, 2) + "-" + ts.Substring(6, 2) + " " + ts.Substring(8, 2) + ":" + ts.Substring(10, 2);
            return p;
        }

        public static List<PostItem> List(string owner, string repo, string branch, string token)
        {
            List<PostItem> list = new List<PostItem>();
            Dictionary<string, string> files = GitHubSync.ListFileShas(owner, repo, Dir, branch, token);
            foreach (KeyValuePair<string, string> kv in files)
            {
                if (!kv.Key.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)) continue;
                PostItem p = ParseId(kv.Key.Substring(0, kv.Key.Length - 4));
                if (p == null) continue;
                string sha = null;
                p.Body = GitHubSync.GetFile(owner, repo, Dir + "/" + kv.Key, branch, token, out sha) ?? "";
                list.Add(p);
            }
            list.Sort(delegate(PostItem a, PostItem b) { return string.CompareOrdinal(b.Id, a.Id); });
            return list;
        }

        public static string Publish(string owner, string repo, string branch, string token, string login, string body)
        {
            string id = DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "-" + login + "-" + Guid.NewGuid().ToString("N").Substring(0, 4);
            GitHubSync.PutFile(owner, repo, Dir + "/" + id + ".txt", branch, token, body, null);
            return id;
        }

        public static List<string> ReadIds(string owner, string repo, string branch, string token, string login)
        {
            string sha = null;
            string json = GitHubSync.GetFile(owner, repo, ReadPath(login), branch, token, out sha);
            if (string.IsNullOrEmpty(json)) return new List<string>();
            try
            {
                List<string> parsed = SyncData.Serializer().Deserialize<List<string>>(json);
                return parsed == null ? new List<string>() : parsed;
            }
            catch { return new List<string>(); }
        }

        public static void MarkRead(string owner, string repo, string branch, string token, string login, string id)
        {
            for (int attempt = 0; ; attempt++)
            {
                string sha = null;
                string json = GitHubSync.GetFile(owner, repo, ReadPath(login), branch, token, out sha);
                List<string> ids = new List<string>();
                if (!string.IsNullOrEmpty(json))
                {
                    try { List<string> p = SyncData.Serializer().Deserialize<List<string>>(json); if (p != null) ids = p; }
                    catch { }
                }
                if (!ids.Contains(id)) ids.Add(id);
                if (ids.Count > 2000) ids.RemoveRange(0, ids.Count - 2000);
                string body = SyncData.Serializer().Serialize(ids);
                try { GitHubSync.PutFile(owner, repo, ReadPath(login), branch, token, body, sha); return; }
                catch (GitHubApiException ex) { if (attempt >= 2 || (ex.StatusCode != 409 && ex.StatusCode != 422)) throw; }
            }
        }

        public static void Delete(string owner, string repo, string branch, string token, string id)
        {
            string path = Dir + "/" + id + ".txt";
            Dictionary<string, string> map = GitHubSync.ListFileShas(owner, repo, Dir, branch, token);
            string sha;
            if (!map.TryGetValue(id + ".txt", out sha)) throw new InvalidDataException("帖子不存在或已被删除。");
            GitHubSync.DeleteFile(owner, repo, path, branch, token, sha);
        }
    }
}
