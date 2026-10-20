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
            Directory.CreateDirectory(Path.Combine(root, "PersonalWorkspaceSRRec"));
            return root;
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
