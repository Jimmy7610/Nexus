# NEXUS UI Layout Alignment

This document maps the visual regions of the mission control mockup to the implemented WPF Grid structure.

## Region Mapping

| Mockup Area | Implementation Region | Component Type |
|-------------|-----------------------|----------------|
| **Top Bar** | Row 0 | Global Header (Title + Navigation) |
| **Gauges**  | Row 1 | quad circular Gauges (CPU, GPU, RAM, DISK) |
| **Chart**   | Row 2, Col 1 | Dominant Performance Visualization |
| **Left Col**| Row 2, Col 0 | Network Panel (Top), Storage Panel (Bottom) |
| **Right Col**| Row 2, Col 2 | Active Processes (Top), AI Copilot (Bottom) |
| **Footer**  | Row 3 | System Status & Build Version |

## Visual Directives
- **Color**: Cyber Cyan (#00FFFF) is the primary interactive and high-status color.
- **Depth**: Panels utilize semi-transparent backgrounds (#10161E) with inner glows and border accents to create a layered HUD effect.
- **Typography**: Clean, Segoe UI based typography with variations in opacity and weight to denote hierarchy.
- **AI Presence**: The AI Copilot is treated as a resident system service with status reporting, not a standard chat window.
