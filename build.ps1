$ErrorActionPreference = "Stop"

$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $projectDir

$distDir = Join-Path $projectDir "dist"
if (-not (Test-Path -LiteralPath $distDir)) {
    New-Item -ItemType Directory -Path $distDir | Out-Null
}

$compilerCandidates = @(
    (Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"),
    (Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe")
)
$compiler = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $compiler) {
    throw "C# compiler was not found in .NET Framework v4.x directories."
}

$outputPath = Join-Path $distDir "ak-diagrams.exe"

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

Write-Host "Built:" $outputPath
