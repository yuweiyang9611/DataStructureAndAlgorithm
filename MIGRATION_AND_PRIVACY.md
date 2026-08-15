# 仓库迁移与隐私说明

本公开仓库由原私有仓库的当前文件快照重新建立。

原私有仓库的 Git 提交元数据和 Pull Request 记录包含个人隐私信息。为避免这些信息随仓库公开，旧仓库的提交历史、分支、标签和 Pull Request 记录均未迁移；原私有仓库已经删除。本仓库从一个全新的根提交开始，因此旧提交 SHA 和旧 Pull Request 编号不属于本仓库。

这次迁移只放弃版本控制历史，不改变当前代码快照的内容和用途。

## 邮箱隐私要求

本仓库的提交作者邮箱和提交者邮箱只允许使用 GitHub 提供的 noreply 地址：

- `<数字 ID>+<GitHub 用户名>@users.noreply.github.com`
- `noreply@github.com`

提交前请在 GitHub 的邮箱设置中启用邮箱隐私，并将 Git 配置为 GitHub noreply 地址。不要在 Issue、Pull Request、评论、提交消息、日志或文档中粘贴私人邮箱。如果分支中已经出现私人邮箱，应先在本地重写相关提交，再推送或创建 Pull Request。

仓库使用可跟踪的本地 Git hook 和持续集成检查提交元数据。检查失败时只报告提交 SHA 和违规字段，不回显邮箱本身。

克隆仓库后，推荐运行启用脚本：

```powershell
pwsh -NoProfile -File ./tools/enable_git_hooks.ps1
```

没有 PowerShell 时可直接运行等价命令：

```bash
git config --local core.hooksPath .githooks
```

该配置只作用于当前克隆。启用后，`pre-commit` 会拒绝使用非 GitHub noreply 身份的新提交，`pre-push` 会检查所推送引用可达的全部提交。完整步骤见 [Git 隐私 hooks 启用说明](GIT_HOOKS_SETUP.md)。
