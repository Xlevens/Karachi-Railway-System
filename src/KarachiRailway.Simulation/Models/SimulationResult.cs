namespace KarachiRailway.Simulation.Models;

/// <summary>
/// Aggregated results produced after a batch simulation run.
/// </summary>
public class SimulationResult
{
    // ── M/M/1 Analytical KPIs ────────────────────────────────────────────────

    /// <summary>Server utilisation ρ = λ / μ.</summary>
    public double Utilization { get; set; }

    /// <summary>Average waiting time in queue Wq (minutes).</summary>
    public double AvgQueueWaitTime { get; set; }

    /// <summary>Average time in system W (minutes).</summary>
    public double AvgSystemTime { get; set; }

    /// <summary>Average queue length Lq.</summary>
    public double AvgQueueLength { get; set; }

    /// <summary>Average number in system L.</summary>
    public double AvgNumberInSystem { get; set; }

    // ── Simulation Counts ────────────────────────────────────────────────────

    /// <summary>Total passengers that arrived during the run.</summary>
    public int TotalArrived { get; set; }

    /// <summary>Passengers that completed the journey (boarded and departed).</summary>
    public int TotalCompleted { get; set; }

    /// <summary>Passengers that left the system before boarding.</summary>
    public int TotalLeft { get; set; }

    /// <summary>Simulated duration in minutes.</summary>
    public double SimulationDurationMinutes { get; set; }

    // ── Simulation-measured averages (from event traces) ─────────────────────

    /// <summary>Average wait time measured from simulation events (minutes).</summary>
    public double SimAvgWaitTime { get; set; }

    /// <summary>Average system time measured from simulation events (minutes).</summary>
    public double SimAvgSystemTime { get; set; }

    // ── Passenger Traces (for single-passenger trace mode) ───────────────────

    /// <summary>All simulated passengers (populated in trace mode; may be empty in batch mode).</summary>
    public List<Passenger> Passengers { get; set; } = new();

    // ── Helper Properties ────────────────────────────────────────────────────

    /// <summary>Throughput: completed passengers per minute.</summary>
    public double Throughput =>
        SimulationDurationMinutes > 0
            ? TotalCompleted / SimulationDurationMinutes
            : 0;

    /// <summary>Completion rate as a percentage.</summary>
    public double CompletionRate =>
        TotalArrived > 0
            ? (double)TotalCompleted / TotalArrived * 100.0
            : 0;
}
