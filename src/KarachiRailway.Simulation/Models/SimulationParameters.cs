using System.ComponentModel.DataAnnotations;

namespace KarachiRailway.Simulation.Models;

/// <summary>
/// All configurable parameters for one simulation run.
/// Includes validation annotations that the UI layer can surface.
/// </summary>
public class SimulationParameters
{
    // ── M/M/1 Rates ─────────────────────────────────────────────────────────

    /// <summary>Passenger arrival rate λ (passengers per minute).</summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "Arrival rate (λ) must be greater than 0.")]
    public double ArrivalRate { get; set; } = 8.0;

    /// <summary>Service rate μ (passengers per minute).</summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "Service rate (μ) must be greater than 0.")]
    public double ServiceRate { get; set; } = 10.0;

    // ── Simulation Duration ──────────────────────────────────────────────────

    /// <summary>Total simulated time in minutes.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Simulation duration must be at least 1 minute.")]
    public int SimulationDurationMinutes { get; set; } = 120;

    // ── Flow Probabilities ───────────────────────────────────────────────────

    /// <summary>Probability that a passenger already has/needs a ticket (direct to counter).</summary>
    [Range(0.0, 1.0, ErrorMessage = "Probabilities must be between 0 and 1.")]
    public double TicketRequiredProbability { get; set; } = 0.65;

    /// <summary>Probability that a passenger at the inquiry desk decides to buy a ticket.</summary>
    [Range(0.0, 1.0, ErrorMessage = "Probabilities must be between 0 and 1.")]
    public double BuyTicketProbability { get; set; } = 0.80;

    /// <summary>
    /// Probability that a passenger at the payment stage uses a card
    /// (as opposed to cash).
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "Probabilities must be between 0 and 1.")]
    public double CardUsageProbability { get; set; } = 0.45;

    /// <summary>Probability that a passenger's card is valid.</summary>
    [Range(0.0, 1.0, ErrorMessage = "Probabilities must be between 0 and 1.")]
    public double CardValidProbability { get; set; } = 0.95;

    /// <summary>Probability that a passenger's bank account is valid.</summary>
    [Range(0.0, 1.0, ErrorMessage = "Probabilities must be between 0 and 1.")]
    public double AccountValidProbability { get; set; } = 0.97;

    /// <summary>
    /// Probability that a passenger has sufficient funds (applies to both
    /// cash-sufficient-funds and card-funds-available checks).
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "Probabilities must be between 0 and 1.")]
    public double SufficientFundsProbability { get; set; } = 0.90;

    // ── Derived / Computed ───────────────────────────────────────────────────

    /// <summary>Server utilisation ρ = λ / μ.</summary>
    public double Utilization => ServiceRate > 0 ? ArrivalRate / ServiceRate : double.NaN;

    /// <summary>True when the system is stable (ρ &lt; 1).</summary>
    public bool IsStable => Utilization < 1.0;

    /// <summary>Returns a deep copy with the same parameter values.</summary>
    public SimulationParameters Clone() => new()
    {
        ArrivalRate = ArrivalRate,
        ServiceRate = ServiceRate,
        SimulationDurationMinutes = SimulationDurationMinutes,
        TicketRequiredProbability = TicketRequiredProbability,
        BuyTicketProbability = BuyTicketProbability,
        CardUsageProbability = CardUsageProbability,
        CardValidProbability = CardValidProbability,
        AccountValidProbability = AccountValidProbability,
        SufficientFundsProbability = SufficientFundsProbability,
    };
}
