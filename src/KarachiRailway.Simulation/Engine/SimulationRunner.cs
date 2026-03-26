using KarachiRailway.Simulation.Models;

namespace KarachiRailway.Simulation.Engine;

/// <summary>
/// Discrete-event simulator for the Karachi Railway M/M/1 single-server queue.
/// Generates exponentially-distributed inter-arrival and service times,
/// then routes each passenger through the decision-flow engine.
/// </summary>
public class SimulationRunner
{
    private readonly SimulationParameters _params;
    private readonly Random _rng;
    private volatile bool _paused;
    private volatile bool _cancelled;

    /// <summary>
    /// Raised after each passenger is processed (useful for real-time UI updates).
    /// The argument is the current <see cref="Passenger"/>.
    /// </summary>
    public event EventHandler<Passenger>? PassengerProcessed;

    // QueueDepthChanged is available for future UI integration (live queue-length display).

    public SimulationRunner(SimulationParameters parameters, Random? random = null)
    {
        _params = parameters?.Clone()
            ?? throw new ArgumentNullException(nameof(parameters));
        _rng = random ?? new Random();
    }

    // ── Control ───────────────────────────────────────────────────────────────

    public void Pause()  => _paused = true;
    public void Resume() => _paused = false;
    public void Cancel() => _cancelled = true;

    // ── Run ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Executes the full batch simulation.
    /// </summary>
    /// <param name="progressCallback">
    /// Optional callback invoked after each passenger is processed.
    /// Receives (passenger, simulationTimeNow).
    /// </param>
    /// <param name="cancellationToken">
    /// External cancellation support (e.g. from UI Stop button).
    /// </param>
    public SimulationResult Run(
        Action<Passenger, double>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        _paused    = false;
        _cancelled = false;

        var flowEngine = new PassengerFlowEngine(_params, _rng);
        var passengers = new List<Passenger>();

        double simTime      = 0.0;
        double serviceEndAt = 0.0;  // When the single server becomes free
        int    passengerIdCounter = 1;

        double durationMinutes = _params.SimulationDurationMinutes;

        // Generate arrivals until simulation time runs out
        while (simTime < durationMinutes)
        {
            if (_cancelled || cancellationToken.IsCancellationRequested)
                break;

            // Respect pause (tight-loop poll – caller should use async Task to avoid blocking)
            while (_paused && !_cancelled && !cancellationToken.IsCancellationRequested)
                Thread.Sleep(50);

            if (_cancelled || cancellationToken.IsCancellationRequested)
                break;

            // Next arrival (exponential inter-arrival)
            double interArrival = SampleExponential(_params.ArrivalRate);
            simTime += interArrival;

            if (simTime > durationMinutes)
                break;

            // Build passenger
            var passenger = new Passenger
            {
                Id          = passengerIdCounter++,
                ArrivalTime = simTime,
            };

            // Queue / server assignment
            double startService = Math.Max(simTime, serviceEndAt);
            passenger.ServiceStartTime = startService;

            // Service duration (exponential)
            double serviceDuration = SampleExponential(_params.ServiceRate);
            serviceEndAt = startService + serviceDuration;
            passenger.ExitTime = serviceEndAt;

            // Run the passenger through the decision-flow
            flowEngine.ProcessPassenger(passenger);

            passengers.Add(passenger);

            PassengerProcessed?.Invoke(this, passenger);
            progressCallback?.Invoke(passenger, simTime);
        }

        return BuildResult(passengers, durationMinutes);
    }

    /// <summary>
    /// Async wrapper so the UI can run without blocking the dispatch thread.
    /// </summary>
    public Task<SimulationResult> RunAsync(
        Action<Passenger, double>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(
            () => Run(progressCallback, cancellationToken),
            cancellationToken);
    }

    // ── Result builder ────────────────────────────────────────────────────────

    private SimulationResult BuildResult(List<Passenger> passengers, double duration)
    {
        // Analytical M/M/1 KPIs
        var (rho, lq, l, wq, w) = MM1Calculator.Compute(
            _params.ArrivalRate, _params.ServiceRate);

        int completed = passengers.Count(p => p.Completed);
        int left      = passengers.Count(p => p.LeftSystem);

        double simAvgWait = passengers.Count > 0
            ? passengers.Average(p => p.WaitTime)
            : 0;
        double simAvgSystem = passengers.Count > 0
            ? passengers.Average(p => p.SystemTime)
            : 0;

        return new SimulationResult
        {
            Utilization             = rho,
            AvgQueueWaitTime        = wq,
            AvgSystemTime           = w,
            AvgQueueLength          = lq,
            AvgNumberInSystem       = l,
            TotalArrived            = passengers.Count,
            TotalCompleted          = completed,
            TotalLeft               = left,
            SimulationDurationMinutes = duration,
            SimAvgWaitTime          = simAvgWait,
            SimAvgSystemTime        = simAvgSystem,
            Passengers              = passengers,
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Samples from Exponential(rate) distribution via inverse CDF.</summary>
    private double SampleExponential(double rate)
    {
        double u;
        do { u = _rng.NextDouble(); } while (u == 0.0);
        return -Math.Log(u) / rate;
    }
}
