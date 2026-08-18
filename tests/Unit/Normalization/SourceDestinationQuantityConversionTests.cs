using FluentAssertions;
using TraderIntelligence.Domain.Execution;

namespace TraderIntelligence.Tests.Unit.Normalization;

/// <summary>
/// A09 / A43 §12 / A89 #45. Full SUT is the missing IQuantityConverter.
/// These cases lock the binding fixtures against <see cref="QuantityNormalizer"/>
/// so G7 passthrough is measured, not assumed.
/// </summary>
public class SourceDestinationQuantityConversionTests
{
    private readonly QuantityNormalizer _n = new();

    private static InstrumentQuantitySpec DestBaseUnits1Oz => new(0.01m, 5_000m, 0.01m, 2);

    private static InstrumentQuantitySpec DestLots100Oz => new(0.01m, 50m, 0.01m, 2);

    [Fact]
    public void QuantityNormalizer_passthroughs_0_10_lots_when_allocation_is_one()
    {
        _n.Normalize(0.10m, 1m, DestBaseUnits1Oz).Should().Be(0.10m);
        _n.Normalize(0.10m, 1m, DestBaseUnits1Oz).Should().NotBe(10.00m);
    }

    [Fact]
    public void Mini_contract_same_lots_same_normalizer_output()
    {
        _n.Normalize(0.10m, 1m, DestBaseUnits1Oz).Should().Be(0.10m);
    }

    [Fact]
    public void Lots_convention_row_also_returns_source_lots()
    {
        _n.Normalize(0.10m, 1m, DestLots100Oz).Should().Be(0.10m);
    }

    [Fact]
    public void Respects_min_qty_and_step_as_last_stage()
    {
        var whole = new InstrumentQuantitySpec(1m, 5_000m, 1m, 2);
        _n.Normalize(12.30m, 1m, DestBaseUnits1Oz).Should().Be(12.30m);
        _n.Normalize(12.30m, 1m, whole).Should().Be(12.00m);
        _n.Normalize(0.50m, 1m, whole).Should().Be(0m);
        var minOne = new InstrumentQuantitySpec(1m, 5_000m, 0.01m, 2);
        _n.Normalize(0.99m, 1m, minOne).Should().Be(0m);
    }

    [Fact(Skip = "A43 G7 / E01: IQuantityConverter missing. 0.10 MT5 lots × 100 oz → BaseUnits OrderQty 10.00, not 0.10.")]
    public void Never_passthrough_MT5_lots()
    {
        Assert.Fail("Call IQuantityConverter.Convert; do not implement ounces math in the test.");
    }

    [Theory(Skip = "A43 §10.1 E01–E09: IQuantityConverter missing.")]
    [InlineData(1_000UL, 100, "BaseUnits", 10.00)]
    [InlineData(100UL, 100, "BaseUnits", 1.00)]
    [InlineData(10_000UL, 100, "BaseUnits", 100.00)]
    [InlineData(10UL, 100, "BaseUnits", 0.10)]
    [InlineData(1UL, 100, "BaseUnits", 0.01)]
    [InlineData(1_000UL, 10, "BaseUnits", 1.00)]
    [InlineData(1_000UL, 1, "BaseUnits", 0.10)]
    [InlineData(1_000UL, 100, "Lots", 0.10)]
    [InlineData(100UL, 100, "Lots", 0.01)]
    public void Known_lot_to_OrderQty_examples(ulong ticks, int contractSize, string convention, double expected)
    {
        _ = (ticks, contractSize, convention, expected);
        Assert.Fail("Call IQuantityConverter.Convert for E01–E09.");
    }

    [Fact(Skip = "A43 E06 vs E01: mini contract_size=10 must yield 1.00 oz, not the same qty as contract_size=100.")]
    public void Mini_and_nano_contracts_differ()
    {
        Assert.Fail("Call IQuantityConverter.Convert; contract_size is not on InstrumentQuantitySpec.");
    }

    [Fact(Skip = "A43 E08: Lots convention is the only mapping where 0.10 lots may equal OrderQty 0.10.")]
    public void Lots_convention_only_when_mapped()
    {
        Assert.Fail("Call IQuantityConverter.Convert with QuantityConvention.Lots vs BaseUnits.");
    }

    [Fact(Skip = "A38: source ticks / 10_000 = lots. Converter not implemented.")]
    public void Mt5_ticks_scale_is_10000()
    {
        Assert.Fail("Converter must use VolumeConverter.Manager (1 lot = 10_000), never /100.");
    }

