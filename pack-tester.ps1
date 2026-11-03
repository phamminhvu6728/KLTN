$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath($PSScriptRoot)
$handoffRoot = Join-Path $projectRoot '.data\handoff'
New-Item -ItemType Directory -Path $handoffRoot -Force | Out-Null
$archivePath = Join-Path $handoffRoot ('TuTuc-tester-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.zip')
$sourceFolders = @('Controllers','Data','Models','Properties','Services','Views','wwwroot','tests','docs')
$sourceFiles = @(Get-ChildItem -LiteralPath $projectRoot -File | Where-Object { $_.Extension -in @('.cs','.csproj','.json','.md','.ps1','.cmd','.mjs') })
foreach ($folder in $sourceFolders) {
    $sourceFiles += Get-ChildItem -LiteralPath (Join-Path $projectRoot $folder) -File -Recurse |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj|__pycache__|node_modules)[\\/]' }
}
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.IO.Compression
$archive = [IO.Compression.ZipFile]::Open($archivePath, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in $sourceFiles) {
        $relative = $file.FullName.Substring($projectRoot.Length + 1).Replace('\','/')
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive,$file.FullName,$relative,[IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
    foreach ($evidence in @('summary.json','acceptance.trx')) {
        $evidencePath = Join-Path $projectRoot ('TestResults\' + $evidence)
        if (Test-Path -LiteralPath $evidencePath) {
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive,$evidencePath,'evidence/' + $evidence,[IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    }
    $summaryPath=Join-Path $projectRoot 'TestResults\summary.json'
    if(Test-Path -LiteralPath $summaryPath){
        $summary=Get-Content -LiteralPath $summaryPath -Raw | ConvertFrom-Json
        if($summary.CoverageFile){
            $coveragePath=[IO.Path]::GetFullPath($summary.CoverageFile)
            $testResultsRoot=[IO.Path]::GetFullPath((Join-Path $projectRoot 'TestResults'))+[IO.Path]::DirectorySeparatorChar
            if($coveragePath.StartsWith($testResultsRoot,[StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $coveragePath)){
                [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive,$coveragePath,'evidence/coverage.cobertura.xml',[IO.Compression.CompressionLevel]::Optimal) | Out-Null
            }
        }
    }
} finally { $archive.Dispose() }
Write-Output $archivePath
Write-Output ('Source files: ' + $sourceFiles.Count + '; excludes local databases, keys, uploads, bin and obj.')
