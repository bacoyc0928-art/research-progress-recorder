# SRPRecorder2027 0.2.15 · 公开测试版 / Public Test

发布日期 / Released: 2026-11-05

## 下载与升级 / Download and upgrade

在下方 Assets 下载：Windows 用 `research-progress-recorder-0.2.15-Windows.zip`；M 系列 Mac 用 `research-progress-recorder-0.2.15-macOS-arm64.dmg`；Intel Mac 用 `research-progress-recorder-0.2.15-macOS-x64.dmg`。另附 SHA256SUMS.txt 和本说明。下载无需账号或令牌。

- Windows：点左下角「检查更新」即可升级（下载、校验 SHA-256、退出后替换并自动重启，上一版留作 .previous）。
- Mac：点窗口左下角「检查更新」，下载并校验后程序会打开 DMG 并询问是否现在退出；点「是」后把新 app 拖入 Applications 选择「替换」，再重新打开。若检查更新失败，请用浏览器从本页下载对应芯片的 DMG。用户数据保存在 `~/Library/Application Support/ResearchProgressRecorder/`，替换 app 时保留。

## 新功能与变化 / What's new

- 参考资料页改版：左边竖排列出文件夹（团队默认资料库、公共资料、我的参考资料、他人内容），右边显示所选文件夹里的文件，可滚动，双击打开。
- 新增公共资料库：进度同步页「上传至公共资料库」供全员下载，上传前二次确认，上传后不可撤销，也不能覆盖同名文件；「下载公共资料库」把文件永久保存到 References\PublicLib（中文名“公共资料”），不会在退出时清空。
- 取消所有日志同步：不再记录或读取活动日志，去掉「查看活动日志」和使用说明的「改动日志」。成员状态改为“已上传资料 / 已加入 · 尚未上传 / 待接受邀请”；红点只根据成员、帖子、资料库文件、本机文件和使用说明的实际变化提示。
- Quick Post：历史记录里自己发的帖子也可以删除，别人的帖子不能删。
- 修复：英文页标题 “References & Materials” 中的 & 不再丢失；Mac 表格在高分辨率下不再截字。

The References page now lists folders vertically with the selected folder's files on the right. A new public library lets members upload files for everyone (irreversible, no overwrites) and download them permanently to References\PublicLib. All activity-log syncing is removed; member states and badges rely on actual content. Authors can delete their own posts from History.

## 系统与限制 / Requirements and limitations

Windows 10/11 + .NET Framework 4.8；Apple Silicon macOS 14+；Intel macOS 15+。

Mac 包仍为 ad-hoc 签名，没有 Developer ID / Apple 公证。确认下载来自本仓库后，若仅提示无法验证开发者，可由本人按 Apple 官方步骤操作：尝试打开后进入 系统设置 → 隐私与安全性 → 仍要打开。若提示文件损坏或会损害电脑，应停止并反馈原文。https://support.apple.com/en-us/102445

云端已有的活动日志文件保留在私有仓库中，新版本不再读写它们。真实同僚双账号/双设备同步仍需实测。
