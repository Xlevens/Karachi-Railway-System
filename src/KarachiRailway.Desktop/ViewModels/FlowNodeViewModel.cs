using System.Collections.ObjectModel;
using System.Windows.Media;

namespace KarachiRailway.Desktop.ViewModels;

public enum FlowNodeType { Start, Process, Decision, Success, Failure }

/// <summary>
/// Represents a single node (stage) in the flow-block diagram.
/// Exposes all visual and state properties used by the XAML DataTemplate.
/// </summary>
public class FlowNodeViewModel : ViewModelBase
{
    private bool _isActive;
    private int  _activeCount;
    private readonly ObservableCollection<string> _activePassengers = new();

    public FlowNodeViewModel()
    {
        ActivePassengers = new ReadOnlyObservableCollection<string>(_activePassengers);
    }

    public required string       Id    { get; init; }
    public required string       Title { get; init; }
    public required FlowNodeType Type  { get; init; }

    public double Left   { get; init; }
    public double Top    { get; init; }
    public double Width  { get; init; } = 180;
    public double Height { get; init; } = 44;

    public ReadOnlyObservableCollection<string> ActivePassengers { get; }

    // ── Geometry helpers ──────────────────────────────────────────────────────

    public double CenterX => Left + Width  / 2;
    public double CenterY => Top  + Height / 2;

    public bool IsDecision    => Type == FlowNodeType.Decision;
    public bool IsNotDecision => Type != FlowNodeType.Decision;

    /// <summary>Diamond polygon points for decision nodes.</summary>
    public PointCollection DiamondPoints => new(new[]
    {
        new System.Windows.Point(0,         Height / 2),
        new System.Windows.Point(Width / 2, 0),
        new System.Windows.Point(Width,     Height / 2),
        new System.Windows.Point(Width / 2, Height),
    });

    /// <summary>Corner radius for non-decision nodes.</summary>
    public double CornerRadius => Type is FlowNodeType.Start or FlowNodeType.Success or FlowNodeType.Failure ? 22 : 6;

    // ── State ─────────────────────────────────────────────────────────────────

    public bool IsActive
    {
        get => _isActive;
        private set => SetProperty(ref _isActive, value);
    }

    public int ActiveCount
    {
        get => _activeCount;
        set
        {
            if (SetProperty(ref _activeCount, value))
            {
                IsActive = value > 0;
                OnPropertyChanged(nameof(HasActivePassengers));
                OnPropertyChanged(nameof(ActiveCountLabel));
                OnPropertyChanged(nameof(CurrentBackground));
                OnPropertyChanged(nameof(CurrentBorderColor));
            }
        }
    }

    public bool   HasActivePassengers => _activeCount > 0;
    public string ActiveCountLabel    => _activeCount > 0 ? _activeCount.ToString() : string.Empty;

    public void EnterPassenger(int passengerId)
    {
        string label = $"Customer {passengerId}";
        if (_activePassengers.Contains(label)) return;

        _activePassengers.Add(label);
        ActiveCount = _activePassengers.Count;
    }

    public void LeavePassenger(int passengerId)
    {
        string label = $"Customer {passengerId}";
        if (_activePassengers.Remove(label))
            ActiveCount = _activePassengers.Count;
    }

    public void ClearPassengers()
    {
        _activePassengers.Clear();
        ActiveCount = 0;
    }

    // ── Visual colors (hex strings consumed by HexToBrush converter) ──────────

    public string NormalBackground => Type switch
    {
        FlowNodeType.Start    => "#1E3A5F",
        FlowNodeType.Success  => "#0F3320",
        FlowNodeType.Failure  => "#3D1010",
        FlowNodeType.Decision => "#1C2E40",
        _                     => "#1A2744",
    };

    public string ActiveBackground => Type switch
    {
        FlowNodeType.Start    => "#1D4ED8",
        FlowNodeType.Success  => "#15803D",
        FlowNodeType.Failure  => "#B91C1C",
        FlowNodeType.Decision => "#B45309",
        _                     => "#1D4ED8",
    };

    public string NormalBorderColor => Type switch
    {
        FlowNodeType.Start    => "#3B82F6",
        FlowNodeType.Success  => "#22C55E",
        FlowNodeType.Failure  => "#EF4444",
        FlowNodeType.Decision => "#F59E0B",
        _                     => "#334155",
    };

    public string ActiveBorderColor => Type switch
    {
        FlowNodeType.Start    => "#93C5FD",
        FlowNodeType.Success  => "#86EFAC",
        FlowNodeType.Failure  => "#FCA5A5",
        FlowNodeType.Decision => "#FDE68A",
        _                     => "#93C5FD",
    };

    public string CurrentBackground  => IsActive ? ActiveBackground  : NormalBackground;
    public string CurrentBorderColor => IsActive ? ActiveBorderColor : NormalBorderColor;
}
