using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using KarachiRailway.Simulation.Engine;
using KarachiRailway.Simulation.Models;

namespace KarachiRailway.Desktop.ViewModels;

public enum SimulationState { Idle, Running, Paused, Completed }

/// <summary>
/// Main view model for the Karachi Railway Simulation desktop application.
/// Exposes all configurable parameters, simulation controls, and result KPIs.
/// </summary>
public sealed class MainViewModel : ViewModelBase
{
    // ── Fields ────────────────────────────────────────────────────────────────

    private SimulationRunner?       _runner;
    private CancellationTokenSource? _cts;
    private SimulationState         _state = SimulationState.Idle;
    private SimulationResult?       _result;

    // ── Construction ──────────────────────────────────────────────────────────

    public MainViewModel()
    {
        StartCommand  = new AsyncRelayCommand(StartSimulationAsync, () => State is SimulationState.Idle or SimulationState.Completed);
        PauseCommand  = new RelayCommand(PauseSimulation,  () => State == SimulationState.Running);
        ResumeCommand = new RelayCommand(ResumeSimulation, () => State == SimulationState.Paused);
        StopCommand   = new RelayCommand(StopSimulation,   () => State is SimulationState.Running or SimulationState.Paused);
        ResetCommand  = new RelayCommand(Reset,            () => State != SimulationState.Running);
    }

    // ── Simulation State ──────────────────────────────────────────────────────

    public SimulationState State
    {
        get => _state;
        private set
        {
            if (SetProperty(ref _state, value))
            {
                OnPropertyChanged(nameof(IsIdle));
                OnPropertyChanged(nameof(IsRunning));
                OnPropertyChanged(nameof(IsPaused));
                OnPropertyChanged(nameof(IsCompleted));
                OnPropertyChanged(nameof(StatusLabel));
                OnPropertyChanged(nameof(StatusColor));
            }
        }
    }

    public bool IsIdle      => State == SimulationState.Idle;
    public bool IsRunning   => State == SimulationState.Running;
    public bool IsPaused    => State == SimulationState.Paused;
    public bool IsCompleted => State == SimulationState.Completed;

    public string StatusLabel => State switch
    {
        SimulationState.Idle      => "Ready",
        SimulationState.Running   => "Running…",
        SimulationState.Paused    => "Paused",
        SimulationState.Completed => "Completed",
        _                         => "Unknown",
    };

    public string StatusColor => State switch
    {
        SimulationState.Running   => "#22C55E",
        SimulationState.Paused    => "#F59E0B",
        SimulationState.Completed => "#3B82F6",
        _                         => "#94A3B8",
    };

    // ── Configurable Parameters ───────────────────────────────────────────────

    private double _arrivalRate = 8.0;
    public double ArrivalRate
    {
        get => _arrivalRate;
        set
        {
            if (SetProperty(ref _arrivalRate, value))
            {
                OnPropertyChanged(nameof(UtilizationPreview));
                OnPropertyChanged(nameof(IsStablePreview));
                OnPropertyChanged(nameof(StabilityHint));
            }
        }
    }

    private double _serviceRate = 10.0;
    public double ServiceRate
    {
        get => _serviceRate;
        set
        {
            if (SetProperty(ref _serviceRate, value))
            {
                OnPropertyChanged(nameof(UtilizationPreview));
                OnPropertyChanged(nameof(IsStablePreview));
                OnPropertyChanged(nameof(StabilityHint));
            }
        }
    }

    private int _durationMinutes = 120;
    public int DurationMinutes
    {
        get => _durationMinutes;
        set => SetProperty(ref _durationMinutes, value);
    }

    private double _ticketRequiredProb = 0.65;
    public double TicketRequiredProb
    {
        get => _ticketRequiredProb;
        set => SetProperty(ref _ticketRequiredProb, value);
    }

    private double _buyTicketProb = 0.80;
    public double BuyTicketProb
    {
        get => _buyTicketProb;
        set => SetProperty(ref _buyTicketProb, value);
    }

    private double _cardUsageProb = 0.45;
    public double CardUsageProb
    {
        get => _cardUsageProb;
        set => SetProperty(ref _cardUsageProb, value);
    }

