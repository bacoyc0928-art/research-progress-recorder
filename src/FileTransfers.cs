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
