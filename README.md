# Research Progress Recorder 0.0.2 / 研究进展记录器

Windows 和 macOS 桌面客户端，共用私有数据仓库 bacoyc0928-art/research-data。

## 下载与启动

程序仓库现已公开，可直接从 [Releases](https://github.com/bacoyc0928-art/research-progress-recorder/releases/latest) 下载，无需登录、邀请或令牌。研究数据仓库仍为私有，参与团队同步需接受数据仓库邀请并使用自己的令牌。公开访问不授予修改本仓库的权限。

| 下载文件 | 平台 |
| --- | --- |
| research-progress-recorder-0.0.2-Windows.zip | Windows 10/11，.NET Framework 4.8；解压后运行 exe |
| research-progress-recorder-0.0.2-macOS-arm64.zip | Apple Silicon M 系列，macOS 14+ |
| research-progress-recorder-0.0.2-macOS-x64.zip | Intel Mac，macOS 15+ |

Mac 包自带 Python/Tk，无需另装 Python。解压后将 ResearchProgressRecorder.app 放进 Applications。当前没有 Apple Developer ID 签名或公证，首次打开可能被系统拦截；接收者应核对来源和 SHA-256，并按单位安全政策决定是否运行。本项目不修改系统安全设置。

## 协作配置

1. 首次启动选择自己的工作文件夹，用于资料和 Word 文档。
2. 团队同步填写仓库 bacoyc0928-art/research-data、分支 main、路径 sync/research_log.json。
3. 输入自己的 GitHub PAT。仓库所有者可使用限选数据仓库、Contents 读写权限的 fine-grained PAT；个人仓库协作者目前需 classic PAT 的 repo 权限，建议设置到期日。不要分享令牌。
4. 验证并保存配置，勾选记录/类别，点击一键同步。上传前合并远端，保留未选频道的远端数据，冲突会重试。同条记录按 UTC 更新时间取较新版本，请保持系统时钟准确。
5. 研究记录与类别的删除标记均同步。所有协作者应升级至 0.0.2。
6. 团队研究页选择个人文件夹后，可上传参考资料、个人研究和文档清单到 team/<GitHub登录名>/，并查看成员研究。这里只上传 JSON 元数据，PDF、DOCX、附件文件本身不会上传。

team/<login>/ 是应用选择的上传路径，不是 GitHub 权限边界；整个仓库的写入者仍可通过 GitHub 修改其他人的目录。同步由按钮触发，无后台常驻服务。

## 本机数据与升级

Windows 日志保存在 exe 同目录 research_log.json；令牌只存 Windows 凭据管理器。
Mac 日志保存在 ~/Library/Application Support/ResearchProgressRecorder/research_log.json；令牌只存 macOS 钥匙串。
原子保存保留 .bak 供恢复。更换工作目录保留现有日志，只改变后续资料和文档目录。

升级 Windows 前备份并保留 research_log.json、workspace.json、sync_config.json、personal.json、localrefs.json、lang.txt，替换程序即可。不要把个人 JSON 文件提交到程序仓库。

六个栏目：概览、团队研究、参考文献与资料、本地进度、团队同步、帮助。支持中英文、研究日志自动保存与搜索、可编辑类别和附件链接、本地 Word 文档、参考资料。删除磁盘文件先确认并移到系统回收站/废纸篓；工作目录、程序目录及其父目录受保护。来自其他电脑的路径可能需要重新定位。

## 构建与验证

Windows 在 PowerShell 执行 ./build.ps1。Mac 开发运行 python3 mac-client.py；在 macOS 用 PyInstaller 构建 .app。
.github/workflows/release.yml 在 Windows、Apple Silicon macOS 和 Intel macOS 上分别测试和打包。测试覆盖合并、删除、范围、时区、子秒精度、损坏数据保护、备份、Word XML、冲突重试和界面启动。
仍需团队实际双账号/双设备验收同步、首次安装和钥匙串授权；自动化检查不保证没有缺陷。

## English quick start

Download the ZIP matching your platform from Releases. Mac bundles include their runtime; use arm64 for Apple Silicon, x64 for Intel. They are not Developer ID signed/notarized; follow your organization's installation policy.
The program repository is public; downloading requires no sign-in, invitation, or token. Accept the private data repository invitation to join team sync, choose a workspace, configure bacoyc0928-art/research-data / main / sync/research_log.json, and use your own GitHub token. Tokens stay in Windows Credential Manager or macOS Keychain. Sync selected records/categories manually; unselected remote data is preserved and deletions propagate. Personal channels upload metadata under your own GitHub login. Files themselves are not transferred. Keep local backups and upgrade all team devices to 0.0.2.
