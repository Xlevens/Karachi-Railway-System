namespace KarachiRailway.Simulation.Models;

/// <summary>
/// Represents every distinct step a passenger can be at during the simulation.
/// </summary>
public enum PassengerStep
{
    // Entry
    Arrived,

    // Main branch: ticket already required?
    TicketRequired_Yes,
    TicketRequired_No,

    // Ticket Counter (direct path)
    TicketCounter,
    SecurityCheck,
    WaitingArea,
    TrainArrival,
    Boarding,
    PassengerDeparts,

    // Inquiry Desk path
    InquiryDesk,
    BuyTicket_Yes,
    BuyTicket_No,

    // Payment flow
    HasCash_Yes,
    HasCash_No,
    CashSufficientFunds_Yes,
    CashSufficientFunds_No,

    HasCard_Yes,
    HasCard_No,
    CardValid_Yes,
    CardValid_No,
    CardFundsAvailable_Yes,
    CardFundsAvailable_No,
    AccountValid_Yes,
    AccountValid_No,
    PaymentVerifiedByBank,
    TransactionComplete,
    TicketReceipt,

    // Terminal states
    PassengerLeftSystem,
    Completed,
}
