[CmdletBinding()]
param(
    [string] $OutputDirectory,
    [string] $DotNetPath = "dotnet"
)

$ErrorActionPreference = "Stop"
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$project = Join-Path $PSScriptRoot "ReignsAccess.Installer\ReignsAccess.Installer.csproj"

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot "release\assets"
}
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

$publishDirectory = Join-Path $repoRoot "installer\ReignsAccess.Installer\bin\publish\win-x64"
New-Item -ItemType Directory -Force -Path $publishDirectory | Out-Null

Write-Host "Building the rolling accessible installer..."
& $DotNetPath publish $project `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output $publishDirectory `
    -p:PublishSingleFile=true `
    -p:PublishTrimmed=false `
    -p:DebugType=None
if ($LASTEXITCODE -ne 0) {
    throw "Installer publish failed with exit code $LASTEXITCODE."
}

$publishedExecutable = Join-Path $publishDirectory "ReignsAccessInstaller.exe"
if (-not (Test-Path -LiteralPath $publishedExecutable -PathType Leaf)) {
    throw "The publish completed without creating ReignsAccessInstaller.exe."
}

$destination = Join-Path $OutputDirectory "ReignsAccessInstaller.exe"
Copy-Item -LiteralPath $publishedExecutable -Destination $destination -Force
Write-Host "Installer created: $destination"
