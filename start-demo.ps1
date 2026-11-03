$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
$demoUrl = 'http://localhost:5080'
function Test-DemoReady {
    try { $health = Invoke-RestMethod "$demoUrl/health" -TimeoutSec 2; return $health.application -eq 'TuTuc' -and $health.status -eq 'ready' } catch { return $false }
}
if (Test-DemoReady) {
    Write-Host "Website dang chay: $demoUrl"
    Start-Process $demoUrl
    exit 0
}
if (Get-NetTCPConnection -LocalPort 5080 -State Listen -ErrorAction SilentlyContinue) {
    Write-Error 'Cong 5080 dang bi chiem boi ung dung khac hoac ban cu. Dong dung cua so website cu (Ctrl+C), sau do chay lai.'
    exit 1
}
dotnet build --nologo
if ($LASTEXITCODE -ne 0) { throw 'Build failed. See errors above.' }
$logDirectory = Join-Path $PSScriptRoot '.data'
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
$server = Start-Process -FilePath (Get-Command dotnet).Source -ArgumentList @('bin/Debug/net10.0/TuTuc.dll','--urls',$demoUrl) -WorkingDirectory $PSScriptRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $logDirectory 'demo.stdout.log') -RedirectStandardError (Join-Path $logDirectory 'demo.stderr.log')
for ($attempt=0; $attempt -lt 40; $attempt++) {
    if (Test-DemoReady) {
        Write-Host "Website san sang: $demoUrl (PID $($server.Id))" -ForegroundColor Green
        Start-Process $demoUrl
        exit 0
    }
    if ($server.HasExited) { break }
    Start-Sleep -Milliseconds 500
}
Get-Content -LiteralPath (Join-Path $logDirectory 'demo.stderr.log') -Tail 25
throw 'Server chua san sang. Xem .data/demo.stdout.log va .data/demo.stderr.log.'
