using System;
using System.IO;
using System.Windows.Forms;

namespace SRLogbook
{
    public partial class MainForm
    {
        private void ChangeWorkspace()
        {
            if (!RequireLogin("工作文件夹")) return;
            using (FolderBrowserDialog d = new FolderBrowserDialog())
            {
                d.Description = "选择工作文件夹（程序将从中读取资料并保存本地文档）";
                if (Directory.Exists(projectRoot)) d.SelectedPath = projectRoot;
                if (d.ShowDialog(this) != DialogResult.OK) return;
                workspaceFolder = d.SelectedPath;
                SaveWorkspace(workspaceFolder);
                projectRoot = workspaceFolder;
                if (store != null) store.ProjectRoot = projectRoot;
                Persist();
                RebuildRefs();
                RefreshDocPage();
                RefreshOverview2();
                RefreshLocalRefs();
                Msg.Show("工作文件夹已切换：" + projectRoot, "工作文件夹");
            }
        }
    }
}
