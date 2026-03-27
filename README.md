# Karachi Railway System — M/M/1 Queue Simulation

A professional desktop simulation application for the Karachi Railway System, built with **C# and WPF (.NET 8)**. The application models a **single-server M/M/1 queueing system** and simulates the complete passenger flow decision tree — from arrival through payment, security, boarding, and departure.

---

## Screenshots

> *(Run the application on Windows to see the full UI)*

The application features:
- **Modern deep-dark themed** dashboard (darker background layers, gradient brushes, drop-shadows)
- **Dynamic animations**: window entrance fade-in, button hover/click scale micro-interactions, active node glow pulse, status-dot heartbeat, token/badge scale-in
- Real-time flow diagram with animated passenger tokens
- Real-time KPI cards (ρ, Wq, W, Lq, L) with hover highlight
- Live passenger log with step traces
- Configurable parameters panel
- Plain-language results summary

---

## Startup Notes

- **Global error handler**: The app registers a `DispatcherUnhandledException` handler on startup.
  Any unhandled WPF dispatcher exception shows a friendly dialog instead of silently crashing.
- **Windows only**: This is a WPF application (`net8.0-windows`). It must be run on Windows.
- **HexToBrush alpha support**: The `HexColorToBrushConverter` now accepts an optional
  `ConverterParameter` (0–100) to set alpha as a percentage (e.g. `ConverterParameter=15` → 15% opacity).

---

## Solution Structure

```
KarachiRailwaySystem.slnx
├── src/
│   ├── KarachiRailway.Simulation/      # Core simulation & domain logic
│   │   ├── Models/
│   │   │   ├── Passenger.cs            # Individual passenger journey
│   │   │   ├── PassengerStep.cs        # All decision-flow steps (enum)
│   │   │   ├── SimulationParameters.cs # Configurable run parameters
│   │   │   └── SimulationResult.cs     # Aggregated KPI results
│   │   └── Engine/
│   │       ├── MM1Calculator.cs        # Analytical M/M/1 formulas
│   │       ├── PassengerFlowEngine.cs  # Decision-tree flow processor
│   │       └── SimulationRunner.cs     # Discrete-event batch simulator
│   └── KarachiRailway.Desktop/         # WPF desktop application (MVVM)
│       ├── ViewModels/
│       │   ├── ViewModelBase.cs        # INotifyPropertyChanged + RelayCommand
│       │   └── MainViewModel.cs        # All UI state & simulation control
│       ├── Converters/
│       │   └── ValueConverters.cs      # WPF value converters
│       ├── App.xaml / App.xaml.cs      # Application + global styles
│       └── MainWindow.xaml / .cs       # Main window (XAML-bound to ViewModel)
└── tests/
    └── KarachiRailway.Tests/           # xUnit unit tests
        ├── MM1CalculatorTests.cs       # Analytical formula verification
        ├── PassengerFlowEngineTests.cs # Decision-branch coverage tests
        └── SimulationRunnerTests.cs    # Integration / runner tests
```

---

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Windows** OS (required for WPF)

---

## Setup & Build

```bash
# Clone the repository
git clone https://github.com/Xlevens/Karachi-Railway-System.git
cd Karachi-Railway-System

# Build entire solution
dotnet build KarachiRailwaySystem.slnx

# Run unit tests
dotnet test tests/KarachiRailway.Tests/

# Run the desktop application (Windows only)
dotnet run --project src/KarachiRailway.Desktop/
```

---

## Default Parameters & Definitions

| Parameter | Default | Description |
|-----------|---------|-------------|
| **λ** (Arrival Rate) | `8.0` passengers/min | Average rate at which passengers arrive |
| **μ** (Service Rate) | `10.0` passengers/min | Average rate at which the server processes passengers |
| **Duration** | `120` minutes | Total simulated time |
| **Ticket Required Probability** | `0.65` | Chance a passenger already has/needs a ticket (goes directly to counter) |
| **Buy Ticket Probability** | `0.80` | Chance a passenger at the inquiry desk decides to buy |
| **Card Usage Probability** | `0.45` | Chance a passenger pays by card (vs cash) |
| **Card Valid Probability** | `0.95` | Chance the card is valid |
| **Account Valid Probability** | `0.97` | Chance the bank account is valid |
| **Sufficient Funds Probability** | `0.90` | Chance there are enough funds (cash or card) |

All parameters are editable in the UI before each run. Probabilities are constrained to [0, 1]; rates must be positive.

---

## How the Simulation Works

### M/M/1 Queue Model

The system models a **single-server queue** with:
- Exponentially distributed inter-arrival times (parameter λ)
- Exponentially distributed service times (parameter μ)

**Analytical KPIs** (steady-state formulas, valid when ρ = λ/μ < 1):

| Metric | Formula | Meaning |
|--------|---------|---------|
| ρ | λ / μ | Server utilisation |
| Lq | ρ² / (1 − ρ) | Avg passengers in queue |
| L | ρ / (1 − ρ) | Avg passengers in system |
| Wq | Lq / λ | Avg wait time in queue (min) |
| W | L / λ | Avg time in system (min) |

### Passenger Decision Flow

Each arriving passenger is routed through the following decision tree:

```
Passenger Arrives
│
├─ Ticket Required? (prob = ticketRequiredProbability)
│   ├─ YES → Ticket Counter → Security Check → Waiting Area
│   │        → Train Arrives → Boarding → Passenger Departs ✔
│   │
│   └─ NO → Inquiry Desk
│             │
│             ├─ Buy Ticket? (prob = buyTicketProbability)
│             │   ├─ NO → Passenger Leaves System ✘
│             │   └─ YES → Payment Flow:
│             │
│             │       Uses Card? (prob = cardUsageProbability)
│             │       ├─ CASH path:
│             │       │   Sufficient Funds? → YES: proceed / NO: Leave ✘
│             │       └─ CARD path:
│             │           Card Valid? → NO: try cash fallback / Leave ✘
│             │           Funds Available? → NO: Leave ✘
│             │           Account Valid? → NO: Leave ✘
│             │           Payment Verified by Bank
│             │           Transaction Complete → Ticket Receipt
│             │           → Security Check → Waiting Area → Boarding ✔
```

### Simulation Modes

- **Batch mode**: Simulates all passengers for the configured duration (default). Results shown after completion.
- **Trace mode**: Enable the "Show step trace per passenger" checkbox to see each passenger's full decision path in the log.

### Controls

| Button | Action |
|--------|--------|
| ▶ Start | Begin simulation with current parameters |
| ⏸ Pause | Temporarily pause the running simulation |
| ▶ Resume | Continue a paused simulation |
| ⏹ Stop | Cancel the simulation immediately |
| ↺ Reset | Clear results and restore to idle state |

---

## Running Tests

```bash
dotnet test tests/KarachiRailway.Tests/ --verbosity normal
```

Tests cover:
- M/M/1 analytical formula correctness
- All decision-flow branches (ticket required, inquiry desk, payment paths)
- Edge cases: unstable systems, zero/negative rates
- Simulation runner: cancellation, count correctness, KPI values

---

## Architecture Notes

- **MVVM pattern**: All UI logic lives in `MainViewModel`. The View (XAML) binds directly to ViewModel properties and commands.
- **Separation of concerns**: The `KarachiRailway.Simulation` library has no WPF dependency and can be used headlessly (e.g. from tests or a future API).
- **Async simulation**: The runner executes on a background thread (`Task.Run`), with UI updates marshalled via `Dispatcher.InvokeAsync`.
- **Seeded randomness**: The `PassengerFlowEngine` and `SimulationRunner` both accept an optional `Random` instance, enabling deterministic tests.
