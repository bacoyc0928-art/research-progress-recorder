# SRPRecorder2027 0.2.10 · 公开测试版 / Public Test

发布日期 / Released: 2026-11-03

## 下载 / Download

| 系统 | 文件 | 要求 |
| --- | --- | --- |
| Windows 10/11 | research-progress-recorder-0.2.10-Windows.zip | .NET Framework 4.8 |
| Apple Silicon / M 系列 | research-progress-recorder-0.2.10-macOS-arm64.dmg | macOS 14+ |
| Intel Mac | research-progress-recorder-0.2.10-macOS-x64.dmg | macOS 15+ |

三个文件都在本 Release 的 Assets 里，另有 `SHA256SUMS.txt` 可核对摘要。下载不需要登录，也不需要令牌。

## 安装 / Install

- **Windows**：关闭旧程序 → 解压 ZIP 到自己的可写文件夹 → 覆盖旧目录 → 运行 `SRCRec\SRPRecorder2027.exe`。必须保留随 exe 的 `.exe.config`。
- **macOS**：打开对应芯片的 DMG → 把 `ResearchProgressRecorder.app` 拖进 Applications 覆盖旧版 → 弹出镜像。

可直接覆盖任何旧版本；配置、日志、下载缓存与个人资料保留。0.2.5 起可在程序内「检查更新」升级。

## 0.2.10 主要变化

### 「下载他人内容」列出全部成员

- 之前只列 `team/` 下已上传的成员；如果其他成员都没上传，列表就是空的。
- 现在列表 = 已上传成员 ∪ 仓库协作者 ∪ 待接受邀请者：
  - 有内容：`账号 · references: N · progress ✓/—`
  - 无内容：`账号 · 尚未上传 / no upload yet`
- 勾选无内容的成员不会下载任何文件；真正有内容的成员才会计入下载数量。
- Windows 与 macOS 同步修复。

### 保留

- 成员资料页的连接状态（登录过即“已连接”）、界面改名、本地记录卡片、中英文切换、左下角检查更新、记住登录 + 6 位 PIN。

## 限制 / Limitations

- 两端都未做商业代码签名：Windows 首次运行提示 SmartScreen，macOS 为 ad-hoc 签名。
- 应用内更新依赖 GitHub API/Releases；离线请手动下载。
- 仍需真实双账号、双设备同步验收。
