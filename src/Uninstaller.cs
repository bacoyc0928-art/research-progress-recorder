using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace SRUninstall
{
    public static class UninstallCore
    {
        public const string Marker = ".srprec-install";
        public static readonly string[] DataFiles = { "research_log.json", "workspace.json", "progress.json", "personal.json", "sync_config.json", "localrefs.json", "lang.txt" };
        private static readonly string[] ProgramNames = {
            "SRPRecorder2027.exe", "卸载SRPRecorder2027.exe", "app.ico", "app.manifest", "app.config", "build.ps1", "package-local.ps1",
            "SRPRecorder2027.exe.config", "卸载SRPRecorder2027.exe.config",
            "说明.md", "说明.txt", "分发与使用说明.md", "README.md", "SHA256SUMS.txt", Marker,
            "src/AppPaths.cs", "src/FileTransfers.cs", "src/GitHubSync.cs", "src/Lang.cs", "src/LocalDocs.cs",
            "src/MainForm.Categories.cs", "src/MainForm.Combined.cs", "src/MainForm.Sync.cs", "src/MainForm.Team.cs",
            "src/MainForm.Workspace.cs", "src/Msg.cs", "src/ResearchLogbook.cs", "src/SecretStore.cs", "src/SyncData.cs", "src/Uninstaller.cs", "src/Version.cs",
            "卸载工具/uninstall-macos.command", "卸载工具/说明.md"
        };
        public static void NoLinks(string path)
        {
            for (string p = Path.GetFullPath(path); !string.IsNullOrEmpty(p); p = Path.GetDirectoryName(p))
                if ((Directory.Exists(p) || File.Exists(p)) && (File.GetAttributes(p) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Cannot uninstall through a linked path: " + p);
        }
        public static string Child(string root, string relative)
        {
            root = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);
            string path = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
            if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new IOException("Path is outside the installation.");
            NoLinks(path); return path;
        }
        public static List<string> Plan(string root, bool deleteData)
        {
            root = Path.GetFullPath(root); NoLinks(root);
            if (!File.Exists(Child(root, Marker)) || File.ReadAllText(Child(root, Marker)).Trim() != "SRPRecorder2027 portable v1")
                throw new IOException("Installation marker missing or invalid. No files were removed.");
            List<string> files = new List<string>();
            foreach (string name in ProgramNames) { string p = Child(root, name); if (File.Exists(p)) files.Add(p); }
            if (deleteData)
            {
                foreach (string name in DataFiles)
                    foreach (string suffix in new[] { "", ".bak" }) { string p = Child(root, name + suffix); if (File.Exists(p)) files.Add(p); }
                foreach (string name in new[] { "References", "PersonalWorkspaceSRRec" }) Collect(Child(root, name), files);
            }
            return files;
        }
        private static void Collect(string root, List<string> files)
        {
            NoLinks(root); if (!Directory.Exists(root)) return;
            foreach (string p in Directory.GetFiles(root)) { NoLinks(p); files.Add(p); }
            foreach (string p in Directory.GetDirectories(root)) Collect(p, files);
        }
        public static string Backup(string root, string backupRoot)
        {
            string target = Path.Combine(backupRoot, DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6));
            NoLinks(target); Directory.CreateDirectory(target);
            foreach (string name in DataFiles)
                foreach (string suffix in new[] { "", ".bak" })
                {
                    string from = Child(root, name + suffix);
                    if (File.Exists(from)) File.Copy(from, Path.Combine(target, name + suffix), false);
                }
            return target;
        }
        public static void Remove(string root, bool deleteData)
        {
            // Only delete a prevalidated allowlist. Never recursively remove the install root or external workspaces.
            List<string> plan = Plan(root, deleteData);
            string marker = Child(root, Marker);
            foreach (string file in plan) { if (file == marker) continue; NoLinks(file); File.Delete(file); }
            File.Delete(marker);
        }
    }
    public class UninstallForm : Form
    {
        private readonly CheckBox deleteData;
        public UninstallForm()
        {
            Text = "卸载 SRPRecorder2027"; ClientSize = new Size(580, 300);
            StartPosition = FormStartPosition.CenterScreen; FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; Font = new Font("Microsoft YaHei UI", 9.5f);
            Label text = new Label { Text = "默认只移除程序，研究资料保留在 SRCRec 原目录。\r\n配置另存到“文档\\SRPRecorder2027-数据备份”的独立目录。\r\n外部自选的工作/个人文件夹始终保留。", AutoSize = false };
            text.SetBounds(24, 30, 530, 88); Controls.Add(text);
            deleteData = new CheckBox { Text = "同时删除 SRCRec 内的研究数据、References 和默认进度文件夹", AutoSize = false };
            deleteData.SetBounds(24, 130, 530, 52); Controls.Add(deleteData);
            Button uninstall = new Button { Text = "卸载" }; uninstall.SetBounds(330, 230, 100, 36);
            uninstall.Click += delegate { Uninstall(); }; Controls.Add(uninstall);
            Button cancel = new Button { Text = "取消" }; cancel.SetBounds(444, 230, 100, 36);
            cancel.Click += delegate { Close(); }; Controls.Add(cancel);
        }
        private void Uninstall()
        {
            string root = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            if (MessageBox.Show("请先关闭主程序。确认卸载 SRPRecorder2027？", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                UninstallCore.Plan(root, deleteData.Checked);
                foreach (Process p in Process.GetProcessesByName("SRPRecorder2027"))
                    if (Path.GetFullPath(p.MainModule.FileName).Equals(Path.Combine(root, "SRPRecorder2027.exe"), StringComparison.OrdinalIgnoreCase))
                        throw new IOException("SRPRecorder2027 is running. Close it and retry.");
                string backup = UninstallCore.Backup(root, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SRPRecorder2027-数据备份"));
                string helper = Path.Combine(Path.GetTempPath(), "srprec-uninstall-" + Guid.NewGuid().ToString("N") + ".exe");
                File.Copy(Application.ExecutablePath, helper);
                Process.Start(new ProcessStartInfo { FileName = helper,
                    Arguments = "--remove-program \"" + root + "\" " + Process.GetCurrentProcess().Id + " " + (deleteData.Checked ? "delete-data" : "keep-data"),
                    UseShellExecute = false, WindowStyle = ProcessWindowStyle.Hidden });
                MessageBox.Show("已启动卸载。配置备份：\r\n" + backup + "\r\n未选择删除的数据和外部文件夹将保留。", Text);
                Close();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
    }
    static class Program
    {
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)] private static extern bool CredDelete(string target, uint type, uint flags);
        [STAThread] static void Main(string[] args)
        {
            if (args.Length == 4 && args[0] == "--remove-program")
            {
                try
                {
                    try { Process.GetProcessById(int.Parse(args[2])).WaitForExit(); } catch (ArgumentException) { }
                    string root = Path.GetFullPath(args[1]);
                    UninstallCore.Remove(root, args[3] == "delete-data");
                    CredDelete("SRLogbook/github_token", 1, 0); RemoveShortcuts(root);
                    MessageBox.Show("程序已移除。未选择删除的数据仍保存在原目录。", "SRPRecorder2027");
                }
                catch (Exception ex) { MessageBox.Show("卸载未完成：" + ex.Message, "SRPRecorder2027"); }
                return;
            }
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new UninstallForm());
        }
        private static void RemoveShortcuts(string root)
        {
            try
            {
                Type type = Type.GetTypeFromProgID("WScript.Shell"); object shell = Activator.CreateInstance(type);
                foreach (string folder in new[] { Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), Environment.GetFolderPath(Environment.SpecialFolder.Programs) })
                foreach (string link in Directory.GetFiles(folder, "*.lnk", SearchOption.AllDirectories))
                {
                    object shortcut = type.InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { link });
                    string target = Convert.ToString(shortcut.GetType().InvokeMember("TargetPath", System.Reflection.BindingFlags.GetProperty, null, shortcut, null));
                    if (!string.IsNullOrEmpty(target) && Path.GetFullPath(target).Equals(Path.Combine(root, "SRPRecorder2027.exe"), StringComparison.OrdinalIgnoreCase)) File.Delete(link);
                    Marshal.ReleaseComObject(shortcut);
                }
                Marshal.ReleaseComObject(shell);
            }
            catch { }
        }
    }
}
