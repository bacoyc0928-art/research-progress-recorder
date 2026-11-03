# SRPRecorder2027 0.2.9 · 公开测试版 / Public Test

发布日期 / Released: 2026-11-03

## 下载 / Download

| 系统 | 文件 | 要求 |
| --- | --- | --- |
| Windows 10/11 | research-progress-recorder-0.2.9-Windows.zip | .NET Framework 4.8 |
| Apple Silicon / M 系列 | research-progress-recorder-0.2.9-macOS-arm64.dmg | macOS 14+ |
| Intel Mac | research-progress-recorder-0.2.9-macOS-x64.dmg | macOS 15+ |

三个文件都在本 Release 的 Assets 里，另有 `SHA256SUMS.txt` 可核对摘要。下载不需要登录，也不需要令牌。

## 安装 / Install

- **Windows**：关闭旧程序 → 解压 ZIP 到自己的可写文件夹 → 覆盖旧目录 → 运行 `SRCRec\SRPRecorder2027.exe`。必须保留随 exe 的 `.exe.config`。
- **macOS**：打开对应芯片的 DMG → 把 `ResearchProgressRecorder.app` 拖进 Applications 覆盖旧版 → 弹出镜像。首次打开若被系统拦截，右键 →“打开”。

可直接覆盖任何旧版本；配置、日志、下载缓存与个人资料保留。0.2.5 起的版本可在程序内「检查更新」升级。

## 0.2.9 主要变化

### 成员列表与连接状态（Windows + macOS）

- **macOS 修复只显示自己**：成员列表现在 = 本人 ∪ 已上传成员 ∪ 仓库协作者，全部成员都会出现。
- **“已连接”按登录判定**：只要该账号在公开活动日志里出现过“登录”，就显示 **已连接 · 最近活动 时间**（绿色）。
- 其余状态：**已上传 · 无登录记录**、**已加入 · 未连接**、**待接受邀请**（仅所有者可见）。
- 自己的卡片显示“已连接 GitHub 账号”；进度同步页右上角也显示自己的连接状态。

### 界面改名

| 旧 | 新 |
|---|---|
| 团队研究 | **成员资料 / Members** |
| 团队同步 | **进度同步 / Progress Sync** |
| 本地进度 | **本地记录 / Local Records** |

### 本地记录页

- 三个固定文件夹改为功能卡片：工作资料 `Mat_Local`、个人参考资料 `Reference`、他人内容（临时）`Public_Temp`，标题是功能名、副标题保留原名与用途。
- 新建文档保存到 **`Mat_Local`**，按钮下方有说明；下方列表只列 `Mat_Local` 内容。

### 中英文切换

- Windows：左上角 `SR` 旁 **EN / 中文** 按钮；macOS：页头右侧；切换后单语显示并记住偏好（macOS 会重启应用）。

### 保留

- 左下角「检查更新」；记住登录 + 6 位 PIN；不删除用户数据。

## 限制 / Limitations

- 两端都未做商业代码签名：Windows 首次运行提示 SmartScreen，macOS 为 ad-hoc 签名。
- 应用内更新依赖 GitHub API/Releases；离线请手动下载。
- 仍需真实双账号、双设备同步验收。
