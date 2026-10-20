using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace SRLogbook
{
    /// <summary>
    /// 运行时中英文切换：按控件 Text 精确映射 + 常见动态文案替换。
    /// 语言选择保存在程序目录的 lang.txt，不写入任何敏感信息。
    /// </summary>
    public static class Lang
    {
        public static bool En;

        private static readonly Dictionary<string, string> exact = new Dictionary<string, string>
        {
            { "研究进展记录器", "Research Progress Recorder" },
            { "研究进展记录器 · Synchrotron Radiation Project 2027 · 0.0.2", "Research Progress Recorder · Synchrotron Radiation Project 2027 · 0.0.2" },
            { "文档可打开或归入类别；移除会将磁盘文件移入回收站。", "Open or categorize documents; Remove moves the file to Recycle Bin." },
            { "仓库（owner/repo）", "Repository (owner/repo)" },
            { "文件路径", "File path" },
            { "分支", "Branch" },
            { "类别名称", "Category name" },
            { "访问密钥（仓库所有者可用 fine-grained PAT；个人仓库协作者需 classic PAT）", "Token (owner: fine-grained PAT; collaborators: classic PAT)" },
            { "请填写你自己的 GitHub 访问密钥。", "Enter your own GitHub token." },
            { "查看研究", "View Research" },
            { "仓库请填写为 owner/repo，例如 yourname/research-data。", "Repository must be owner/repo, e.g. yourname/research-data." },
            { "请填写 GitHub 访问密钥（fine-grained PAT）。", "Enter a GitHub access token (fine-grained PAT)." },
            { "请先在“团队同步”页填写仓库并保存配置。", "Configure the repository on the Team Sync page first." },
            { "暂无改动记录。", "No change log yet." },
            { "文档已创建，是否现在打开？", "Document created. Open it now?" },
            { "请填写类别名称。", "Please enter a category name." },
            { "未选择工作文件夹，程序将退出。", "No workspace folder selected. The program will exit." },
            { "确定删除这条记录吗？该操作不可撤销。", "Delete this entry? This cannot be undone." },
            { "找不到该文件。", "File not found." },
            { "研究进展记录器已经打开。请切换到现有窗口，避免同时修改研究数据。", "Research Progress Recorder is already running. Switch to the existing window." },
            { "文件不存在", "File Not Found" },
            { "删除类别", "Delete Category" },
            { "移除记录", "Remove Record" },
            { "提示", "Notice" },
            { "需要工作文件夹", "Workspace Required" },
            { "错误", "Error" },
            { "研究进展记录器 · Synchrotron Radiation Project 2027", "Research Progress Recorder · Synchrotron Radiation Project 2027" },
            { "项目概览", "Project Overview" },
            { "团队研究", "Team Research" },
            { "个人研究", "Personal Research" },
            { "选择个人文件夹", "Choose Personal Folder" },
            { "打开个人文件夹", "Open Personal Folder" },
            { "未选择个人文件夹：个人研究上传将失败。", "No personal folder selected: personal uploads will fail." },
            { "团队同步（GitHub 仓库）", "Team Sync (GitHub)" },
            { "上传参考资料", "Upload References" },
            { "上传个人研究", "Upload My Research" },
            { "参考文献与资料", "References & Materials" },
            { "本地进度", "Local Progress" },
            { "团队同步", "Team Sync" },
            { "使用说明", "Help" },
            { "研究日志", "Research Log" },
            { "研究类别", "Categories" },
            { "本地文档", "Local Docs" },
            { "本地资料", "Local Materials" },
            { "参考资料", "References" },
            { "打开工作文件夹", "Open Workspace Folder" },
            { "更换工作文件夹", "Change Workspace Folder" },
            { "删除全部类别", "Delete All Categories" },
            { "删除此类别", "Delete This Category" },
            { "删除此记录", "Delete This Entry" },
            { "删除文档", "Delete Document" },
            { "不能删除工作文件夹本身。", "The workspace folder itself cannot be deleted." },
            { "取消", "Cancel" },
            { "确定", "OK" },
            { "＋  新建记录", "＋ New Entry" },
            { "＋ 添加研究类别", "＋ Add Category" },
            { "＋ 添加类别", "＋ Add Category" },
            { "研究内容 / 备注", "Notes" },
            { "研究类别分布", "Category Distribution" },
            { "上次保存：", "Last saved: " },
            { "已保存 ", "Saved " },
            { "自动保存", "Auto-save" },
            { "上传失败，详见日志。", "Upload failed. See log." },
            { "个人研究上传完成。", "Personal research uploaded." },
            { "参考资料上传完成。", "References uploaded." },
            { "同步失败，详见日志。", "Sync failed. See log." },
            { "同步完成。", "Sync complete." },
            { "拉取失败，详见日志。", "Pull failed. See log." },
            { "正在上传个人研究信息…", "Uploading personal research…" },
            { "正在上传参考资料…", "Uploading references…" },
            { "正在从 GitHub 读取…", "Reading from GitHub…" },
            { "正在同步…", "Syncing…" },
            { "正在读取团队成员…", "Reading team members…" },
            { "正在连接 GitHub…", "Connecting to GitHub…" },
            { "请先在“团队同步”页配置仓库和访问密钥。", "Configure repository and token on the Team Sync page first." },
            { "还没有记录。切换到“研究日志”开始第一条。", "No entries yet. Open Research Log to add the first one." },
            { "远程文件不存在。", "Remote file does not exist." },
            { "连接失败，详见日志。", "Connection failed. See log." },
            { "配置已保存；密钥存放在 Windows 凭据管理器。", "Configuration saved; token is stored in Windows Credential Manager." },
            { "类别可自行添加、编辑、删除，并可设置任务数量、预计时间与相关材料。", "Categories can be added, edited or deleted; set tasks, estimated time and materials." },
            { "添加研究类别", "Add Category" },
            { "编辑研究类别", "Edit Category" },
            { "当前没有类别。", "No categories." },
            { "工作文件夹", "Workspace Folder" },
            { "＋ 新建记录", "＋ New Entry" },
            { "保存", "Save" },
            { "删除记录", "Delete Entry" },
            { "添加文件", "Add File" },
            { "添加文件夹", "Add Folder" },
            { "定位", "Locate" },
            { "打开", "Open" },
            { "移除", "Remove" },
            { "改动记录", "Change Log" },
            { "选择文件夹", "Choose Folder" },
            { "补录文件", "Add Files" },
            { "保存配置", "Save Config" },
            { "测试连接", "Test Connection" },
            { "从 GitHub 合并", "Pull & Merge" },
            { "合并后上传", "Merge & Upload" },
            { "一键同步", "Sync All" },
            { "新建文档", "New Document" },
            { "打开文档文件夹", "Open Docs Folder" },
            { "归入所选类别", "Assign to Category" },
            { "刷新", "Refresh" },
            { "编辑", "Edit" },
            { "删除", "Delete" },
            { "资料库", "Library" },
            { "最近更新", "Recent Updates" },
            { "各类别进度", "Category Progress" },
            { "总体进度", "Overall Progress" },
            { "任务完成", "Tasks Done" },
            { "预计总工时", "Est. Hours" },
            { "搜索标题或内容", "Search title or content" },
            { "研究内容/备注", "Notes" },
            { "日期", "Date" },
            { "类别", "Category" },
            { "状态", "Status" },
            { "进度", "Progress" },
            { "项目名称", "Project" },
            { "归属类别", "Category" },
            { "文档标题（可修改）", "Title (editable)" },
            { "类别名称（固定）", "Category (fixed)" },
            { "任务总数", "Total tasks" },
            { "已完成", "Done" },
            { "预计时间（小时）", "Est. hours" },
            { "同步内容：", "Sync:" },
            { "点击卡片即可打开对应的书籍、文档或文件夹", "Click a card to open the file or folder." },
            { "每个类别可以设置任务数量、预计时间与相关材料。", "Set task counts, estimated time and materials for each category." },
            { "每位成员加入仓库后，程序用其 GitHub 账号自动建立个人档案文件夹（team/<用户名>/）。", "Each member gets a personal folder (team/<login>/) created automatically from their GitHub account." },
            { "文档保存在工作文件夹内的“本地文档”文件夹，命名为“日期 + 项目名称”，并归入所选研究类别。", "Documents are saved in the workspace folder as 'date + project' and linked to the selected category." },
            { "本地资料：选定一个文件夹作为参考，程序读取其中所有文件；未选定则为空。", "Local materials: choose a reference folder; all files in it are listed." },
            { "记录通过本程序创建的文档；可直接打开、归入类别或从清单移除（不会删除文件）。", "Documents created by this app; open, assign to a category, or delete to Recycle Bin." },
            { "每位成员使用自己的 GitHub 账号和访问密钥。进度记录、类别及相关材料链接会同步；本地文件和文档本身不会上传。", "Each member uses their own GitHub account and token. Progress, categories and material links sync; local files are not uploaded." },
            { "记录", "Entries" },
            { "文档清单", "Documents" },
            { "同步日志", "Sync Log" },
            { "新建记录", "New Entry" },
            { "还没有类别。到“研究类别”页添加。", "No categories yet." },
            { "文档保存位置：", "Documents saved in: " },
            { "提示：仓库需为私有仓库；每人使用自己的令牌。个人仓库的协作者目前需使用 classic PAT（repo 权限）。",
              "Note: use a private repository; each member uses their own token." },
            { "已配置 Google API（密钥保存在 Windows 凭据管理器）。首次创建会打开浏览器要求授权（只授予访问本应用自己创建的 Drive 文件）。",
              "Google API configured." },
        };

        private static readonly string[,] subs = new string[,]
        {
            { "已完成 ", "Done " },
            { " 项任务", " tasks" },
            { "预计 ", "Est. " },
            { " 小时", " h" },
            { "进度 ", "Progress " },
            { "未分类", "Uncategorized" },
            { "暂无", "None" },
            { "删除全部 ", "Delete all " },
            { " 个类别？记录会变为未归类，且不可恢复。", " categories? Entries become uncategorized; this cannot be undone." },
            { "找不到文件：", "File not found: " },
            { "是否重新定位？（其他人同步过来的路径在你的电脑上可能不存在）", "Relocate? (Paths synced from others may not exist on your computer.)" },
            { "是否重新定位？", "Relocate?" },
            { "删除类别“", "Delete category \"" },
            { "”？该类别下的记录不会被删除，会变为未归类。", "\"? Its records will not be deleted; they become uncategorized." },
            { "从清单移除“", "Remove \"" },
            { "”？（不会删除本地文件）", "\" from the list? (The local file is not deleted.)" },
            { "从资料库中移除“", "Remove \"" },
            { "”？\r\n（不会删除原文件）", "\" from the library? (The original file is not deleted.)" },
            { "创建失败：", "Create failed: " },
            { "无法打开链接：", "Cannot open link: " },
            { "无法打开文件：", "Cannot open file: " },
            { "无法打开：", "Cannot open: " },
            { "保存失败：", "Save failed: " },
            { "读取改动记录失败：", "Failed to read change log: " },
            { "找不到该文件：", "File not found: " },
            { "它可能已被移动或删除。可以移除这张卡片后重新添加。", "It may have been moved or deleted. Remove the card and add it again." },
            { "（不会删除原文件）", "(the original file is not deleted)" },
            { "”？\r\n这会同时从磁盘删除该文件或文件夹，且不可恢复。", "\"? The file or folder will also be deleted from disk. This cannot be undone." },
            { "”？\r\n这会同时从磁盘删除该文档，且不可恢复。", "\"? The document will also be deleted from disk. This cannot be undone." },
            { "删除“", "Delete \"" },
            { "删除失败：", "Delete failed: " },
            { "已创建：", "Created: " },
            { "连接成功：", "Connected: " },
            { "读取失败：", "Read failed: " },
            { "合并完成，新增/更新 ", "Merged, added/updated " },
            { "共 ", "Total " },
            { " 位成员", " members" },
            { "已把“", "Assigned “" },
            { "”归入类别“", "” to category “" },
        };

        private static readonly Dictionary<string, string> rev = BuildReverse();
        private static readonly string[,] revSubs = new string[,]
        {
            { "Done ", "已完成 " },
            { " tasks", " 项任务" },
            { "Est. ", "预计 " },
            { " h", " 小时" },
            { "Progress ", "进度 " },
            { "Uncategorized", "未分类" },
            { "None", "暂无" },
        };

        private static Dictionary<string, string> BuildReverse()
        {
            Dictionary<string, string> r = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> kv in exact) if (!r.ContainsKey(kv.Value)) r[kv.Value] = kv.Key;
            return r;
        }

        public static string T(string s)
        {
            if (s == null) return s;
            string v;
            if (En)
            {
                if (exact.TryGetValue(s, out v)) return v;
                for (int i = 0; i < subs.GetLength(0); i++)
                    if (s.IndexOf(subs[i, 0], StringComparison.Ordinal) >= 0)
                        s = s.Replace(subs[i, 0], subs[i, 1]);
                return s;
            }
            if (rev.TryGetValue(s, out v)) return v;
            for (int i = 0; i < revSubs.GetLength(0); i++)
                if (s.IndexOf(revSubs[i, 0], StringComparison.Ordinal) >= 0)
                    s = s.Replace(revSubs[i, 0], revSubs[i, 1]);
            return s;
        }

        public static string HelpText()
        {
            if (!En) return HelpZh;
            return HelpEn;
        }



        private const string HelpZh =
            "使用说明\r\n\r\n" +
            "一、界面与语言\r\n" +
            "· 右上角“EN / 中文”即时切换界面语言，选择保存在程序目录 lang.txt。\r\n" +
            "· “更换工作文件夹”重新选择工作目录；“打开工作文件夹”在资源管理器中打开当前目录。\r\n\r\n" +
            "二、项目概览\r\n" +
            "· 汇总总体进度、任务完成数、预计工时、各类别进度与最近更新。\r\n" +
            "· 点类别名称直接编辑；点“最近更新”里的条目跳到研究日志。\r\n\r\n" +
            "三、团队研究\r\n" +
            "· 列出仓库 team/ 下的成员；每位成员首次打开本页时，程序按其 GitHub 账号自动创建 team/<用户名>/profile.json。\r\n" +
            "· “合并团队参考资料”读取各成员的 references.json，按路径去重后并入本地，不修改他人文件。未选择个人文件夹时，个人研究上传会失败。\r\n\r\n" +
            "四、参考文献与资料\r\n" +
            "· 两个子栏：参考资料、研究类别。\r\n" +
            "· 资料卡片点击即打开文件/文件夹；文件缺失时提示重新定位。\r\n" +
            "· 可“添加文件/添加文件夹”，也可把文件拖入资料卡区域；移除会同时删除磁盘文件（有确认）。\r\n" +
            "· 研究类别可添加、编辑、删除，支持任务数、预计工时与任意类型附件；删除类别只把所属记录改为未归类。\r\n\r\n" +
            "五、本地进度\r\n" +
            "· 三个子栏：研究日志、本地文档、本地资料。\r\n" +
            "· 研究日志：新建、编辑标题/日期/类别/状态/进度/正文，自动保存，可搜索与删除记录。\r\n" +
            "· 本地文档：填写项目名称与归属类别，在工作文件夹内新建“日期 + 项目名称”.docx；删除记录会同时删除该文件。\r\n" +
            "· 本地资料：选择参考文件夹后列出其中文件，双击打开；“补录文件”会把选中的文件复制进该文件夹。\r\n\r\n" +
            "六、团队同步\r\n" +
            "· 填写 owner/repo、文件路径、分支与访问密钥；密钥保存在 Windows 凭据管理器，不写入项目文件。\r\n" +
            "· 一键同步：先合并远端较新的记录与类别，再上传；可勾选记录/类别；未勾选的远端数据保持原样，记录删除会同步。\r\n" +
            "· 上传参考资料：仅写入 team/<你的用户名>/references.json；请先在团队研究页选择本机个人文件夹。\r\n" +
            "· 上传个人研究：写入 team/<你的用户名>/research.json 与 documents.json，不会覆盖他人。\r\n" +
            "· 改动记录：读取仓库 sync/history.json，显示时间、用户与同步范围。\r\n\r\n" +
            "七、常见问题\r\n" +
            "· 找不到文件：可重新定位或移除；他人同步来的路径可能不存在。\r\n" +
            "· 同步冲突：记录与类别以较新的 Updated 为准；应用按账号选择上传目录；仓库写权限仍允许通过 GitHub 修改其他目录。\r\n" +
            "· 访问密钥：使用个人 GitHub PAT，不要分享；泄露后立即在 GitHub 撤销。\r\n";

        private const string HelpEn =
            "Help\r\n\r\n" +
            "1. Interface and language\r\n" +
            "· Use EN / 中文 at the top right to switch instantly; the choice is stored in lang.txt.\r\n" +
            "· Change Workspace Folder re-selects the working directory; Open Workspace Folder opens it in Explorer.\r\n\r\n" +
            "2. Project Overview\r\n" +
            "· Shows overall progress, completed tasks, estimated hours, category progress and recent updates.\r\n" +
            "· Click a category name to edit it; click a recent item to jump to the Research Log.\r\n\r\n" +
            "3. Team Info\r\n" +
            "· Lists members under team/; each member gets team/<login>/profile.json created automatically on first open.\r\n" +
            "· Merge Team References reads every member's references.json and merges by path without modifying others' files. Personal uploads fail until a personal folder is chosen.\r\n\r\n" +
            "4. References & Materials\r\n" +
            "· Two sub-tabs: References and Categories.\r\n" +
            "· Click a card to open the file or folder; if missing, you are prompted to relocate it.\r\n" +
            "· Use Add File / Add Folder, or drag files in; Remove also deletes the file from disk after confirmation.\r\n" +
            "· Categories can be added, edited or deleted, with task counts, estimated hours and any type of attachments; deleting a category only marks its entries as uncategorized.\r\n\r\n" +
            "5. Local Progress\r\n" +
            "· Three sub-tabs: Research Log, Local Docs, Local Materials.\r\n" +
            "· Research Log: create and edit entries (title, date, category, status, progress, notes), auto-save, search and delete.\r\n" +
            "· Local Docs: enter a project name and category to create 'date + project'.docx in the workspace; deleting a record also deletes the file.\r\n" +
            "· Local Materials: choose a reference folder to list its files (double-click to open); Add Files copies selected files into that folder.\r\n\r\n" +
            "6. Team Sync\r\n" +
            "· Enter owner/repo, file path, branch and access token; the token is stored in Windows Credential Manager, never in project files.\r\n" +
            "· Sync All merges newer records and categories first, then uploads selected records/categories; unselected remote data is preserved and entry deletions propagate.\r\n" +
            "· Upload References writes only to team/<your login>/references.json, choose a local personal folder first.\r\n" +
            "· Upload My Research writes team/<your login>/research.json and documents.json without overwriting others.\r\n" +
            "· Change Log reads sync/history.json and shows time, user and scope.\r\n\r\n" +
            "7. FAQ\r\n" +
            "· Missing file: relocate or remove it; paths synced from others may not exist locally.\r\n" +
            "· Conflicts: records and categories use the newer Updated value; the app chooses your own folder; GitHub repository writers can still modify other folders.\r\n" +
            "· Token: use your own GitHub PAT and never share it; revoke it on GitHub if leaked.\r\n";

        public static void Apply(Control root)
        {
            if (root == null) return;
            TextBox tb = root as TextBox;
            if (tb != null && tb.Name == "helpText") { tb.Text = HelpText(); return; }
            Form f = root as Form;
            if (f != null) TranslateControl(f);
            Label l = root as Label;
            if (l != null) TranslateControl(l);
            Button b = root as Button;
            if (b != null) TranslateControl(b);
            CheckBox c = root as CheckBox;
            if (c != null) TranslateControl(c);
            LinkLabel ll = root as LinkLabel;
            if (ll != null) TranslateControl(ll);
            GroupBox g = root as GroupBox;
            if (g != null) TranslateControl(g);
            foreach (Control child in root.Controls) Apply(child);
        }

        private class Translation { public string Original; public string Rendered; }
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Control, Translation> originals = new System.Runtime.CompilerServices.ConditionalWeakTable<Control, Translation>();
        private static void TranslateControl(Control c)
        {
            Translation item = originals.GetOrCreateValue(c);
            if (item.Original == null || c.Text != item.Rendered) item.Original = c.Text;
            item.Rendered = En ? T(item.Original) : item.Original;
            c.Text = item.Rendered;
        }

        public static void ApplyAll()
        {
            foreach (Form f in Application.OpenForms) Apply(f);
        }

        public static void Load(string appDir)
        {
            try
            {
                string p = Path.Combine(appDir, "lang.txt");
                if (File.Exists(p)) En = File.ReadAllText(p, Encoding.UTF8).Trim() == "en";
            }
            catch { }
        }

        public static void Save(string appDir)
        {
            try { File.WriteAllText(Path.Combine(appDir, "lang.txt"), En ? "en" : "zh", new UTF8Encoding(false)); }
            catch { }
        }
    }
}
