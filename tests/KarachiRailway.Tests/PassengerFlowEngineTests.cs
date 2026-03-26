using KarachiRailway.Simulation.Engine;
using KarachiRailway.Simulation.Models;

namespace KarachiRailway.Tests;

/// <summary>
/// Unit tests for the passenger decision-flow engine.
/// Uses seeded Randoms to force deterministic branches.
/// </summary>
public class PassengerFlowEngineTests
{
    private static SimulationParameters DefaultParams() => new SimulationParameters();

    private static Passenger MakePassenger(int id = 1) => new Passenger
    {
        Id          = id,
        ArrivalTime = 0,
    };

    // ── Seeded random helpers ────────────────────────────────────────────────

    /// <summary>
    /// Returns a Random that produces only values below <paramref name="threshold"/>
    /// for the first <paramref name="count"/> draws, by scanning seeds.
    /// For simplicity we use a StubRandom approach.
    /// </summary>
    private static Random AllLow() => new SeededRandom(alwaysReturn: 0.01);
    private static Random AllHigh() => new SeededRandom(alwaysReturn: 0.99);

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Process_TicketRequired_DirectPath_Completes()
    {
        // TicketRequiredProbability = 0.65; value 0.01 < 0.65 → ticket required = YES
        var engine = new PassengerFlowEngine(DefaultParams(), AllLow());
        var p = MakePassenger();

        engine.ProcessPassenger(p);

        Assert.True(p.Completed);
        Assert.False(p.LeftSystem);
        Assert.Contains(PassengerStep.TicketRequired_Yes, p.StepTrace);
        Assert.Contains(PassengerStep.TicketCounter,      p.StepTrace);
        Assert.Contains(PassengerStep.SecurityCheck,      p.StepTrace);
        Assert.Contains(PassengerStep.Boarding,           p.StepTrace);
        Assert.Contains(PassengerStep.Completed,          p.StepTrace);
    }

    [Fact]
    public void Process_NoTicketRequired_DeclinesToBuy_LeavesSystem()
    {
        // value 0.99:
        //   TicketRequired check: 0.99 >= 0.65 → No ticket required
        //   BuyTicket check: 0.99 >= 0.80 → Won't buy
        var engine = new PassengerFlowEngine(DefaultParams(), AllHigh());
        var p = MakePassenger();

        engine.ProcessPassenger(p);

        Assert.False(p.Completed);
        Assert.True(p.LeftSystem);
        Assert.Contains(PassengerStep.TicketRequired_No,    p.StepTrace);
        Assert.Contains(PassengerStep.InquiryDesk,          p.StepTrace);
        Assert.Contains(PassengerStep.BuyTicket_No,         p.StepTrace);
        Assert.Contains(PassengerStep.PassengerLeftSystem,  p.StepTrace);
    }

    [Fact]
    public void Process_AllDecisionsSucceed_Completes()
    {
        // Use probabilities that make every check pass:
        // Set all probabilities to 1.0 → every positive branch taken
        var paramsAllPass = new SimulationParameters
        {
            TicketRequiredProbability = 0.0,  // → No ticket required (inquiry path)
            BuyTicketProbability      = 1.0,  // → Buys ticket
            CardUsageProbability      = 1.0,  // → Uses card
            CardValidProbability      = 1.0,  // → Card valid
            SufficientFundsProbability = 1.0, // → Funds available
            AccountValidProbability   = 1.0,  // → Account valid
        };

        var engine = new PassengerFlowEngine(paramsAllPass, AllLow());
        var p = MakePassenger();

        engine.ProcessPassenger(p);

        Assert.True(p.Completed);
        Assert.Contains(PassengerStep.HasCard_Yes,          p.StepTrace);
        Assert.Contains(PassengerStep.CardValid_Yes,        p.StepTrace);
        Assert.Contains(PassengerStep.CardFundsAvailable_Yes, p.StepTrace);
        Assert.Contains(PassengerStep.AccountValid_Yes,     p.StepTrace);
        Assert.Contains(PassengerStep.PaymentVerifiedByBank, p.StepTrace);
        Assert.Contains(PassengerStep.TransactionComplete,  p.StepTrace);
        Assert.Contains(PassengerStep.TicketReceipt,        p.StepTrace);
        Assert.Contains(PassengerStep.Boarding,             p.StepTrace);
        Assert.Contains(PassengerStep.Completed,            p.StepTrace);
    }

