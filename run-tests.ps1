$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
dotnet test tests/TuTuc.Tests/TuTuc.Tests.csproj -p:UseAppHost=false --collect:'XPlat Code Coverage' --settings tests/coverage.runsettings --results-directory TestResults --logger:'trx;LogFileName=acceptance.trx'
if ($LASTEXITCODE -ne 0) { throw 'Tests failed. See TestResults/acceptance.trx.' }
$coverageFile = Get-ChildItem -LiteralPath TestResults -Recurse -Filter coverage.cobertura.xml | Sort-Object LastWriteTime -Descending | Select-Object -First 1
[xml]$coverage = Get-Content -LiteralPath $coverageFile.FullName
[xml]$trx = Get-Content -LiteralPath 'TestResults/acceptance.trx'
$line = [double]::Parse($coverage.coverage.'line-rate',[cultureinfo]::InvariantCulture) * 100
$branch = [double]::Parse($coverage.coverage.'branch-rate',[cultureinfo]::InvariantCulture) * 100
$summary = [ordered]@{
    Time = (Get-Date).ToString('o')
    Total = $trx.TestRun.ResultSummary.Counters.total
    Passed = $trx.TestRun.ResultSummary.Counters.passed
    Failed = $trx.TestRun.ResultSummary.Counters.failed
    LineCoveragePercent = [math]::Round($line,2)
    BranchCoveragePercent = [math]::Round($branch,2)
    CoverageFile = $coverageFile.FullName
    Note = 'Excludes generated obj and EF migration files. Line coverage is distinct from branch coverage and acceptance.'
}
$summary | ConvertTo-Json | Set-Content -LiteralPath 'TestResults/summary.json' -Encoding utf8
$summary | Format-List
if ($line -lt 80) { throw 'Line coverage below the 80% target.' }
Write-Host 'PASS: tests passed and combined line coverage >= 80%.' -ForegroundColor Green
