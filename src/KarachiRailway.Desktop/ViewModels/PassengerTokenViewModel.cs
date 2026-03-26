namespace KarachiRailway.Desktop.ViewModels;

/// <summary>
/// Represents a visible passenger token moving through the flow-block canvas.
/// X and Y are the Canvas.Left / Canvas.Top of the token's top-left corner.
/// </summary>
public class PassengerTokenViewModel : ViewModelBase
{
    private double _x;
    private double _y;
    private bool   _isCompleted;
    private bool   _isLeft;

    public required int PassengerId { get; init; }

    public double X
    {
        get => _x;
        set => SetProperty(ref _x, value);
    }

    public double Y
    {
        get => _y;
        set => SetProperty(ref _y, value);
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        set
        {
            if (SetProperty(ref _isCompleted, value))
                OnPropertyChanged(nameof(TokenColor));
        }
    }

    public bool IsLeft
    {
        get => _isLeft;
        set
        {
            if (SetProperty(ref _isLeft, value))
                OnPropertyChanged(nameof(TokenColor));
        }
    }

    /// <summary>Hex color: green = completed, red = left, blue = in-progress.</summary>
    public string TokenColor =>
        IsCompleted ? "#22C55E" :
        IsLeft      ? "#EF4444" :
                      "#3B82F6";

    /// <summary>Short label shown inside the token circle.</summary>
    public string Label => PassengerId > 99 ? "…" : PassengerId.ToString();
}
