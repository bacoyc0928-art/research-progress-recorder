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

        /// <summary>true 表示最近一次 Check 因 GitHub API 限流而改用发布页（界面据此说明更新说明在网页上）。</summary>
        public static bool LastCheckUsedReleasePage { get; private set; }

        public static Release Check()
        {
            LastCheckUsedReleasePage = false;
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                Release rel;
                try
                {
                    string url = "https://api.github.com/repos/" + Owner + "/" + Repo + "/releases/latest";
                    rel = Json.Deserialize<Release>(HttpGet(url, "application/vnd.github+json"));
                }
                catch (WebException ex)
                {
                    // 匿名 API 按 IP 限流（共享出口网络常见）：改走不受该额度限制的发布页与下载地址。
                    HttpWebResponse resp = ex.Response as HttpWebResponse;
                    int code = resp == null ? 0 : (int)resp.StatusCode;
                    if (code != 403 && code != 429) throw;
                    rel = CheckFromReleasePage();
                    LastCheckUsedReleasePage = true;
                }
                if (rel == null || string.IsNullOrEmpty(rel.tag_name) || rel.assets == null) return null;
                Asset win = rel.assets.Find(a => a != null && a.name != null && a.name.EndsWith("-Windows.zip", StringComparison.OrdinalIgnoreCase));
                if (win == null) return null;
                return rel;
            }
            catch { return null; }
        }

        /// <summary>
        /// 从 github.com/&lt;repo&gt;/releases/latest 的跳转得到标签，再读同一 Release 的 SHA256SUMS.txt 得到安装包清单；
        /// 返回与 API 相同的结构，下载后仍按 SHA256SUMS 校验。
        /// </summary>
        public static Release CheckFromReleasePage()
        {
            string baseUrl = "https://github.com/" + Owner + "/" + Repo + "/releases";
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(baseUrl + "/latest");
            req.UserAgent = "SRPRecorder2027";
            req.Timeout = 30000;
            Uri final;
            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse()) final = resp.ResponseUri;
            string tag = TagFromReleaseUrl(final);
            if (tag == null) throw new InvalidDataException("The public release page did not identify a version tag.");
            string download = baseUrl + "/download/" + tag + "/";
            List<string> names = AssetNamesFromSums(HttpGet(download + "SHA256SUMS.txt", "text/plain"));
            if (names.Count == 0) throw new InvalidDataException("The public release checksum file lists no packages.");
            Release rel = new Release { tag_name = tag, name = tag, body = "", assets = new List<Asset>() };
            foreach (string name in names) rel.assets.Add(new Asset { name = name, browser_download_url = download + name });
            rel.assets.Add(new Asset { name = "SHA256SUMS.txt", browser_download_url = download + "SHA256SUMS.txt" });
            return rel;
        }

        /// <summary>只接受 https://github.com/&lt;owner&gt;/&lt;repo&gt;/releases/tag/vX.Y.Z。</summary>
        public static string TagFromReleaseUrl(Uri final)
        {
            if (final == null || final.Scheme != Uri.UriSchemeHttps || !string.Equals(final.Host, "github.com", StringComparison.OrdinalIgnoreCase)) return null;
            string prefix = "/" + Owner + "/" + Repo + "/releases/tag/";
            string path = final.AbsolutePath;
            if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return null;
            string tag = path.Substring(prefix.Length);
            return System.Text.RegularExpressions.Regex.IsMatch(tag, @"^v\d+\.\d+\.\d+$") ? tag : null;
        }

        /// <summary>从 SHA256SUMS.txt 取出本项目的安装包文件名（忽略任何不符合命名规则的行）。</summary>
        public static List<string> AssetNamesFromSums(string sums)
        {
            List<string> names = new List<string>();
            foreach (string line in (sums ?? "").Split('\n'))
            {
                string[] parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2 || !System.Text.RegularExpressions.Regex.IsMatch(parts[0], "^[0-9a-fA-F]{64}$")) continue;
                string name = parts[parts.Length - 1].TrimStart('*');
                if (System.Text.RegularExpressions.Regex.IsMatch(name, @"^research-progress-recorder-\d+\.\d+\.\d+-(Windows\.zip|macOS-(arm64|x64)\.dmg)$") && !names.Contains(name))
                    names.Add(name);
            }
            return names;
        }

        /// <summary>下载 Windows ZIP、按 Release 的 SHA256SUMS.txt 校验并解压到临时目录；返回 SRCRec 目录。</summary>
        public static string DownloadAndStage(Release rel, IProgress<int> progress)
        {
            Asset win = rel.assets.Find(a => a != null && a.name != null && a.name.EndsWith("-Windows.zip", StringComparison.OrdinalIgnoreCase));
            Asset sums = rel.assets.Find(a => a != null && string.Equals(a.name, "SHA256SUMS.txt", StringComparison.OrdinalIgnoreCase));
            if (win == null) throw new InvalidOperationException("Release has no Windows zip.");
            if (sums == null) throw new InvalidDataException("Release has no SHA256SUMS.txt; update cancelled.");
            if (Path.GetFileName(win.name) != win.name || win.name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new InvalidDataException("Invalid update asset name.");
            string stage = Path.Combine(Path.GetTempPath(), "SRPRecorder2027", "update-" + Sanitize(rel.tag_name) + "-" + Guid.NewGuid().ToString("N"));
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
            string script = Path.Combine(Path.GetTempPath(), "SRPRecorder2027", "apply-update-" + Guid.NewGuid().ToString("N") + ".ps1");
            Directory.CreateDirectory(Path.GetDirectoryName(script));
            string body =
                "param([int]$AppPid,[string]$Source,[string]$Target,[string]$Exe)\r\n" +
                "$ErrorActionPreference = 'Stop'\r\n" +
                "try { Wait-Process -Id $AppPid -Timeout 120 -ErrorAction SilentlyContinue } catch { }\r\n" +
                "if (Get-Process -Id $AppPid -ErrorAction SilentlyContinue) { throw 'Application is still running; update cancelled.' }\r\n" +
                "Start-Sleep -Milliseconds 800\r\n" +
                "$exeName = 'SRPRecorder2027.exe'\r\n" +
                "$oldExe = Join-Path $Target $exeName\r\n" +
                "if (Test-Path -LiteralPath $oldExe) { Copy-Item -LiteralPath $oldExe -Destination ($oldExe + '.previous') -Force }\r\n" +
                "$programNames = @('app.ico','app.manifest','app.config','README.md','使用说明.md','使用说明-EN.md','build.ps1','package-local.ps1','说明.md','说明.txt','分发与使用说明.md','SRPRecorder2027.exe.config','卸载SRPRecorder2027.exe','卸载SRPRecorder2027.exe.config','.srprec-install','SHA256SUMS.txt','src','卸载工具')\r\n" +
                "Get-ChildItem -LiteralPath $Source -Force | ForEach-Object {\r\n" +
                "  if ($programNames -notcontains $_.Name) { return }\r\n" +
                "  Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $Target $_.Name) -Recurse -Force\r\n" +
                "}\r\n" +
                "$newExe = $oldExe + '.new'\r\n" +
                "Copy-Item -LiteralPath (Join-Path $Source $exeName) -Destination $newExe -Force\r\n" +
                "if (Test-Path -LiteralPath $oldExe) { [IO.File]::Replace($newExe, $oldExe, ($oldExe + '.previous'), $true) } else { Move-Item -LiteralPath $newExe -Destination $oldExe }\r\n" +
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
