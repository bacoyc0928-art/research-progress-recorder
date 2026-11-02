# SRPRecorder2027 0.2.4 · 公开测试版 / Public Test

发布日期 / Released: 2026-11-02

## 下载 / Download

| 系统 | 文件 | 要求 |
| --- | --- | --- |
| Windows 10/11 | research-progress-recorder-0.2.4-Windows.zip | .NET Framework 4.8 |
| Apple Silicon / M 系列 | research-progress-recorder-0.2.4-macOS-arm64.dmg | macOS 14+ |
| Intel Mac | research-progress-recorder-0.2.4-macOS-x64.dmg | macOS 15+ |

三个文件都在本 Release 的 Assets 里，另有 `SHA256SUMS.txt` 可核对摘要。下载不需要登录，也不需要令牌。

## 安装 / Install

- **Windows**：关闭旧程序 → 解压 ZIP 到自己的可写文件夹 → 覆盖旧目录 → 运行 `SRCRec\SRPRecorder2027.exe`。必须保留随 exe 的 `.exe.config`。
- **macOS**：打开对应芯片的 DMG → 把 `ResearchProgressRecorder.app` 拖进 Applications 覆盖旧版 → 弹出镜像。首次打开若被系统拦截，右键 →“打开”。

可以直接覆盖任何旧版本（0.0.x / 0.1.x / 0.2.x），不需要先卸载；配置、日志、下载缓存与个人资料保留。

## 0.2.4 主要变化

### 记住登录 + 6 位 PIN（Windows 与 macOS）

- 第一次仍用**自己的 GitHub 令牌**登录；勾选“记住登录”，设置 6 位数字 PIN。
- 令牌存入系统凭据库（Windows 凭据管理器 / macOS 钥匙串），下次启动只输 6 位 PIN 即可解锁，不再粘贴长令牌。
- 连续 5 次 PIN 错误会清除保存的登录，必须改用令牌重新登录。
- PIN 只是本机便捷锁，真正的保护来自系统账户与凭据库；它不是服务器端密码，也不能跨设备使用。
- “退出”仍会直接关闭程序；保存的登录保留，直到点“忘记”或连续输错 5 次。

### 使用说明补齐整体数据架构

- 软件内「使用说明」页新增：数据流总览、Windows / macOS 本机目录、私有仓库 `research-data` 结构、五个同步动作的落点、清理与升级规则。
- GitHub 首页 `README.md` 只保留下载链接；完整中英双语说明独立为仓库内 `使用说明.md`，并随 Windows ZIP 打包。

### 其它

- 修复 macOS 把应用版本误当数据迁移标记的问题（0.2.3 起）。
- 使用说明页的双语按钮与树状目录在窄窗口下不再截断。
- 工作区、红点、Quick Post、活动日志等 0.2.3 规则保持不变。

## 工作区与存储

```
SRCRec\
├─ PersonalWorkspaceSRRec\   默认本地进度文件夹
│  ├─ Mat_Local\             非参考资料（上传本地进度的默认来源）
│  ├─ Reference\             个人参考资料（上传参考资料的默认来源）
│  └─ Public_Temp\           他人内容临时区（启动/退出清空）
└─ References\Default\       团队默认资料库（只读）
```

`References\<账号>`、`References\references`、`References\references.json`、`personal.json` 均为废弃路径，启动时自动清理；“个人文件夹”概念已取消。

## 限制 / Limitations

- 两端都未做商业代码签名：Windows 首次运行提示 SmartScreen（更多信息 → 仍要运行），macOS 为 ad-hoc 签名（右键打开）。不影响本地数据。
- 仍需真实双账号、双设备同步验收；自动化检查不保证没有缺陷。
- 公开包不含团队书籍、研究文档、个人 JSON 或令牌。
- 记住登录只在本机有效；更换电脑或删除凭据后需重新输入令牌。
