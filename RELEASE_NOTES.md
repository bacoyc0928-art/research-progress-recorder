# SRPRecorder2027 0.2.12 · 公开测试版 / Public Test

发布日期 / Released: 2026-11-03

## 下载与升级 / Download and upgrade

在下方 Assets 下载：Windows 用 `research-progress-recorder-0.2.12-Windows.zip`；M 系列 Mac 用 `research-progress-recorder-0.2.12-macOS-arm64.dmg`；Intel Mac 用 `research-progress-recorder-0.2.12-macOS-x64.dmg`。另附 SHA256SUMS.txt 和本说明。下载无需账号或令牌。

- Windows：可在左下角「检查更新」升级；或保存并退出程序，解压覆盖程序目录，保留 `.exe.config`。
- Mac：0.2.11 的「检查更新」会下载、校验并打开 DMG，按提示把 app 拖入 Applications 替换。更早版本若检查更新立即失败，请用浏览器下载对应 DMG 手动替换。用户数据保存在 `~/Library/Application Support/ResearchProgressRecorder/`，替换 app 时保留。
- 升级标记仍为 0.2.0，本版不清理、不迁移任何用户数据。

## 界面与功能优化 / Improvements

- 未登录时，进度同步的六个动作、Quick Post 的发布/历史记录、使用说明的四个工具按钮统一显示为灰色禁用，发帖输入框只读；登录后自动恢复。Windows 与 Mac 一致。
- Windows：修复成员资料页参考资料路径被按钮栏遮住下半截、「（未登录）」与标题间距过大；修复进度同步页「未连接 GitHub」上半截被仓库标签盖住。
- Windows：Quick Post、本地记录、进度同步三页卡片铺满窗口，宽屏不再右侧留白；同步日志与帖子列表随窗口变大；本地记录两个操作按钮等宽。
- Windows：帖子全文与已阅历史弹窗改用统一的屏幕限制和底部按钮栏。
- Mac：所有弹窗缩小时按钮栏优先保留，不再被内容挤掉；主窗口在副显示器上时，弹窗留在该显示器，不再跳回主屏。
- 测试：Windows 新增六页 × 中英文 × 两种尺寸的通用布局审计（重叠、裁切、越界）及登录前后按钮状态检查；Mac 新增弹窗几何、按钮栏、登录状态测试；CI 原生截图增加四类弹窗的默认/最小尺寸检查。

Signed-out actions are now visibly disabled on both platforms. Windows fixes clipped labels on Members and Progress Sync, makes all cards fill the window and unifies post dialogs. Mac dialogs keep their button bar when shrunk and stay on the display that holds the main window. New layout audits and native dialog captures guard these fixes.

## 系统与限制 / Requirements and limitations

Windows 10/11 + .NET Framework 4.8；Apple Silicon macOS 14+；Intel macOS 15+。

Mac 包仍为 ad-hoc 签名，没有 Developer ID / Apple 公证。确认下载来自本仓库后，若仅提示无法验证开发者，可由本人按 Apple 官方步骤操作：尝试打开后进入 系统设置 → 隐私与安全性 → 仍要打开。若提示文件损坏或会损害电脑，应停止并反馈原文。https://support.apple.com/en-us/102445

真实同僚双账号/双设备同步及所有终端的系统策略仍需实测。