    private double _cardValidProb = 0.95;
    public double CardValidProb
    {
        get => _cardValidProb;
        set => SetProperty(ref _cardValidProb, value);
    }

    private double _accountValidProb = 0.97;
    public double AccountValidProb
    {
        get => _accountValidProb;
        set => SetProperty(ref _accountValidProb, value);
    }

    private double _sufficientFundsProb = 0.90;
    public double SufficientFundsProb
    {
        get => _sufficientFundsProb;
        set => SetProperty(ref _sufficientFundsProb, value);
    }

    // ── Stability Preview ─────────────────────────────────────────────────────

    public double UtilizationPreview =>
        ServiceRate > 0 ? ArrivalRate / ServiceRate : double.NaN;

    public bool IsStablePreview =>
        ServiceRate > 0 && ArrivalRate / ServiceRate < 1.0;

    public string StabilityHint =>
        IsStablePreview
            ? $"System stable (ρ = {UtilizationPreview:P0})"
            : $"⚠ Unstable! (ρ = {UtilizationPreview:F2} ≥ 1 — queue grows unbounded)";

    // ── Result KPIs ───────────────────────────────────────────────────────────

    private double _kpiRho;
    public double KpiRho { get => _kpiRho; private set => SetProperty(ref _kpiRho, value); }

    private double _kpiWq;
    public double KpiWq  { get => _kpiWq;  private set => SetProperty(ref _kpiWq,  value); }

    private double _kpiW;
    public double KpiW   { get => _kpiW;   private set => SetProperty(ref _kpiW,   value); }

    private double _kpiLq;
    public double KpiLq  { get => _kpiLq;  private set => SetProperty(ref _kpiLq,  value); }

    private double _kpiL;
    public double KpiL   { get => _kpiL;   private set => SetProperty(ref _kpiL,   value); }

    private int _totalArrived;
    public int TotalArrived   { get => _totalArrived;   private set => SetProperty(ref _totalArrived, value); }

    private int _totalCompleted;
    public int TotalCompleted { get => _totalCompleted; private set => SetProperty(ref _totalCompleted, value); }

    private int _totalLeft;
    public int TotalLeft      { get => _totalLeft;      private set => SetProperty(ref _totalLeft, value); }

    private double _simAvgWait;
    public double SimAvgWait  { get => _simAvgWait;     private set => SetProperty(ref _simAvgWait, value); }

    private double _simAvgSys;
    public double SimAvgSys   { get => _simAvgSys;      private set => SetProperty(ref _simAvgSys, value); }

    private double _throughput;
    public double Throughput  { get => _throughput;     private set => SetProperty(ref _throughput, value); }

    private double _completionRate;
    public double CompletionRate { get => _completionRate; private set => SetProperty(ref _completionRate, value); }

    private int _processedCount;
    public int ProcessedCount  { get => _processedCount;  private set => SetProperty(ref _processedCount, value); }

    private double _simCurrentTime;
    public double SimCurrentTime { get => _simCurrentTime; private set => SetProperty(ref _simCurrentTime, value); }

    // ── Plain-Language Summary ────────────────────────────────────────────────

    private string _plainSummary = "Configure parameters and click Start to run the simulation.";
    public string PlainSummary
    {
        get => _plainSummary;
        private set => SetProperty(ref _plainSummary, value);
    }

    // ── Live Passenger Log ────────────────────────────────────────────────────

    public ObservableCollection<string> PassengerLog { get; } = new();

    // ── Trace Mode ────────────────────────────────────────────────────────────

    private bool _traceModeEnabled;
    public bool TraceModeEnabled
    {
        get => _traceModeEnabled;
        set => SetProperty(ref _traceModeEnabled, value);
    }

    private string _traceOutput = string.Empty;
    public string TraceOutput
    {
        get => _traceOutput;
        private set => SetProperty(ref _traceOutput, value);
    }

    // ── Validation ────────────────────────────────────────────────────────────

    private string _validationError = string.Empty;
    public string ValidationError
    {
        get => _validationError;
        private set
        {
            if (SetProperty(ref _validationError, value))
                OnPropertyChanged(nameof(HasValidationError));
        }
    }
    public bool HasValidationError => !string.IsNullOrEmpty(_validationError);

    // ── Commands ──────────────────────────────────────────────────────────────

