[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Get-Command git -ErrorAction SilentlyContinue))
{
    throw "Git is required but was not found on PATH."
}

$repositoryRootOutput = & git -C $PSScriptRoot rev-parse --show-toplevel 2>$null
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($repositoryRootOutput))
{
    throw "Unable to locate the Git repository containing this script."
}

$repositoryRoot = [System.IO.Path]::GetFullPath($repositoryRootOutput.Trim())
$hooksDirectory = Join-Path $repositoryRoot ".githooks"
$requiredHooks = @("pre-commit", "pre-push")

foreach ($hook in $requiredHooks)
{
    $hookPath = Join-Path $hooksDirectory $hook
    if (-not (Test-Path -LiteralPath $hookPath -PathType Leaf))
    {
        throw "Required hook is missing: .githooks/$hook"
    }
}

& git -C $repositoryRoot config --local core.hooksPath .githooks
if ($LASTEXITCODE -ne 0)
{
    throw "Unable to configure core.hooksPath for this clone."
}

$configuredHooksPath = (& git -C $repositoryRoot config --local --get core.hooksPath).Trim()
if ($LASTEXITCODE -ne 0 -or $configuredHooksPath -ne ".githooks")
{
    throw "core.hooksPath verification failed."
}

Write-Host "Git privacy hooks are enabled for this clone."
Write-Host "core.hooksPath=$configuredHooksPath"
