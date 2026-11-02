# SRPRecorder2027 0.2.5 · 公开测试版 / Public Test

发布日期 / Released: 2026-11-02

## 下载 / Download

| 系统 | 文件 | 要求 |
| --- | --- | --- |
| Windows 10/11 | research-progress-recorder-0.2.5-Windows.zip | .NET Framework 4.8 |
| Apple Silicon / M 系列 | research-progress-recorder-0.2.5-macOS-arm64.dmg | macOS 14+ |
| Intel Mac | research-progress-recorder-0.2.5-macOS-x64.dmg | macOS 15+ |

三个文件都在本 Release 的 Assets 里，另有 `SHA256SUMS.txt` 可核对摘要。下载不需要登录，也不需要令牌。

## 安装 / Install

- **Windows**：关闭旧程序 → 解压 ZIP 到自己的可写文件夹 → 覆盖旧目录 → 运行 `SRCRec\SRPRecorder2027.exe`。必须保留随 exe 的 `.exe.config`。
- **macOS**：打开对应芯片的 DMG → 把 `ResearchProgressRecorder.app` 拖进 Applications 覆盖旧版 → 弹出镜像。首次打开若被系统拦截，右键 →“打开”。

可直接覆盖任何旧版本；配置、日志、下载缓存与个人资料保留。

## 0.2.5 主要变化

### 应用内更新（Windows 与 macOS）

- 「使用说明 / Help」页新增 **「检查更新 / Check updates」**。
- 点击后读取公开仓库的最新 Release；有新版时显示版本与发布说明，确认后下载对应安装包。
- 下载完成后按同一 Release 的 `SHA256SUMS.txt` 校验 SHA-256，校验失败会丢弃并报错。
- **Windows**：退出程序后由脚本只替换程序文件（exe、配置、文档、`src`、卸载工具），自动重启；上一版 exe 保留为 `SRPRecorder2027.exe.previous`。
- **macOS**：退出后由脚本替换 `.app`；若没有权限，自动退回打开 DMG，由用户拖入 Applications。
- **不会删除用户数据**：`PersonalWorkspaceSRRec`、`References`、`*.json` 配置、下载缓存与 macOS `Application Support` 均不触碰。
- 注意：0.2.4 及更早版本没有这个按钮，需要**手动安装一次 0.2.5**；之后才能用应用内更新。

### 保留 0.2.4 的登录改进

- 记住登录 + 6 位 PIN（系统凭据库 + PBKDF2 校验，连续 5 次错误清除）。

## 工作区与存储

```
SRCRec\
├─ PersonalWorkspaceSRRec\   默认本地进度文件夹
│  ├─ Mat_Local\             非参考资料（上传本地进度的默认来源）
│  ├─ Reference\             个人参考资料（上传参考资料的默认来源）
│  └─ Public_Temp\           他人内容临时区（启动/退出清空）
└─ References\Default\       团队默认资料库（只读）
```

`References\<账号>`、`References\references`、`References\references.json`、`personal.json` 均为废弃路径，启动时自动清理。

## 限制 / Limitations

- 两端都未做商业代码签名：Windows 首次运行提示 SmartScreen（更多信息 → 仍要运行），macOS 为 ad-hoc 签名（右键打开）。不影响本地数据。
- 应用内更新依赖 `api.github.com` 与 GitHub Releases；离线或代理受限时请手动下载。
- 仍需真实双账号、双设备同步验收；自动化检查不保证没有缺陷。
- 公开包不含团队书籍、研究文档、个人 JSON 或令牌。
