# 启用 Git 隐私 Hooks

Git 出于安全原因，不会在克隆仓库时自动启用仓库内的 hooks。每个新克隆都需要执行一次本地配置。

## 推荐方式

在项目根目录运行：

```powershell
pwsh -NoProfile -File ./tools/enable_git_hooks.ps1
```

Windows PowerShell 也可以运行同一个脚本：

```powershell
powershell -NoProfile -ExecutionPolicy RemoteSigned -File .\tools\enable_git_hooks.ps1
```

这里的 `RemoteSigned` 只作用于本次 PowerShell 进程，不会永久修改系统执行策略。如果设备由组织策略禁止运行脚本，请使用下方的直接 Git 命令。

脚本可以重复执行。它只修改当前克隆的 `.git/config`，不会修改全局 Git 配置。

## 没有 PowerShell

直接运行等价的 Git 命令：

```bash
git config --local core.hooksPath .githooks
```

## 验证

```bash
git config --local --get core.hooksPath
```

预期输出：

```text
.githooks
```

启用后：

- `pre-commit` 拒绝使用非 GitHub noreply 作者或提交者邮箱的新提交。
- `pre-push` 检查待推送引用可达的全部提交，并拒绝包含非 GitHub noreply 邮箱的历史。
- 两个 hook 的错误信息不会回显被拒绝的邮箱。

还可以随时运行以下只读检查：

```powershell
pwsh -NoProfile -File ./tools/check_git_emails.ps1
```

更多背景参阅 [仓库迁移与隐私说明](MIGRATION_AND_PRIVACY.md)。
