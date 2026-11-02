using System;
using System.Collections.Generic;
using System.IO;

namespace SRLogbook
{
    public static class FileTransfers
    {
        public const long MaxUploadBytes = 50L * 1024 * 1024;

        public static void NoLinks(string path)
        {
            for (string p = Path.GetFullPath(path); !string.IsNullOrEmpty(p); p = Path.GetDirectoryName(p))
                if ((File.Exists(p) || Directory.Exists(p)) && (File.GetAttributes(p) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Linked folders/files are not supported: " + p);
        }

        public static string Child(string parent, string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name == "." || name == ".." ||
                name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.Contains("/") || name.Contains("\\") ||
                name != name.Trim().TrimEnd('.') || AppPaths.Sanitize(name) != name)
                throw new InvalidDataException("Invalid downloaded filename: " + name);
            string full = Path.GetFullPath(Path.Combine(parent, name));
            if (!SyncData.IsWithin(full, parent)) throw new InvalidDataException("Download escaped its destination.");
            NoLinks(full);
            return full;
        }

        public static List<string> UploadFiles(string root)
        {
            List<string> result = new List<string>();
            Collect(Path.GetFullPath(root), result);
            return result;
        }

        public static string ProgressRelativePath(string file, string sourceRoot)
        {
            string full = Path.GetFullPath(file);
            string root = Path.GetFullPath(sourceRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            NoLinks(full);
            string rel = SyncData.IsWithin(full, root)
                ? full.Substring(root.Length).TrimStart('\\', '/') : Path.GetFileName(full);
            foreach (string segment in rel.Replace('\\', '/').Split('/')) Child(root, segment);
            return rel.Replace('\\', '/');
        }

        public static List<KeyValuePair<string, string>> ProgressUploads(string sourceRoot, IEnumerable<string> files)
        {
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();
            HashSet<string> paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string file in files)
            {
                string full = Path.GetFullPath(file);
                string rel = ProgressRelativePath(full, sourceRoot);
                if (!File.Exists(full) || new FileInfo(full).Length > MaxUploadBytes)
                    throw new IOException("Missing or oversized upload file: " + full);
                if (!paths.Add(rel)) throw new IOException("Selected progress files have duplicate destination names; rename them first: " + rel);
                result.Add(new KeyValuePair<string, string>(full, rel));
            }
            return result;
        }

        public static string LocalReferenceCopy(string referenceRoot, string name)
        {
            string direct = Child(referenceRoot, name);
            if (File.Exists(direct)) return direct;
            // Older macOS clients kept upload copies one level too deep.
            string legacy = Child(Child(referenceRoot, "references"), name);
            return File.Exists(legacy) ? legacy : direct;
        }

        private static void Collect(string folder, List<string> result)
        {
            NoLinks(folder);
            foreach (string file in Directory.GetFiles(folder))
            {
                NoLinks(file);
                if (new FileInfo(file).Length > MaxUploadBytes) throw new IOException("File exceeds 50 MiB: " + file);
                result.Add(file);
            }
            foreach (string child in Directory.GetDirectories(folder)) Collect(child, result);
        }

        // Download into a sibling directory. Only a fully successful, nonempty download replaces the old copy.
        // The previous copy remains in a timestamped backup, including any files removed by the remote member.
        public static string ReplaceFromDownload(string destination, Action<string> download)
        {
            destination = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar);
            NoLinks(destination);
            // Keep the suffixes short: long member filenames plus a long suffix can exceed MAX_PATH.
            string stage = destination + ".dl-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string backup = destination + ".bak-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 4);
            Directory.CreateDirectory(stage);
            bool moved = false;
            try
            {
                download(stage);
                if (Directory.GetFiles(stage, "*", SearchOption.AllDirectories).Length == 0)
                    throw new InvalidDataException("Remote folder is empty or unavailable; the previous local copy was preserved.");
                if (Directory.Exists(destination)) { Directory.Move(destination, backup); moved = true; }
                try { Directory.Move(stage, destination); }
                catch { if (moved) Directory.Move(backup, destination); throw; }
                return moved ? backup : null;
            }
            finally
            {
                if (Directory.Exists(stage)) { NoLinks(stage); Directory.Delete(stage, true); }
            }
        }
    }
}
