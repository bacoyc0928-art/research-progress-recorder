# SRPRecorder2027 0.2.13 · 公开测试版 / Public Test

发布日期 / Released: 2026-11-04

## 下载与升级 / Download and upgrade

在下方 Assets 下载：Windows 用 `research-progress-recorder-0.2.13-Windows.zip`；M 系列 Mac 用 `research-progress-recorder-0.2.13-macOS-arm64.dmg`；Intel Mac 用 `research-progress-recorder-0.2.13-macOS-x64.dmg`。另附 SHA256SUMS.txt 和本说明。下载无需账号或令牌。

- Windows：点左下角「检查更新」即可升级（下载、校验 SHA-256、退出后替换并自动重启，上一版留作 .previous）。
- Mac：点窗口左下角「检查更新」，程序下载并校验 DMG 后打开它，请退出程序，把新 app 拖入 Applications 替换。若检查更新立即失败，请用浏览器下载对应芯片的 DMG 手动替换。用户数据保存在 `~/Library/Application Support/ResearchProgressRecorder/`，替换 app 时保留。
- 升级标记仍为 0.2.0，本版不清理、不迁移任何用户数据。

## 界面与功能 / What's new

- 按钮分主次：每页最常用的操作（发布、上传参考资料、上传本地记录、新建文档、登录/解锁）用蓝色主要按钮，其余为白色；未登录时统一变灰。两端一致，Mac 使用系统原生蓝色按钮。
- 进度同步页按用途分成三组：从团队获取、分享我的资料、管理我的上传，每组一句话说明文件去向。
- 本地记录页整理按钮：Mac 去掉重复按钮，分为常用操作与「打开文件夹」两行；Mac 的「上传本地进度」更名为与 Windows 一致的「上传本地记录」。
- 检查更新更可靠：GitHub 接口对匿名请求按网络限流（校园网、公司网等共享出口常见）时，自动改用发布页获取版本与校验文件，下载后仍按 SHA-256 校验。
- 使用说明：重写为简洁的中英对照版本（7 节），并修复 Windows 上云端说明全部挤成一段的问题；Mac 统一换行。
- 测试：Windows 新增布局审计与按钮状态检查；CI 的 Mac 更新运行时检查同时实测发布页回退。0.2.11 → 0.2.12 的软件内更新已在 Windows 与两种真实 Mac 上实测通过。

Primary actions are now highlighted on both platforms, Progress Sync groups its six actions into three explained sections, and the Mac Local Records toolbar is tidied. Update checks fall back to the public release page when GitHub's anonymous API is rate-limited, still verifying SHA-256. The in-app help is rewritten and now shows line breaks correctly on Windows.

## 系统与限制 / Requirements and limitations

Windows 10/11 + .NET Framework 4.8；Apple Silicon macOS 14+；Intel macOS 15+。

Mac 包仍为 ad-hoc 签名，没有 Developer ID / Apple 公证。确认下载来自本仓库后，若仅提示无法验证开发者，可由本人按 Apple 官方步骤操作：尝试打开后进入 系统设置 → 隐私与安全性 → 仍要打开。若提示文件损坏或会损害电脑，应停止并反馈原文。https://support.apple.com/en-us/102445

0.2.12 及更早版本没有限流回退；若它们的「检查更新」因限流失败，请稍后重试或从发布页下载。真实同僚双账号/双设备同步仍需实测。
