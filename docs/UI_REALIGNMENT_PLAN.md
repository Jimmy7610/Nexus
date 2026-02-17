# NEXUS SHIP-READY MVP — Execution Plan

## Goal
Implement a production-quality HUD with full functional depth, matching the approved mockup and satisfying the four mandatory execution locks.

## Mandatory Execution Locks (DoD)
1. **No Placeholders**: Real data OR "Unavailable/WIP" fallback actions.
2. **Storage Scan v1**: Top 10 folders, Top 20 files, file type distribution (top 10), total count, summary (drives, duration, errors), cancel/completion.
3. **AI Copilot v1**: Ollama status (Ready/Offline), active model, chat loop, quick prompts (Slow PC / Find Large Files / Health Report), context injection.
4. **Restart Button**: Top-right icon, "Restart NEXUS" tooltip, confirmation dialog, clean process reboot.

## Proposed Changes

### [Core] Models & Services
- **Models.cs**: Extend `StorageAnalysisResult` and `SystemMetrics` for full telemetry and scan reporting.
- **MetricsService.cs**: Use performance counters for real-time network throughput.
- **StorageScanner.cs**: Implement recursive scan with metadata extraction (File Extension distribution).

### [App] HUD Design System
- **HUDResources.xaml**: 
    - `HUDPanel`: Chrome with header strip and inner bevel.
    - `CircularGauge`: Instrument-grade arc with ticks and animations.
    - `TabItem`: Futuristic selected/hover states.

### [App] ViewModel & View
- **MainViewModel.cs**: Handle tab switching, restart command with confirmation, and Ollama integration.
- **MainWindow.xaml**: Final layout realignment to mockup with high-fidelity visual chrome.

## Verification Plan
### Automated Tests
- `dotnet build` (Zero warnings/errors).
- `dotnet test` (Verify metric calculations and scan logic).
### Manual Verification
- Verify "Restart" cleans up current process and starts new.
- Verify Storage Scan produces accurate file type distribution.
- Compare final UI screenshot against mockup.
