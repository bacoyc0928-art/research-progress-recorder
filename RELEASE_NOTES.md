# SRPRecorder2027 0.2.14 · 公开测试版 / Public Test

发布日期 / Released: 2026-11-04

## 下载与升级 / Download and upgrade

在下方 Assets 下载：Windows 用 `research-progress-recorder-0.2.14-Windows.zip`；M 系列 Mac 用 `research-progress-recorder-0.2.14-macOS-arm64.dmg`；Intel Mac 用 `research-progress-recorder-0.2.14-macOS-x64.dmg`。另附 SHA256SUMS.txt 和本说明。下载无需账号或令牌。

- Windows：点左下角「检查更新」即可升级（下载、校验 SHA-256、退出后替换并自动重启，上一版留作 .previous）。
- Mac：点窗口左下角「检查更新」。下载并校验后程序会打开 DMG，并询问是否现在退出；点「是」后把新 app 拖入 Applications 选择「替换」，再重新打开。若这次检查更新失败，请用浏览器从本页下载对应芯片的 DMG 手动替换。用户数据保存在 `~/Library/Application Support/ResearchProgressRecorder/`，替换 app 时保留。

## 修复 / Fixes

- 资料安全：启动时的一次性迁移不再因为缺少 `data_version.txt` 就删除程序目录里的研究记录、配置和使用说明草稿。只有确认是 0.1.x 及更早版本时才迁移，而且移入 `upgrade-backup-<时间>` 文件夹保存，不再删除。Windows 与 Mac 一致。
- Mac 更新更顺畅：下载校验后分步说明替换方法，并可一键退出程序，避免程序仍在运行时拖入替换被 Finder 拒绝；任一步失败时保存诊断记录（`updates/check-<时间>.log`，不含令牌），并可一键用浏览器打开发布页手动下载。
- Windows：检查更新或下载校验失败时，同样可以一键打开发布页；取消下载不再弹出错误。
- Windows：修复软件内更新会在程序目录里套出 `src\src`、`卸载工具\卸载工具` 嵌套副本的问题（每更新一次多一层）。新版替换脚本按内容合并文件夹；从旧版更新上来时，新版首次启动会自动清掉这些只含程序源码与说明的副本。

Startup migration no longer deletes records, settings or help drafts just because `data_version.txt` is missing; genuine 0.1.x data is moved to `upgrade-backup-<time>` instead of deleted. On Mac, the verified update now offers to quit so the app can be replaced, and any failed step saves a diagnostic log and offers to open the release page. Windows offers the release page on update failures too, and in-app updates no longer nest `src\src` copies inside the program folder (existing nested copies are removed on first start).

## 系统与限制 / Requirements and limitations

Windows 10/11 + .NET Framework 4.8；Apple Silicon macOS 14+；Intel macOS 15+。

Mac 包仍为 ad-hoc 签名，没有 Developer ID / Apple 公证。确认下载来自本仓库后，若仅提示无法验证开发者，可由本人按 Apple 官方步骤操作：尝试打开后进入 系统设置 → 隐私与安全性 → 仍要打开。若提示文件损坏或会损害电脑，应停止并反馈原文。https://support.apple.com/en-us/102445

真实同僚双账号/双设备同步仍需实测。
