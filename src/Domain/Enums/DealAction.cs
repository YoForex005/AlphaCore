namespace TraderIntelligence.Domain.Enums;

/// <summary>
/// Mirrors IMTDeal::EnDealAction in MetaTrader5SDK Include/Bases/MT5APIDeal.h.
/// </summary>
public enum DealAction : uint
{
    Buy = 0,
    Sell = 1,
    Balance = 2,
    Credit = 3,
    Charge = 4,
    Correction = 5,
    Bonus = 6,
    Commission = 7,
    CommissionDaily = 8,
    CommissionMonthly = 9,
    AgentDaily = 10,
    AgentMonthly = 11,
    InterestRate = 12,
    BuyCanceled = 13,
    SellCanceled = 14,
    Dividend = 15,
    DividendFranked = 16,
    Tax = 17,
    Agent = 18,
    StopOutCompensation = 19,
    StopOutCompensationCredit = 20
}
