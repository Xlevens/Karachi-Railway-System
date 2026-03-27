# Karachi Railway System — Queue Simulation Suite

Desktop simulation software for Karachi Railway, built with C# and WPF on .NET 8.
The application now supports three single-server queue models:

- M/M/1
- M/G/1
- G/G/1

Users select the model after the welcome screen, then run the passenger-flow simulation with live playback, metrics, and step-by-step inspection.

---

## What’s New (Recent Changes)

- Added model-selection screen after welcome: choose M/M/1, M/G/1, or G/G/1.
- Extended simulation engine for model-specific sampling:
  - M/M/1: exponential arrivals + exponential service
  - M/G/1: exponential arrivals + general service (gamma)
  - G/G/1: general arrivals + general service (gamma)
- Added queue metrics calculator for MM1/MG1/GG1 (MM1 exact, MG1 P-K, GG1 Kingman approximation).
- Added welcome splash with logo and fade-out sequence.
- Added tabbed center area:
  - Flow Diagram
  - Block Flow (real-time customer-in-block view)
- Added scalable controls:
  - Flow Size (inside Flow Diagram tab)
  - Block Size (inside Block Flow tab)
- Added Next Step playback mode (single-event stepping):
  - Button in controls
  - Keyboard shortcuts: Ctrl+N and Ctrl+Right Arrow
- Added collapsible side panels (settings/metrics) with title-bar toggle buttons.
- Updated application branding:
  - Custom logo in title bar and splash
  - EXE/window icon configured from assets
- Updated dark ComboBox styling and spacing polish for side-panel scroll content.

---

## Requirements

- .NET 8 SDK
- Windows OS (WPF app, net8.0-windows)

---

## Project Structure

```
KarachiRailwaySystem.slnx
├── src/
│   ├── KarachiRailway.Simulation/
│   │   ├── Models/
│   │   │   ├── QueueModelType.cs
│   │   │   ├── SimulationParameters.cs
│   │   │   ├── SimulationResult.cs
│   │   │   ├── Passenger.cs
│   │   │   ├── PassengerStep.cs
│   │   │   └── PlaybackEvent.cs
│   │   └── Engine/
│   │       ├── MM1Calculator.cs
│   │       ├── QueueMetricsCalculator.cs
│   │       ├── PassengerFlowEngine.cs
│   │       └── SimulationRunner.cs
│   └── KarachiRailway.Desktop/
│       ├── Assets/
│       ├── Playback/
│       ├── ViewModels/
│       ├── Converters/
│       ├── App.xaml
│       └── MainWindow.xaml
└── tests/
    └── KarachiRailway.Tests/
```

---

## Run Locally

```bash
dotnet build KarachiRailwaySystem.slnx
dotnet test KarachiRailwaySystem.slnx -c Debug
dotnet run --project src/KarachiRailway.Desktop/KarachiRailway.Desktop.csproj
```

---

## Model Behavior

### M/M/1

- Inter-arrival times: exponential (λ)
- Service times: exponential (μ)
- Uses standard M/M/1 exact formulas.

### M/G/1

- Inter-arrival times: exponential (λ)
- Service times: general (gamma), controlled by Service CV (Cs)
- Uses Pollaczek–Khinchine-based metrics.

### G/G/1

- Inter-arrival times: general (gamma), controlled by Arrival CV (Ca)
- Service times: general (gamma), controlled by Service CV (Cs)
- Uses Kingman approximation for waiting-time metrics.

---

## UI Workflow

1. Welcome splash (logo + fade-out)
2. Model selection screen
3. Main dashboard:
   - Left: settings + controls
   - Center: flow/block tabs with scalable views
   - Right: live and analytical metrics

Key controls include Start, Pause, Resume, Next Step, Stop, and Reset.

---

## Build EXE (Release)

Generate a Windows x64 self-contained single-file executable:

```bash
Get-Process KarachiRailway.Desktop -ErrorAction SilentlyContinue | Stop-Process -Force
dotnet publish src/KarachiRailway.Desktop/KarachiRailway.Desktop.csproj \
  -c Release -r win-x64 --self-contained true \
  /p:PublishSingleFile=true \
  /p:PublishTrimmed=false \
  /p:IncludeNativeLibrariesForSelfExtract=true
```

Output EXE:

`src/KarachiRailway.Desktop/bin/Release/net8.0-windows/win-x64/publish/KarachiRailway.Desktop.exe`

---

## Testing

Current tests include:

- MM1 analytical checks
- Model-aware queue metrics checks (MM1/MG1/GG1)
- Passenger flow branch coverage
- Simulation runner correctness and cancellation behavior

Run all tests:

```bash
dotnet test KarachiRailwaySystem.slnx -c Debug
```

