$ErrorActionPreference = "Stop"

$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $projectDir

$version = "3.0.1"
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

$iconSourcePath = Join-Path $projectDir "media\logo.png"
$iconPath = Join-Path $projectDir "ak-diagrams.ico"

function New-IcoFromPng {
    param(
        [Parameter(Mandatory = $true)][string]$PngPath,
        [Parameter(Mandatory = $true)][string]$IcoPath
    )

    if (-not (Test-Path -LiteralPath $PngPath)) {
        throw "Icon source image was not found: $PngPath"
    }

    $pngBytes = [System.IO.File]::ReadAllBytes($PngPath)
    $memoryStream = New-Object System.IO.MemoryStream
    $writer = New-Object System.IO.BinaryWriter($memoryStream)
    try {
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([UInt16]1)
        $writer.Write([UInt16]1)
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([UInt16]1)
        $writer.Write([UInt16]32)
        $writer.Write([UInt32]$pngBytes.Length)
        $writer.Write([UInt32](6 + 16))
        $writer.Write($pngBytes)
        [System.IO.File]::WriteAllBytes($IcoPath, $memoryStream.ToArray())
    }
    finally {
        $writer.Dispose()
        $memoryStream.Dispose()
    }
}

New-IcoFromPng -PngPath $iconSourcePath -IcoPath $iconPath

$outputPath = Join-Path $versionDistDir "ak-diagrams-v$version.exe"
$latestOutputPath = Join-Path $distDir "ak-diagrams.exe"
$releaseOutputPath = Join-Path $releaseDir "ak-diagrams-v$version.exe"

$sourceFiles = Get-ChildItem -Path $projectDir -Filter *.cs | ForEach-Object { $_.FullName }

& $compiler `
    /nologo `
    /target:winexe `
    /out:$outputPath `
    /win32icon:$iconPath `
    /r:System.Windows.Forms.dll `
    /r:System.Drawing.dll `
    /r:System.Runtime.Serialization.dll `
    /r:System.IO.Compression.dll `
    /r:System.IO.Compression.FileSystem.dll `
    $sourceFiles

if ($LASTEXITCODE -ne 0) {
    throw "Compilation failed."
}

Copy-Item -LiteralPath $outputPath -Destination $latestOutputPath -Force
Copy-Item -LiteralPath $outputPath -Destination $releaseOutputPath -Force
Copy-Item -LiteralPath $iconPath -Destination (Join-Path $distDir "ak-diagrams.ico") -Force
Copy-Item -LiteralPath $iconPath -Destination (Join-Path $versionDistDir "ak-diagrams.ico") -Force
Copy-Item -LiteralPath $iconPath -Destination (Join-Path $releaseDir "ak-diagrams.ico") -Force

Write-Host "Built:" $outputPath
Write-Host "Latest:" $latestOutputPath
Write-Host "Release copy:" $releaseOutputPath
