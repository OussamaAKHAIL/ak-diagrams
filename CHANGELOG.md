# Changelog

## v2.1.1

- Fixed a crash when starting a wire and the preview briefly had fewer than two drawable points.
- Added a defensive SVG export guard for collapsed wire geometry.

## v2.1.0

- Added mouse wheel and touchpad zoom.
- Added keyboard shortcuts for copy, paste, duplicate, zoom, save, open, new, and delete.
- Added right-click context menus for elements, wires, and the canvas.
- Moved canvas color picking into each color control.
- Added per-wire modes: orthogonal turns, flexible angles, and extra flexible curves.
- Kept orthogonal wire routing as the default for schematic diagrams.

## v2.0.0

- Added flexible multi-turn wires with draggable point handles.
- Added wire endpoint connections to blocks, devices, and images.
- Added resize handles and numeric property editing.
- Added fill, outline, line, text, and background color controls.
- Added canvas/image color picker.
- Added Times New Roman, Cambria, and Georgia font selection.
- Added image insertion with embedded image data.
- Added PNG and PDF export.
- Added high-DPI awareness and improved drawing quality.
- Added versioned release output folders.

## v1.0.0

- Initial Windows executable.
- Added blocks, devices, wires, and text.
- Added `.akd` project saving and SVG export.
