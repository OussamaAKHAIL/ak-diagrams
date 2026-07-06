![ak-diagrams logo](media/logo1.png)

# ak-diagrams

![Language](https://img.shields.io/badge/Language-C%23-blue.svg)
![Platform](https://img.shields.io/badge/Platform-Windows-blue.svg)
![License](https://img.shields.io/badge/License-MIT-green.svg)
![Version](https://img.shields.io/badge/Version-3.0.1-orange.svg)
![Status](https://img.shields.io/badge/Status-Active%20Development-brightgreen.svg)

`ak-diagrams` is a lightweight Windows diagram editor for research-paper schematics, block diagrams, technical figures, and electronics-style layouts.

It focuses on fast editing, clear exports, and a clean workflow for diagrams that need to stay readable in papers and reports.

## Overview

- Draw rectangles, circles, wires, text labels, and reference images
- Keep wire endpoints connected while moving blocks and devices
- Route wires with orthogonal turns by default, plus flexible and curved modes
- Edit line style, line width, colors, font, and background from the UI
- Import or build custom component libraries and reuse them later
- Export editable projects as `.akd`, and final figures as SVG, PNG, or PDF

## Quickstart

1. Download the latest executable from `dist\ak-diagrams.exe` or a versioned release folder.
2. Run the app and choose a tool from the toolbar or the `Components` sidebar.
3. Place shapes, wires, text, or images on the canvas.
4. Use `Select` to move items, edit properties, and reconnect wires.
5. Save your diagram as `.akd`, then export SVG/PNG/PDF when you are done.

## Components Sidebar

Open `Components` from the top menu to show the left sidebar.

- `Shapes` contains rectangle, circle, and square presets.
- `Lines` contains solid, dashed, and dotted wire presets.
- `Custom` lets you create a new component, import a zip package, or export your library.

Custom components are saved as a zip package so you can share them with other users of `ak-diagrams`.

## Wire Editing

- Drag wire turns in orthogonal mode to rearrange the path while keeping right angles.
- Right-click a wire to switch between orthogonal, angled, and curved modes.
- Drag a connected endpoint away from a block or device to disconnect it.
- Drag the endpoint back onto a connectable object to reconnect it.

## Shortcuts

- `Ctrl+S`: save
- `Ctrl+O`: open
- `Ctrl+N`: new diagram
- `Ctrl+C`: copy
- `Ctrl+V`: paste
- `Ctrl+D`: duplicate
- `Ctrl+Z`: undo
- `Ctrl+Y`: redo
- `Delete`: delete selected item
- `Ctrl++` / `Ctrl+-`: zoom in/out
- `Ctrl+0`: reset zoom

## Building From Source

Requirements:

- Windows
- PowerShell
- .NET Framework compiler from `Microsoft.NET\Framework` or `Microsoft.NET\Framework64`

Build:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

Build output:

```text
dist\v3.0.1\ak-diagrams-v3.0.1.exe
dist\ak-diagrams.exe
releases\v3.0.1\ak-diagrams-v3.0.1.exe
```

The build also generates the app icon next to the executable so the program looks like a normal Windows app.

## Local Configuration

Optional local settings can be placed in `.env`.

Create it from the example:

```powershell
Copy-Item .env.example .env
```

Then set:

```text
AK_DIAGRAMS_DEFAULT_DIR=./my-diagrams
```

`.env` is ignored by Git, so private paths stay local. Commit `.env.example`, not `.env`.

## Version History

Versioned executable builds are stored in `releases/`.

- `v3.0.1`: transparent component studio with straight lines, editable shapes, and cropped PNG previews
- `v3.0.0`: orthogonal wire editing, line styles, a Components sidebar, and reusable component packages
- `v2.2.0`: stable pre-`v3` base release
- `v2.1.1`: fixed the wire preview crash when starting a new wire
- `v2.1.0`: zoom, shortcuts, right-click menus, color picking, and wire mode switching
- `v2.0.0`: flexible wires, connected endpoints, image insertion, color picker, property editing, PNG/PDF export, and high-DPI display improvements
- `v1.0.0`: initial public Windows executable

## Project Goal

`ak-diagrams` is meant to stay small, practical, and easy to share, while still being powerful enough for clean schematic-style figures.