    [Fact(Skip = "A43 E05: 1 tick × 100 oz = 0.01 dest; must stay decimal 0.01m.")]
    public void Decimal_not_double_for_0_0001_lot()
    {
        Assert.Fail("IQuantityConverter must return exactly 0.01m for 1 tick × 100 oz.");
    }

    [Fact(Skip = "A43 E21: confidence_scale > 1 is illegal. QuantityNormalizer has no confidence input.")]
    public void Confidence_cannot_exceed_one()
    {
        true.Should().BeFalse("IQuantityConverter must reject confidence_scale > 1");
    }

    [Fact(Skip = "A43 §4.7 E32/E38: REDUCE/CLOSE uses mapped dest qty, not source lots × allocation.")]
    public void Close_uses_mapped_destination_qty()
    {
        true.Should().BeFalse("IQuantityConverter CLOSE path is not QuantityNormalizer.Normalize");
    }

    [Fact(Skip = "A43 E37: leftover < dest min promotes REDUCE to CLOSE.")]
    public void Dust_leftover_promotes_to_close()
    {
        true.Should().BeFalse("IQuantityConverter REDUCE dust policy is not implemented");
    }

    [Fact(Skip = "A43 E39: Unverified dest spec must reject. InstrumentQuantitySpec has no spec_status.")]
    public void Unverified_dest_spec_rejects()
    {
        true.Should().BeFalse("destination_symbols.spec_status is not on InstrumentQuantitySpec");
    }

    [Fact(Skip = "A43 E16–E21: allocation × confidence before dest step. QuantityNormalizer has no confidence input.")]
    public void Allocation_and_confidence_scale()
    {
        true.Should().BeFalse("IQuantityConverter allocation/confidence path is missing");
    }

    [Fact(Skip = "A43 E22–E26: dest max and risk caps reduce then re-quantize.")]
    public void Dest_max_and_risk_caps_reduce()
    {
        true.Should().BeFalse("IQuantityConverter risk-cap reduce is missing");
    }

    [Fact(Skip = "A43 E14/E20/E25/E29: below min after cap is REJECT, not a sendable 0.")]
    public void Below_min_after_cap_rejects()
    {
        true.Should().BeFalse("QuantityNormalizer returns 0m; converter must emit SIZE_BELOW_MIN");
    }

    [Fact(Skip = "A43 E27–E29: margin room reduces qty. No quote/leverage on QuantityNormalizer.")]
    public void Margin_room_reduces_qty()
    {
        true.Should().BeFalse("IQuantityConverter margin path is missing");
    }

    [Fact(Skip = "A43 E30–E31: INCREASE uses incremental source ticks, not position max_volume.")]
    public void Increase_uses_incremental_volume()
    {
        true.Should().BeFalse("IQuantityConverter INCREASE path is missing");
    }

    [Fact(Skip = "A43 E33–E37: REDUCE is a fraction of mapped dest qty.")]
    public void Partial_reduce_is_fraction_of_dest()
    {
        true.Should().BeFalse("IQuantityConverter REDUCE path is missing");
    }

    [Fact(Skip = "A43 E40–E41: contract_size <= 0 / NaN rejects.")]
    public void Invalid_contract_size_rejects()
    {
        true.Should().BeFalse("InstrumentQuantitySpec has no contract_size");
    }

    [Fact(Skip = "A43 E42–E43: missing dest spec or REDUCE link rejects.")]
    public void Missing_mapping_rejects()
    {
        true.Should().BeFalse("IQuantityConverter mapping checks are missing");
    }

    [Fact(Skip = "A43 E44–E46: step 0 / step 0.001 / min not multiple of step reject the spec.")]
    public void Invalid_step_or_min_rejects()
    {
        true.Should().BeFalse("InstrumentQuantitySpec is an unvalidated record");
    }

    [Fact(Skip = "A43 §6: shadow and live must call the same converter.")]
    public void Shadow_and_live_share_converter()
    {
        true.Should().BeFalse("QuantityNormalizer is unused by ShadowCopyEngine and RiskEngine");
    }

    [Fact(Skip = "A43 §6: FIX worker must not rescale requested_quantity.")]
    public void Fix_worker_does_not_rescale()
    {
        true.Should().BeFalse("No FIX NOS builder consumes QuantityNormalizer output");
    }
}
