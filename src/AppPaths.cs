using System;
using System.IO;

namespace SRLogbook
{
    /// <summary>The executable's own directory is the portable SRCRec installation root.</summary>
    public static class AppPaths
    {
        public static string Root(string appDir)
        {
            return Path.GetFullPath(appDir);
        }

        public static string Ensure(string appDir)
        {
            string root = Root(appDir);
            Directory.CreateDirectory(root);
            Directory.CreateDirectory(Path.Combine(root, "References"));
            Directory.CreateDirectory(Path.Combine(root, "References", "Default"));
            // 默认本地进度文件夹：PersonalWorkspaceSRRec，内含三个子文件夹（用户 2026-10-21 要求）。
            Directory.CreateDirectory(Path.Combine(root, "PersonalWorkspaceSRRec"));
            Directory.CreateDirectory(Path.Combine(root, "PersonalWorkspaceSRRec", "Mat_Local"));
            Directory.CreateDirectory(Path.Combine(root, "PersonalWorkspaceSRRec", "Reference"));
            Directory.CreateDirectory(Path.Combine(root, "PersonalWorkspaceSRRec", "Public_Temp"));
            return root;
        }

        /// <summary>非参考资料的个人本地资料（上传本地进度的默认来源）。</summary>
        public static string MatLocal(string appDir)
        {
            Ensure(appDir);
            return Path.Combine(Root(appDir), "PersonalWorkspaceSRRec", "Mat_Local");
        }

        /// <summary>个人参考资料（上传参考资料的默认来源，也显示在参考资料页）。</summary>
        public static string ReferenceDir(string appDir)
        {
            Ensure(appDir);
            return Path.Combine(Root(appDir), "PersonalWorkspaceSRRec", "Reference");
        }

        /// <summary>他人上传内容的临时下载区：启动时清空、退出时清空。</summary>
        public static string PublicTemp(string appDir)
        {
            Ensure(appDir);
            return Path.Combine(Root(appDir), "PersonalWorkspaceSRRec", "Public_Temp");
        }

        /// <summary>清空 Public_Temp（保留文件夹本身）。</summary>
        public static void ClearPublicTemp(string appDir)
        {
            try
            {
                string dir = PublicTemp(appDir);
                if (!Directory.Exists(dir)) return;
                foreach (string f in Directory.GetFiles(dir)) { try { File.Delete(f); } catch { } }
                foreach (string d in Directory.GetDirectories(dir)) { try { Directory.Delete(d, true); } catch { } }
            }
            catch { }
        }

        /// <summary>
        /// 0.2.2 起“个人参考资料”统一放在 PersonalWorkspaceSRRec\Reference，他人内容放 Public_Temp。
        /// 这里清掉旧设计留在 References 根下的人员目录、遗留清单和误建的 references 子目录，只保留 Default。
        /// </summary>
        public static void MigrateLegacyReferences(string appDir)
        {
            try
            {
                string refs = Path.Combine(Root(appDir), "References");
                if (!Directory.Exists(refs)) return;
                foreach (string dir in Directory.GetDirectories(refs))
                {
                    string name = Path.GetFileName(dir);
                    if (string.Equals(name, "Default", StringComparison.OrdinalIgnoreCase)) continue;
                    try { Directory.Delete(dir, true); } catch { }
                }
                foreach (string file in Directory.GetFiles(refs, "*.json")) { try { File.Delete(file); } catch { } }
                try { File.Delete(Path.Combine(Root(appDir), "personal.json")); } catch { }
            }
            catch { }
        }

        public static string DefaultReferences(string appDir)
        {
            Ensure(appDir);
            return Path.Combine(Root(appDir), "References", "Default");
        }

        public static string PersonalProgress(string appDir)
        {
            Ensure(appDir);
            return Path.Combine(Root(appDir), "PersonalWorkspaceSRRec");
        }

        public static string MemberReferences(string appDir, string login)
        {
            Ensure(appDir);
            string safe = Sanitize(login);
            if (safe != login || safe.Equals("Default", StringComparison.OrdinalIgnoreCase)) safe = "@" + safe;
            string dir = Path.Combine(Root(appDir), "References", safe);
            Directory.CreateDirectory(dir);
            return dir;
        }

        public static string Sanitize(string name)
        {
            if (string.IsNullOrEmpty(name)) return "unknown";
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            foreach (char c in new char[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' }) name = name.Replace(c, '_');
            name = name.Trim().TrimEnd('.');
            if (name.Length == 0 || name == "." || name == "..") return "unknown";
            string stem = name.Split('.')[0].ToUpperInvariant();
            if (stem == "CON" || stem == "PRN" || stem == "AUX" || stem == "NUL" ||
                System.Text.RegularExpressions.Regex.IsMatch(stem, @"^(COM|LPT)[1-9]$")) name = "member-" + name;
            return name;
        }
    }
}
