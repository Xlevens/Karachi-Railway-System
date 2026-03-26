using KarachiRailway.Simulation.Models;

namespace KarachiRailway.Simulation.Engine;

/// <summary>
/// Encodes the complete passenger decision-flow from the diagram.
/// Uses a deterministic Random instance so flows can be seeded for testing.
/// </summary>
public class PassengerFlowEngine
{
    private readonly SimulationParameters _params;
    private readonly Random _rng;

    public PassengerFlowEngine(SimulationParameters parameters, Random? random = null)
    {
        _params = parameters ?? throw new ArgumentNullException(nameof(parameters));
        _rng = random ?? new Random();
    }

    /// <summary>
    /// Runs a single passenger through the full decision tree.
    /// All steps are recorded in <paramref name="passenger"/>.StepTrace.
    /// </summary>
    public void ProcessPassenger(Passenger passenger)
    {
        var trace = passenger.StepTrace;
        trace.Add(PassengerStep.Arrived);

        // ── Decision: Ticket Required? ─────────────────────────────────────
        bool ticketRequired = _rng.NextDouble() < _params.TicketRequiredProbability;
        if (ticketRequired)
        {
            trace.Add(PassengerStep.TicketRequired_Yes);
            RunDirectTicketPath(passenger);
        }
        else
        {
            trace.Add(PassengerStep.TicketRequired_No);
            RunInquiryPath(passenger);
        }
    }

    // ── Direct ticket path ────────────────────────────────────────────────────

    private void RunDirectTicketPath(Passenger passenger)
    {
        var trace = passenger.StepTrace;
        trace.Add(PassengerStep.TicketCounter);
        trace.Add(PassengerStep.SecurityCheck);
        trace.Add(PassengerStep.WaitingArea);
        trace.Add(PassengerStep.TrainArrival);
        trace.Add(PassengerStep.Boarding);
        trace.Add(PassengerStep.PassengerDeparts);
        trace.Add(PassengerStep.Completed);
        passenger.Completed = true;
    }

    // ── Inquiry desk path ─────────────────────────────────────────────────────

    private void RunInquiryPath(Passenger passenger)
    {
        var trace = passenger.StepTrace;
        trace.Add(PassengerStep.InquiryDesk);

        bool buyTicket = _rng.NextDouble() < _params.BuyTicketProbability;
        if (!buyTicket)
        {
            trace.Add(PassengerStep.BuyTicket_No);
            LeaveSystem(passenger);
            return;
        }

        trace.Add(PassengerStep.BuyTicket_Yes);
        RunPaymentFlow(passenger);
    }

    // ── Payment/validation flow ───────────────────────────────────────────────

    private void RunPaymentFlow(Passenger passenger)
    {
        var trace = passenger.StepTrace;

        // Card vs Cash decision (probability of using a card)
        bool useCard = _rng.NextDouble() < _params.CardUsageProbability;

        if (!useCard)
        {
            // ── Cash path ─────────────────────────────────────────────────
            trace.Add(PassengerStep.HasCash_Yes);
            bool sufficientFunds = _rng.NextDouble() < _params.SufficientFundsProbability;
            if (!sufficientFunds)
            {
                trace.Add(PassengerStep.CashSufficientFunds_No);
                LeaveSystem(passenger);
                return;
            }

            trace.Add(PassengerStep.CashSufficientFunds_Yes);
            CompletePayment(passenger);
        }
        else
        {
            // ── Card path ─────────────────────────────────────────────────
            trace.Add(PassengerStep.HasCard_Yes);

            bool cardValid = _rng.NextDouble() < _params.CardValidProbability;
            if (!cardValid)
            {
                trace.Add(PassengerStep.CardValid_No);
                // Try cash fallback
                RunCashFallback(passenger);
                return;
            }

            trace.Add(PassengerStep.CardValid_Yes);

            bool fundsAvailable = _rng.NextDouble() < _params.SufficientFundsProbability;
            if (!fundsAvailable)
            {
                trace.Add(PassengerStep.CardFundsAvailable_No);
                LeaveSystem(passenger);
                return;
            }

            trace.Add(PassengerStep.CardFundsAvailable_Yes);

            bool accountValid = _rng.NextDouble() < _params.AccountValidProbability;
            if (!accountValid)
            {
                trace.Add(PassengerStep.AccountValid_No);
                LeaveSystem(passenger);
                return;
            }

            trace.Add(PassengerStep.AccountValid_Yes);
            CompletePayment(passenger);
        }
    }

    /// <summary>
    /// When card is invalid, passenger attempts cash payment.
    /// If no cash, or insufficient cash, they leave the system.
    /// </summary>
    private void RunCashFallback(Passenger passenger)
    {
        var trace = passenger.StepTrace;

        // Re-check: does passenger have cash?
        bool hasCash = _rng.NextDouble() >= _params.CardUsageProbability; // inverse card prob ≈ cash prob
        if (!hasCash)
        {
            trace.Add(PassengerStep.HasCash_No);
            trace.Add(PassengerStep.HasCard_No);
            LeaveSystem(passenger);
            return;
        }

        trace.Add(PassengerStep.HasCash_Yes);
        bool sufficientFunds = _rng.NextDouble() < _params.SufficientFundsProbability;
        if (!sufficientFunds)
        {
            trace.Add(PassengerStep.CashSufficientFunds_No);
            LeaveSystem(passenger);
            return;
        }

        trace.Add(PassengerStep.CashSufficientFunds_Yes);
        CompletePayment(passenger);
    }

    private void CompletePayment(Passenger passenger)
    {
        var trace = passenger.StepTrace;
        trace.Add(PassengerStep.PaymentVerifiedByBank);
        trace.Add(PassengerStep.TransactionComplete);
        trace.Add(PassengerStep.TicketReceipt);

        // After ticket receipt → same boarding sequence
        trace.Add(PassengerStep.SecurityCheck);
        trace.Add(PassengerStep.WaitingArea);
        trace.Add(PassengerStep.TrainArrival);
        trace.Add(PassengerStep.Boarding);
        trace.Add(PassengerStep.PassengerDeparts);
        trace.Add(PassengerStep.Completed);
        passenger.Completed = true;
    }

    private static void LeaveSystem(Passenger passenger)
    {
        passenger.StepTrace.Add(PassengerStep.PassengerLeftSystem);
        passenger.LeftSystem = true;
    }
}
