[CmdletBinding()]
param(
    [string]$Runtime = "win-x64",
    [string]$Version = "v0.5",
    [switch]$FrameworkDependent
)

$ErrorActionPreference = "Stop"
$workspace = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$project = Join-Path $workspace "windows-shell\MyCat.WindowsShell.csproj"
$publishRoot = Join-Path $workspace "artifacts\playtest"
$packageName = "MyCat-$Version-playtest-$Runtime"
$publishDir = Join-Path $publishRoot $packageName
$zipPath = Join-Path $publishRoot "$packageName.zip"

$env:DOTNET_CLI_HOME = Join-Path $workspace ".dotnet-home"
$env:APPDATA = Join-Path $workspace ".nuget-home"
$env:NUGET_PACKAGES = Join-Path $workspace ".nuget-home\packages"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"

New-Item -ItemType Directory -Force -Path $publishRoot | Out-Null
if (Test-Path -LiteralPath $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

$publishArgs = @(
    "publish",
    $project,
    "-c", "Release",
    "-r", $Runtime,
    "-o", $publishDir
)

if ($FrameworkDependent) {
    $publishArgs += @("--configfile", (Join-Path $workspace "NuGet.Config"), "--self-contained", "false")
} else {
    $publishArgs += @("--configfile", (Join-Path $workspace "NuGet.Publish.Config"), "--self-contained", "true")
}

dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$sourceArtRoot = Join-Path $workspace "cat-assets\cats\my-cat"
$publishedArtRoot = Join-Path $publishDir "cats\my-cat"
$publishedManifest = Join-Path $publishedArtRoot "manifest.json"
if (-not (Test-Path -LiteralPath $publishedManifest)) {
    throw "The playtest package is missing cats\my-cat\manifest.json."
}

$sourceFrameCount = @(Get-ChildItem -LiteralPath $sourceArtRoot -Recurse -Filter "frame_*.png" -File).Count
$publishedFrameCount = @(Get-ChildItem -LiteralPath $publishedArtRoot -Recurse -Filter "frame_*.png" -File).Count
if ($publishedFrameCount -ne $sourceFrameCount) {
    throw "The playtest package has $publishedFrameCount art frames; expected $sourceFrameCount."
}

Copy-Item -Force (Join-Path $workspace "docs\playtest-runbook.txt") (Join-Path $publishDir "PLAYTEST.txt")
Copy-Item -Force (Join-Path $workspace "docs\playtest-guide.md") (Join-Path $publishDir "playtest-guide.md")
Copy-Item -Force (Join-Path $workspace "docs\playtest-feedback.md") (Join-Path $publishDir "playtest-feedback.md")

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -Force

Write-Output "Playtest folder: $publishDir"
Write-Output "Playtest zip: $zipPath"
