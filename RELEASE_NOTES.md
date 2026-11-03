# SRPRecorder2027 0.2.11 · 公开测试版 / Public Test

发布日期 / Released: 2026-11-03

## 下载与升级 / Download and upgrade

在下方 Assets 下载：Windows 用 `research-progress-recorder-0.2.11-Windows.zip`；M 系列 Mac 用 `research-progress-recorder-0.2.11-macOS-arm64.dmg`；Intel Mac 用 `research-progress-recorder-0.2.11-macOS-x64.dmg`。另附 SHA256SUMS.txt 和本说明。下载无需账号或令牌。

- Windows：保存并退出程序，解压覆盖程序目录，保留 `.exe.config`；应用内更新仍可使用。
- Mac：建议这次通过浏览器下载 DMG，保存并退出旧版，把 app 拖入 Applications 替换，再弹出 DMG。用户数据保存在 `~/Library/Application Support/ResearchProgressRecorder/`，替换 app 时保留该目录。
- 旧 Mac 版本若点击检查更新立即失败，可能无法通过内置更新获得这个补丁，请使用此页手动下载。没有原始报错，不能认定所有失败都是签名问题。

For this upgrade, Mac users should download the matching DMG in their browser, quit the old app, replace it in Applications, eject the image and reopen the installed app. Data in Application Support is preserved. An older updater that fails before download cannot repair itself.

## 主要修复 / Fixes

- Windows 选择对话框、窄窗口卡片、成员状态、同步按钮自适应布局；修复中文仓库名/账号被截掉和表头翻译遗漏。
- Mac 中英文原位切换，保留当前页面、登录状态和帮助/帖子编辑内容；修复活动日志大小写字段兼容和成员状态。
- 两端完整分页读取协作者、待接受邀请；标注尚未上传的成员，跳过空下载；本人资料使用本地 Reference。
- 帮助页未登录只读、退出保存草稿、后台读取不覆盖等待期间的新编辑；Mac 帮助修改写入共享改动历史。
- 更新下载与同步统一使用经过验证的 Mac 系统 CA，区分 HTTPS 证书失败、GitHub 请求限制和目录权限错误。
- 当前 ad-hoc Mac 构建下载、校验后打开 DMG 并提示手动替换，不再承诺无人值守安装和自动重开；正式 Developer ID 分支进一步核对同一开发者与 Gatekeeper 并记录失败日志。
- Windows 更新要求校验文件，仅替换程序白名单，原子替换 exe 并保留上一版；升级标记仍为 0.2.0，不重复迁移用户数据。

Responsive Windows dialogs/cards, complete member lists, cross-platform activity logs, in-place Mac language switching, draft preservation and verified HTTPS update requests are repaired. Unsigned/unnotarized test builds use a transparent manual DMG installation flow.

## 系统与限制 / Requirements and limitations

Windows 10/11 + .NET Framework 4.8；Apple Silicon macOS 14+；Intel macOS 15+。

Mac 包仍为 ad-hoc 签名，没有 Developer ID / Apple 公证。确认下载来自本仓库后，若仅提示无法验证开发者，可由本人按 Apple 官方步骤操作：尝试打开后进入 系统设置 → 隐私与安全性 → 仍要打开。若提示文件损坏或会损害电脑，应停止并反馈原文。https://support.apple.com/en-us/102445

SHA-256 与代码完整性检查不能代替系统启动许可。真实同僚双账号/双设备同步及所有终端的系统策略仍需实测。
