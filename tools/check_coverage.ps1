param(
    [Parameter(Mandatory = $true)]
    [string]$Report,

    [ValidateRange(0, 100)]
    [double]$MinimumLineCoverage = 80,

    [ValidateRange(0, 100)]
    [double]$MinimumBranchCoverage = 70
)

$ErrorActionPreference = 'Stop'
$reports = Get-ChildItem -Path $Report -Filter coverage.cobertura.xml -File -Recurse -ErrorAction Stop
if ($reports.Count -ne 1) {
    throw "Expected exactly one Cobertura report under '$Report', found $($reports.Count)."
}

[xml]$coverage = Get-Content -LiteralPath $reports[0].FullName -Raw
$lineCoverage = [double]$coverage.coverage.'line-rate' * 100
$branchCoverage = [double]$coverage.coverage.'branch-rate' * 100

Write-Host ("Line coverage:   {0:N2}% (minimum {1:N2}%)" -f $lineCoverage, $MinimumLineCoverage)
Write-Host ("Branch coverage: {0:N2}% (minimum {1:N2}%)" -f $branchCoverage, $MinimumBranchCoverage)

if ($lineCoverage -lt $MinimumLineCoverage -or $branchCoverage -lt $MinimumBranchCoverage) {
    throw 'Coverage is below the repository quality gate.'
}
