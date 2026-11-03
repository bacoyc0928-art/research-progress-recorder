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
            { "上传本地记录", "Upload progress" },
            { "撤除我上传的参考资料", "Remove my refs" },
            { "撤除所选", "Remove Selected" },
            { "撤除参考资料", "Remove References" },
            { "登录", "Sign In" },
            { "退出", "Sign Out" },
            { "未登录", "Not signed in" },
            { "上传到 GitHub", "Upload" },
            { "读取云端版本", "Reload" },
            { "恢复默认", "Restore Default" },
            { "改动日志", "Change Log" },
            { "快速发行", "Quick Post" },
            { "发布", "Publish" },
            { "历史记录", "History" },
            { "已阅", "Mark Read" },
            { "显示", "Show" },
            { "隐藏", "Hide" },
            { "已登录 GitHub", "Signed in with GitHub" },
            { "本地记录文件夹", "Local Records Folder" },
            { "打开本地资料文件夹", "Open Progress Folder" },
            { "所有本地记录内容都存放在这个文件夹；未选择时无法进入“进度同步”。", "Create documents here, then upload this folder from Progress Sync." },
            { "每位成员使用自己的 GitHub 账号和访问密钥。可下载成员文件或默认资料库，并上传本人选择的参考文件、本地记录文件夹；每个文件最大 50 MiB。", "Use your own GitHub account/token. Download team/default files; upload selected references or your progress folder (50 MiB per file)." },
            { "他人内容下载到 PersonalWorkspaceSRRec\\Public_Temp（退出即清空）。", "Others' files go to PersonalWorkspaceSRRec\\Public_Temp (cleared on exit)." },
            { "已连接 GitHub 账号", "Connected to GitHub account" },
            { "团队成员", "Team member" },
            { "请先在左下角登录 GitHub 账号。", "Sign in at the bottom-left first." },
            { "写下要告诉全组的信息；发布后所有人登录即可看到，已阅的帖子会收进右上角“历史记录”。", "Write a note for the whole team; once posted everyone signed in can read it. Read posts move to History." },
            { "查看活动日志", "Activity log" },
            { "登录后在进度同步页操作即可；仓库已绑定，无需填写任何配置。", "Operate from Progress Sync after signing in; the repository is already bound." },
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
            { "请先在“进度同步”页填写仓库并保存配置。", "Configure the repository on the Progress Sync page first." },
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
            { "成员资料", "Members" },
            { "我的参考资料", "My References" },
            { "打开 Reference", "Open Reference" },
            { "（未登录）", "(Not signed in)" },
            { "个人研究", "Personal" },
            { "选择个人文件夹", "Choose folder" },
            { "打开个人文件夹", "Open folder" },
            { "进度同步（GitHub 仓库）", "Progress Sync (GitHub)" },
            { "上传参考资料", "Upload refs" },
            { "上传个人研究", "Upload My Research" },
            { "参考文献与资料", "References & Materials" },
            { "本地记录", "Local Records" },
            { "进度同步", "Progress Sync" },
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
            { "请先在“进度同步”页配置仓库和访问密钥。", "Configure repository and token on the Progress Sync page first." },
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
            if (s == "Quick Post") return s;
            string other;
            if (!En)
            {
                if (exact.ContainsKey(s)) return s;
                int zh = s.IndexOf(" / ", StringComparison.Ordinal);
                return zh > 0 ? s.Substring(0, zh) : s;
            }
            if (exact.TryGetValue(s, out other)) return other;
            int en = s.IndexOf(" / ", StringComparison.Ordinal);
            if (en > 0) return s.Substring(en + 3);
            return s;
        }

        public static string HelpText()
        {
            return HelpBoth;
        }



        private const string HelpBoth = "SRPRecorder2027 · 0.2.10 · 中英双语界面 / Bilingual UI\r\n" +
            "数据架构与使用说明 / Data architecture and usage guide\r\n" +
            "\r\n" +
            "【0】总览 / Overview\r\n" +
            "本程序没有自建服务器：团队数据全部存放在私有 GitHub 仓库 bacoyc0928-art/research-data，本机只保留工作副本、缓存与配置；程序源码、安装包和文档在公开仓库 research-progress-recorder，下载不需要登录。\r\n" +
            "There is no self-hosted server: all team data lives in the private GitHub repository bacoyc0928-art/research-data; each machine keeps only working copies, caches and settings. Source, installers and docs live in the public repository research-progress-recorder and need no sign-in.\r\n" +
            "\r\n" +
            "  本机 Local  ⇄  api.github.com  ⇄  私有仓库 research-data  ⇄  其他成员 other members\r\n" +
            "                        └─ 公开仓库 research-progress-recorder：只放程序与说明 / program and docs only\r\n" +
            "\r\n" +
            "同步是手动的：上传＝把勾选的文件复制到远端；下载＝把远端文件复制到本机（临时区或默认库）。没有后台实时同步，也没有自动冲突合并。\r\n" +
            "Sync is manual: upload copies checked files to the remote; download copies remote files to this machine (temp area or default library). There is no background sync and no automatic conflict merging.\r\n" +
            "第一次使用：左下角登录（可勾选“记住登录”，设置 6 位 PIN）→ 成员资料刷新成员 → 本地记录选择专用文件夹 → 进度同步按需上传/下载。\r\n" +
            "First run: sign in at the bottom-left (optionally tick Remember and set a 6-digit PIN) → refresh members on Members → pick a dedicated folder on Local Records → upload/download from Progress Sync.\r\n" +
            "\r\n" +
            "【1】Windows 本机目录 / Windows local layout\r\n" +
            "SRCRec\\                            程序与数据根目录（exe 所在处，可整体搬移）\r\n" +
            "├─ SRPRecorder2027.exe             程序 0.2.10\r\n" +
            "├─ SRPRecorder2027.exe.config      高 DPI / 长路径配置，必须随 exe 保留\r\n" +
            "├─ research_log.json               旧版研究记录\r\n" +
            "├─ workspace.json                  工作区路径\r\n" +
            "├─ progress.json                   进度文件夹路径（改过文件夹才有）\r\n" +
            "├─ sync_config.json                绑定仓库信息（owner/repo/branch，无令牌）\r\n" +
            "├─ news.json                       各栏目未读红点状态\r\n" +
            "├─ data_version.txt                一次性迁移标记，固定 0.2.0\r\n" +
            "├─ upgrade_cleanup.log             迁移删除记录\r\n" +
            "├─ help_local.md                   使用说明本机草稿\r\n" +
            "├─ References\\\r\n" +
            "│  └─ Default\\                    团队默认资料库镜像（只读）\r\n" +
            "└─ PersonalWorkspaceSRRec\\        默认本地记录文件夹\r\n" +
            "   ├─ Mat_Local\\                  个人非参考资料（上传本地记录的默认来源）\r\n" +
            "   ├─ Reference\\                  个人参考资料（上传参考资料的默认来源）\r\n" +
            "   ├─ Public_Temp\\                他人内容临时区（启动清空、退出清空）\r\n" +
            "   └─ references.json             上传后缓存的资料清单\r\n" +
            "SRCRec\\ is the program and data root next to the exe; the whole folder is portable. References\\Default is the read-only team library. PersonalWorkspaceSRRec is the default progress workspace: Mat_Local for non-reference files (default progress-upload source), Reference for personal references (default reference-upload source), Public_Temp for other members' files (cleared on start and exit).\r\n" +
            "\r\n" +
            "【2】macOS 本机目录 / macOS local layout\r\n" +
            "/Applications/ResearchProgressRecorder.app        只放程序 / program only\r\n" +
            "~/Library/Application Support/ResearchProgressRecorder/\r\n" +
            "├─ config.json                     进度文件夹、登录名、未读状态\r\n" +
            "├─ research_log.json               旧版研究记录\r\n" +
            "├─ data_version.txt                迁移标记 0.2.0\r\n" +
            "├─ upgrade_cleanup.log\r\n" +
            "└─ SRCRec\\\r\n" +
            "   ├─ References\\Default\\          团队默认资料库\r\n" +
            "   └─ PersonalWorkspaceSRRec\\\r\n" +
            "      ├─ Mat_Local\\                个人非参考资料\r\n" +
            "      ├─ Reference\\                个人参考资料（上传副本直接放在 Reference\\）\r\n" +
            "      └─ Public_Temp\\              他人内容（启动/退出清空）\r\n" +
            "macOS keeps user data in Application Support; the app bundle holds only the program. The SRCRec subfolder mirrors the Windows layout with the same three workspace folders (uploaded reference copies are stored directly in Reference\\ on both platforms).\r\n" +
            "\r\n" +
            "【3】私有仓库 research-data / Private team repository\r\n" +
            "team\\<账号>\\\r\n" +
            "├─ references\\<文件>               上传的参考资料本体\r\n" +
            "├─ references.json                 该账号的资料清单\r\n" +
            "├─ progress\\<相对路径>             本地记录文件夹的镜像\r\n" +
            "└─ profile.json                    首次上传时自动创建\r\n" +
            "default\\<文件>                     团队默认资料库（成员只读）\r\n" +
            "logs\\public\\activity.json          公开活动日志（≤2000 条，全员可见）\r\n" +
            "logs\\private\\<账号>\\activity.json  本人明细（≤500 条）\r\n" +
            "help\\help.md                       全员共享的使用说明（本页）\r\n" +
            "help\\changelog.json                说明改动日志（≤200 条）\r\n" +
            "posts\\<UTC时间>-<账号>-<4位随机>.txt  Quick Post 帖子\r\n" +
            "posts\\read\\<账号>.json             该账号已阅的帖子 id\r\n" +
            "sync\\                              0.0.2 遗留，只读保留\r\n" +
            "team/<login>/references holds your uploaded files and references.json is their manifest; progress mirrors your progress folder; default is the team library; logs/public is shared and logs/private/<login> is yours; help and posts are shared; sync/ is 0.0.2 legacy and read-only.\r\n" +
            "\r\n" +
            "【4】同步动作与数据流向 / Sync actions and data flow\r\n" +
            "上传参考资料 Upload refs：Reference\\ → 勾选 → team\\<我>\\references\\ + references.json（同时在本机 Reference 留一份副本 / also keeps a local copy）\r\n" +
            "上传本地记录 Upload progress：Mat_Local\\ → 勾选 → team\\<我>\\progress\\<相对路径>（按进度文件夹的相对路径镜像 / mirrors the relative path）\r\n" +
            "下载他人内容 Download team：team\\<成员>\\ → Public_Temp\\<成员>\\（退出即清空 / cleared on exit）\r\n" +
            "下载默认库 Download default：default\\ → References\\Default\\（覆盖前保留 .bak-时间戳 / keeps a .bak copy）\r\n" +
            "撤除参考资料 Remove refs：只删本人上传的文件与清单条目，团队区立即不可下载；本机 Reference 中的副本需自行清理 / removes your own remote files and manifest entries only; delete local copies manually\r\n" +
            "Quick Post：发布 posts\\<id>.txt；已阅 posts\\read\\<我>.json；只能删自己的帖子 / publish, mark read, delete your own posts\r\n" +
            "活动日志 Activity log：登录、上传、下载、撤除、发帖、删帖、改说明 → logs\\public + logs\\private\\<我> / sign-in, uploads, downloads, removals, posts, help edits\r\n" +
            "\r\n" +
            "【5】清理、保留与升级 / Cleanup, retention and upgrade\r\n" +
            "- Public_Temp 每次启动和每次退出都会清空，只用于临时查看他人内容。\r\n" +
            "- References 下只保留 Default；References\\<账号>\\、References\\references\\、References\\references.json、personal.json 都是废弃路径，启动时自动删除。\r\n" +
            "- data_version.txt 固定 0.2.0：只有从更早版本首次升级时清理一次，0.2.10 不会重复清理。\r\n" +
            "- 升级不会删除本地记录文件夹、Reference 中的资料和下载缓存；默认库可登录后重新下载。\r\n" +
            "- 远端上传只新增或覆盖，不自动删除旧文件。\r\n" +
            "Cleanup rules: Public_Temp is cleared on every start and exit; under References only Default is kept, and the legacy paths References/<login>, References/references, References/references.json and personal.json are deleted at startup; data_version.txt stays 0.2.0 so the one-time cleanup never repeats; upgrades never delete your progress folder, Reference files or download cache, and the default library can be re-downloaded; remote uploads never delete older files.\r\n" +
            "\r\n" +
            "【6】限制与安全 / Limits and safety\r\n" +
            "- 单文件上限 50 MiB；不支持符号链接/目录联接；非法文件名会被拒绝。\r\n" +
            "- 未登录不能进入“参考文献与资料”，不能上传、下载、发帖、改说明，也不会写任何 JSON。\r\n" +
            "- 令牌默认只保存在内存，每次启动重新登录；勾选“记住登录”后，令牌存入系统凭据库（Windows 凭据管理器 / macOS 钥匙串），用 6 位 PIN 解锁，连续 5 次错误会清除。PIN 只是本机便捷锁，真正的保护是系统账户。\r\n" +
            "- 同一文件被两人先后上传时，后上传覆盖先上传，请按成员目录分工并备份。\r\n" +
            "Limits: 50 MiB per file; symlinks and invalid filenames are rejected; while signed out you cannot open References, upload, download, post or edit this page, and no JSON is written. The token lives only in memory by default; with Remember ticked it is stored in the system credential store (Windows Credential Manager / macOS Keychain) and unlocked with a 6-digit PIN after five wrong attempts clear it. The PIN is a local convenience lock; the real protection is your system account. If two people upload the same path the later upload wins, so divide work by member folder and keep backups.\r\n" +
            "\r\n" +
            "【7】本页 / This help page\r\n" +
            "本页可编辑：点“上传到 GitHub”写入 help\\help.md，全员登录后读到同一份，并在 help\\changelog.json 留下改动日志；“恢复默认”回到本文件，“读取云端版本”重新拉取。\r\n" +
            "Windows 左下侧栏（登录面板下方）与 macOS 窗口底部左侧的“检查更新 / Check updates”会从 GitHub 读取最新版本、按 SHA-256 校验后下载，重启时只替换程序文件；本地记录、参考资料与配置不会被删除。两处都显示当前版本号。\r\n" +
            "This page is editable: Upload to GitHub writes help\\help.md for everyone and appends help\\changelog.json; Restore Default returns to this built-in text; Load From GitHub re-fetches the cloud copy.\r\n" +
            "“Check updates” in the bottom-left of the sidebar on Windows and at the bottom-left of the window on macOS downloads the latest release from GitHub, verifies its SHA-256 and replaces only program files after restart; local progress, references and settings are not touched. Both show the current version.";

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
        public static void Load(string appDir)
        {
            try
            {
                string path = Path.Combine(appDir, "lang.txt");
                if (File.Exists(path)) En = File.ReadAllText(path).Trim().Equals("en", StringComparison.OrdinalIgnoreCase);
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
