using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using KarachiRailway.Desktop.Playback;
using KarachiRailway.Simulation.Engine;
using KarachiRailway.Simulation.Models;

namespace KarachiRailway.Desktop.ViewModels;

public enum SimulationState { Idle, Running, Paused, Completed }

/// <summary>
/// Main view model for the Karachi Railway Simulation desktop application.
/// </summary>
public sealed class MainViewModel : ViewModelBase
{
    private SimulationRunner?        _runner;
    private CancellationTokenSource? _cts;
    private SimulationState          _state = SimulationState.Idle;
    private SimulationResult?        _result;

    private readonly PlaybackController _playback = new();
    private readonly Dictionary<int, string>                   _passengerCurrentNodes = new();
    private readonly Dictionary<int, PassengerTokenViewModel>  _tokenMap              = new();
    private readonly Dictionary<string, FlowNodeViewModel>     _nodeMap               = new();
    private const int MaxVisibleTokens = 15;

    public MainViewModel()
    {
        SpeedOptions = new List<SpeedOption>
        {
            new("0.5x", 0.5),
            new("1x",   1.0),
            new("2x",   2.0),
            new("4x",   4.0),
        };
        _selectedSpeed = SpeedOptions[1];

        StartCommand  = new AsyncRelayCommand(StartSimulationAsync,
                            () => State is SimulationState.Idle or SimulationState.Completed);
        PauseCommand  = new RelayCommand(PausePlayback,  () => State == SimulationState.Running);
        ResumeCommand = new RelayCommand(ResumePlayback, () => State == SimulationState.Paused);
        StopCommand   = new RelayCommand(StopSimulation, () => State is SimulationState.Running or SimulationState.Paused);
        ResetCommand  = new RelayCommand(Reset,          () => State != SimulationState.Running);

        _playback.EventApplied      += OnEventApplied;
        _playback.PlaybackCompleted += OnPlaybackCompleted;

        BuildFlowDiagram();
    }

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
                OnPropertyChanged(nameof(CanEditParams));
            }
        }
    }

    public bool IsIdle        => State == SimulationState.Idle;
    public bool IsRunning     => State == SimulationState.Running;
    public bool IsPaused      => State == SimulationState.Paused;
    public bool IsCompleted   => State == SimulationState.Completed;
    public bool CanEditParams => State is SimulationState.Idle or SimulationState.Completed;

    public string StatusLabel => State switch
    {
        SimulationState.Idle      => "Ready",
        SimulationState.Running   => "Playing...",
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

    public List<SpeedOption> SpeedOptions { get; }

    private SpeedOption _selectedSpeed;
    public SpeedOption SelectedSpeed
    {
        get => _selectedSpeed;
        set
        {
            if (SetProperty(ref _selectedSpeed, value) && value != null)
                _playback.SpeedMultiplier = value.Value;
        }
    }

    private double _playbackProgress;
    public double PlaybackProgress
    {
        get => _playbackProgress;
        private set => SetProperty(ref _playbackProgress, value);
    }

    private double _playbackTotal = 1;
    public double PlaybackTotal
    {
        get => _playbackTotal;
        private set => SetProperty(ref _playbackTotal, value);
    }

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

    public double UtilizationPreview =>
        ServiceRate > 0 ? ArrivalRate / ServiceRate : double.NaN;

    public bool IsStablePreview =>
        ServiceRate > 0 && ArrivalRate / ServiceRate < 1.0;

    public string StabilityHint =>
        IsStablePreview
            ? $"System stable (rho = {UtilizationPreview:P0})"
            : $"Unstable! (rho = {UtilizationPreview:F2} >= 1)";

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
    public int TotalArrived   { get => _totalArrived;   private set => SetProperty(ref _totalArrived,   value); }
    private int _totalCompleted;
    public int TotalCompleted { get => _totalCompleted; private set => SetProperty(ref _totalCompleted, value); }
    private int _totalLeft;
    public int TotalLeft      { get => _totalLeft;      private set => SetProperty(ref _totalLeft,      value); }

    private double _simAvgWait;
    public double SimAvgWait { get => _simAvgWait; private set => SetProperty(ref _simAvgWait, value); }
    private double _simAvgSys;
    public double SimAvgSys  { get => _simAvgSys;  private set => SetProperty(ref _simAvgSys,  value); }
    private double _throughput;
    public double Throughput { get => _throughput; private set => SetProperty(ref _throughput, value); }
    private double _completionRate;
    public double CompletionRate { get => _completionRate; private set => SetProperty(ref _completionRate, value); }
    private int _processedCount;
    public int ProcessedCount { get => _processedCount; private set => SetProperty(ref _processedCount, value); }
    private double _simCurrentTime;
    public double SimCurrentTime { get => _simCurrentTime; private set => SetProperty(ref _simCurrentTime, value); }

    private string _plainSummary = "Configure parameters and click Simulate to run.";
    public string PlainSummary
    {
        get => _plainSummary;
        private set => SetProperty(ref _plainSummary, value);
    }

    public ObservableCollection<string> PassengerLog { get; } = new();

    private string _traceOutput = string.Empty;
    public string TraceOutput
    {
        get => _traceOutput;
        private set => SetProperty(ref _traceOutput, value);
    }

    private bool _traceModeEnabled;
    public bool TraceModeEnabled
    {
        get => _traceModeEnabled;
        set => SetProperty(ref _traceModeEnabled, value);
    }

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

    public ObservableCollection<FlowNodeViewModel>       FlowNodes       { get; } = new();
    public ObservableCollection<FlowEdgeViewModel>       FlowEdges       { get; } = new();
    public ObservableCollection<PassengerTokenViewModel> PassengerTokens { get; } = new();

    public ICommand StartCommand  { get; }
    public ICommand PauseCommand  { get; }
    public ICommand ResumeCommand { get; }
    public ICommand StopCommand   { get; }
    public ICommand ResetCommand  { get; }

    private async Task StartSimulationAsync()
    {
        if (!ValidateParameters()) return;

        _cts    = new CancellationTokenSource();
        _runner = new SimulationRunner(BuildParameters());

        PassengerLog.Clear();
        TraceOutput      = string.Empty;
        PlainSummary     = "Computing simulation...";
        ProcessedCount   = 0;
        TotalArrived     = TotalCompleted = TotalLeft = 0;
        SimCurrentTime   = 0;
        PlaybackProgress = 0;
        ResetFlowDiagram();
        State = SimulationState.Running;

        try
        {
            var (result, events) = await _runner.RunForPlaybackAsync(cancellationToken: _cts.Token);

            _result       = result;
            PlaybackTotal = Math.Max(1, events.Count);
            ApplyResult(result);

            if (TraceModeEnabled)
            {
                foreach (var p in result.Passengers.Take(50))
                {
                    var trace = string.Join(" > ", p.StepTrace.Select(StepLabel));
                    PassengerLog.Insert(0, $"#{p.Id}: {trace}");
                }
            }

            _playback.Load(events);
            _playback.SpeedMultiplier = SelectedSpeed.Value;
            _playback.Start();
            PlainSummary = "Playback started - watch passengers move through the diagram...";
        }
        catch (OperationCanceledException)
        {
            PlainSummary = "Simulation stopped by user.";
            State = SimulationState.Idle;
        }
        catch (Exception ex)
        {
            PlainSummary = $"Error: {ex.Message}";
            State = SimulationState.Idle;
        }
    }

    private void PausePlayback()
    {
        _playback.Pause();
        State = SimulationState.Paused;
        PlainSummary = "Playback paused. Click Resume to continue.";
    }

    private void ResumePlayback()
    {
        _playback.SpeedMultiplier = SelectedSpeed.Value;
        _playback.Resume();
        State = SimulationState.Running;
        PlainSummary = "Playback running...";
    }

    private void StopSimulation()
    {
        _cts?.Cancel();
        _playback.Pause();
        State = SimulationState.Idle;
        PlainSummary = "Stopped.";
    }

    private void Reset()
    {
        _cts?.Cancel();
        _playback.Reset();
        _runner = null;
        _cts    = null;
        _result = null;
        State   = SimulationState.Idle;

        KpiRho = KpiWq = KpiW = KpiLq = KpiL = 0;
        TotalArrived = TotalCompleted = TotalLeft = 0;
        SimAvgWait = SimAvgSys = Throughput = CompletionRate = 0;
        ProcessedCount   = 0;
        SimCurrentTime   = 0;
        PlaybackProgress = 0;
        PlaybackTotal    = 1;

        PassengerLog.Clear();
        TraceOutput     = string.Empty;
        ValidationError = string.Empty;
        PlainSummary    = "Parameters reset. Configure and click Simulate to run again.";
        ResetFlowDiagram();
    }

    private void OnEventApplied(PlaybackEvent evt)
    {
        string newNodeId = StepToNodeId(evt.Step);

        if (_passengerCurrentNodes.TryGetValue(evt.PassengerId, out var oldNodeId) &&
            oldNodeId != newNodeId &&
            _nodeMap.TryGetValue(oldNodeId, out var oldNode))
        {
            oldNode.ActiveCount = Math.Max(0, oldNode.ActiveCount - 1);
        }

        if (_nodeMap.TryGetValue(newNodeId, out var newNode))
            newNode.ActiveCount++;

        _passengerCurrentNodes[evt.PassengerId] = newNodeId;

        EnsureToken(evt.PassengerId);
        if (_tokenMap.TryGetValue(evt.PassengerId, out var token) && newNode != null)
        {
            int    slot = GetNodeSlot(newNodeId, evt.PassengerId);
            double dx   = (slot % 4) * 16 - 24;
            double dy   = (slot / 4) * 16;
            token.X = newNode.CenterX + dx - 8;
            token.Y = newNode.CenterY + dy - 8;
        }

        switch (evt.Step)
        {
            case PassengerStep.Arrived:
                TotalArrived++;
                ProcessedCount = TotalArrived;
                break;
            case PassengerStep.Completed:
                TotalCompleted++;
                if (_tokenMap.TryGetValue(evt.PassengerId, out var ct)) ct.IsCompleted = true;
                break;
            case PassengerStep.PassengerLeftSystem:
                TotalLeft++;
                if (_tokenMap.TryGetValue(evt.PassengerId, out var lt)) lt.IsLeft = true;
                break;
        }

        SimCurrentTime   = evt.SimTime;
        PlaybackProgress = _playback.EventsDone;
    }

    private void OnPlaybackCompleted()
    {
        foreach (var n in FlowNodes)
            n.ActiveCount = 0;
        _passengerCurrentNodes.Clear();

        if (_result != null)
            PlainSummary = BuildPlainSummary(_result);

        State = SimulationState.Completed;
    }

    private void EnsureToken(int passengerId)
    {
        if (_tokenMap.ContainsKey(passengerId)) return;

        if (PassengerTokens.Count >= MaxVisibleTokens)
        {
            var evict = PassengerTokens.FirstOrDefault(t => t.IsCompleted || t.IsLeft)
                     ?? PassengerTokens.FirstOrDefault();
            if (evict != null)
            {
                PassengerTokens.Remove(evict);
                _tokenMap.Remove(evict.PassengerId);
            }
        }

        var token = new PassengerTokenViewModel { PassengerId = passengerId };
        _tokenMap[passengerId] = token;
        PassengerTokens.Add(token);
    }

    private int GetNodeSlot(string nodeId, int passengerId)
    {
        int slot = 0;
        foreach (var (pid, nid) in _passengerCurrentNodes)
        {
            if (nid == nodeId && pid < passengerId)
                slot++;
        }
        return slot;
    }

    private void BuildFlowDiagram()
    {
        var nodes = new FlowNodeViewModel[]
        {
            new() { Id="start",         Title="Start",               Type=FlowNodeType.Start,    Left=380, Top=  10, Width=140, Height=40  },
            new() { Id="arrival",       Title="Passenger Arrival",   Type=FlowNodeType.Process,  Left=340, Top=  78, Width=220, Height=44  },
            new() { Id="ticketReq",     Title="Ticket Required?",    Type=FlowNodeType.Decision, Left=325, Top= 155, Width=250, Height=52  },
            new() { Id="ticketCounter", Title="Ticket Counter",      Type=FlowNodeType.Process,  Left= 60, Top= 265, Width=175, Height=44  },
            new() { Id="inquiry",       Title="Inquiry Desk",        Type=FlowNodeType.Process,  Left=665, Top= 265, Width=175, Height=44  },
            new() { Id="buyTicket",     Title="Buy Ticket?",         Type=FlowNodeType.Decision, Left=640, Top= 365, Width=220, Height=52  },
            new() { Id="payment",       Title="Payment Processing",  Type=FlowNodeType.Process,  Left=645, Top= 470, Width=210, Height=44  },
            new() { Id="leave",         Title="Passenger Leaves",    Type=FlowNodeType.Failure,  Left=865, Top= 380, Width=175, Height=44  },
            new() { Id="receipt",       Title="Ticket / Receipt",    Type=FlowNodeType.Process,  Left=645, Top= 567, Width=210, Height=44  },
            new() { Id="security",      Title="Security Check",      Type=FlowNodeType.Process,  Left=340, Top= 685, Width=220, Height=44  },
            new() { Id="waiting",       Title="Waiting Area",        Type=FlowNodeType.Process,  Left=340, Top= 768, Width=220, Height=44  },
            new() { Id="trainArrival",  Title="Train Arrival",       Type=FlowNodeType.Process,  Left=340, Top= 851, Width=220, Height=44  },
            new() { Id="boarding",      Title="Boarding",            Type=FlowNodeType.Process,  Left=340, Top= 934, Width=220, Height=44  },
            new() { Id="departs",       Title="Passenger Departs",   Type=FlowNodeType.Process,  Left=340, Top=1017, Width=220, Height=44  },
            new() { Id="complete",      Title="Journey Complete",    Type=FlowNodeType.Success,  Left=320, Top=1103, Width=260, Height=52  },
        };

        foreach (var n in nodes) { FlowNodes.Add(n); _nodeMap[n.Id] = n; }

        FlowEdgeViewModel Edge(string from, string to, string? lbl, params (double x, double y)[] pts)
        {
            var e = new FlowEdgeViewModel { FromId = from, ToId = to, Label = lbl };
            foreach (var (x, y) in pts) e.Points.Add(new Point(x, y));
            e.Build();
            return e;
        }

        var edges = new[]
        {
            Edge("start",         "arrival",       null,   (450, 50),  (450, 78)),
            Edge("arrival",       "ticketReq",     null,   (450, 122), (450, 155)),
            Edge("ticketReq",     "ticketCounter", "Yes",  (325, 181), (147, 181), (147, 265)),
            Edge("ticketReq",     "inquiry",       "No",   (575, 181), (752, 181), (752, 265)),
            Edge("ticketCounter", "security",      null,   (147, 309), (147, 665), (400, 665), (400, 685)),
            Edge("inquiry",       "buyTicket",     null,   (752, 309), (750, 365)),
            Edge("buyTicket",     "payment",       "Yes",  (750, 417), (750, 470)),
            Edge("buyTicket",     "leave",         "No",   (860, 391), (865, 402)),
            Edge("payment",       "receipt",       null,   (750, 514), (750, 567)),
            Edge("payment",       "leave",         "Fail", (855, 492), (952, 492), (952, 424)),
            Edge("receipt",       "security",      null,   (750, 611), (750, 655), (520, 655), (520, 685)),
            Edge("security",      "waiting",       null,   (450, 729), (450, 768)),
            Edge("waiting",       "trainArrival",  null,   (450, 812), (450, 851)),
            Edge("trainArrival",  "boarding",      null,   (450, 895), (450, 934)),
            Edge("boarding",      "departs",       null,   (450, 978), (450, 1017)),
            Edge("departs",       "complete",      null,   (450, 1061),(450, 1103)),
        };
        foreach (var e in edges) FlowEdges.Add(e);
    }

    private void ResetFlowDiagram()
    {
        foreach (var n in FlowNodes) n.ActiveCount = 0;
        PassengerTokens.Clear();
        _tokenMap.Clear();
        _passengerCurrentNodes.Clear();
    }

    private static string StepToNodeId(PassengerStep step) => step switch
    {
        PassengerStep.Arrived                => "arrival",
        PassengerStep.TicketRequired_Yes      => "ticketReq",
        PassengerStep.TicketRequired_No       => "ticketReq",
        PassengerStep.TicketCounter           => "ticketCounter",
        PassengerStep.InquiryDesk             => "inquiry",
        PassengerStep.BuyTicket_Yes           => "buyTicket",
        PassengerStep.BuyTicket_No            => "leave",
        PassengerStep.HasCash_Yes             => "payment",
        PassengerStep.HasCash_No              => "leave",
        PassengerStep.CashSufficientFunds_Yes => "payment",
        PassengerStep.CashSufficientFunds_No  => "leave",
        PassengerStep.HasCard_Yes             => "payment",
        PassengerStep.HasCard_No              => "payment",
        PassengerStep.CardValid_Yes           => "payment",
        PassengerStep.CardValid_No            => "payment",
        PassengerStep.CardFundsAvailable_Yes  => "payment",
        PassengerStep.CardFundsAvailable_No   => "leave",
        PassengerStep.AccountValid_Yes        => "payment",
        PassengerStep.AccountValid_No         => "leave",
        PassengerStep.PaymentVerifiedByBank   => "payment",
        PassengerStep.TransactionComplete     => "receipt",
        PassengerStep.TicketReceipt           => "receipt",
        PassengerStep.SecurityCheck           => "security",
        PassengerStep.WaitingArea             => "waiting",
        PassengerStep.TrainArrival            => "trainArrival",
        PassengerStep.Boarding                => "boarding",
        PassengerStep.PassengerDeparts        => "departs",
        PassengerStep.Completed               => "complete",
        PassengerStep.PassengerLeftSystem     => "leave",
        _                                     => "arrival",
    };

    private SimulationParameters BuildParameters() => new()
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
        if (ArrivalRate <= 0)  { ValidationError = "Arrival rate must be > 0."; return false; }
        if (ServiceRate <= 0)  { ValidationError = "Service rate must be > 0."; return false; }
        if (DurationMinutes < 1) { ValidationError = "Duration must be >= 1 min."; return false; }
        double[] probs = { TicketRequiredProb, BuyTicketProb, CardUsageProb,
                           CardValidProb, AccountValidProb, SufficientFundsProb };
        if (probs.Any(p => p < 0 || p > 1))
        { ValidationError = "All probabilities must be between 0 and 1."; return false; }
        return true;
    }

    private void ApplyResult(SimulationResult result)
    {
        KpiRho = result.Utilization; KpiWq = result.AvgQueueWaitTime;
        KpiW   = result.AvgSystemTime; KpiLq = result.AvgQueueLength;
        KpiL   = result.AvgNumberInSystem;
        SimAvgWait = result.SimAvgWaitTime; SimAvgSys = result.SimAvgSystemTime;
        Throughput = result.Throughput; CompletionRate = result.CompletionRate;
    }

    private static string BuildPlainSummary(SimulationResult r)
    {
        bool stable = !double.IsNaN(r.AvgQueueWaitTime);
        string u = $"Server busy {r.Utilization:P0} of the time (rho={r.Utilization:F2}).";
        if (!stable) return $"{u}\nUNSTABLE - arrivals exceeded capacity.";
        return $"{u}\nAvg queue wait: {r.AvgQueueWaitTime:F2} min  System time: {r.AvgSystemTime:F2} min\n" +
               $"{r.TotalArrived} arrived, {r.TotalCompleted} boarded ({r.CompletionRate:F1}%), " +
               $"{r.TotalLeft} left.  Throughput: {r.Throughput:F2} pax/min";
    }

    private static string StepLabel(PassengerStep step) => step switch
    {
        PassengerStep.Arrived               => "Arrived",
        PassengerStep.TicketRequired_Yes     => "Ticket+",
        PassengerStep.TicketRequired_No      => "No Ticket",
        PassengerStep.TicketCounter          => "Ticket Counter",
        PassengerStep.SecurityCheck          => "Security",
        PassengerStep.WaitingArea            => "Waiting",
        PassengerStep.TrainArrival           => "Train",
        PassengerStep.Boarding               => "Boarding",
        PassengerStep.PassengerDeparts       => "Departs",
        PassengerStep.InquiryDesk            => "Inquiry",
        PassengerStep.BuyTicket_Yes          => "Buy+",
        PassengerStep.BuyTicket_No           => "Won't Buy",
        PassengerStep.PaymentVerifiedByBank  => "Bank+",
        PassengerStep.TransactionComplete    => "Txn+",
        PassengerStep.TicketReceipt          => "Receipt",
        PassengerStep.PassengerLeftSystem    => "LEFT",
        PassengerStep.Completed              => "DONE",
        _                                    => step.ToString(),
    };
}

/// <summary>Playback speed option for the speed ComboBox.</summary>
public record SpeedOption(string Label, double Value)
{
    public override string ToString() => Label;
}
