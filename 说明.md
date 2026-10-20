# Research Progress Recorder / SRPRecorder2027 · 0.1.0

**公开测试版 / Public Test**

警告：当前版本为测试版本，上传的所有数据信息，以及程序运行时的信息和内容随时有可能丢失。后续代码以及程序功能的根本性变更有可能随时发生。

## 下载与安装

安装文件统一位于 [最新发布页](https://github.com/bacoyc0928-art/research-progress-recorder/releases/latest)，下载不需要 GitHub 登录或令牌。请勿使用仓库根目录历史遗留的 0.0.2 exe/ZIP。

| 系统 | 0.1.0 下载 | 要求 |
| --- | --- | --- |
| Windows | [Windows ZIP](https://github.com/bacoyc0928-art/research-progress-recorder/releases/download/v0.1.0/research-progress-recorder-0.1.0-Windows.zip) | Windows 10/11，.NET Framework 4.8 |
| Apple Silicon / M 系列 | [arm64 DMG](https://github.com/bacoyc0928-art/research-progress-recorder/releases/download/v0.1.0/research-progress-recorder-0.1.0-macOS-arm64.dmg) | macOS 14+ |
| Intel Mac | [x64 DMG](https://github.com/bacoyc0928-art/research-progress-recorder/releases/download/v0.1.0/research-progress-recorder-0.1.0-macOS-x64.dmg) | macOS 15+ |

Windows：将 ZIP 完整解压到自己的可写文件夹，运行 SRCRec/SRPRecorder2027.exe。Mac：打开 DMG，将 ResearchProgressRecorder.app 拖到 Applications 后弹出镜像；无需另装 Python。Mac 目前只有临时签名，没有 Apple Developer ID 签名或公证，系统可能拦截首次运行；请核对来源及 SHA256SUMS.txt，并遵循所在单位安全政策。本程序不会修改系统安全设置。Windows 也未购买代码签名证书。

## 两个仓库

本仓库是公开的程序源码、构建流程和安装包仓库；普通下载者不会获得修改源码的权限。团队研究文件放在**私有**数据仓库 `bacoyc0928-art/research-data`。邀请仅针对数据仓库。`team/<login>` 是程序使用的路径，不是仓库权限边界；数据仓库写入者可在 GitHub 修改其他成员文件，请只邀请信任的协作者。

## 首次同步（Windows / Mac 相同）

1. 接受私有数据仓库邀请，确认登录本人 GitHub 后可以打开它。
2. 在“团队同步 / Team Sync”填写仓库 `bacoyc0928-art/research-data`、分支 `main` 和**本人令牌**，点“验证并保存配置 / Validate & Save”。仓库所有者可用限选此仓库且 Contents 读写的 fine-grained PAT；个人仓库的外部协作者使用 classic PAT 的 `repo` 权限并设置到期日。不要向同僚索取、转发或共享令牌。令牌只存 Windows 凭据管理器 / macOS 钥匙串。
3. 点“下载他人内容 / Download Team Files”读取成员已上传的实际文件；点“下载默认资料库 / Download Default Library”读取私有 `default/`。若远端尚无内容，原本地库保留。
4. 在“本地进度 / Local Progress”选择一个**专用研究文件夹**，可新建 Word 文档或打开文件夹编辑；不要选择整个用户目录、程序目录或混有私人文件的文件夹。
5. “上传本地进度 / Upload Local Progress”递归上传该专用文件夹到 `team/<本人账号>/progress/`；“上传参考资料 / Upload References”只上传本次选择的文件和合并后的资料清单到 `team/<本人账号>/references/`。
6. 他人点击下载即可取得更新。修改本地文件不会自动上传，软件没有后台实时同步。上传可能部分成功；失败后可重新上传。同名远端文件更新，不会自动删除远端旧文件。

## 五栏与文件保护

团队研究、参考资料、本地进度、团队同步、使用说明。支持中文 / English。团队页刷新只读取，上传时才创建个人档案。上传前验证路径和 50 MiB/文件限制；不支持符号链接、目录联接及 Windows 不兼容文件名。下载先暂存，成功后替换，保留原目录备份；失败、空库或无权限不会清空旧库。不同人编辑同一个文件仍可能后写覆盖，请按成员目录协作并备份。

Windows 文件根为 `SRCRec`；默认进度 `PersonalWorkspaceSRRec`，默认库 `References/Default`，团队下载 `References/<本人账号>/TeamConRef/<成员>/`。Mac 用户数据保存在 `~/Library/Application Support/ResearchProgressRecorder/`，其下 SRCRec 使用相同文件结构，安装包目录只放程序。公开包不含团队书籍、研究文档、个人 JSON、令牌或配置。成员资料仅放本地缓存及私有数据仓库。

## 从 0.0.2 升级与卸载

升级前关闭软件并备份完整用户数据。Windows 建议解压到新目录，再按需迁移旧配置和专用资料目录；不要盲目覆盖或删除旧目录。Mac 替换 Applications 内的 app，保留 Application Support 数据。0.0.2 的日志 JSON 继续保留；0.1.0 当前界面以文件工作流为主，不会自动把旧日志转换成 Word，也不继续旧的共享日志合并界面。全员升级后使用新文件通道。

Windows 运行“卸载SRPRecorder2027.exe”；默认保留研究文件，配置另存备份，只按程序清单删除，不删除外部自选文件夹。Mac DMG 中的 `uninstall-macos.command` 默认保留研究数据并将 app 移到废纸篓；可选的数据删除需用户明确确认。卸载不撤销 GitHub 令牌，请按需在 GitHub 撤销本人令牌。

## 测试与构建

Windows：`./test-local.ps1 -TestDirectory <新建的独立测试目录>`；`./package-local.ps1 -Destination <新的输出目录>` 生成干净的 Windows ZIP。默认不打包任何真实默认资料，`-IncludePrivateDefault` 仅供私有本地用途，禁止将其输出作为公开安装包。

Mac：`python -m unittest test_mac_client.py test_file_channels.py -v` 和 `python mac-client.py --smoke-test`。GitHub Actions `Build 0.1.0 packages` 只读权限、手动触发，在 Windows、Apple Silicon 和 Intel Mac 分别验证并打包。发布由维护者手动上传，流程没有数据仓库密钥。

仍需真实同僚完成两账号、两设备、首次安装、首次钥匙串授权验收。自动化检查不代表没有缺陷。反馈时附系统版本、芯片型号、软件版本、按钮名称和去除令牌/研究内容后的错误信息。404 下载问题请附完整下载 URL。

## English quick start

Download your platform's 0.1.0 package from Releases (no sign-in required). Mac DMGs include the runtime; apps are ad-hoc signed, not Developer ID signed or notarized. Keep backups. Accept the private data-repository invitation, configure `bacoyc0928-art/research-data` / `main` using your own token and Validate & Save. Use Download Team Files and Download Default Library to receive files. Choose a dedicated progress folder before Upload Local Progress; Upload References transfers selected files. Transfers are manual, up to 50 MiB per file; remote deletions are not automatic. Downloads preserve previous folders as backups. Legacy 0.0.2 JSON is preserved; the 0.1.0 UI uses file workflows. Do not share tokens or upload private files to the public program repository.