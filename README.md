# Reigns Access

Reigns Access is an accessibility mod for the PC version of **Reigns**. It adds screen reader output and complete keyboard navigation so blind players can play the game start to finish.

## Game description (from the Steam store page)

"Sit on the throne as a benevolent (or malevolent) medieval monarch of the modern age."

Reigns is a swipe-driven monarchy sim built around constant decisions. Each card presents a request from advisors, peasants, allies, or enemies, and you swipe left or right (your “royal controller”) to impose your will. You face a seemingly never-ending gauntlet of requests while balancing the church, the people, the army, and the treasury. Every choice has consequences that can end a reign and put your dynasty at risk.

- An Unpredictable Kingdom: Each year brings another important, seemingly random request. Careful planning helps, but surprise events, hidden motives, and bad luck can end even a long reign.
- Dynasty Expansion: Extend your dynasty across ages, forge alliances, make enemies, and discover new ways to die. Some events span centuries, with intrigue that can involve witches, science, politics, and darker forces.
- Royal Challenges: Pursue goals at the start of a reign to cement your legacy and unlock new cards and content.
- Eclectic Presentation: A score by Disasterpeace complements elegant gameplay and bold art direction.

## Features

- Full keyboard control for gameplay, menus, dialogs, narrative screens, and death screens.
- Screen reader output via Tolk (NVDA, JAWS, etc.).
- Spoken feedback for card text, options, stats, and objectives.
- Localization support with JSON language files.

## Current compatibility

**Reigns Access 1.1 is compatible with the current Steam version of Reigns.** It was tested on September 14, 2026 with Steam build `24471015` and BepInEx `5.4.23.5`.

