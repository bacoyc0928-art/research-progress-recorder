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
            { "验证并保存配置", "Validate and Save" },
            { "从 GitHub 下载他人内容", "Download team files" },
            { "从 GitHub 下载默认资料库", "Download default library" },
            { "上传本地进度", "Upload progress" },
            { "撤除我上传的参考资料", "Remove my refs" },
            { "撤除所选", "Remove Selected" },
            { "撤除参考资料", "Remove References" },
            { "登录", "Sign In" },
            { "退出", "Sign Out" },
            { "未登录", "Not signed in" },
            { "上传到 GitHub", "Upload to GitHub" },
            { "读取云端版本", "Load From GitHub" },
            { "恢复默认", "Restore Default" },
            { "改动日志", "Change Log" },
            { "快速发行", "Quick Post" },
            { "发布", "Publish" },
            { "历史记录", "History" },
            { "已阅", "Mark Read" },
            { "显示", "Show" },
            { "隐藏", "Hide" },
            { "已登录 GitHub", "Signed in with GitHub" },
            { "本地进度文件夹", "Local Progress Folder" },
            { "打开本地资料文件夹", "Open Progress Folder" },
            { "所有本地进度内容都存放在这个文件夹；未选择时无法进入“团队同步”。", "Create documents here, then upload this folder from Team Sync." },
            { "每位成员使用自己的 GitHub 账号和访问密钥。可下载成员文件或默认资料库，并上传本人选择的参考文件、本地进度文件夹；每个文件最大 50 MiB。", "Use your own GitHub account/token. Download team/default files; upload selected references or your progress folder (50 MiB per file)." },
            { "成员内容下载到 References/<账号>/TeamConRef/。", "Team files go to References/<login>/TeamConRef/。" },
            { "已连接 GitHub 账号", "Connected to GitHub account" },
            { "团队成员", "Team member" },
            { "请先在左下角登录 GitHub 账号。", "Sign in at the bottom-left first." },
            { "写下要告诉全组的信息；发布后所有人登录即可看到，已阅的帖子会收进右上角“历史记录”。", "Write a note for the whole team; once posted everyone signed in can read it. Read posts move to History." },
            { "查看活动日志", "Activity log" },
            { "登录后在团队同步页操作即可；仓库已绑定，无需填写任何配置。", "Operate from Team Sync after signing in; the repository is already bound." },
            { "项目成员", "Project member" },
            { "协作者（尚未上传）", "Collaborator (no upload yet)" },
            { "研究进展记录器", "Research Progress Recorder" },
            { "研究进展记录器 · Synchrotron Radiation Project 2027 · 0.0.2", "Research Progress Recorder · Synchrotron Radiation Project 2027 · 0.0.2" },
            { "文档可打开或归入类别；移除会将磁盘文件移入回收站。", "Open or categorize documents; Remove moves the file to Recycle Bin." },
            { "仓库（owner/repo）", "Repository (owner/repo)" },
            { "文件路径", "File path" },
            { "分支", "Branch" },
            { "类别名称", "Category name" },
            { "访问密钥（每个成员使用自己的令牌）", "Access token (each member uses their own)" },
            { "外部协作者请用 classic PAT 并勾选 repo；fine-grained 令牌无法访问他人账号的私有仓库。", "External collaborators need a classic PAT with the repo scope; fine-grained tokens cannot reach another account's private repository." },
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
            { "个人研究", "Personal" },
            { "选择个人文件夹", "Choose folder" },
            { "打开个人文件夹", "Open folder" },
            { "团队同步（GitHub 仓库）", "Team Sync (GitHub)" },
            { "上传参考资料", "Upload refs" },
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
            { "打开工作文件夹", "Open folder" },
            { "更换工作文件夹", "Change folder" },
            { "下载并合并团队参考资料", "Merge refs" },
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

        /// <summary>界面统一中英双语并显：中文原文 + " / " + 英文。已含 " / " 的字符串原样返回，避免重复叠加。</summary>
        public static string T(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (s == "Quick Post") return s;   // 该栏目名按用户要求只保留英文
            if (s.IndexOf(" / ", StringComparison.Ordinal) >= 0) return s;
            string other;
            if (exact.TryGetValue(s, out other)) return s + " / " + other;
            if (rev.TryGetValue(s, out other)) return other + " / " + s;
            string original = s;
            for (int i = 0; i < subs.GetLength(0); i++)
                if (s.IndexOf(subs[i, 0], StringComparison.Ordinal) >= 0)
                    s = s.Replace(subs[i, 0], subs[i, 0] + " / " + subs[i, 1]);
            if (!string.Equals(s, original, StringComparison.Ordinal)) return s;
            for (int i = 0; i < revSubs.GetLength(0); i++)
                if (s.IndexOf(revSubs[i, 0], StringComparison.Ordinal) >= 0)
                    s = s.Replace(revSubs[i, 0], revSubs[i, 1] + " / " + revSubs[i, 0]);
            return s;
        }

        public static string HelpText()
        {
            return HelpBoth;
        }



        private const string HelpBoth = "SRPRecorder2027 · 0.2.0 · 中英双语界面 / Bilingual UI\r\n\r\n" +
            "1. 登录 / Sign in\r\n" +
            "在窗口左下角粘贴自己的 GitHub 访问密钥，点“登录 / Sign In”。令牌只保存在内存，每次启动都要重新登录；未登录时不能进入参考文献页，也不能发起任何同步、发帖或改动。\r\n" +
            "Paste your own GitHub token at the bottom-left of the window and press Sign In. The token stays in memory only, so every launch needs a fresh sign-in. While signed out you cannot open References or start any sync, post or edit.\r\n\r\n" +
            "2. 团队研究 / Team Research\r\n" +
            "登录后刷新即可看到全部项目成员；点“打开文件夹 / Open Folder”查看已下载的成员内容；自己的卡片上可撤除参考资料。\r\n" +
            "Refresh after signing in to list every project member. Open Folder shows the downloaded member files; your own card can remove your uploaded references.\r\n\r\n" +
            "3. 参考资料与本地进度 / References and Local Progress\r\n" +
            "References/Default 是团队默认资料库；本地进度请先选一个专用文件夹，可新建 Word 或直接编辑其中的文件。\r\n" +
            "References/Default is the team library. Pick a dedicated progress folder first, then create Word files or edit existing ones.\r\n\r\n" +
            "4. 团队同步 / Team Sync（已绑定 / bound to bacoyc0928-art/research-data · main）\r\n" +
            "五个动作：下载他人内容、下载默认资料库、上传参考资料、上传本地进度、撤除我上传的参考资料。每个动作都会先列出可下载或可上传的项目/文件，勾选后才会执行，不会整批覆盖。单个文件最大 50 MiB；下载先暂存，成功后替换并保留旧副本；上传不会自动删除远端旧文件。\r\n" +
            "Five actions: Download Team Files, Download Default Library, Upload References, Upload Local Progress, Remove My Uploaded References. Each one first lists the items or files that can be transferred, and only the checked ones are processed. 50 MiB per file; downloads stage first and keep a backup; uploads never delete remote older files.\r\n\r\n" +
            "5. 活动日志 / Activity log\r\n" +
            "登录、上传、下载、撤除、发帖、删帖、改使用说明都会写入 logs/public/activity.json（全员可见）与 logs/private/<账号>/activity.json（本人明细）。\r\n" +
            "Sign-in, uploads, downloads, removals, posts and help edits are written to logs/public/activity.json (visible to all) and logs/private/<login>/activity.json (your own detail).\r\n\r\n" +
            "6. 本页 / This help page\r\n" +
            "本页可自由编辑；点“上传到 GitHub / Upload to GitHub”写入 help/help.md，所有成员读到同一份，并在 help/changelog.json 留下改动日志。“恢复默认 / Restore Default”回到自带文本，“读取云端版本 / Load From GitHub”重新拉取。\r\n" +
            "Edit this page freely, then Upload to GitHub to store help/help.md for everyone; each upload adds an entry to help/changelog.json. Restore Default returns to the built-in text, Load From GitHub re-fetches the cloud copy.";

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
            item.Rendered = T(item.Original);
            c.Text = item.Rendered;
        }

        public static void ApplyAll()
        {
            foreach (Form f in Application.OpenForms) Apply(f);
        }

        /// <summary>界面固定中英并显，不再读取/保存语言偏好；保留方法仅为兼容旧调用。</summary>
        public static void Load(string appDir) { En = false; }

        public static void Save(string appDir) { En = false; }
    }
}
