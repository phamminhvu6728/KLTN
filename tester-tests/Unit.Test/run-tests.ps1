param([string]$Filter = "")

$project = Join-Path $PSScriptRoot 'Unit.Test.csproj'
$results = Join-Path $PSScriptRoot 'TestResults'
$trx = Join-Path $results 'UnitTest_Report.trx'
$details = Join-Path $PSScriptRoot 'bin\Debug\net10.0\TestResults\UnitTest_Details.jsonl'
$stdout = Join-Path $results 'dotnet-stdout.log'
$stderr = Join-Path $results 'dotnet-stderr.log'

if (-not (Test-Path $project)) {
    Write-Host '[ERROR] Khong tim thay Unit.Test.csproj.' -ForegroundColor Red
    exit 2
}
New-Item -ItemType Directory -Path $results -Force | Out-Null
Remove-Item $trx, $details, $stdout, $stderr -ErrorAction SilentlyContinue

# Goi dotnet truc tiep, khong dung Start-Process -Wait.
# Mang tham so giup truyen dung duong dan va filter co khoang trang.
$testArgs = @(
    'test', $project,
    '--no-restore',
    '--logger', 'trx;LogFileName=UnitTest_Report.trx',
    '--results-directory', $results,
    '--verbosity', 'quiet',
    '--nologo'
)
if (-not [string]::IsNullOrWhiteSpace($Filter)) {
    $testArgs += @('--filter', $Filter)
}

try {
    # Luu dau ra vao log, khong in stack trace dai len man hinh.
    # dotnet test FAIL co exit code 1 la binh thuong; van doc TRX.
    & dotnet @testArgs 1> $stdout 2> $stderr
    $exitCode = $LASTEXITCODE
} catch {
    Write-Host '[ERROR] Khong chay duoc dotnet test.' -ForegroundColor Red
    Write-Host $_.Exception.Message
    exit 2
}

if (-not (Test-Path $trx)) {
    Write-Host '[ERROR] Test khong tao bao cao TRX. Loi build hoac dotnet test:' -ForegroundColor Red
    if (Test-Path $stdout) { Get-Content $stdout -Tail 15 }
    if (Test-Path $stderr) { Get-Content $stderr -Tail 15 }
    exit 2
}

try {
    [xml]$xml = Get-Content $trx -Raw -Encoding UTF8
    $tests = @($xml.SelectNodes("//*[local-name()='UnitTestResult']"))
} catch {
    Write-Host '[ERROR] File TRX khong hop le.' -ForegroundColor Red
    exit 2
}
if ($tests.Count -eq 0) {
    Write-Host '[NOT RUN] Khong tim thay test phu hop.' -ForegroundColor Yellow
    exit 3
}

# Lay du lieu thuc te do cac bai test C# ghi, khong hard-code trong PS1.
$byId = @{}
if (Test-Path $details) {
    foreach ($line in (Get-Content $details -Encoding UTF8)) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        try {
            $item = $line | ConvertFrom-Json
            $byId[[string]$item.id] = $item
        } catch { }
    }
}

Write-Host '========================================'
Write-Host 'UNIT TESTING - KET QUA KIEM THU' -ForegroundColor Cyan
Write-Host '========================================'

$pass = 0; $fail = 0; $skip = 0
foreach ($test in ($tests | Sort-Object { [string]$_.GetAttribute('testName') })) {
    $name = [string]$test.GetAttribute('testName')
    $outcome = [string]$test.GetAttribute('outcome')
    switch ($outcome) {
        'Passed' { $pass++; $state = 'PASS'; $color = 'Green' }
        'Failed' { $fail++; $state = 'FAIL'; $color = 'Red' }
        default  { $skip++; $state = 'SKIP'; $color = 'Yellow' }
    }

    $match = [regex]::Match($name, 'UT-\d{2}[a-z]?')
    $id = if ($match.Success) { $match.Value } else { $name }
    $record = if ($byId.ContainsKey($id)) { $byId[$id] } else { $null }
    $title = if ($null -ne $record) { "$id | $($record.step) | $($record.title)" } else { $name }
    Write-Host "[$state] $title" -ForegroundColor $color

    if ($null -ne $record) {
        Write-Host "  Dau vao: $($record.input)"
        Write-Host "  Ky vong: $($record.expected)"
        Write-Host "  Thuc te: $($record.actual)"
        if ($state -eq 'FAIL') {
            $reason = [string]$record.problem
            if (-not [string]::IsNullOrWhiteSpace($reason)) {
                $firstReason = @($reason -split '\r?\n' | Select-Object -First 1)[0]
                Write-Host "  Ly do FAIL: $firstReason"
            }
        }
    } else {
        if ($state -eq 'FAIL') {
            $node = $test.SelectSingleNode("./*[local-name()='Output']/*[local-name()='ErrorInfo']/*[local-name()='Message']")
            if ($null -ne $node) {
                $first = @($node.InnerText -split '\r?\n' | Where-Object { $_.Trim() } | Select-Object -First 1)
                if ($first.Count -gt 0) { Write-Host "  Ly do FAIL: $($first[0])" }
            }
        }
        Write-Host '  Chi tiet: test chua ghi ban ghi ket qua thuc te (JSONL).' -ForegroundColor Yellow
    }
    $conclusion = if ($state -eq 'PASS') { 'ASSERT DAT' } elseif ($state -eq 'FAIL') { 'KHONG DAT' } else { 'CHUA CHAY' }
    Write-Host "  Ket luan: $conclusion"
    Write-Host ''
}
Write-Host '----------------------------------------'
Write-Host "TONG KET: $($tests.Count) | PASS: $pass | FAIL: $fail | SKIP: $skip"
Write-Host '========================================'
if ($fail -gt 0 -or $exitCode -ne 0) { exit 1 }
exit 0
