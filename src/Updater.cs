using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace SRLogbook
{
    /// <summary>
    /// 应用内更新：查询公开仓库的最新 Release、下载并校验安装包、退出后由脚本替换程序文件。
    /// 只覆盖程序文件（exe / 配置 / 文档 / src / 卸载工具），绝不删除 PersonalWorkspaceSRRec、
    /// References、*.json 等用户数据；替换前保留上一版 exe 为 .previous。
    /// </summary>
    public static class Updater
    {
        public const string Owner = "bacoyc0928-art";
        public const string Repo = "research-progress-recorder";
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer();

        public class Asset
        {
            public string name { get; set; }
            public string browser_download_url { get; set; }
            public long size { get; set; }
        }

        public class Release
        {
            public string tag_name { get; set; }
            public string name { get; set; }
            public string body { get; set; }
            public List<Asset> assets { get; set; }
        }

        public static Version Current()
        {
            return typeof(Updater).Assembly.GetName().Version;
        }

        public static string CurrentText()
        {
            Version v = Current();
            return v.Major + "." + v.Minor + "." + v.Build;
        }

        public static bool IsNewer(string tag)
        {
            Version remote;
            if (!Version.TryParse((tag ?? "").TrimStart('v', 'V'), out remote)) return false;
            Version local = Current();
            return remote > new Version(local.Major, local.Minor, local.Build);
        }

        public static Release Check()
        {
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                string url = "https://api.github.com/repos/" + Owner + "/" + Repo + "/releases/latest";
                string json = HttpGet(url, "application/vnd.github+json");
                Release rel = Json.Deserialize<Release>(json);
                if (rel == null || string.IsNullOrEmpty(rel.tag_name) || rel.assets == null) return null;
                Asset win = rel.assets.Find(a => a != null && a.name != null && a.name.EndsWith("-Windows.zip", StringComparison.OrdinalIgnoreCase));
                if (win == null) return null;
                return rel;
            }
            catch { return null; }
        }

        /// <summary>下载 Windows ZIP、按 Release 的 SHA256SUMS.txt 校验并解压到临时目录；返回 SRCRec 目录。</summary>
        public static string DownloadAndStage(Release rel, IProgress<int> progress)
        {
            Asset win = rel.assets.Find(a => a != null && a.name != null && a.name.EndsWith("-Windows.zip", StringComparison.OrdinalIgnoreCase));
            Asset sums = rel.assets.Find(a => a != null && string.Equals(a.name, "SHA256SUMS.txt", StringComparison.OrdinalIgnoreCase));
            if (win == null) throw new InvalidOperationException("Release has no Windows zip.");
            string stage = Path.Combine(Path.GetTempPath(), "SRPRecorder2027", "update-" + Sanitize(rel.tag_name));
            try { if (Directory.Exists(stage)) Directory.Delete(stage, true); } catch { }
            Directory.CreateDirectory(stage);
            string zip = Path.Combine(stage, win.name);
            Download(win.browser_download_url, zip, win.size, progress);
            if (sums != null)
            {
                string expected = FindSha(HttpGet(sums.browser_download_url, "text/plain"), win.name);
                string actual = Sha256(zip);
                if (string.IsNullOrEmpty(expected) || !string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("SHA-256 mismatch; the download was discarded.");
            }
            string files = Path.Combine(stage, "files");
            Directory.CreateDirectory(files);
            ZipFile.ExtractToDirectory(zip, files);
            string source = Path.Combine(files, "SRCRec");
            if (!File.Exists(Path.Combine(source, "SRPRecorder2027.exe")))
                throw new InvalidDataException("The update package has no SRCRec\\SRPRecorder2027.exe.");
            return source;
        }

        /// <summary>写出替换脚本并启动它；调用方随后应立即关闭程序。</summary>
        public static void ApplyAndRestart(string source, string appDir, string exePath, int? pidOverride = null)
        {
            string script = Path.Combine(Path.GetTempPath(), "SRPRecorder2027", "apply-update.ps1");
            Directory.CreateDirectory(Path.GetDirectoryName(script));
            string body =
                "param([int]$AppPid,[string]$Source,[string]$Target,[string]$Exe)\r\n" +
                "$ErrorActionPreference = 'SilentlyContinue'\r\n" +
                "try { Wait-Process -Id $AppPid -Timeout 120 } catch { }\r\n" +
                "Start-Sleep -Milliseconds 800\r\n" +
                "$exeName = 'SRPRecorder2027.exe'\r\n" +
                "$oldExe = Join-Path $Target $exeName\r\n" +
                "if (Test-Path -LiteralPath $oldExe) { Copy-Item -LiteralPath $oldExe -Destination ($oldExe + '.previous') -Force }\r\n" +
                "Get-ChildItem -LiteralPath $Source -Force | ForEach-Object {\r\n" +
                "  if ($_.Name -eq $exeName) { return }\r\n" +
                "  Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $Target $_.Name) -Recurse -Force\r\n" +
                "}\r\n" +
                "Copy-Item -LiteralPath (Join-Path $Source $exeName) -Destination $oldExe -Force\r\n" +
                "Start-Process -FilePath $Exe\r\n";
            File.WriteAllText(script, body, new UTF8Encoding(true));
            int pid = pidOverride ?? Process.GetCurrentProcess().Id;
            string args = "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"" + script + "\"" +
                " -AppPid " + pid +
                " -Source \"" + source + "\"" +
                " -Target \"" + appDir + "\"" +
                " -Exe \"" + exePath + "\"";
            Process.Start(new ProcessStartInfo("powershell.exe", args) { UseShellExecute = false, CreateNoWindow = true });
        }

        private static string HttpGet(string url, string accept)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.UserAgent = "SRPRecorder2027";
            req.Accept = accept;
            req.Timeout = 30000;
            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                return sr.ReadToEnd();
        }

        private static void Download(string url, string path, long size, IProgress<int> progress)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.UserAgent = "SRPRecorder2027";
            req.Timeout = 60000;
            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            using (Stream input = resp.GetResponseStream())
            using (FileStream output = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                byte[] buffer = new byte[81920];
                long total = 0, expected = size > 0 ? size : resp.ContentLength;
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    output.Write(buffer, 0, read);
                    total += read;
                    if (progress != null && expected > 0)
                        progress.Report((int)Math.Min(100, total * 100 / expected));
                }
            }
        }

        private static string Sha256(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream fs = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "").ToLowerInvariant();
        }

        private static string FindSha(string sums, string name)
        {
            foreach (string line in (sums ?? "").Split('\n'))
            {
                string text = line.Trim();
                int space = text.IndexOf(' ');
                if (space <= 0) continue;
                string hash = text.Substring(0, space).Trim();
                string file = text.Substring(space).Trim().TrimStart('*');
                if (string.Equals(file, name, StringComparison.OrdinalIgnoreCase)) return hash;
            }
            return null;
        }

        private static string Sanitize(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return name;
        }
    }
}
