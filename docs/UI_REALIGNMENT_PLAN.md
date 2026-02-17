# NEXUS UI Realignment Plan

## Goal
Rework the existing WPF UI to structurally and visually match the "Mission Control / HUD" mockup. Prioritize visual fidelity, depth, and specific layout proportions.

## Proposed Changes

### Documentation
- [NEW] [UI_ALIGNMENT.md](file:///c:/Codes/Nexus/docs/UI_ALIGNMENT.md): Mapping of regions to mockup.

### Styles & Resources
- [MODIFY] [HUDResources.xaml](file:///c:/Codes/Nexus/src/Nexus.App/Styles/HUDResources.xaml):
    - Implement `DropShadowEffect` and `InnerGlow` using `Border` nesting or `Effect` properties.
    - Refine `CircularGauge` with arc segments and glow.
    - Add `HUDPanel` with status label headers and corner accents.

### Main View
- [MODIFY] [MainWindow.xaml](file:///c:/Codes/Nexus/src/Nexus.App/MainWindow.xaml):
    - Change Grid structure:
        - Row 0: Header (50px)
        - Row 1: Gauges (180px)
        - Row 2: Content (Active Area)
        - Row 3: Footer (30px)
    - Active Area Columns:
        - Column 0: Left Column (280px) - Network, Storage.
        - Column 1: Center (Star) - Live Performance Chart.
        - Column 2: Right Column (320px) - Processes, AI Copilot.

### Core/App Logic
- [MODIFY] [MainViewModel.cs](file:///c:/Codes/Nexus/src/Nexus.App/ViewModels/MainViewModel.cs): Ensure all properties required for the new layout are properly bound.

## Verification Plan
### Manual Verification
- Run the app and visually compare side-by-side with `nexus_dashboard_mockup.png`.
- Check that the Cyan accent (#00FFFF) is dominant and depth is apparent in panels.
