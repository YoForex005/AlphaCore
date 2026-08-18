namespace TraderIntelligence.Domain.Enums;

/// <summary>
/// Mirrors IMTDeal::EnDealReason. Reconstruction treats only a subset as trader activity.
/// </summary>
public enum DealReason : uint
{
    Client = 0,
    Expert = 1,
    Dealer = 2,
    StopLoss = 3,
    TakeProfit = 4,
    StopOut = 5,
    Rollover = 6,
    ExternalClient = 7,
    VariationMargin = 8,
    Gateway = 9,
    Signal = 10,
    Settlement = 11,
    Transfer = 12,
    Sync = 13,
    ExternalService = 14,
    Migration = 15,
    Mobile = 16,
    Web = 17,
    Split = 18,
    CorporateAction = 19
}

public static class DealReasons
{
    public static bool CountsAsTraderActivity(DealReason? reason)
    {
        if (reason is null)
            return true;

        return reason.Value is
            DealReason.Client or
            DealReason.Expert or
            DealReason.Dealer or
            DealReason.StopLoss or
            DealReason.TakeProfit or
            DealReason.StopOut or
            DealReason.ExternalClient or
            DealReason.Gateway or
            DealReason.Signal or
            DealReason.Mobile or
            DealReason.Web;
    }
}
