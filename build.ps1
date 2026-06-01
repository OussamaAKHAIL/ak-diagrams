$ErrorActionPreference = "Stop"

$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $projectDir

$version = "2.1.1"
$distDir = Join-Path $projectDir "dist"
if (-not (Test-Path -LiteralPath $distDir)) {
    New-Item -ItemType Directory -Path $distDir | Out-Null
}

$versionDistDir = Join-Path $distDir "v$version"
if (-not (Test-Path -LiteralPath $versionDistDir)) {
    New-Item -ItemType Directory -Path $versionDistDir | Out-Null
}

$releaseDir = Join-Path $projectDir "releases\v$version"
if (-not (Test-Path -LiteralPath $releaseDir)) {
    New-Item -ItemType Directory -Path $releaseDir | Out-Null
}

$compilerCandidates = @(
    (Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"),
    (Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe")
)
$compiler = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $compiler) {
    throw "C# compiler was not found in .NET Framework v4.x directories."
}

$outputPath = Join-Path $versionDistDir "ak-diagrams-v$version.exe"
$latestOutputPath = Join-Path $distDir "ak-diagrams.exe"
$releaseOutputPath = Join-Path $releaseDir "ak-diagrams-v$version.exe"

& $compiler `
    /nologo `
    /target:winexe `
    /out:$outputPath `
    /r:System.Windows.Forms.dll `
    /r:System.Drawing.dll `
    /r:System.Runtime.Serialization.dll `
    AKDiagrams.cs

if ($LASTEXITCODE -ne 0) {
    throw "Compilation failed."
}

Copy-Item -LiteralPath $outputPath -Destination $latestOutputPath -Force
Copy-Item -LiteralPath $outputPath -Destination $releaseOutputPath -Force

Write-Host "Built:" $outputPath
Write-Host "Latest:" $latestOutputPath
Write-Host "Release copy:" $releaseOutputPath