    public ICommand StartCommand  { get; }
    public ICommand PauseCommand  { get; }
    public ICommand ResumeCommand { get; }
    public ICommand StopCommand   { get; }
    public ICommand ResetCommand  { get; }

    // ── Command Implementations ───────────────────────────────────────────────

    private async Task StartSimulationAsync()
    {
        if (!ValidateParameters())
            return;

        _cts = new CancellationTokenSource();
        var parameters = BuildParameters();
        _runner = new SimulationRunner(parameters);

        PassengerLog.Clear();
        TraceOutput = string.Empty;
        PlainSummary = "Simulation running…";
        ProcessedCount = 0;

        State = SimulationState.Running;

        try
        {
            var result = await _runner.RunAsync(
                progressCallback: (passenger, simTime) =>
                {
                    _ = Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        ProcessedCount = passenger.Id;
                        SimCurrentTime = simTime;
                        TotalCompleted = _result is null ? 0 : _result.TotalCompleted;

                        // Only add to log if not in trace mode (avoids flooding)
                        if (!TraceModeEnabled && PassengerLog.Count < 200)
                        {
                            PassengerLog.Insert(0, passenger.ToString());
                        }
                        else if (TraceModeEnabled)
                        {
                            var trace = string.Join(" → ",
                                passenger.StepTrace.Select(s => StepLabel(s)));
                            PassengerLog.Insert(0,
                                $"#{passenger.Id}: {trace}");
                        }
                    });
                },
                cancellationToken: _cts.Token);

            _result = result;

