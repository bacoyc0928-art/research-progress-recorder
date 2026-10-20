# 研究进展记录器（Windows 桌面版）

此仓库提供桌面程序、完整 C# 源码和构建脚本。程序文件与团队进度数据分别存放：

- 程序仓库：`bacoyc0928-art/research-progress-recorder`（本仓库，私有）
- 数据仓库：`bacoyc0928-art/research-data`（私有），共享记录位于 `sync/research_log.json`

**仓库中没有个人访问令牌、本机 `research_log.json`、Word 文档或私人本地路径。**

## 下载和运行

1. 仓库所有者将同僚的 GitHub 用户名加入**程序仓库和数据仓库**的 Collaborators；同僚接受两份邀请。
2. 从本仓库下载 `研究进展记录器-Windows.zip` 并解压到个人有写权限的目录，运行其中的 `研究进展记录器.exe`。需要 Windows 10/11 和 .NET Framework 4.8。
3. 首次运行会在 exe 同目录创建本机 `research_log.json`。这个文件是个人本地缓存，不要提交到程序仓库。
4. 打开「团队同步」。仓库应为 `bacoyc0928-art/research-data`，路径为 `sync/research_log.json`，分支为 `main`。
5. 每人输入**自己的** GitHub 访问令牌，点「保存配置」「测试连接」，再点「一键同步」。此后编辑记录后再次点「一键同步」，其他人点「一键同步」读取新进度。目前不是后台自动同步。

## 各自的访问令牌

仓库所有者可使用仅选中 `research-data`、授予 Contents 读写权限的 fine-grained PAT。GitHub 当前不支持个人仓库的协作者用 fine-grained PAT 提交，所以同僚需使用各自账号创建有 `repo` 权限、设有到期日的 classic PAT。**不要把所有者的令牌发给同僚，也不要把任何令牌放入仓库或聊天。**classic PAT 权限较广；建议使用专门用于本项目的 GitHub 账号，或以后将仓库迁入组织并改用更细的权限方案。

令牌只保存在各自电脑的 Windows 凭据管理器。`sync_config.json` 只包含仓库地址、文件路径和分支。

## 同步范围与冲突

- 同步研究记录和类别名称、任务数、已完成数及预计工时。
- 同一条记录或类别发生并行修改时，最后更新时间较新的版本生效；上传前会读取远端并合并，提交冲突会重试。
- 删除记录/类别不会同步为远端删除；请勿把「本机删除」当作团队删除。
- 参考资料、本地 DOCX 文件、文件路径和类别里的本地文件链接不上传。Word 文档保存在个人电脑的 `本地文档` 文件夹，需另行共享时请使用双方都能访问的文件服务。

## 从源码构建

在 Windows PowerShell 中运行 `./build.ps1`。脚本用系统 .NET Framework C# 编译器生成 exe，不下载依赖。源码位于 `src/`。

## 维护

升级时仅替换 exe；保留个人电脑上的 `research_log.json` 和 `sync_config.json`。发布包中不应包含这些个人文件。此程序当前未签名，安装前可核对仓库内容和下载包的 SHA-256。
