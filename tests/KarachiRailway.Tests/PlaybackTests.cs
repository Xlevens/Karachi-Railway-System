using KarachiRailway.Simulation.Engine;
using KarachiRailway.Simulation.Models;

namespace KarachiRailway.Tests;

/// <summary>
/// Tests that verify the playback-oriented simulation path produces correctly
/// ordered events and matches the underlying run result.
/// </summary>
public class PlaybackTests
{
    private static SimulationParameters MakeParams(int durationMinutes = 20) => new()
    {
        ArrivalRate               = 8,
        ServiceRate               = 10,
        SimulationDurationMinutes = durationMinutes,
    };

    // ── Ordering ─────────────────────────────────────────────────────────────

    [Fact]
    public void RunForPlayback_Events_AreOrderedBySimTime()
    {
        var runner = new SimulationRunner(MakeParams(), new Random(42));
        var (_, events) = runner.RunForPlayback();

        for (int i = 1; i < events.Count; i++)
            Assert.True(events[i].SimTime >= events[i - 1].SimTime,
                $"Event at index {i} (t={events[i].SimTime:F4}) is earlier than " +
                $"event at index {i - 1} (t={events[i - 1].SimTime:F4}).");
    }

    [Fact]
    public void RunForPlayback_ReturnsNonEmptyEventList_WhenPassengersArrive()
    {
        var runner = new SimulationRunner(MakeParams(), new Random(1));
        var (result, events) = runner.RunForPlayback();

        Assert.True(result.TotalArrived > 0, "Expected some arrivals.");
        Assert.NotEmpty(events);
    }

    // ── Per-passenger step coverage ───────────────────────────────────────────

    [Fact]
    public void RunForPlayback_EachPassengerStep_HasCorrespondingEvent()
    {
        var runner = new SimulationRunner(MakeParams(5), new Random(123));
        var (result, events) = runner.RunForPlayback();

        foreach (var passenger in result.Passengers)
        {
            int expectedCount = passenger.StepTrace.Count;
            int actualCount   = events.Count(e => e.PassengerId == passenger.Id);

            Assert.Equal(expectedCount, actualCount);
        }
    }

    [Fact]
    public void RunForPlayback_EventSteps_MatchPassengerStepTrace()
    {
        var runner = new SimulationRunner(MakeParams(5), new Random(7));
        var (result, events) = runner.RunForPlayback();

        foreach (var passenger in result.Passengers)
        {
            var passengerEvents = events
                .Where(e => e.PassengerId == passenger.Id)
                .OrderBy(e => e.SimTime)
                .ToList();

            Assert.Equal(passenger.StepTrace.Count, passengerEvents.Count);

            for (int i = 0; i < passenger.StepTrace.Count; i++)
                Assert.Equal(passenger.StepTrace[i], passengerEvents[i].Step);
        }
    }

    // ── SimTime bounds ────────────────────────────────────────────────────────

    [Fact]
    public void RunForPlayback_AllEventSimTimes_AreNonNegative()
    {
        var runner = new SimulationRunner(MakeParams(), new Random(99));
        var (_, events) = runner.RunForPlayback();

        Assert.All(events, e =>
            Assert.True(e.SimTime >= 0,
                $"Expected SimTime >= 0 but got {e.SimTime:F4} for passenger {e.PassengerId}."));
    }

    [Fact]
    public void RunForPlayback_TotalsMatchRunResult()
    {
        // Both the plain Run() and RunForPlayback() should produce the same summary totals.
        var parameters = MakeParams(30);
        int seed       = 555;

        var resultA  = new SimulationRunner(parameters, new Random(seed)).Run();
        var (resultB, _) = new SimulationRunner(parameters, new Random(seed)).RunForPlayback();

        Assert.Equal(resultA.TotalArrived,   resultB.TotalArrived);
        Assert.Equal(resultA.TotalCompleted, resultB.TotalCompleted);
        Assert.Equal(resultA.TotalLeft,      resultB.TotalLeft);
    }

    // ── Cancellation ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RunForPlaybackAsync_RespectsPreCancelledToken()
    {
        var parameters = MakeParams(10);
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel before we even start.

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new SimulationRunner(parameters, new Random(0))
                      .RunForPlaybackAsync(cancellationToken: cts.Token));
    }
}
