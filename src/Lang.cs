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
            { "从团队获取", "Get from the team" },
            { "分享我的资料", "Share my files" },
            { "管理我的上传", "Manage my uploads" },
            { "下载到本机查看，不会改动团队资料。", "Downloads to this computer; team data is not changed." },
            { "把你勾选的文件上传到团队区你自己的文件夹；每个文件最大 50 MiB，只新增或覆盖。", "Uploads the files you tick to your own team folder; up to 50 MiB each, only adds or overwrites." },
            { "撤除只影响你自己上传的文件；活动日志记录每个人的操作。", "Removal affects only files you uploaded; the activity log shows everyone's actions." },
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



        private const string HelpBoth =
            "SRPRecorder2027 使用说明 / User Guide\r\n" +
            "适用版本 / Version 0.2.13 · 中文在前，English follows each section\r\n" +
            "\r\n" +
            "本程序帮助课题组记录和交换研究资料。团队资料保存在私有 GitHub 仓库 bacoyc0928-art/research-data；程序本身从公开仓库 research-progress-recorder 下载，不需要登录。没有服务器，也没有实时同步：上传和下载都由你手动发起。\r\n" +
            "The app helps the group record and exchange research files. Team data lives in the private GitHub repository bacoyc0928-art/research-data; the app is downloaded from the public repository research-progress-recorder without signing in. There is no server and no real-time sync: you start every upload and download yourself.\r\n" +
            "\r\n" +
            "\r\n" +
            "【1】第一次使用 / Getting started\r\n" +
            "\r\n" +
            "1. 接受邀请：用你的 GitHub 账号接受 research-data 仓库的协作邀请。\r\n" +
            "2. 创建令牌：GitHub → Settings → Developer settings → Personal access tokens → Tokens (classic)，勾选 repo，设置有效期。成员必须用 classic 令牌；fine-grained 令牌访问他人的私有仓库一律返回 404。\r\n" +
            "3. 登录：Windows 在窗口左下角，macOS 在窗口底部。粘贴令牌，点「登录」。\r\n" +
            "4. 可选「记住登录」：设置 6 位 PIN，令牌存入系统凭据库（Windows 凭据管理器 / macOS 钥匙串）。下次启动只输 PIN；连续 5 次输错会清除保存的令牌。\r\n" +
            "5. 「退出」会在确认后直接关闭程序。\r\n" +
            "\r\n" +
            "1. Accept the invitation to the research-data repository with your GitHub account.\r\n" +
            "2. Create a token: GitHub → Settings → Developer settings → Personal access tokens → Tokens (classic), tick repo and set an expiry. Members must use a classic token; fine-grained tokens always get 404 on another account's private repository.\r\n" +
            "3. Sign in at the bottom-left (Windows) or bottom bar (macOS): paste the token and press Sign In.\r\n" +
            "4. Optional \"Remember\": set a 6-digit PIN and the token is stored in the system credential store (Windows Credential Manager / macOS Keychain). Next time you only enter the PIN; five wrong PINs clear the saved token.\r\n" +
            "5. \"Sign Out\" asks for confirmation and then closes the app.\r\n" +
            "\r\n" +
            "未登录时：同步、发帖、使用说明工具栏的按钮都是灰色的，参考资料页不能进入，程序不写任何配置。\r\n" +
            "While signed out, sync, posting and Help toolbar buttons are greyed out, the References page is locked and the app writes no settings.\r\n" +
            "\r\n" +
            "\r\n" +
            "【2】六个页面 / The six pages\r\n" +
            "\r\n" +
            "成员资料：列出所有成员及其状态（已连接 · 最近活动 / 已上传 / 已加入 · 未连接 / 待接受邀请）。打开他人的文件夹会先下载其文件再列出（macOS 先选中成员，再点「打开成员文件」）；自己的卡片打开本机 Reference 文件夹。\r\n" +
            "Quick Post：给团队发短消息。点卡片看全文；「已阅」后移入「历史记录」；只能删除自己的帖子。\r\n" +
            "参考资料：只读浏览团队默认资料库、我的参考资料，以及临时下载的他人内容。\r\n" +
            "本地记录：选择本地记录文件夹；「新建文档」在 Mat_Local 中生成 Word 文档；下方列出 Mat_Local 中的文件。\r\n" +
            "进度同步：所有上传、下载和撤除操作都在这里（见第 3 节）。\r\n" +
            "使用说明：本页。登录后可编辑并上传给所有成员。\r\n" +
            "\r\n" +
            "Members: every member and their state (Connected · last active / Uploaded / Joined · not connected / Invitation pending). Opening another member's folder downloads and then lists their files (on macOS, select the member and press Open Member Files); your own card opens your local Reference folder.\r\n" +
            "Quick Post: short team messages. Click a card for the full text; \"Mark Read\" moves it to History; you can delete only your own posts.\r\n" +
            "References: read-only view of the team default library, your references and others' temporary downloads.\r\n" +
            "Local Records: choose your local records folder; \"New Document\" creates a Word file in Mat_Local; the list shows the files in Mat_Local.\r\n" +
            "Progress Sync: every upload, download and removal (see section 3).\r\n" +
            "Help: this page. When signed in you can edit it and upload it for everyone.\r\n" +
            "\r\n" +
            "页面名称旁的红点表示该页有新内容，进入页面后消失。界面可用 EN / 中文 按钮切换语言（Windows 在左上角，macOS 在右上角）。\r\n" +
            "A red dot next to a page name means something new; it clears when you open the page. Use the EN / 中文 button to switch the interface language (top-left on Windows, top-right on macOS).\r\n" +
            "\r\n" +
            "\r\n" +
            "【3】同步操作 / Sync actions（进度同步页 / Progress Sync page）\r\n" +
            "\r\n" +
            "每个操作都会先列出清单，只处理你勾选的项目。\r\n" +
            "Each action first shows a checklist and processes only the items you tick.\r\n" +
            "\r\n" +
            "- 下载他人内容：把选中成员的文件下载到 Public_Temp\\<成员>\\。临时区在启动和退出时清空。\r\n" +
            "- 下载默认资料库：把团队默认资料下载到 References\\Default\\，覆盖前保留旧副本。\r\n" +
            "- 上传参考资料：默认列出 Reference 中的文件，也可添加其他文件；上传到团队区你自己的目录。\r\n" +
            "- 上传本地记录：默认列出 Mat_Local 中的文件，保持原有的子文件夹结构。\r\n" +
            "- 撤除我上传的参考资料：从团队区删除你自己上传的文件，同时删除本机 Reference 中的副本，其他人从此无法下载。只能撤除自己的。\r\n" +
            "- 查看活动日志：查看全员的登录、上传、下载、撤除、发帖和改说明记录。\r\n" +
            "\r\n" +
            "- Download team files: selected members' files go to Public_Temp\\<member>\\, which is cleared on start and exit.\r\n" +
            "- Download default library: team default files go to References\\Default\\; older copies are kept before overwriting.\r\n" +
            "- Upload references: lists files in Reference by default (you can add others) and uploads them to your own team folder.\r\n" +
            "- Upload local records: lists files in Mat_Local by default and keeps their sub-folder structure.\r\n" +
            "- Remove my uploaded references: deletes your own uploads from the team area and the copy in your local Reference, so others can no longer download them. You can remove only your own.\r\n" +
            "- Activity log: everyone's sign-ins, uploads, downloads, removals, posts and help edits.\r\n" +
            "\r\n" +
            "需要记住的规则 / Rules to remember\r\n" +
            "- 上传前先保存文件；本地保存不等于上传。\r\n" +
            "- 两人上传同名文件时，后上传的会覆盖先上传的，程序不做合并。请按各自目录分工。\r\n" +
            "- 单个文件不超过 50 MiB。\r\n" +
            "- 上传只新增或覆盖，不会删除团队区里的旧文件。\r\n" +
            "\r\n" +
            "- Save before uploading; saving locally is not uploading.\r\n" +
            "- If two people upload the same file name, the later upload wins; nothing is merged. Keep to your own folder.\r\n" +
            "- Each file must be 50 MiB or smaller.\r\n" +
            "- Uploads only add or overwrite; they never delete older files in the team area.\r\n" +
            "\r\n" +
            "\r\n" +
            "【4】文件放在哪里 / Where files live\r\n" +
            "\r\n" +
            "Windows：程序所在的 SRCRec 文件夹\r\n" +
            "macOS：~/Library/Application Support/ResearchProgressRecorder/SRCRec\r\n" +
            "\r\n" +
            "PersonalWorkspaceSRRec\\Mat_Local      你的工作资料，上传本地记录的默认来源 / your working files; default source for local-record uploads\r\n" +
            "PersonalWorkspaceSRRec\\Reference      你的参考资料，上传参考资料的默认来源 / your references; default source for reference uploads\r\n" +
            "PersonalWorkspaceSRRec\\Public_Temp    他人内容的临时区，启动和退出时清空 / temporary area for others' files, cleared on start and exit\r\n" +
            "References\\Default                    团队默认资料库，只读 / team default library, read-only\r\n" +
            "*.json                                程序配置，请勿手动修改 / app settings, do not edit by hand\r\n" +
            "\r\n" +
            "令牌不会写入这些文件。替换或升级程序不会删除上面的资料。\r\n" +
            "The token is never written to these files. Replacing or upgrading the app keeps all of the above.\r\n" +
            "\r\n" +
            "\r\n" +
            "【5】更新与升级 / Updates\r\n" +
            "\r\n" +
            "Windows：点左下角「检查更新」。程序下载新版并校验 SHA-256，退出后只替换程序文件，然后自动重启；上一版保留为 SRPRecorder2027.exe.previous。\r\n" +
            "macOS：点窗口左下角「检查更新」。程序下载并校验 DMG 后打开它，请退出程序，把新的 app 拖进「应用程序」替换旧版。较早版本若检查更新立即失败，请用浏览器从发布页下载对应芯片的 DMG：Apple 芯片用 arm64，Intel 用 x64。\r\n" +
            "发布页：https://github.com/bacoyc0928-art/research-progress-recorder/releases/latest\r\n" +
            "\r\n" +
            "Windows: press \"Check updates\" at the bottom-left. The app downloads the new version, verifies its SHA-256, replaces only program files after exit and restarts; the previous exe is kept as SRPRecorder2027.exe.previous.\r\n" +
            "macOS: press \"Check updates\" at the bottom-left. The app downloads and verifies the DMG and opens it; quit the app and drag the new app into Applications to replace the old one. If an older version fails immediately, download the DMG for your chip from the release page in a browser: arm64 for Apple silicon, x64 for Intel.\r\n" +
            "Release page: https://github.com/bacoyc0928-art/research-progress-recorder/releases/latest\r\n" +
            "\r\n" +
            "\r\n" +
            "【6】常见问题 / Troubleshooting\r\n" +
            "\r\n" +
            "- 登录提示 401：令牌无效或已过期，请重新创建。\r\n" +
            "- 登录提示 403 / 404：确认已接受仓库邀请，并且使用的是勾选了 repo 的 classic 令牌。\r\n" +
            "- 看不到成员或帖子：先确认已登录，再在成员资料页点「刷新」。\r\n" +
            "- Windows 首次运行出现 SmartScreen：点「更多信息 → 仍要运行」。程序没有购买代码签名证书。\r\n" +
            "- macOS 提示无法验证开发者：确认 DMG 来自上述发布页后，先尝试打开一次，再到「系统设置 → 隐私与安全性」点「仍要打开」。如果提示文件已损坏或会损害电脑，请停止使用并反馈原文。\r\n" +
            "- 上传只成功一部分：多文件上传不是一次性完成的，重新上传即可，同名文件会被覆盖。\r\n" +
            "- 检查更新失败：多为网络问题或 GitHub 暂时限流，请稍后重试，或直接从发布页下载。\r\n" +
            "\r\n" +
            "- 401 on sign-in: the token is invalid or expired; create a new one.\r\n" +
            "- 403 / 404 on sign-in: make sure you accepted the invitation and use a classic token with repo.\r\n" +
            "- No members or posts: check that you are signed in, then press Refresh on Members.\r\n" +
            "- SmartScreen on first run (Windows): choose \"More info → Run anyway\". The app has no paid code-signing certificate.\r\n" +
            "- \"Developer cannot be verified\" (macOS): after confirming the DMG came from the release page, try to open the app once, then choose Open Anyway in System Settings → Privacy & Security. If macOS says the file is damaged or harmful, stop and report the exact message.\r\n" +
            "- Upload only partly succeeded: multi-file uploads are not all-or-nothing; upload again and same-named files are overwritten.\r\n" +
            "- Update check failed: usually the network or a temporary GitHub rate limit; retry later or download from the release page.\r\n" +
            "\r\n" +
            "\r\n" +
            "【7】安全与隐私 / Security and privacy\r\n" +
            "\r\n" +
            "- 令牌只用于连接 GitHub，不会上传、打印或写进日志。请不要把令牌发给别人；不用时可在 GitHub 上撤销。\r\n" +
            "- 能写入 research-data 的人在技术上可以修改任何成员的目录。请只邀请信任的成员。\r\n" +
            "- 卸载程序不会撤销令牌，请到 GitHub 自行撤销。\r\n" +
            "\r\n" +
            "- The token is used only to talk to GitHub; it is never uploaded, printed or logged. Do not share it; revoke it on GitHub when no longer needed.\r\n" +
            "- Anyone who can write to research-data can technically change any member's folder. Invite only trusted members.\r\n" +
            "- Uninstalling does not revoke the token; revoke it on GitHub yourself.\r\n";

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
            ListView list = root as ListView;
            if (list != null)
                foreach (ColumnHeader column in list.Columns)
                {
                    string original = column.Tag as string;
                    if (original == null) { original = column.Text; column.Tag = original; }
                    column.Text = T(original);
                }
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

        /// <summary>读取和保存中英文界面偏好。</summary>
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
