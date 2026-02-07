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

## Requirements
- Reigns (PC, Steam).
- `Tolk.dll` available to the game process.
- `nvdaControllerClient64.dll` available to the game process.
- BepInEx 5.x (only if you use the standard package without bundled BepInEx).

## Links
- [Reigns on Steam](https://store.steampowered.com/app/474750/Reigns/)
- [BepInEx downloads (stable releases)](https://github.com/BepInEx/BepInEx/releases)
- [BepInEx installation guide](https://docs.bepinex.dev/articles/user_guide/installation/index.html)

## Installation

### First-time install (bundle with BepInEx)
Use this if you downloaded the release `.zip` that already includes BepInEx.

1. Extract the `.zip`.
2. Open the extracted folder.
3. Copy **all files and folders** from it to the Reigns **game root folder** (the same folder as `Reigns.exe`).
4. Launch the game and wait for the “mod loaded” announcement.

### First-time install (standard package, no bundled BepInEx)
Use this if you downloaded the standard release `.zip`.

1. Install BepInEx 5.x into the Reigns game folder.
2. Launch the game once with **no mods installed** so BepInEx can create its folders (like `BepInEx/plugins/`).
3. Close the game.
4. Copy `Tolk.dll` and `nvdaControllerClient64.dll` into the **game root folder** (the same folder as `Reigns.exe`).
5. Copy `ReignsAccess.dll` into `BepInEx/plugins/`.
6. Launch the game again and wait for the “mod loaded” announcement.

### Updating to future versions
If Reigns Access is already installed and working, future updates only require replacing:

- `BepInEx/plugins/ReignsAccess.dll`

After replacing the DLL, launch the game normally.

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
- There is an unmapped screen at the very end after the king dies. Press `Enter` to continue.
- If you have died several times and want to fully restart the game, go to the title screen and, when you hear the music, hold `R`. A dialog will appear asking if you want to restart the entire game.
- To fully close the game, use `Alt+F4`.

## Translations
See [TRANSLATE.md](TRANSLATE.md) for how to add or update language files.

## Contributing
See [CONTRIBUTING.md](CONTRIBUTING.md) for development setup and guidelines.

## Disclaimer
Reigns Access is a fan-made accessibility mod and is not affiliated with Nerial or Devolver Digital.
