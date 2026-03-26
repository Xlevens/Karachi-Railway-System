using KarachiRailway.Simulation.Engine;
using KarachiRailway.Simulation.Models;

namespace KarachiRailway.Tests;

/// <summary>
/// Integration-style tests for the SimulationRunner batch mode.
/// </summary>
public class SimulationRunnerTests
{
    private static SimulationParameters MakeParams(int seed = 42) => new SimulationParameters
    {
        ArrivalRate               = 8,
        ServiceRate               = 10,
        SimulationDurationMinutes = 60,
    };

    [Fact]
    public void Run_ProducesNonZeroPassengers()
    {
        var runner = new SimulationRunner(MakeParams(), new Random(42));
        var result = runner.Run();

        Assert.True(result.TotalArrived > 0, "Should have processed passengers.");
    }

    [Fact]
    public void Run_TotalArrivedEqualsCompletedPlusLeft()
    {
        var runner = new SimulationRunner(MakeParams(), new Random(42));
        var result = runner.Run();

        Assert.Equal(result.TotalArrived, result.TotalCompleted + result.TotalLeft);
    }

    [Fact]
    public void Run_UtilizationIsCorrect()
    {
        var p = MakeParams();
        var runner = new SimulationRunner(p, new Random(42));
        var result = runner.Run();

        Assert.Equal(p.ArrivalRate / p.ServiceRate, result.Utilization, precision: 10);
    }

    [Fact]
    public void Run_StableSystem_KPIsArePositive()
    {
        var runner = new SimulationRunner(MakeParams(), new Random(42));
        var result = runner.Run();

        Assert.True(result.AvgQueueWaitTime  >= 0, "Wq must be non-negative.");
        Assert.True(result.AvgSystemTime     >= 0, "W  must be non-negative.");
        Assert.True(result.AvgQueueLength    >= 0, "Lq must be non-negative.");
        Assert.True(result.AvgNumberInSystem >= 0, "L  must be non-negative.");
    }

    [Fact]
    public async Task Cancel_StopsSimulationEarly()
    {
        var p = new SimulationParameters
        {
            ArrivalRate               = 8,
            ServiceRate               = 10,
            SimulationDurationMinutes = 10000, // very long
        };

        using var cts = new CancellationTokenSource();
        var runner = new SimulationRunner(p, new Random(42));

        int count = 0;

        var result = await runner.RunAsync(
            progressCallback: (_, _) =>
            {
                count++;
                if (count >= 10)
                    cts.Cancel();
            },
            cancellationToken: cts.Token);

        Assert.NotNull(result);
        // Should have stopped well before 10000 minutes worth of passengers
        Assert.True(result.TotalArrived < 1000,
            "Simulation should have cancelled early.");
    }
}
