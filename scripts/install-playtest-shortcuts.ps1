[CmdletBinding()]
param(
    [switch]$NoLaunch
)

$ErrorActionPreference = "Stop"
$installRoot = $PSScriptRoot
$exePath = Join-Path $installRoot "MyCat.WindowsShell.exe"

if (-not (Test-Path -LiteralPath $exePath)) {
    throw "MyCat.WindowsShell.exe was not found next to this installer script."
}

$desktop = [Environment]::GetFolderPath("DesktopDirectory")
$programs = [Environment]::GetFolderPath("Programs")
$startMenuFolder = Join-Path $programs "My Cat"
New-Item -ItemType Directory -Force -Path $startMenuFolder | Out-Null

$shortcutTargets = @(
    (Join-Path $desktop "My Cat.lnk"),
    (Join-Path $startMenuFolder "My Cat.lnk")
)

$shell = New-Object -ComObject WScript.Shell
foreach ($shortcutPath in $shortcutTargets) {
    $shortcut = $shell.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = $exePath
    $shortcut.WorkingDirectory = $installRoot
    $shortcut.IconLocation = "$exePath,0"
    $shortcut.Description = "My Cat desktop companion"
    $shortcut.Save()
}

Write-Output "Created desktop shortcut: $($shortcutTargets[0])"
Write-Output "Created Start Menu shortcut: $($shortcutTargets[1])"

if (-not $NoLaunch) {
    Start-Process -FilePath $exePath -WorkingDirectory $installRoot
}