    [Fact]
    public void Process_CashPath_InsufficientFunds_LeavesSystem()
    {
        // Inquiry path, buys ticket, uses cash, but insufficient funds
        var paramsSetup = new SimulationParameters
        {
            TicketRequiredProbability  = 0.0,  // → Inquiry path
            BuyTicketProbability       = 1.0,  // → Buys
            CardUsageProbability       = 0.0,  // → Cash
            SufficientFundsProbability = 0.0,  // → Insufficient
        };

        var engine = new PassengerFlowEngine(paramsSetup, AllLow());
        var p = MakePassenger();

        engine.ProcessPassenger(p);

        Assert.True(p.LeftSystem);
        Assert.Contains(PassengerStep.HasCash_Yes,              p.StepTrace);
        Assert.Contains(PassengerStep.CashSufficientFunds_No,   p.StepTrace);
        Assert.Contains(PassengerStep.PassengerLeftSystem,       p.StepTrace);
    }

    [Fact]
    public void Process_CardInvalid_NoFallbackCash_LeavesSystem()
    {
        // Card invalid, and no cash either
        var paramsSetup = new SimulationParameters
        {
            TicketRequiredProbability  = 0.0,  // → Inquiry path
            BuyTicketProbability       = 1.0,
            CardUsageProbability       = 1.0,  // → Tries card
            CardValidProbability       = 0.0,  // → Card invalid
            // After card invalid, cash fallback: hasCash check uses inverse of CardUsageProbability
            // We set CardUsageProbability = 1.0 so hasCash probability = 0 → no cash
        };

        var engine = new PassengerFlowEngine(paramsSetup, AllLow());
        var p = MakePassenger();

        engine.ProcessPassenger(p);

        Assert.True(p.LeftSystem);
        Assert.Contains(PassengerStep.CardValid_No, p.StepTrace);
        Assert.Contains(PassengerStep.PassengerLeftSystem, p.StepTrace);
    }

    [Fact]
    public void Process_StepTrace_AlwaysStartsWithArrived()
    {
        var engine = new PassengerFlowEngine(DefaultParams());
        var p = MakePassenger();

        engine.ProcessPassenger(p);

        Assert.Equal(PassengerStep.Arrived, p.StepTrace[0]);
    }

    [Fact]
    public void Process_ExactlyOneTerminalState()
    {
        // Every passenger must end in exactly one of: Completed or PassengerLeftSystem
        var engine = new PassengerFlowEngine(DefaultParams());

        for (int i = 1; i <= 100; i++)
        {
            var p = MakePassenger(i);
            engine.ProcessPassenger(p);

            bool hasCompleted = p.StepTrace.Contains(PassengerStep.Completed);
            bool hasLeft      = p.StepTrace.Contains(PassengerStep.PassengerLeftSystem);

            Assert.True(hasCompleted ^ hasLeft,
                $"Passenger #{i} must end in exactly one terminal state.");
            Assert.Equal(hasCompleted, p.Completed);
            Assert.Equal(hasLeft,      p.LeftSystem);
        }
    }
}

// ── Stub random helper ────────────────────────────────────────────────────────

/// <summary>
/// A deterministic fake Random that always returns the same double value.
/// Used to force specific branches in the flow engine.
/// </summary>
internal sealed class SeededRandom : Random
{
    private readonly double _value;
    public SeededRandom(double alwaysReturn) => _value = alwaysReturn;
    public override double NextDouble() => _value;
    public override int    Next()               => _value < 0.5 ? 0 : 1;
    public override int    Next(int maxValue)   => (int)(_value * maxValue);
    public override int    Next(int min, int max) => (int)(min + _value * (max - min));
}
