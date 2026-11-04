using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SRLogbook
{
    /// <summary>
    /// 0.2.0 一次性升级：把旧版本（0.0.x / 0.1.x）留在程序目录里的软件数据移出工作区。
    /// 只在确实发现旧版痕迹时才迁移；新解压、尚未启动过的目录即使已经放了资料也只写版本标记、不动任何文件。
    /// 迁移时文件移入 upgrade-backup-&lt;时间&gt;\，不再直接删除，需要时可以找回。
    /// 范围：程序自己生成的 JSON / 配置 / 草稿、成员下载缓存（References/&lt;账号&gt;/TeamConRef 与 references.json）、
    /// 默认资料库副本（References/Default，保留包内 README.txt）。不涉及本地进度文件夹与个人参考资料。
    /// 移动清单写入 upgrade_cleanup.log，版本标记写入 data_version.txt。
    /// </summary>
    public static class CleanUpgrade
    {
        public const string Target = "0.2.0";

        private static readonly string[] OwnedFiles = new string[]
        {
            "research_log.json", "progress.json", "personal.json", "workspace.json",
            "sync_config.json", "news.json", "lang.txt", "help_local.md", "startup_error.txt"
        };

        public static bool Run(string appDir)
        {
            try
            {
                string marker = Path.Combine(appDir, "data_version.txt");
                string current = File.Exists(marker) ? File.ReadAllText(marker, Encoding.UTF8).Trim() : null;
                if (current == Target) return false;
                if (!HasLegacyData(appDir, current))
                {
                    // 没有旧版痕迹（例如全新解压后先放入了资料）：只记下版本，保留所有文件。
                    File.WriteAllText(marker, Target, new UTF8Encoding(false));
                    return false;
                }
                string backup = Path.Combine(appDir, "upgrade-backup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
                List<string> moved = new List<string>();
                foreach (string name in OwnedFiles)
                    foreach (string file in Directory.GetFiles(appDir, name + "*"))
                        MoveToBackup(file, appDir, backup, moved);
                string refs = Path.Combine(appDir, "References");
                if (Directory.Exists(refs))
                {
                    string def = Path.Combine(refs, "Default");
                    if (Directory.Exists(def))
                    {
                        foreach (string file in Directory.GetFiles(def, "*", SearchOption.AllDirectories))
                        {
                            if (string.Equals(Path.GetFileName(file), "README.txt", StringComparison.OrdinalIgnoreCase)) continue;
                            MoveToBackup(file, appDir, backup, moved);
                        }
                        foreach (string dir in Directory.GetDirectories(def))
                            try { if (Directory.GetFileSystemEntries(dir, "*", SearchOption.AllDirectories).Length == 0 || IsEmptyTree(dir)) Directory.Delete(dir, true); } catch { }
                    }
                    foreach (string memberDir in Directory.GetDirectories(refs))
                    {
                        if (string.Equals(Path.GetFileName(memberDir), "Default", StringComparison.OrdinalIgnoreCase)) continue;
                        string team = Path.Combine(memberDir, "TeamConRef");
                        if (Directory.Exists(team)) MoveToBackup(team, appDir, backup, moved);
                        foreach (string manifest in Directory.GetFiles(memberDir, "references.json"))
                            MoveToBackup(manifest, appDir, backup, moved);
                    }
                }
                File.WriteAllText(marker, Target, new UTF8Encoding(false));
                try { File.WriteAllLines(Path.Combine(appDir, "upgrade_cleanup.log"), moved.ToArray(), new UTF8Encoding(false)); } catch { }
                return true;
            }
            catch { return false; }
        }

        /// <summary>0.1.x 及更早版本才有的文件或缓存结构；仅凭标记缺失不足以判定为旧版。</summary>
        public static bool HasLegacyData(string appDir, string markerText)
        {
            if (!string.IsNullOrEmpty(markerText)) return true;   // 明确写着其他版本号
            if (File.Exists(Path.Combine(appDir, "personal.json"))) return true;
            string refs = Path.Combine(appDir, "References");
            if (!Directory.Exists(refs)) return false;
            if (File.Exists(Path.Combine(refs, "references.json")) || Directory.Exists(Path.Combine(refs, "references"))) return true;
            foreach (string memberDir in Directory.GetDirectories(refs))
            {
                if (string.Equals(Path.GetFileName(memberDir), "Default", StringComparison.OrdinalIgnoreCase)) continue;
                if (Directory.Exists(Path.Combine(memberDir, "TeamConRef")) || File.Exists(Path.Combine(memberDir, "references.json"))) return true;
            }
            return false;
        }

        private static void MoveToBackup(string path, string appDir, string backup, List<string> moved)
        {
            try
            {
                string relative = path.Substring(appDir.TrimEnd('\\', '/').Length).TrimStart('\\', '/');
                string target = Path.Combine(backup, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                if (Directory.Exists(path)) Directory.Move(path, target); else File.Move(path, target);
                moved.Add(path + " -> " + target);
            }
            catch { }
        }

        private static bool IsEmptyTree(string dir)
        {
            return Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Length == 0;
        }
    }
}
