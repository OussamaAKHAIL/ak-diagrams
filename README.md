# ak-diagrams

![Language](https://img.shields.io/badge/Language-C%23-blue.svg)
![Platform](https://img.shields.io/badge/Platform-Windows-blue.svg)
![License](https://img.shields.io/badge/License-MIT-green.svg)
![Version](https://img.shields.io/badge/Version-2.0.0-orange.svg)
![Status](https://img.shields.io/badge/Status-Active%20Development-brightgreen.svg)

`ak-diagrams` is a lightweight Windows diagram editor for clean technical schematics, block diagrams, research-paper figures, and electronics-style system sketches.

It is designed for people who want something faster and smaller than a full CAD or drawing suite, while still keeping diagrams editable, structured, and exportable.

## Features

- Draw blocks, devices, text labels, reference images, and multi-turn wires
- Edit wire turns by dragging individual wire points
- Connect wire endpoints to blocks/devices/images so wires follow when connected objects move
- Resize blocks, devices, and images with selection handles or numeric properties
- Change fill, outline, line, text, and background colors
- Pick colors from the canvas or from inserted reference images
- Choose paper-friendly fonts: Times New Roman, Cambria, and Georgia
- Save editable `.akd` project files
- Export figures as SVG, PNG, or PDF
- Uses local `.env` settings without exposing private machine paths in Git

## Quickstart

Download or build the executable, then run:

```powershell
.\dist\ak-diagrams.exe
```

If you are using the versioned release folders, run:

```powershell
.\releases\v2.0.0\ak-diagrams-v2.0.0.exe
```

## Basic Workflow

1. Select a tool from the toolbar: `Block`, `Device`, `Wire`, `Text`, `Image`, or `Color Picker`.
2. Place elements on the canvas.
3. Use `Select` to move, resize, rename, recolor, or edit properties.
4. Save the editable project as `.akd`.
5. Export the final figure as SVG, PNG, or PDF.

## Wires

Wires are flexible polylines:

- Click once to start a wire.
- Click additional points to add turns.
- Double-click, right-click, press `Enter`, or use `Finish Wire` to complete it.
- Select a wire and drag any point handle to adjust that turn.
- Drag a connected endpoint away from its target to disconnect it.
- Drop a wire endpoint onto a block, device, or image to connect it.

## Images And Color Picking

Use the `Image` tool or `Insert > Image` to add a PNG, JPG, JPEG, or BMP reference image.

The image is embedded into the `.akd` file as data, not as a local file path. This keeps projects portable and avoids exposing your private folder structure.

To sample a color:

1. Choose the target from the color target dropdown: `Fill`, `Outline`, `Line`, `Text`, or `Background`.
2. Select `Color Picker`.
3. Click the canvas or an inserted image.

## Building From Source

Requirements:

- Windows
- .NET Framework compiler included with Windows at `Microsoft.NET\Framework` or `Microsoft.NET\Framework64`
- PowerShell

Build:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

Build output:

```text
dist\v2.0.0\ak-diagrams-v2.0.0.exe
dist\ak-diagrams.exe
releases\v2.0.0\ak-diagrams-v2.0.0.exe
```

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

When running from source, place `.env` in the project folder. When running only a copied executable, place `.env` next to that executable.

## Version History

Versioned executable builds are stored in `releases/`.

- `v1.0.0`: first public Windows executable
- `v2.0.0`: flexible wires, connected endpoints, image insertion, color picker, property editing, PNG/PDF export, and high-DPI display improvements

For larger public distribution, GitHub Releases is the recommended place to attach `.exe` files.
