namespace KarachiRailway.Simulation.Models;

/// <summary>
/// A single timestamped event produced during simulation playback.
/// The UI consumes these events in order to animate passenger movement through the flow diagram.
/// </summary>
public record PlaybackEvent(int PassengerId, PassengerStep Step, double SimTime);
