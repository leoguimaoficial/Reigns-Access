# Changelog

All notable changes to Reigns Access are documented in this file. Compatibility entries identify the exact Steam build used for testing so users can tell whether the game has updated since a mod release.

## [Unreleased]

## [1.1] - 2026-09-14

### Compatibility

- Tested with the current Windows Steam release of Reigns, build `24471015`.
- Updated the mod for the game's Unity 6 runtime and .NET Standard 2.1 API profile.
- Requires BepInEx `5.4.23.5` or newer in the 5.x line. This version contains the Unity 6 log-writer fix.
- The GOG release remains unsupported.

### Added

- Added a native, screen-reader-accessible Windows installer.
- Added a rolling installer that is not tied to a mod version and automatically discovers newer mod releases.
- Added automatic detection of the default Steam installation and additional Steam libraries.
- Added selection of stable versions and optional test releases directly from GitHub Releases.
- Added complete first-time installation of BepInEx, Tolk, the NVDA controller library, language files, and Reigns Access.
- Added safe ZIP path validation, package integrity checking, file backup, and automatic rollback on failure.
- Added local ZIP installation for testing releases before publication.
- Added safe mod removal that keeps shared BepInEx and screen-reader runtime files.
- Added release scripts that build the mod, download the pinned BepInEx version, assemble the complete package, create its SHA-256 checksum, and build the self-contained installer.
- Added more detailed startup diagnostics in `BepInEx/LogOutput.log`.
- Added a real, screen-reader-accessible **Quit game** command to the Options tab. It uses Reigns' native confirmation and save-before-exit flow.
- Added screen-reader output and Enter activation to the startup/disclaimer scene, which loads before the main game scene.
- Added `F6` as a global fallback that reads every visible UI text on the current screen.

### Fixed

- Renamed the asset-backed Options button from **Exit** to **Return to game**, matching its actual behavior.
- Corrected the same misleading **Exit** label in the Kingdom and Effects tabs; all three menu-level buttons now announce **Return to game**.
- Renamed the close controls in Royal Deeds, Memento Mori, and Portrait Gallery to **Back to Kingdom**, matching their real destination.
- Fixed Enter on special screens: their implemented actions were never called by the central screen navigator, so buttons such as **Back to Kingdom** and **Advance** could be announced without activating.
- Prevented the generic screen scanner from repeating confirmation dialogs already announced by the dedicated dialog navigator.
- Restored keyboard and screen-reader access to the quit confirmation after its hierarchy changed to `Canvas/dialog` and `Canvas/XLdialog`.
- Restored announcements and activation for notification pop-ups under the Unity 6 `Canvas/modals` hierarchy.
- Prevented dialog buttons from being invoked twice by a single Enter press.
- Made quit confirmations accessible on every Canvas, including the separate `Canvas (1)` used by the startup scene.
- Restored the missing king text on the Japanese PC death screen, whose asset is named `king_old` instead of `king`.
- Recognized all current `reigns_*` PC/mobile scene variants without announcing their internal Unity scene names.
- Prevented a single Enter in Kingdom, Effects, or Options from also activating Unity's stale visual focus and reopening the previously visited screen.
- Rebuilt the Effects tab from `EffectAct.GetLiveEffects()` so titles, descriptions, and stat changes are current and use the active game language instead of serialized English placeholders.
- Restored the fourth Kingdom record despite the shipped asset repeating the internal name `yearsinpower3`, and now announces the three visible new-content badges.
- Removed the nonfunctional online leaderboard from accessible navigation. The Steam build does not define the legacy mobile leaderboard referenced by the asset, and the game's click handler is empty. The four real save-based results are now identified as the local leaderboard.
- The chronology screen now announces each Devil-path symbol together with its year and whether the king is facing the symbol or has his back to it, making the dungeon sequence accessible without visual assistance.
- Restored screen-reader output and keyboard navigation after the Reigns Unity 6 update.
- Updated the Unity input assembly reference to `UnityEngine.InputLegacyModule`.
- Prevented one navigation failure from stopping all subsequent keyboard processing.
- Fixed the installer's **Close** button so it always closes the window and can cancel an active operation.

## [1.0.0] - 2026-02-07

### Added

- Initial public release of Reigns Access.
- Screen-reader output for cards, choices, statistics, objectives, menus, dialogs, narrative sequences, and death screens.
- Complete keyboard navigation for the supported game screens.
- English, Portuguese, and Spanish localization files.
- Manual packages with and without bundled BepInEx.

[Unreleased]: https://github.com/leoguimaoficial/Reigns-Access/compare/v1.1...HEAD
[1.1]: https://github.com/leoguimaoficial/Reigns-Access/compare/v1.0...v1.1
[1.0.0]: https://github.com/leoguimaoficial/Reigns-Access/releases/tag/v1.0
