# SRPRecorder2027 0.2.8 · 公开测试版 / Public Test

发布日期 / Released: 2026-11-02

## 下载 / Download

| 系统 | 文件 | 要求 |
| --- | --- | --- |
| Windows | research-progress-recorder-0.2.8-Windows.zip | Windows 10/11 + .NET Framework 4.8 |
| Apple Silicon | research-progress-recorder-0.2.8-macOS-arm64.dmg | macOS 14+ |
| Intel Mac | research-progress-recorder-0.2.8-macOS-x64.dmg | macOS 15+ |

下载不需要 GitHub 登录；摘要见本 Release 的 SHA256SUMS.txt。

## 修复 / Fixes

- Mac 最小窗口下登录按钮、检查更新按钮和版本号完整显示；团队、进度、帮助工具栏按宽度换行，说明文字自动折行。帮助页增加滚动条。
- Mac 未登录时帮助正文只读；修复栏目红点刷新和退出时 Public_Temp 清理。
- 两端统一上传参考资料副本位置为 PersonalWorkspaceSRRec/Reference；撤除时删除正确的本机副本，并兼容旧 Mac 嵌套副本。
- 两端进度上传保留 Mat_Local 中的子目录；另选文件夹和添加外部文件均使用正确目标路径。外部文件按文件名上传，重名冲突在任何上传前拒绝。
- App UI screenshots are captured on real macOS arm64 and Intel virtual machines at normal and minimum window sizes. Preview data is synthetic and offline.

## 安装与升级 / Install and upgrade

可使用左下角“检查更新”从 0.2.5–0.2.7 升级。也可手动关闭程序后替换 Windows ZIP 中的程序文件，或把 Mac DMG 中的 app 拖入 Applications。
保留 Windows exe.config；data_version.txt 仍为 0.2.0，本次升级不重复清理配置、个人进度和资料。

## 限制 / Limitations

- Windows 无商业代码签名；Mac 是 ad-hoc 签名、未公证，首次打开可能出现系统提示。
- 本次验证使用隔离测试和离线示例数据；同僚双账号、双设备的真实协作仍需验收。
- Manual synchronization remains manual; concurrent edits are not merged automatically.
