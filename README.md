# ak-diagrams

`ak-diagrams` is a lightweight Windows schematic editor for quick research-paper style diagrams.

## Features

- Draw blocks, devices (ellipse), orthogonal wires, and text labels
- Select and move components
- Rename labels with double-click or the `Rename` button
- Customize fill/line/text colors and line width
- Toggle snap-to-grid, wire arrows, and grid visibility
- Save/open project files (`.akd` JSON format)
- Export clean vector output as SVG

## Build the `.exe`

1. Open PowerShell in the project folder.
2. (Optional, recommended) create local env config:
   - `Copy-Item .env.example .env`
   - edit `.env` and set your own private local path in `AK_DIAGRAMS_DEFAULT_DIR`
3. Run:
   - `.\build.ps1`
4. Your executable will be created at:
   - `.\dist\ak-diagrams.exe`

## Run

- Double-click `dist\ak-diagrams.exe`, or run:
  - `.\dist\ak-diagrams.exe`

## Privacy notes

- `.env` is ignored by Git via `.gitignore`, so your personal local path stays private.
- Commit `.env.example` only, and keep your real machine-specific values in `.env`.

## Quick workflow

1. Choose a tool: `Block`, `Device`, `Wire`, or `Text`
2. Place elements on the canvas
3. Use `Select` to move and style them
4. Save as `.akd` and export final figure to `.svg`
