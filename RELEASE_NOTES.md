# SRPRecorder2027 0.2.7 · 公开测试版 / Public Test

发布日期 / Released: 2026-11-02

## 下载 / Download

| 系统 | 文件 | 要求 |
| --- | --- | --- |
| Windows 10/11 | research-progress-recorder-0.2.7-Windows.zip | .NET Framework 4.8 |
| Apple Silicon / M 系列 | research-progress-recorder-0.2.7-macOS-arm64.dmg | macOS 14+ |
| Intel Mac | research-progress-recorder-0.2.7-macOS-x64.dmg | macOS 15+ |

三个文件都在本 Release 的 Assets 里，另有 `SHA256SUMS.txt` 可核对摘要。下载不需要登录，也不需要令牌。

## 安装 / Install

- **Windows**：关闭旧程序 → 解压 ZIP 到自己的可写文件夹 → 覆盖旧目录 → 运行 `SRCRec\SRPRecorder2027.exe`。必须保留随 exe 的 `.exe.config`。
- **macOS**：打开对应芯片的 DMG → 把 `ResearchProgressRecorder.app` 拖进 Applications 覆盖旧版 → 弹出镜像。首次打开若被系统拦截，右键 →“打开”。

可直接覆盖任何旧版本；配置、日志、下载缓存与个人资料保留。0.2.6 用户可直接点「检查更新」升级。

## 0.2.7 主要变化

### macOS 也把「检查更新」放到窗口左下角

- 窗口底部新增状态栏：最左侧是 **「检查更新 / Check updates」**，右侧是状态文字，最右显示 `v0.2.7`。
- 不再放在「使用说明」页工具栏；帮助页恢复为 4 个按钮。
- 与 Windows 的左下角按钮行为一致：读取最新 Release → 下载 DMG → 按 `SHA256SUMS.txt` 校验 → 退出后替换 `.app`，失败则打开 DMG。
- 不删除用户数据：`~/Library/Application Support/ResearchProgressRecorder/` 不受影响。

### Windows 保持 0.2.6 布局

- 左下侧栏「检查更新」+ `v0.2.7` 版本号不变；0.2.6 的按钮遮挡问题已在该版本重发的附件中修复。

## 工作区与存储

```
SRCRec\
├─ PersonalWorkspaceSRRec\   默认本地进度文件夹
│  ├─ Mat_Local\             非参考资料（上传本地进度的默认来源）
│  ├─ Reference\             个人参考资料（上传参考资料的默认来源）
│  └─ Public_Temp\           他人内容临时区（启动/退出清空）
└─ References\Default\       团队默认资料库（只读）
```

## 限制 / Limitations

- 两端都未做商业代码签名：Windows 首次运行提示 SmartScreen（更多信息 → 仍要运行），macOS 为 ad-hoc 签名（右键打开）。不影响本地数据。
- 应用内更新依赖 `api.github.com` 与 GitHub Releases；离线或代理受限时请手动下载。
- 仍需真实双账号、双设备同步验收。
