# Research Progress Recorder / SRPRecorder2027 · 0.1.0（装补丁后为 0.1.1）

**公开测试版 / Public Test**

警告：当前版本为测试版本，上传的所有数据信息，以及程序运行时的信息和内容随时有可能丢失。后续代码以及程序功能的根本性变更有可能随时发生。

## 版本状态（0.1.0 / 0.1.1）

- **没装补丁 = 0.1.0**：直接用下方 Release 里的安装包。
- **装了 0.1.1 补丁 = 0.1.1**：程序标题显示 `0.1.1`，文件/内部版本 `0.1.1.0`；功能与修复见“0.1.1 补丁”一节。
- 补丁只替换程序本体，**不新建 Release、不改数据结构**；0.1.0 与 0.1.1 的数据完全互通，装与不装都能读写同一份研究数据。

## 下载与安装

安装文件统一位于 [最新发布页](https://github.com/bacoyc0928-art/research-progress-recorder/releases/latest)，下载不需要 GitHub 登录或令牌。请勿使用仓库根目录历史遗留的 0.0.2 exe/ZIP。

| 系统 | 0.1.0 下载 | 要求 |
| --- | --- | --- |
| Windows | [Windows ZIP](https://github.com/bacoyc0928-art/research-progress-recorder/releases/download/v0.1.0/research-progress-recorder-0.1.0-Windows.zip) | Windows 10/11，.NET Framework 4.8 |
| Apple Silicon / M 系列 | [arm64 DMG](https://github.com/bacoyc0928-art/research-progress-recorder/releases/download/v0.1.0/research-progress-recorder-0.1.0-macOS-arm64.dmg) | macOS 14+ |
| Intel Mac | [x64 DMG](https://github.com/bacoyc0928-art/research-progress-recorder/releases/download/v0.1.0/research-progress-recorder-0.1.0-macOS-x64.dmg) | macOS 15+ |

Windows：将 ZIP 完整解压到自己的可写文件夹，运行 SRCRec/SRPRecorder2027.exe。Mac：打开 DMG，将 ResearchProgressRecorder.app 拖到 Applications 后弹出镜像；无需另装 Python。Mac 目前只有临时签名，没有 Apple Developer ID 签名或公证，系统可能拦截首次运行；请核对来源及 SHA256SUMS.txt，并遵循所在单位安全政策。本程序不会修改系统安全设置。Windows 也未购买代码签名证书。

## 0.1.1 补丁 / Patch

0.1.0 存在登录缺陷：程序要求“令牌身份”和“仓库访问”两步同时成功才保存令牌。仓库检查失败时（尤其是外部协作者用 fine-grained 令牌访问他人私有仓库，GitHub 一律返回 404）令牌根本没有保存，团队页就永远显示“（未登录）”。

0.1.1 是**补丁**，不是新版本：不建 0.1.1 Release，只替换程序本体，不改变数据结构、配置格式和文件位置。装上它就是 0.1.1，不装仍是 0.1.0。

| 平台 | 0.1.1 补丁下载 | 覆盖方式 |
| --- | --- | --- |
| Windows | [Windows 补丁 ZIP](https://raw.githubusercontent.com/bacoyc0928-art/research-progress-recorder/main/patches/0.1.1/research-progress-recorder-0.1.1-Windows.zip) | 解压后覆盖 SRCRec 内同名 exe 与 .exe.config |
| Apple Silicon / M 系列 | [macOS arm64 补丁](https://raw.githubusercontent.com/bacoyc0928-art/research-progress-recorder/main/patches/0.1.1/research-progress-recorder-0.1.1-macOS-arm64.dmg) | 打开 DMG，把 app 拖到 Applications 覆盖旧版 |
| Intel Mac | [macOS x64 补丁](https://raw.githubusercontent.com/bacoyc0928-art/research-progress-recorder/main/patches/0.1.1/research-progress-recorder-0.1.1-macOS-x64.dmg) | 同上 |

三个文件的大小与 SHA-256 见仓库内 [`patches/0.1.1/SHA256SUMS.txt`](https://github.com/bacoyc0928-art/research-progress-recorder/blob/main/patches/0.1.1/SHA256SUMS.txt)。raw 直链走 CDN，刚更新过可能短暂返回旧文件；遇到时用该文件核对摘要，或稍后重试。

补丁内容（0.1.1 相对 0.1.0）：
- 登录改成窗口左下角独立面板，令牌只存在内存，每次启动重新登录；“退出”＝二次确认后关闭程序。
- 登录与“仓库是否可见”拆成两步（先校验令牌身份并立即保存，再单独检查仓库）；失败按 HTTP 401/403/404 说明原因（404 明确提示外部协作者必须用 classic PAT）。
- 未登录时全面拦截：不能进“参考文献与资料”、帮助只读、工作/个人/进度文件夹、新建文档、类别增删改、文档移除、发帖与所有同步动作，且不写任何 JSON。
- 团队研究页列出全部协作者，除本人外统一显示“项目成员”。
- 新增“撤除我上传的参考资料”（只删除本人 `team/<账号>/references/` 下的文件与清单条目）。
- 新增 **Quick Post**：帖子便签、已阅进历史、只能删自己的帖子。
- 新增活动日志：`logs/public/activity.json`（全员，2000 条）与 `logs/private/<账号>/activity.json`（本人，500 条）。
- 使用说明改为可编辑并上传 `help/help.md`，附 `help/changelog.json` 改动日志。
- 修复 Windows 长路径导致的“未能找到路径…的一部分”、高 DPI 下登录面板被二次放大、以及未登录拦截弹模态对话框导致截图自检/CI 卡死。

## 两个仓库

本仓库是公开的程序源码、构建流程和安装包仓库；普通下载者不会获得修改源码的权限。团队研究文件放在**私有**数据仓库 `bacoyc0928-art/research-data`。邀请仅针对数据仓库。`team/<login>` 是程序使用的路径，不是仓库权限边界；数据仓库写入者可在 GitHub 修改其他成员文件，请只邀请信任的协作者。

## 首次同步（Windows / Mac 相同）

1. 接受私有数据仓库邀请，确认登录本人 GitHub 后可以打开它。
2. 在窗口**左下角**登录：粘贴**本人令牌**，点“登录 / Sign In”。仓库已内置绑定 `bacoyc0928-art/research-data · main`，界面里不再需要填写仓库、分支或路径。仓库所有者可用限选此仓库且 Contents 读写的 fine-grained PAT；**外部协作者必须用 classic PAT 的 `repo` 权限**（fine-grained 令牌访问他人账号的私有仓库一律 404），并设置到期日。不要向同僚索取、转发或共享令牌；令牌只存在内存，退出或重启即失效，不会写入磁盘或钥匙串。
3. 点“下载他人内容 / Download Team Files”读取成员已上传的实际文件；点“下载默认资料库 / Download Default Library”读取私有 `default/`。若远端尚无内容，原本地库保留。
4. 在“本地进度 / Local Progress”选择一个**专用研究文件夹**，可新建 Word 文档或打开文件夹编辑；不要选择整个用户目录、程序目录或混有私人文件的文件夹。
5. “上传本地进度 / Upload Local Progress”递归上传该专用文件夹到 `team/<本人账号>/progress/`；“上传参考资料 / Upload References”只上传本次选择的文件和合并后的资料清单到 `team/<本人账号>/references/`。
6. 他人点击下载即可取得更新。修改本地文件不会自动上传，软件没有后台实时同步。上传可能部分成功；失败后可重新上传。同名远端文件更新，不会自动删除远端旧文件。

## 六页与文件保护

团队研究、Quick Post、参考文献与资料、本地进度、团队同步、使用说明；Mac 六个标签页一一对应。**界面固定中英双语并显，不再提供语言切换**；提示、弹窗、活动日志同样双语。团队页刷新只读取，上传时才创建个人档案。上传前验证路径和 50 MiB/文件限制；不支持符号链接、目录联接及 Windows 不兼容文件名。下载先暂存，成功后替换，保留原目录备份；失败、空库或无权限不会清空旧库。不同人编辑同一个文件仍可能后写覆盖，请按成员目录协作并备份。

未登录时不能查看“参考文献与资料”，也不能发起任何同步、发帖或改动；团队研究页只显示提示。登录、上传、下载、撤除、发帖、删帖与改使用说明都会写入活动日志（公开 + 本人各一份）。

Windows 文件根为 `SRCRec`；默认进度 `PersonalWorkspaceSRRec`，默认库 `References/Default`，团队下载 `References/<本人账号>/TeamConRef/<成员>/`。Mac 用户数据保存在 `~/Library/Application Support/ResearchProgressRecorder/`，其下 SRCRec 使用相同文件结构，安装包目录只放程序。公开包不含团队书籍、研究文档、个人 JSON、令牌或配置。成员资料仅放本地缓存及私有数据仓库。

## 从 0.0.2 升级与卸载

升级前关闭软件并备份完整用户数据。Windows 建议解压到新目录，再按需迁移旧配置和专用资料目录；不要盲目覆盖或删除旧目录。Mac 替换 Applications 内的 app，保留 Application Support 数据。0.0.2 的日志 JSON 继续保留；0.1.0 当前界面以文件工作流为主，不会自动把旧日志转换成 Word，也不继续旧的共享日志合并界面。全员升级后使用新文件通道。

Windows 运行“卸载SRPRecorder2027.exe”；默认保留研究文件，配置另存备份，只按程序清单删除，不删除外部自选文件夹。Mac DMG 中的 `uninstall-macos.command` 默认保留研究数据并将 app 移到废纸篓；可选的数据删除需用户明确确认。卸载不撤销 GitHub 令牌，请按需在 GitHub 撤销本人令牌。

## 测试与构建

Windows：`./test-local.ps1 -TestDirectory <新建的独立测试目录>`；`./package-local.ps1 -Destination <新的输出目录>` 生成干净的 Windows ZIP。`build.ps1` 现在会自行清理临时编译目录。默认不打包任何真实默认资料，`-IncludePrivateDefault` 仅供私有本地用途，禁止将其输出作为公开安装包。

Mac：`python -m unittest test_mac_client.py test_file_channels.py -v` 和 `python mac-client.py --smoke-test`。GitHub Actions `Build 0.1.1 packages` 只读权限、手动触发，在 Windows、Apple Silicon 和 Intel Mac 分别验证并打包。发布由维护者手动上传，流程没有数据仓库密钥；补丁产物放在 `patches/0.1.1/`。

仍需真实同僚完成两账号、两设备、首次安装、首次钥匙串授权验收。自动化检查不代表没有缺陷。反馈时附系统版本、芯片型号、软件版本、按钮名称和去除令牌/研究内容后的错误信息。404 下载问题请附完整下载 URL。

## English quick start

Download your platform's 0.1.0 package from Releases (no sign-in required). **Installing the 0.1.1 patch makes it 0.1.1** (title bar 0.1.1, internal 0.1.1.0); without the patch it stays 0.1.0, and both share the same data. Mac DMGs include the runtime; apps are ad-hoc signed, not Developer ID signed or notarized. Keep backups. Accept the private data-repository invitation, then sign in with **your own token at the bottom-left of the window** — the repository is already bound to `bacoyc0928-art/research-data` / `main`. External collaborators need a classic PAT with `repo` scope; tokens stay in memory only. Use Download Team Files and Download Default Library to receive files. Choose a dedicated progress folder before Upload Local Progress; Upload References transfers selected files. Transfers are manual, up to 50 MiB per file; remote deletions are not automatic. Downloads preserve previous folders as backups. Legacy 0.0.2 JSON is preserved. Do not share tokens or upload private files to the public program repository.