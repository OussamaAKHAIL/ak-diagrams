# Changelog

## v3.0.1

- Turned the custom-component editor into a small UI studio with straight lines, editable shapes, and adjustable thickness.
- Saved custom-component previews as transparent PNGs instead of white-backed images.
- Kept the connection-point workflow so custom symbols can still connect cleanly in diagrams.

## v3.0.0

- Added orthogonal wire turn dragging so elbows can be rearranged without breaking right-angle routing.
- Added line style support for solid, dashed, and dotted elements.
- Added a Components sidebar with shapes, lines, and custom component packages.
- Added a custom component editor with connection points and zip import/export.
- Added a reusable app icon and logo-based branding for the executable and README.

## v2.2.0

- Stabilized the editor before the v3 component-library work.
- Kept the wire, color, export, and property-editing workflow from the earlier releases.

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
