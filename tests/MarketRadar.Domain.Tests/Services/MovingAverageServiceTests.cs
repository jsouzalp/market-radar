using MarketRadar.Domain.Enums;
using MarketRadar.Domain.Services;
using MarketRadar.Domain.Tests.Helpers;

namespace MarketRadar.Domain.Tests.Services;

public class MovingAverageServiceTests
{
    private readonly MovingAverageService _sut = new();

    [Fact]
    public void Period3_FiveUptrendCandles_ReturnsExactValues()
    {
        var candles = CandleBuilder.LinearSequence(5, 100m, 1m);

        var results = _sut.CalculateEma(candles, [3]);

        results.Should().HaveCount(1);
        var ema = results.First();
        ema.Period.Should().Be(3);
        ema.CurrentValue.Should().Be(103.0m);
        ema.PreviousValue.Should().Be(102.0m);
        ema.Direction.Should().Be(MovingAverageDirection.Up);
        ema.CurrentPriceIsAbove.Should().BeTrue();
    }

    [Fact]
    public void Period9_Converged25Candles_ReturnsExactValues()
    {
        var candles = CandleBuilder.LinearSequence(25, 100m, 1m);

        var results = _sut.CalculateEma(candles, [9]);

        var ema = results.First();
        ema.CurrentValue.Should().Be(120.0m);
        ema.PreviousValue.Should().Be(119.0m);
        ema.Direction.Should().Be(MovingAverageDirection.Up);
    }

    [Fact]
    public void Period21_Converged25Candles_ReturnsExactValues()
    {
        var candles = CandleBuilder.LinearSequence(25, 100m, 1m);

        var results = _sut.CalculateEma(candles, [21]);

        var ema = results.First();
        ema.CurrentValue.Should().Be(114.0m);
        ema.PreviousValue.Should().Be(113.0m);
        ema.Direction.Should().Be(MovingAverageDirection.Up);
    }

    [Fact]
    public void Period50_55Candles_ReturnsExactValues()
    {
        // close[i]=100+i → SMA=124.5, each EMA step = (close-prev)*2/51 = 25.5*2/51 = 1 exactly
        var candles = CandleBuilder.LinearSequence(55, 100m, 1m);

        var results = _sut.CalculateEma(candles, [50]);

        var ema = results.First();
        ema.CurrentValue.Should().Be(129.5m);
        ema.PreviousValue.Should().Be(128.5m);
        ema.Direction.Should().Be(MovingAverageDirection.Up);
    }

    [Fact]
    public void InsufficientCandles_SkipsPeriod()
    {
        var candles = CandleBuilder.LinearSequence(3, 100m, 1m);

        var results = _sut.CalculateEma(candles, [9]);

        results.Should().BeEmpty();
    }

    [Fact]
    public void MultiplePeriods_SomeSkipped()
    {
        var candles = CandleBuilder.LinearSequence(25, 100m, 1m);

        var results = _sut.CalculateEma(candles, [9, 21, 50]);

        results.Should().HaveCount(2);
        results.Select(r => r.Period).Should().BeEquivalentTo([9, 21]);
    }

    [Fact]
    public void FlatPrices_ReturnsNeutralDirection()
    {
        var candles = CandleBuilder.Flat(5, 100m);

        var results = _sut.CalculateEma(candles, [3]);

        var ema = results.First();
        ema.Direction.Should().Be(MovingAverageDirection.Neutral);
        ema.CurrentValue.Should().Be(100.0m);
        ema.PreviousValue.Should().Be(100.0m);
    }

    [Fact]
    public void DowntrendPrices_ReturnsDownDirection()
    {
        var candles = CandleBuilder.LinearSequence(5, 104m, -1m);

        var results = _sut.CalculateEma(candles, [3]);

        var ema = results.First();
        ema.CurrentValue.Should().Be(101.0m);
        ema.PreviousValue.Should().Be(102.0m);
        ema.Direction.Should().Be(MovingAverageDirection.Down);
    }

    [Fact]
    public void CurrentPriceIsAbove_True_UptrendConverged()
    {
        var candles = CandleBuilder.LinearSequence(25, 100m, 1m);

        var results = _sut.CalculateEma(candles, [9]);

        results.First().CurrentPriceIsAbove.Should().BeTrue();
    }

    [Fact]
    public void CurrentPriceIsAbove_False_DowntrendConverged()
    {
        var candles = CandleBuilder.LinearSequence(25, 124m, -1m);

        var results = _sut.CalculateEma(candles, [9]);

        results.First().CurrentPriceIsAbove.Should().BeFalse();
    }

    [Fact]
    public void EmptyPeriods_ReturnsEmptyCollection()
    {
        var candles = CandleBuilder.LinearSequence(10, 100m, 1m);

        var results = _sut.CalculateEma(candles, []);

        results.Should().BeEmpty();
    }
}
