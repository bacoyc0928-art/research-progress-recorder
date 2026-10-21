using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SRLogbook
{
    /// <summary>
    /// 0.2.0 首次启动：清除旧版本（0.0.x / 0.1.x）留在程序目录里的软件数据，保证新版本从一个干净状态开始。
    /// 删除范围：程序自己生成的 JSON / 配置 / 日志、成员下载缓存（References/&lt;账号&gt;/TeamConRef）、
    /// 默认资料库副本（References/Default，保留包内 README.txt）。
    /// 不删除：用户自选的本地进度文件夹及其中的文档，以及个人文件夹里的 references 上传副本。
    /// 清理清单写入 upgrade_cleanup.log，版本标记写入 data_version.txt。
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
                if (File.Exists(marker))
                {
                    string current = File.ReadAllText(marker, Encoding.UTF8).Trim();
                    if (current == Target) return false;
                }
                List<string> removed = new List<string>();
                foreach (string name in OwnedFiles)
                {
                    foreach (string file in Directory.GetFiles(appDir, name + "*"))
                    {
                        try { File.Delete(file); removed.Add(file); } catch { }
                    }
                }
                string refs = Path.Combine(appDir, "References");
                if (Directory.Exists(refs))
                {
                    string def = Path.Combine(refs, "Default");
                    if (Directory.Exists(def))
                    {
                        foreach (string file in Directory.GetFiles(def, "*", SearchOption.AllDirectories))
                        {
                            if (string.Equals(Path.GetFileName(file), "README.txt", StringComparison.OrdinalIgnoreCase)) continue;
                            try { File.Delete(file); removed.Add(file); } catch { }
                        }
                        foreach (string dir in Directory.GetDirectories(def))
                        {
                            try { Directory.Delete(dir, true); removed.Add(dir); } catch { }
                        }
                    }
                    foreach (string memberDir in Directory.GetDirectories(refs))
                    {
                        string team = Path.Combine(memberDir, "TeamConRef");
                        if (Directory.Exists(team))
                        {
                            try { Directory.Delete(team, true); removed.Add(team); } catch { }
                        }
                        try
                        {
                            foreach (string manifest in Directory.GetFiles(memberDir, "references.json"))
                            {
                                File.Delete(manifest);
                                removed.Add(manifest);
                            }
                        }
                        catch { }
                    }
                }
                File.WriteAllText(marker, Target, new UTF8Encoding(false));
                try { File.WriteAllLines(Path.Combine(appDir, "upgrade_cleanup.log"), removed.ToArray(), new UTF8Encoding(false)); } catch { }
                return true;
            }
            catch { return false; }
        }
    }
}
