[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $Version,

    [string] $GameDirectory = "C:\Program Files (x86)\Steam\steamapps\common\Reigns",
    [string] $DotNetPath = "dotnet"
)

$ErrorActionPreference = "Stop"
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$releaseDirectory = [IO.Path]::GetFullPath((Join-Path $repoRoot "release"))
$expectedReleasePrefix = $repoRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if (-not $releaseDirectory.StartsWith($expectedReleasePrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to use a release directory outside the repository."
}

$cleanVersion = $Version.Trim().TrimStart('v')
if ($cleanVersion -notmatch '^\d+\.\d+(\.\d+)?([-.][0-9A-Za-z.-]+)?$') {
    throw "Version must look like 1.0.1 or 1.1.0-beta.1."
}

$gameDirectory = [IO.Path]::GetFullPath($GameDirectory)
$requiredRuntimeFiles = @("Tolk.dll", "nvdaControllerClient64.dll")
foreach ($file in $requiredRuntimeFiles) {
    if (-not (Test-Path -LiteralPath (Join-Path $gameDirectory $file) -PathType Leaf)) {
        throw "Missing $file in $gameDirectory. Install a working Tolk runtime before packaging."
    }
}
if (-not (Test-Path -LiteralPath (Join-Path $gameDirectory "Reigns.exe") -PathType Leaf)) {
    throw "Reigns.exe was not found in $gameDirectory."
}

if (Test-Path -LiteralPath $releaseDirectory) {
    Remove-Item -LiteralPath $releaseDirectory -Recurse -Force
}
$assetsDirectory = Join-Path $releaseDirectory "assets"
New-Item -ItemType Directory -Force -Path $assetsDirectory | Out-Null

$workingDirectory = Join-Path ([IO.Path]::GetTempPath()) ("ReignsAccessRelease-" + [Guid]::NewGuid().ToString("N"))
$packageDirectory = Join-Path $workingDirectory "package"
New-Item -ItemType Directory -Force -Path $packageDirectory | Out-Null

try {
    Write-Host "Building Reigns Access $cleanVersion..."
    $modProject = Join-Path $repoRoot "ReignsAccess\ReignsAccess.csproj"
    & $DotNetPath build $modProject `
        --configuration Release `
        -p:DeployToGame=false `
        -p:ReignsPath=$gameDirectory `
        -p:Version=$cleanVersion
    if ($LASTEXITCODE -ne 0) {
        throw "Mod build failed with exit code $LASTEXITCODE."
    }

    $bepInExArchive = Join-Path $workingDirectory "BepInEx.zip"
    $bepInExUrl = "https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.5/BepInEx_win_x64_5.4.23.5.zip"
    Write-Host "Downloading BepInEx 5.4.23.5..."
    Invoke-WebRequest -Uri $bepInExUrl -OutFile $bepInExArchive -Headers @{ "User-Agent" = "ReignsAccessReleaseBuilder/$cleanVersion" }
    Expand-Archive -LiteralPath $bepInExArchive -DestinationPath $packageDirectory -Force

    $pluginsDirectory = Join-Path $packageDirectory "BepInEx\plugins"
    $languagesDirectory = Join-Path $pluginsDirectory "ReignsAccess_Lang"
    New-Item -ItemType Directory -Force -Path $languagesDirectory | Out-Null

    $modAssembly = Join-Path $repoRoot "ReignsAccess\bin\Release\netstandard2.1\ReignsAccess.dll"
    Copy-Item -LiteralPath $modAssembly -Destination (Join-Path $pluginsDirectory "ReignsAccess.dll") -Force
    Get-ChildItem -LiteralPath (Join-Path $repoRoot "ReignsAccess\Lang") -Filter "*.json" -File |
        Where-Object { $_.Name -notin @("template.json", "_template.json") } |
        Copy-Item -Destination $languagesDirectory -Force

    foreach ($file in $requiredRuntimeFiles) {
        Copy-Item -LiteralPath (Join-Path $gameDirectory $file) -Destination (Join-Path $packageDirectory $file) -Force
    }
    Copy-Item -LiteralPath (Join-Path $repoRoot "README.md") -Destination (Join-Path $packageDirectory "README.md") -Force
    Copy-Item -LiteralPath (Join-Path $repoRoot "CHANGELOG.md") -Destination (Join-Path $packageDirectory "CHANGELOG.md") -Force
    Copy-Item -LiteralPath (Join-Path $repoRoot "LICENSE") -Destination (Join-Path $packageDirectory "LICENSE") -Force

    $archiveName = "ReignsAccess-$cleanVersion-windows-with-BepInEx.zip"
    $archivePath = Join-Path $assetsDirectory $archiveName
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [IO.Compression.ZipFile]::CreateFromDirectory($packageDirectory, $archivePath, [IO.Compression.CompressionLevel]::Optimal, $false)

    $hash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
    [IO.File]::WriteAllText("$archivePath.sha256", "$hash  $archiveName`n")

    & (Join-Path $PSScriptRoot "build-installer.ps1") `
        -OutputDirectory $assetsDirectory `
        -DotNetPath $DotNetPath

    Write-Host "Release assets are ready in $assetsDirectory"
    Write-Host "Publish all files in that directory on the GitHub release v$cleanVersion."
}
finally {
    if (Test-Path -LiteralPath $workingDirectory) {
        Remove-Item -LiteralPath $workingDirectory -Recurse -Force
    }
}
