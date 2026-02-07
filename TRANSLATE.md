# Translating Reigns Access

This mod loads its strings from JSON files so the community can add new languages.

## Where language files live
Runtime files are loaded from:
- `BepInEx/plugins/ReignsAccess_Lang/`

On first run, the mod creates:
- `en.json`
- `pt.json`
- `_template.json`

## Quick start
1. Copy `_template.json` to a new file named with your language code, for example `es.json`.
2. Edit the metadata fields at the top:
   - `_language_name`
   - `_language_code`
   - `_author`
   - `_notes`
3. Translate only the values. Keep all keys unchanged.
4. Save as UTF-8 JSON.
5. In game, change the language and apply, then press `F5` to reload the mod.

## Language codes
The mod normalizes several game language codes to two-letter files. Supported mappings include:
- `bp` -> `pt` (Brazilian Portuguese)
- `en`, `es`, `fr`, `de`, `it`, `ru`, `zh`, `ja`, `ko`, `pl`, `nl`, `tr`

If the game reports another language, the mod falls back to the first two letters.

## Translation rules
- Do not rename keys.
- Keep JSON valid: quotes, commas, and braces matter.
- Preserve placeholders if you add any later.
- Keep short action labels short (example: `Advance`, `Exit`).
- You can use punctuation to improve screen reader clarity.

## Testing checklist
- Start a new game and confirm the mod announces the loading message.
- Read a card, options, and stats.
- Open the pause menu and move across all three tabs.
- Trigger a narrative screen and confirm the Advance button reads correctly.

## Submitting a translation
Preferred workflow:
- Add your new file to `ReignsAccess/Lang/`.
- Open a pull request with the new file and your name in `_author`.

If you cannot submit a PR, send the JSON file to the maintainers.
