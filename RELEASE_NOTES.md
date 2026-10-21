# SRPRecorder2027 0.2.2 · 公开测试版 / Public Test

发布日期 / Released: 2026-10-21

## 下载 / Download

| 系统 | 文件 | 要求 |
| --- | --- | --- |
| Windows 10/11 | research-progress-recorder-0.2.2-Windows.zip | .NET Framework 4.8 |
| Apple Silicon / M 系列 | research-progress-recorder-0.2.2-macOS-arm64.dmg | macOS 14+ |
| Intel Mac | research-progress-recorder-0.2.2-macOS-x64.dmg | macOS 15+ |

三个文件都在本 Release 的 Assets 里，另有 `SHA256SUMS.txt` 可核对摘要。下载不需要登录，也不需要令牌。

## 安装 / Install

- **Windows**：关闭旧程序 → 解压 ZIP 到自己的可写文件夹 → 覆盖旧目录 → 运行 `SRCRec\SRPRecorder2027.exe`。必须保留随 exe 的 `.exe.config`。
- **macOS**：打开对应芯片的 DMG → 把 `ResearchProgressRecorder.app` 拖进 Applications 覆盖旧版 → 弹出镜像。首次打开若被系统拦截，右键 →“打开”。

可以直接覆盖任何旧版本（0.0.x / 0.1.x / 0.2.x），不需要先卸载。

## 0.2.2 主要变化

### 工作区文件夹（Windows 与 macOS 同构）

程序位于用户选择的 `SRCRec` 下，默认本地进度文件夹固定为 `SRCRec\PersonalWorkspaceSRRec`，首次启动自动创建，内含：

```
PersonalWorkspaceSRRec\
├─ Mat_Local\     非参考资料的个人本地资料（上传本地进度的默认来源）
├─ Reference\     个人参考资料（上传参考资料的默认来源）
└─ Public_Temp\   他人内容临时区（退出时清空，下次启动前也清空）
```

- 上传参考资料默认从 `Reference` 列文件，上传本地进度默认从 `Mat_Local` 列文件，两者都能“添加文件”选别处。
- 下载他人内容落到 `Public_Temp\<成员>`，只用于本次查看，程序关闭即清除。
- 参考资料页 = 团队默认库 + 我的 `Reference` + `Public_Temp` 下各成员内容。

### 未读红点细分

不再一律红在“团队同步”。团队研究=成员变化；Quick Post=未读帖子或其帖子日志；参考资料=上传/撤除参考资料、下载默认资料库的日志；本地进度=上传本地进度日志或 `Mat_Local`/`Reference` 文件变化；团队同步=活动日志；使用说明=`help.md` 或说明改动日志。点进对应栏目即消失。

### 界面修复

- 团队研究页“个人研究”一行的按钮不再压住路径文字，路径改为按钮下方整行显示并自动省略。
- 其它页面沿用 0.2.1 的窄窗口布局修复。

## 累积功能（相对 0.1.0）

登录面板与未登录拦截、团队研究成员列表、撤除我上传的参考资料、Quick Post、活动日志、可编辑使用说明、同步动作先预览再勾选、双语界面、新建 Word 模板（项目/日期/目的 + 留白）。

## 限制 / Limitations

- 两端都未做商业代码签名：Windows 首次运行提示 SmartScreen（更多信息 → 仍要运行），macOS 为 ad-hoc 签名（右键打开）。不影响本地数据。
- 仍需真实双账号、双设备同步验收；自动化检查不保证没有缺陷。
- 公开包不含团队书籍、研究文档、个人 JSON 或令牌。