- Supported: Reigns for Windows from Steam.
- Not currently supported: the GOG build.
- If Steam updates the game after the tested build above, check the [changelog](CHANGELOG.md) or the [latest release](https://github.com/leoguimaoficial/Reigns-Access/releases/latest) before reinstalling.

## Requirements

- Reigns for Windows installed through Steam.
- A Windows screen reader such as NVDA or JAWS.
- An internet connection while the installer downloads the selected release.

The installer supplies the required BepInEx, Tolk, and NVDA controller files. A separate .NET installation is not required.

## Download and install

The accessible installer is the recommended method. It includes and configures BepInEx, Tolk, and the NVDA controller library automatically. You do **not** need to install BepInEx or .NET separately.

`ReignsAccessInstaller.exe` is a rolling installer and is not tied to a particular mod version. The versions shown inside it are versions of **Reigns Access**, not versions of the installer.

1. Install Reigns through Steam.
2. Close Reigns if it is running.
3. [Download `ReignsAccessInstaller.exe`](https://github.com/leoguimaoficial/Reigns-Access/releases/latest/download/ReignsAccessInstaller.exe).
4. Open the downloaded installer. Windows will request administrator permission because Steam is normally inside `Program Files`.
5. Confirm the detected Reigns folder. The installer also detects additional Steam libraries on other drives.
6. Leave the newest stable version selected and choose **Install**.
7. Wait for the success message, close the installer, and start Reigns normally through Steam.
8. The mod should announce that it loaded. You can then use the keyboard commands documented below.

The installer only accepts the Steam installation of Reigns. It validates the complete package before changing the game and will not install an outdated BepInEx build that is incompatible with the current Unity version.

### Windows security notice

The installer is not currently code-signed, so Windows may display an unknown-publisher or Microsoft Defender SmartScreen warning. Only run an installer downloaded from this repository's official [GitHub Releases page](https://github.com/leoguimaoficial/Reigns-Access/releases).

## Updating Reigns Access

You only need to download the installer once.

1. Close Reigns.
2. Open your existing `ReignsAccessInstaller.exe`.
3. The installer automatically checks GitHub and selects the newest stable mod release.
4. When a newer release exists, its status and the **Update** button appear automatically. Choose **Refresh** only if you want to check again while the installer remains open.
5. Choose **Update** and wait for the success message.

The installer downloads the selected release, backs up files that will be replaced, and restores the previous files automatically if installation fails. Enable **Include test releases** only when you intentionally want to test a prerelease.

## Uninstalling

1. Close Reigns.
2. Open `ReignsAccessInstaller.exe`.
3. Confirm the detected game folder and choose **Uninstall mod**.

The mod and its language files are removed. BepInEx and the shared screen-reader bridge files are deliberately kept because another installed mod may use them.

## Links
- [Reigns on Steam](https://store.steampowered.com/app/474750/Reigns/)
- [Latest Reigns Access release](https://github.com/leoguimaoficial/Reigns-Access/releases/latest)
- [Release history and compatibility notes](CHANGELOG.md)
- [BepInEx downloads (stable releases)](https://github.com/BepInEx/BepInEx/releases)
- [BepInEx installation guide](https://docs.bepinex.dev/articles/user_guide/installation/index.html)

## Manual installation

Use this only if the installer cannot be used.

1. Download the ZIP whose name ends in `windows-with-BepInEx.zip` from the [latest release](https://github.com/leoguimaoficial/Reigns-Access/releases/latest).
2. Close Reigns.
3. Extract the ZIP.
4. Copy every extracted file and folder into the Reigns game folder, next to `Reigns.exe`.
5. Start Reigns normally through Steam and wait for the mod-loaded announcement.

For a manual update, repeat these steps and allow Windows to replace the existing files.

## Installation troubleshooting

- **The installer cannot find Reigns:** choose **Browse** and select the Steam folder containing `Reigns.exe`, normally `C:\Program Files (x86)\Steam\steamapps\common\Reigns`.
- **No versions appear:** check your internet connection and choose **Refresh**. The installer reads the public releases from GitHub.
- **The installer says Reigns is running:** close the game completely before installing, updating, or uninstalling.
- **The installer rejects an old package:** download the current release. Packages with BepInEx older than `5.4.23.5` are intentionally blocked.
- **The game starts but nothing is spoken:** close the game and attach `BepInEx\LogOutput.log` when opening an [issue](https://github.com/leoguimaoficial/Reigns-Access/issues).

## Release history

See [CHANGELOG.md](CHANGELOG.md) for compatibility notes, fixes, and new features in each release.

## Quick start
- Launch Reigns and start a new game or continue.
- Use the shortcuts below to read cards, navigate menus, and make choices.
- During gameplay, choices require two steps: the first Left/Right selects the option and the second Left/Right confirms it. This is Reigns' core mechanic, not a mod behavior.
- If you change the game language, press `F5` to reload the mod strings.
- To return to the title screen, you must close the game and open it again.

## Keyboard shortcuts

### Global
| Key | Action |
| --- | --- |
| `F5` | Reload the mod (use after changing game language). |
| `F6` | Read all visible screen text (fallback for an unexpected screen). |
| `Esc` / `P` | Open the pause menu (from gameplay). |

### Gameplay (card decisions)
| Key | Action |
| --- | --- |
| `Up` | Read current card (character + question). |
| `Down` | Read all stats. |
| `Left` | Swipe left (No). |
| `Right` | Swipe right (Yes). |
| `R` | Repeat current card or intercalated text. |
| `E` | Read both options (without stat changes). |
| `T` | Read which stats will be affected. |
| `A` | Read Church stat. |
| `S` | Read People stat. |
| `D` | Read Army stat. |
| `F` | Read Treasury stat. |
| `I` | Read king info. |
| `O` | Read objective. |
| `H` | Help. |
| `Q` | Silence speech. |

### Pause menu
| Key | Action |
| --- | --- |
| `Tab` | Next tab. |
| `Up` / `Down` | Move between items. |
| `Left` / `Right` | Adjust values. |
| `Enter` | Activate selected item. |
| `Backspace` / `Esc` | Close menu. |

### Dialogs
| Key | Action |
| --- | --- |
| `Left` / `Right` | Move between buttons. |
| `Enter` | Activate selected button. |

### Quit dialog
| Key | Action |
| --- | --- |
| `Left` / `Right` | Move between buttons. |
| `Enter` | Confirm. |
| `Esc` | Close. |

### Narrative screens
| Key | Action |
| --- | --- |
| `Up` / `Down` | Move between narrative text and Advance button. |
| `Enter` | Repeat text or activate Advance. |
| `Space` | Advance. |
| `R` | Repeat current item. |
| `H` | Help. |
| `Q` | Silence speech. |

### Death screen (Game Over)
| Key | Action |
| --- | --- |
| `Up` / `Down` | Move between death text and Advance button. |
| `Enter` / `Space` | Repeat text or advance. |
| `R` | Repeat current item. |

### Special screens (title and kingdom sub-screens)
| Key | Action |
| --- | --- |
| `Up` / `Down` | Move between items. |
| `Enter` | Activate (when available). |
| `R` | Repeat current item. |
| `Backspace` / `Esc` | Close the current screen. |

## Tips and known issues
- After changing the game language, press `F5` to reload the mod strings.
- If a future game update displays an unknown screen, press `F6` to read all visible text and report the screen in an issue.
- If you have died several times and want to fully restart the game, go to the title screen and, when you hear the music, hold `R`. A dialog will appear asking if you want to restart the entire game.
- To fully close the game, open the pause menu, select the Options tab, and choose **Quit game**.

## Translations
See [TRANSLATE.md](TRANSLATE.md) for how to add or update language files.

## Contributing
See [CONTRIBUTING.md](CONTRIBUTING.md) for development setup and guidelines.

## Disclaimer
Reigns Access is a fan-made accessibility mod and is not affiliated with Nerial or Devolver Digital.
