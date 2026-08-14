param(
    [string]$Revision = "HEAD"
)

$ErrorActionPreference = "Stop"
$allowedEmailPattern = "^(?:[^@\s]+@users\.noreply\.github\.com|noreply@github\.com)$"
$format = "%H%x09%ae%x09%ce"
$rows = & git log $Revision --format=$format

if ($LASTEXITCODE -ne 0)
{
    throw "Unable to inspect Git commit metadata."
}

$violations = [System.Collections.Generic.List[string]]::new()

foreach ($row in $rows)
{
    $fields = $row -split "`t", 3
    if ($fields.Count -ne 3)
    {
        throw "Unexpected Git log output while inspecting commit metadata."
    }

    $commit = $fields[0]
    if ($fields[1] -notmatch $allowedEmailPattern)
    {
        $violations.Add("$commit (author)")
    }

    if ($fields[2] -notmatch $allowedEmailPattern)
    {
        $violations.Add("$commit (committer)")
    }
}

if ($violations.Count -gt 0)
{
    $locations = $violations -join ", "
    throw "Private or non-GitHub-noreply commit email detected at: $locations. The email value is intentionally hidden. Rewrite the commit before pushing."
}

Write-Host "Commit email privacy check passed for $Revision."
