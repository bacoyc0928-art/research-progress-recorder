using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace SRLogbook
{
    /// <summary>团队成员的操作日志：每个账号一份私有明细，另有一份全体成员共享的公开日志。</summary>
    public class ActivityEntry
    {
        public string Time { get; set; }
        public string User { get; set; }
        public string Action { get; set; }
        public string Detail { get; set; }
    }

    public static class ActivityLog
    {
        public const int PrivateLimit = 500;
        public const int PublicLimit = 2000;
        public const string PublicPath = "logs/public/activity.json";

        public static string PrivatePath(string login) { return "logs/private/" + login + "/activity.json"; }

        public static void Append(string owner, string repo, string branch, string token, string login, string action, string detail, bool toPublic)
        {
            try { AppendTo(owner, repo, branch, token, PrivatePath(login), login, action, detail, PrivateLimit); }
            catch { }
            if (!toPublic) return;
            try { AppendTo(owner, repo, branch, token, PublicPath, login, action, detail, PublicLimit); }
            catch { }
        }

        public static void AppendTo(string owner, string repo, string branch, string token, string path, string login, string action, string detail, int limit)
        {
            for (int attempt = 0; ; attempt++)
            {
                string sha = null;
                string old = GitHubSync.GetFile(owner, repo, path, branch, token, out sha);
                List<ActivityEntry> list = new List<ActivityEntry>();
                if (!string.IsNullOrEmpty(old))
                {
                    try
                    {
                        List<ActivityEntry> parsed = SyncData.Serializer().Deserialize<List<ActivityEntry>>(old);
                        if (parsed != null) list = parsed;
                    }
                    catch { list = new List<ActivityEntry>(); }
                }
                list.Add(new ActivityEntry { Time = SyncData.Now(), User = login, Action = action, Detail = detail });
                if (list.Count > limit) list.RemoveRange(0, list.Count - limit);
                string json = SyncData.Serializer().Serialize(list);
                try { GitHubSync.PutFile(owner, repo, path, branch, token, json, sha); return; }
                catch (GitHubApiException ex) { if (attempt >= 2 || (ex.StatusCode != 409 && ex.StatusCode != 422)) throw; }
            }
        }

        public static List<ActivityEntry> Read(string owner, string repo, string branch, string token, string path)
        {
            string sha = null;
            string old = GitHubSync.GetFile(owner, repo, path, branch, token, out sha);
            if (string.IsNullOrEmpty(old)) return new List<ActivityEntry>();
            List<ActivityEntry> list = SyncData.Serializer().Deserialize<List<ActivityEntry>>(old);
            return list == null ? new List<ActivityEntry>() : list;
        }
    }
}
