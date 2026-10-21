# SRPRecorder2027 0.2.0 · 公开测试版 / Public Test

发布日期 / Released: 2026-10-20

## 下载 / Download

| 系统 | 文件 | 要求 |
| --- | --- | --- |
| Windows 10/11 | research-progress-recorder-0.2.0-Windows.zip | .NET Framework 4.8 |
| Apple Silicon / M 系列 | research-progress-recorder-0.2.0-macOS-arm64.dmg | macOS 14+ |
| Intel Mac | research-progress-recorder-0.2.0-macOS-x64.dmg | macOS 15+ |

三个文件都在本 Release 的 Assets 里，另有 `SHA256SUMS.txt` 可核对摘要。下载不需要登录，也不需要令牌。

## 安装 / Install

- **Windows**：关闭旧程序 → 解压 ZIP 到自己的可写文件夹 → 覆盖旧目录 → 运行 `SRCRec\SRPRecorder2027.exe`。必须保留随 exe 的 `.exe.config`，只拷 exe 会失去高 DPI 修复。
- **macOS**：打开对应芯片的 DMG → 把 `ResearchProgressRecorder.app` 拖进 Applications 覆盖旧版 → 弹出镜像。首次打开若被系统拦截，右键 →“打开”。

## 覆盖升级任何旧版本 / Upgrade from any older version

0.2.0 可以直接覆盖 **0.0.x / 0.1.0 / 0.1.1**（Windows 与 macOS 都一样），不需要先卸载。

**首次启动会自动清除旧版软件数据**，让新版本从干净状态开始：

- 删除：程序生成的配置 JSON（工作文件夹、进度文件夹、个人文件夹、同步配置、未读状态、语言文件）、旧日志、成员下载缓存 `References/<账号>/TeamConRef/`、以及 `References/Default` 里旧版下载的默认资料库副本。
- 保留：你自己的本地进度文件夹及其中的 Word/PDF 文档、个人文件夹 `references/` 里的上传副本。
- 记录：删除项写入程序目录的 `upgrade_cleanup.log`，版本标记写入 `data_version.txt`（之后启动不再清理）。
- 需要做的事：重新选择工作文件夹与本地进度文件夹、重新登录（令牌只在内存），登录后点“下载默认资料库”把默认库重新拉下来。

## 0.2.0 主要变化

- 界面固定中英双语并显（去掉语言切换），提示、弹窗、活动日志同样双语。
- 左下角独立登录面板；登录与仓库校验拆分，401/403/404 分别说明，404 明确提示外部协作者需 classic PAT；令牌只存内存。
- 未登录全面拦截查看与改动；退出＝二次确认后关闭程序。
- 团队研究列出全部协作者，除本人外统一“项目成员”。
- 新增“撤除我上传的参考资料”。
- 新增 **Quick Post**（便签卡片、已阅进历史、只能删自己的帖子）。
- 新增活动日志（public 2000 / private 500 双写）。
- 使用说明可编辑并上传 `help/help.md`，带 `help/changelog.json`。
- 团队同步四个动作改为先预览再勾选，撤除只删勾选项。
- 栏目未读红点（团队研究 / Quick Post / 团队同步），点进即消失。
- 本地进度页直接列出文件夹内容（名称/大小/修改时间，双击打开）。
- 新建 Word：项目 / 日期 / 目的 + 留白；中文楷体、西文 Times New Roman。
- 修复：长路径报错、高 DPI 登录面板过大、未登录拦截弹模态导致自动化卡死、双语文案重叠。

## 限制 / Limitations

- 两端都未做商业代码签名：Windows 首次运行会提示 SmartScreen（更多信息 → 仍要运行），macOS 为 ad-hoc 签名、无公证（右键打开）。这不影响本地数据。
- 仍需真实双账号、双设备同步验收；自动化检查不保证没有缺陷。
- 公开包不含团队书籍、研究文档、个人 JSON 或令牌。

## 已知问题

- 红点只覆盖团队研究 / Quick Post / 团队同步三个栏目；参考资料与使用说明没有可靠的“新内容”信号。
- 活动日志与帖子是“先读后写”，同一秒并发可能丢一条（已重试 3 次，未做乐观锁）。
