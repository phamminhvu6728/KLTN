param(
    # Mac dinh chay tat ca IT-01, IT-02,... (bo qua IT00 kiem tra moi truong).
    # Vi du chay rieng: -Filter 'FullyQualifiedName~IT_02'
    [string]$Filter = 'FullyQualifiedName~IT_'
)

$project = Join-Path $PSScriptRoot 'Integration.Test.csproj'
$results = Join-Path $PSScriptRoot 'TestResults'
$trx = Join-Path $results 'IntegrationTest_Report.trx'
$details = Join-Path $PSScriptRoot 'bin\Debug\net10.0\TestResults\IntegrationTest_Details.jsonl'
$stdout = Join-Path $results 'dotnet-stdout.log'
$stderr = Join-Path $results 'dotnet-stderr.log'

New-Item -ItemType Directory -Path $results -Force | Out-Null
Remove-Item $trx, $details, $stdout, $stderr -Force -ErrorAction SilentlyContinue

$argsTest = @(
    'test', $project,
    '--logger', 'trx;LogFileName=IntegrationTest_Report.trx',
    '--results-directory', $results,
    '--verbosity', 'quiet',
    '--nologo'
)
if (-not [string]::IsNullOrWhiteSpace($Filter)) {
    $argsTest += @('--filter', $Filter)
}

& dotnet @argsTest 1> $stdout 2> $stderr
$dotnetExit = $LASTEXITCODE

if (-not (Test-Path $trx)) {
    Write-Host '[ERROR] Khong tao duoc bao cao TRX. Kiem tra loi build/chay:' -ForegroundColor Red
    if (Test-Path $stdout) { Get-Content $stdout -Tail 35 }
    if (Test-Path $stderr) { Get-Content $stderr -Tail 20 }
    exit 2
}

try { [xml]$xml = Get-Content $trx -Raw -Encoding UTF8 }
catch {
    Write-Host "[ERROR] Khong doc duoc TRX: $_" -ForegroundColor Red
    exit 2
}
$tests = @($xml.SelectNodes("//*[local-name()='UnitTestResult']"))
# Sap xep CHI PHAN HIEN THI theo ma IT tang dan, khong can thay doi thu tu xUnit thuc thi.
$tests = @($tests | Sort-Object -Property @{ Expression = {
    $testName = [string]$_.GetAttribute('testName')
    $m = [regex]::Match($testName, 'IT[_-](\d{1,3})(?!\d)')
    if ($m.Success) { [int]$m.Groups[1].Value } else { [int]::MaxValue }
}}, @{ Expression = { [string]$_.GetAttribute('testName') } })
if ($tests.Count -eq 0) {
    Write-Host '[ERROR] Khong tim thay test nao khop bo loc.' -ForegroundColor Yellow
    exit 3
}

$records = @{}
if (Test-Path $details) {
    Get-Content $details -Encoding UTF8 | ForEach-Object {
        if (-not [string]::IsNullOrWhiteSpace($_)) {
            try {
                $r = $_ | ConvertFrom-Json
                if ($null -ne $r.id) { $records[[string]$r.id] = $r }
            } catch { }
        }
    }
}

Write-Host '========================================'
Write-Host 'INTEGRATION TESTING - KET QUA KIEM THU' -ForegroundColor Cyan
Write-Host '========================================'
$pass = 0; $fail = 0; $skip = 0
foreach ($t in $tests) {
    $state = [string]$t.GetAttribute('outcome')
    if ($state -eq 'Passed') { $pass++; $tag = 'PASS'; $color = 'Green' }
    elseif ($state -eq 'Failed') { $fail++; $tag = 'FAIL'; $color = 'Red' }
    else { $skip++; $tag = 'SKIP'; $color = 'Yellow' }

    $name = [string]$t.GetAttribute('testName')
    $match = [regex]::Match($name, 'IT[_-](\d{1,3})(?!\d)')
    $id = if ($match.Success) { 'IT-' + ([int]$match.Groups[1].Value).ToString('00') } else { $name }
    $r = if ($records.ContainsKey($id)) { $records[$id] } else { $null }
    $label = if ($null -ne $r) { "$id | $($r.title)" } else { $name }
    Write-Host "[$tag] $label" -ForegroundColor $color

    if ($null -ne $r) {
        Write-Host "  Tien DK: $($r.precondition)"
        Write-Host "  Dau vao: $($r.input)"
        Write-Host "  Ky vong: $($r.expected)"
        Write-Host "  Thuc te: $($r.actual)"
        if ($tag -eq 'FAIL') {
            $reason = if (-not [string]::IsNullOrWhiteSpace([string]$r.problem)) { $r.problem } else { '(Xem bao cao TRX)' }
            Write-Host "  Ly do FAIL: $reason"
        }
    } elseif ($tag -eq 'FAIL') {
        $node = $t.SelectSingleNode("./*[local-name()='Output']/*[local-name()='ErrorInfo']/*[local-name()='Message']")
        if ($null -ne $node) { Write-Host "  Ly do FAIL: $($node.InnerText)" }
    }

    $conclusion = if ($tag -eq 'PASS') { 'ASSERT DAT' } elseif ($tag -eq 'FAIL') { 'KHONG DAT' } else { 'BO QUA' }
    Write-Host "  Ket luan: $conclusion"
    Write-Host ''
}
Write-Host '----------------------------------------'
Write-Host "TONG KET: $($tests.Count) | PASS: $pass | FAIL: $fail | SKIP: $skip"
Write-Host '========================================'
Write-Host "Minh chung TRX: $trx"
if ($dotnetExit -ne 0 -or $fail -gt 0) { exit 1 }
exit 0
