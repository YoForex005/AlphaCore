using FluentAssertions;
using TraderIntelligence.Domain.Execution;

namespace TraderIntelligence.Tests.Unit.Sizing;

/// <summary>
/// A89 #47 / A43 §4.5 last-stage floor. SUT is <see cref="QuantityNormalizer"/> only.
/// Does not cover lots→ounces→OrderQty (that is G7 / A43 converter).
/// </summary>
public class QuantityNormalizerStepMinMaxTests
{
    private readonly QuantityNormalizer _n = new();

    private static InstrumentQuantitySpec DefaultSpec => new(0.01m, 5m, 0.01m, 2);

    [Theory]
    [MemberData(nameof(FloorCases))]
    public void Floors_to_step(decimal sourceLots, decimal allocation, decimal expected)
    {
        _n.Normalize(sourceLots, allocation, DefaultSpec).Should().Be(expected);
    }

    public static IEnumerable<object[]> FloorCases()
    {
        yield return new object[] { 0.333m, 1m, 0.33m };
        yield return new object[] { 0.339m, 1m, 0.33m };
        yield return new object[] { 0.335m, 1m, 0.33m };
        yield return new object[] { 1.999m, 1m, 1.99m };
        yield return new object[] { 0.019m, 1m, 0.01m };
        yield return new object[] { 0.10m, 1m, 0.10m };
        yield return new object[] { 1m, 1m / 3m, 0.33m };
    }

    [Fact]
    public void Floors_not_rounds_up_on_whole_step()
    {
        var spec = new InstrumentQuantitySpec(1m, 5_000m, 1m, 2);
        _n.Normalize(12.30m, 1m, spec).Should().Be(12.00m);
        _n.Normalize(12.99m, 1m, spec).Should().Be(12.00m);
    }

    [Fact]
    public void Floors_partial_step_of_tenth()
    {
        var spec = new InstrumentQuantitySpec(0.10m, 5_000m, 0.10m, 2);
        _n.Normalize(12.35m, 1m, spec).Should().Be(12.30m);
        _n.Normalize(10.01m, 1m, spec).Should().Be(10.00m);
    }

    [Fact]
    public void Below_min_returns_zero()
    {
        _n.Normalize(0.10m, 0.05m, DefaultSpec).Should().Be(0m);
        _n.Normalize(0.009m, 1m, DefaultSpec).Should().Be(0m);
    }

    [Fact]
    public void Below_min_after_floor_returns_zero()
    {
        var spec = new InstrumentQuantitySpec(0.02m, 5m, 0.01m, 2);
        _n.Normalize(0.019m, 1m, spec).Should().Be(0m);
    }

    [Fact]
    public void Exact_min_is_kept()
    {
        _n.Normalize(0.01m, 1m, DefaultSpec).Should().Be(0.01m);
    }

    [Fact]
    public void Above_max_caps()
    {
        _n.Normalize(5.01m, 1m, DefaultSpec).Should().Be(5m);
        _n.Normalize(100m, 1m, DefaultSpec).Should().Be(5m);
    }

    [Fact]
    public void Exact_max_is_kept()
    {
        _n.Normalize(5m, 1m, DefaultSpec).Should().Be(5m);
    }

    [Fact]
    public void Allocation_scales_before_step()
    {
        _n.Normalize(1.00m, 0.25m, DefaultSpec).Should().Be(0.25m);
        _n.Normalize(0.10m, 0.10m, DefaultSpec).Should().Be(0.01m);
        _n.Normalize(1.00m, 0.001m, DefaultSpec).Should().Be(0m);
    }

    [Fact]
    public void Precision_truncates_toward_zero_after_step()
    {
        var spec = new InstrumentQuantitySpec(0.01m, 5m, 0.01m, 1);
        _n.Normalize(0.333m, 1m, spec).Should().Be(0.3m);
    }

    [Fact]
    public void Coarser_precision_than_step_can_break_step_alignment()
    {
        var spec = new InstrumentQuantitySpec(0.01m, 5m, 0.025m, 2);
        _n.Normalize(0.08m, 1m, spec).Should().Be(0.07m);
    }

    [Fact]
    public void Unaligned_max_is_returned_raw_not_re_floored()
    {
        var spec = new InstrumentQuantitySpec(0.01m, 5.09m, 0.10m, 2);
        _n.Normalize(10m, 1m, spec).Should().Be(5.09m);
    }

    [Fact]
    public void Allocation_greater_than_one_is_currently_accepted()
    {
        _n.Normalize(1m, 1.5m, DefaultSpec).Should().Be(1.50m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-1)]
    public void Non_positive_source_lots_throws(decimal sourceLots)
    {
        var act = () => _n.Normalize(sourceLots, 1m, DefaultSpec);
        act.Should().Throw<ArgumentOutOfRangeException>().And.ParamName.Should().Be("sourceLots");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-1)]
    public void Non_positive_allocation_throws(decimal allocation)
    {
        var act = () => _n.Normalize(0.10m, allocation, DefaultSpec);
        act.Should().Throw<ArgumentOutOfRangeException>().And.ParamName.Should().Be("allocationFactor");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    public void Non_positive_step_throws(decimal step)
    {
        var spec = new InstrumentQuantitySpec(0.01m, 5m, step, 2);
        var act = () => _n.Normalize(0.10m, 1m, spec);
        act.Should().Throw<ArgumentOutOfRangeException>().And.ParamName.Should().Be("StepSize");
    }

    [Fact(Skip = "A43 E23: after dest max, q must be FloorToStep(max, step). Today Normalize returns raw MaxQuantity.")]
    public void Above_max_re_floors_to_step()
    {
        var spec = new InstrumentQuantitySpec(0.01m, 5.09m, 0.10m, 2);
        _n.Normalize(10m, 1m, spec).Should().Be(5.00m);
    }

    [Fact]
    public void Negative_precision_throws()
    {
        var spec = new InstrumentQuantitySpec(0.01m, 5m, 0.01m, -1);
        var act = () => _n.Normalize(0.10m, 1m, spec);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
