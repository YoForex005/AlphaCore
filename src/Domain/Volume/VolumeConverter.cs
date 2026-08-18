namespace TraderIntelligence.Domain.Volume;

/// <summary>
/// Converts MT5 native integer volume to lots.
/// IMTDeal::Volume() / SMTMath::VolumeToDouble uses MTAPI_VOLUME_DIV = 10_000
/// (4 decimal places). IMTDeal::VolumeExt() uses 100_000_000.
/// The comment in mt5-sdk mt5_types.h ("hundredths of lots") is incorrect.
/// Existing mt5_manager.cpp copies deal-&gt;Volume(), so the default scale is 10_000.
/// </summary>
public sealed class VolumeConverter
{
    public const decimal ManagerVolumeScale = 10_000m;
    public const decimal ExtendedVolumeScale = 100_000_000m;
    public const decimal HundredthsScale = 100m;

    public decimal Scale { get; }

    public VolumeConverter(decimal scale = ManagerVolumeScale)
    {
        if (scale <= 0)
            throw new ArgumentOutOfRangeException(nameof(scale), "Volume scale must be positive.");
        Scale = scale;
    }

    public decimal ToLots(ulong native) => native / Scale;

    public ulong ToNative(decimal lots)
    {
        if (lots < 0)
            throw new ArgumentOutOfRangeException(nameof(lots));
        return (ulong)decimal.Round(lots * Scale, 0, MidpointRounding.AwayFromZero);
    }

    public static VolumeConverter Manager => new(ManagerVolumeScale);
    public static VolumeConverter Extended => new(ExtendedVolumeScale);
}
