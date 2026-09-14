# Reigns Access installer

This is a native Windows Forms installer designed to work well with NVDA and other screen readers. It is built as a single self-contained `ReignsAccessInstaller.exe`; the end user does not need to install .NET.

The installer follows a rolling model: it has no user-facing release version and is not tied to the version of Reigns Access. The version selector always refers to the **mod**. The same installer checks GitHub whenever it opens and can install every compatible mod release published later.

## End-user behavior

- Detects Reigns in the default Steam folder and in additional Steam libraries.
- Rejects non-Steam installations; the GOG build is not currently compatible with the mod.
- Lists published GitHub releases and lets the user select a stable version.
- Can optionally show test/prerelease versions.
- Downloads the complete package, verifies its GitHub SHA-256 digest when one is available, and rejects unsafe ZIP paths.
- Accepts both packages with a top-level folder and packages whose files are at the ZIP root.
- Requires a complete bundle with Reigns Access, Tolk, and BepInEx 5.4.23.5 or newer in the 5.x line.
- Backs up files that will be replaced and rolls them back if installation fails.
- Refuses to modify the game while `Reigns.exe` is running.
- Uninstalls the mod while deliberately keeping shared BepInEx and screen-reader runtime files.

The installer reads releases from `leoguimaoficial/Reigns-Access`. A release is installable when it has a ZIP asset whose name contains `BepInEx`, `bundle`, or `full`. If the release has only one non-installer ZIP, that ZIP is used.

## Build just the installer

Install the .NET 8 SDK, then run from the repository root:

```powershell
.\installer\build-installer.ps1
```

The output is `release/assets/ReignsAccessInstaller.exe`.

## Build all release assets

The release builder uses a local Steam copy of Reigns for the proprietary game references and for the already-tested Tolk runtime. It downloads the pinned official BepInEx 5.4.23.5 Windows x64 package, builds the mod, creates the complete ZIP and checksum, and builds the installer:

```powershell
.\installer\build-release.ps1 -Version 1.1
```

For a non-default Steam library or SDK location:

```powershell
.\installer\build-release.ps1 -Version 1.1 `
    -GameDirectory "D:\SteamLibrary\steamapps\common\Reigns" `
    -DotNetPath "C:\Program Files\dotnet\dotnet.exe"
```

Upload every file from `release/assets/` to a GitHub release tagged `v1.1`. Future updates normally require no installer code change: publish a new full ZIP and the existing installer will list it automatically.

## How the update automation works

There are two connected parts: release preparation for the maintainer and update discovery for the end user.

### 1. Preparing a release

Running `build-release.ps1` performs these steps:

1. Builds `ReignsAccess.dll` against the locally installed Steam game assemblies without deploying it to the maintainer's game.
2. Downloads the official pinned BepInEx Windows x64 archive.
3. Creates a clean package containing BepInEx, Reigns Access, language files, Tolk, and the NVDA controller library.
4. Creates `ReignsAccess-VERSION-windows-with-BepInEx.zip`.
5. Creates a matching SHA-256 checksum file.
6. Builds the same rolling installer as one self-contained `ReignsAccessInstaller.exe` that does not require .NET on the user's computer.

All generated output goes to the ignored local directory `release/assets/`. These files are not committed into the repository.

### 2. Publishing on GitHub

Create a GitHub release whose tag matches the mod version, such as `v1.0.2`, and attach every file from `release/assets/`. They appear under **Assets** on the release page, not in the repository source tree. The ZIP name contains the mod version; the installer remains simply `ReignsAccessInstaller.exe` and should be attached with that same name to every release so the permanent latest-download link continues to work.

The upload step is intentionally manual at present. The script prepares and verifies the files but does not create or modify a GitHub release.

### 3. Updating for the end user

When a user opens the installer, it automatically reads the public release list from `leoguimaoficial/Reigns-Access`. It finds each complete ZIP asset, selects the newest stable mod version, and optionally displays prereleases. If the selected version is newer than the installed version, the status and primary button announce that an update is available. After the user chooses **Update**, the installer:

1. Downloads the corresponding complete ZIP from GitHub Releases.
2. Checks the GitHub-provided SHA-256 digest when available.
3. Rejects unsafe ZIP paths, incomplete packages, non-Steam game folders, and BepInEx versions older than 5.4.23.5.
4. Backs up every existing file that will be replaced.
5. Installs the package and records the installed mod version.
6. Restores the previous files if any installation step fails.

Because version discovery happens when the installer opens or the user chooses **Refresh**, users normally download the installer only once. Publishing a new correctly named complete ZIP is enough for every existing installer to discover the update.