            _ = Application.Current.Dispatcher.InvokeAsync(() =>
            {
                ApplyResult(result);
                State = SimulationState.Completed;
            });
        }
        catch (OperationCanceledException)
        {
            _ = Application.Current.Dispatcher.InvokeAsync(() =>
            {
                PlainSummary = "Simulation was stopped by user.";
                State = SimulationState.Idle;
            });
        }
        catch (Exception ex)
        {
            _ = Application.Current.Dispatcher.InvokeAsync(() =>
            {
                PlainSummary = $"Error: {ex.Message}";
                State = SimulationState.Idle;
            });
        }
    }

    private void PauseSimulation()
    {
        _runner?.Pause();
        State = SimulationState.Paused;
        PlainSummary = "Simulation paused. Click Resume to continue.";
    }

    private void ResumeSimulation()
    {
        _runner?.Resume();
        State = SimulationState.Running;
        PlainSummary = "Simulation running…";
    }

    private void StopSimulation()
    {
        _cts?.Cancel();
        _runner?.Cancel();
    }

    private void Reset()
    {
        _runner   = null;
        _cts      = null;
        _result   = null;
        State     = SimulationState.Idle;

        KpiRho = KpiWq = KpiW = KpiLq = KpiL = 0;
        TotalArrived = TotalCompleted = TotalLeft = 0;
        SimAvgWait = SimAvgSys = Throughput = CompletionRate = 0;
        ProcessedCount = 0;
        SimCurrentTime = 0;

        PassengerLog.Clear();
        TraceOutput    = string.Empty;
        ValidationError = string.Empty;
        PlainSummary   = "Parameters reset. Configure and click Start to run again.";
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private SimulationParameters BuildParameters() => new SimulationParameters
    {
        ArrivalRate               = ArrivalRate,
        ServiceRate               = ServiceRate,
        SimulationDurationMinutes = DurationMinutes,
        TicketRequiredProbability = TicketRequiredProb,
        BuyTicketProbability      = BuyTicketProb,
        CardUsageProbability      = CardUsageProb,
        CardValidProbability      = CardValidProb,
        AccountValidProbability   = AccountValidProb,
        SufficientFundsProbability = SufficientFundsProb,
    };

    private bool ValidateParameters()
    {
        ValidationError = string.Empty;

        if (ArrivalRate <= 0)
        { ValidationError = "Arrival rate (λ) must be greater than 0."; return false; }

        if (ServiceRate <= 0)
        { ValidationError = "Service rate (μ) must be greater than 0."; return false; }

        if (DurationMinutes < 1)
        { ValidationError = "Simulation duration must be at least 1 minute."; return false; }

        double[] probs = { TicketRequiredProb, BuyTicketProb, CardUsageProb,
                           CardValidProb, AccountValidProb, SufficientFundsProb };
        if (probs.Any(p => p < 0 || p > 1))
        { ValidationError = "All probability values must be between 0 and 1."; return false; }

        return true;
    }

    private void ApplyResult(SimulationResult result)
    {
        KpiRho         = result.Utilization;
        KpiWq          = result.AvgQueueWaitTime;
        KpiW           = result.AvgSystemTime;
        KpiLq          = result.AvgQueueLength;
        KpiL           = result.AvgNumberInSystem;
        TotalArrived   = result.TotalArrived;
        TotalCompleted = result.TotalCompleted;
        TotalLeft      = result.TotalLeft;
        SimAvgWait     = result.SimAvgWaitTime;
        SimAvgSys      = result.SimAvgSystemTime;
        Throughput     = result.Throughput;
        CompletionRate = result.CompletionRate;

        PlainSummary = BuildPlainSummary(result);

        if (TraceModeEnabled && result.Passengers.Count > 0)
        {
            var first = result.Passengers.First();
            TraceOutput = "Single-Passenger Trace (Passenger #1):\n" +
                string.Join(" → ", first.StepTrace.Select(s => StepLabel(s)));
        }
    }

    private static string BuildPlainSummary(SimulationResult r)
    {
        bool stable = !double.IsNaN(r.AvgQueueWaitTime);
        string utilLine = $"The server was busy {r.Utilization:P0} of the time (utilisation ρ = {r.Utilization:F2}).";

        if (!stable)
        {
            return $"{utilLine}\n" +
                   "⚠ The system was UNSTABLE — arrivals exceeded capacity. " +
                   "Consider increasing the service rate or reducing the arrival rate.";
        }

        return $"{utilLine}\n" +
               $"On average, passengers waited {r.AvgQueueWaitTime:F2} min in the queue " +
               $"and spent {r.AvgSystemTime:F2} min total in the system.\n" +
               $"Out of {r.TotalArrived} passengers: " +
               $"{r.TotalCompleted} boarded their train ({r.CompletionRate:F1}% success rate), " +
               $"{r.TotalLeft} left without boarding.\n" +
               $"Throughput: {r.Throughput:F2} passengers/min.";
    }

    private static string StepLabel(PassengerStep step) => step switch
    {
        PassengerStep.Arrived                  => "Arrived",
        PassengerStep.TicketRequired_Yes       => "Ticket✔",
        PassengerStep.TicketRequired_No        => "No Ticket",
        PassengerStep.TicketCounter            => "Ticket Counter",
        PassengerStep.SecurityCheck            => "Security",
        PassengerStep.WaitingArea              => "Waiting Area",
        PassengerStep.TrainArrival             => "Train Arrives",
        PassengerStep.Boarding                 => "Boarding",
        PassengerStep.PassengerDeparts         => "Departs",
        PassengerStep.InquiryDesk              => "Inquiry Desk",
        PassengerStep.BuyTicket_Yes            => "Buy✔",
        PassengerStep.BuyTicket_No             => "Won't Buy",
        PassengerStep.HasCash_Yes              => "Cash✔",
        PassengerStep.HasCash_No               => "No Cash",
        PassengerStep.CashSufficientFunds_Yes  => "Funds✔",
        PassengerStep.CashSufficientFunds_No   => "Insufficient",
        PassengerStep.HasCard_Yes              => "Card✔",
        PassengerStep.HasCard_No               => "No Card",
        PassengerStep.CardValid_Yes            => "Card Valid",
        PassengerStep.CardValid_No             => "Card Invalid",
        PassengerStep.CardFundsAvailable_Yes   => "Funds✔",
        PassengerStep.CardFundsAvailable_No    => "No Funds",
        PassengerStep.AccountValid_Yes         => "Account✔",
        PassengerStep.AccountValid_No          => "Account Invalid",
        PassengerStep.PaymentVerifiedByBank    => "Bank Verified",
        PassengerStep.TransactionComplete      => "Txn Complete",
        PassengerStep.TicketReceipt            => "Receipt",
        PassengerStep.PassengerLeftSystem      => "LEFT SYSTEM",
        PassengerStep.Completed                => "COMPLETED",
        _                                      => step.ToString(),
    };
}
