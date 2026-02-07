# Contributing

Thanks for helping improve Reigns Access. This project focuses on reliable keyboard access and clear screen reader output.

## Ways to contribute
- Report bugs with clear repro steps and logs.
- Suggest improvements to key bindings or announcements.
- Add or update translations.
- Submit fixes for new game versions.

## Development setup
1. Install Reigns (PC) and BepInEx 5.x.
2. Ensure `Tolk.dll` is available to the game process.
3. Open `Reigns Access.sln`.
4. In `ReignsAccess/ReignsAccess.csproj`, set `ReignsPath` to your local install path.
5. Build the project. The post-build step copies the DLL and language files into your BepInEx folder.

## Coding guidelines
- Keep speech output concise and consistent.
- Prefer reading visible UI text instead of hard-coded strings when possible.
- Avoid input conflicts between gameplay, menus, and dialogs.
- Guard reflection calls and nulls carefully.

## Test checklist
- Launch game and confirm the mod announces load messages.
- Read current card, options, and affected stats.
- Swipe left and right and confirm choices execute.
- Open pause menu, navigate all tabs, and adjust sliders/dropdowns.
- Open Memento Mori, Royal Deeds, and Portrait Gallery and exit them.
- Trigger a narrative screen, use Advance, then return to gameplay.
- Trigger a death screen and advance.
- Open any modal dialog and confirm left/right + Enter navigation.

## Reporting bugs
Include:
- Reigns game version and platform (Steam or other).
- Your mod version and any local changes.
- Repro steps and expected vs actual behavior.
- `BepInEx/LogOutput.log` if available.
- Language file used, if the issue is localization-related.

## Pull requests
- Keep PRs focused.
- Describe behavior changes and add test notes.
- For translations, add only the new JSON file.
