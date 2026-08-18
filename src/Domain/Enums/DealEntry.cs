namespace TraderIntelligence.Domain.Enums;

/// <summary>
/// Mirrors IMTDeal::EnDealEntry in MetaTrader5SDK Include/Bases/MT5APIDeal.h.
/// </summary>
public enum DealEntry : uint
{
    In = 0,
    Out = 1,
    InOut = 2,
    OutBy = 3
}
