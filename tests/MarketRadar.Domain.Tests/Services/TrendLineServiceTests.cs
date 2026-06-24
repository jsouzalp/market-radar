using MarketRadar.Domain.Enums;
using MarketRadar.Domain.Services;
using MarketRadar.Domain.Settings;
using MarketRadar.Domain.Tests.Helpers;

namespace MarketRadar.Domain.Tests.Services;

public class TrendLineServiceTests
{
    private readonly TrendLineService _sut = new();

    private static TrendAnalysisSettings Settings(decimal minimumSlope = 0.001m) => new()
    {
        MinimumSlope = minimumSlope
    };

    [Fact]
    public void PerfectUptrend_ReturnsSlope1_DirectionUp()
    {
        var candles = CandleBuilder.LinearSequence(3, 100m, 1m);

        var result = _sut.Calculate(candles, Settings());

        result.Slope.Should().Be(1.0m);
        result.Intercept.Should().Be(100.0m);
        result.ResidualStandardDeviation.Should().Be(0.0m);
        result.Direction.Should().Be(TrendDirection.Up);
    }

    [Fact]
    public void PerfectDowntrend_ReturnsNegativeSlope_DirectionDown()
    {
        var candles = CandleBuilder.LinearSequence(5, 104m, -1m);

        var result = _sut.Calculate(candles, Settings());

        result.Slope.Should().Be(-1.0m);
        result.Intercept.Should().Be(104.0m);
        result.Direction.Should().Be(TrendDirection.Down);
    }

    [Fact]
    public void FlatPrices_ReturnsSlope0_DirectionNeutral()
    {
        var candles = CandleBuilder.Flat(5, 100m);

        var result = _sut.Calculate(candles, Settings());

        result.Slope.Should().Be(0.0m);
        result.Intercept.Should().Be(100.0m);
        result.Direction.Should().Be(TrendDirection.Neutral);
    }

    [Fact]
    public void SlopeBelowMinimum_ReturnsNeutral()
    {
        var candles = CandleBuilder.LinearSequence(3, 100m, 0.0001m);

        var result = _sut.Calculate(candles, Settings(minimumSlope: 0.001m));

        Math.Abs(result.Slope).Should().BeLessThan(0.001m);
        result.Direction.Should().Be(TrendDirection.Neutral);
    }

    [Fact]
    public void CurrentTrendPrice_IsInterceptPlusSlopeTimesLastIndex()
    {
        var candles = CandleBuilder.LinearSequence(3, 100m, 1m);

        var result = _sut.Calculate(candles, Settings());

        result.CurrentTrendPrice.Should().Be(102.0m);
        result.CurrentClosePrice.Should().Be(102.0m);
        result.DistanceFromTrendLine.Should().Be(0.0m);
    }

    [Fact]
    public void DistanceFromLine_IsNegative_WhenCloseBelowTrend()
    {
        var candles = CandleBuilder.BreakoutDown(50, 3, 100m, 2m, 140m);

        var result = _sut.Calculate(candles, Settings());

        result.DistanceFromTrendLine.Should().BeNegative();
    }

    [Fact]
    public void ResidualStdDev_ZigzagPrices_ReturnsNonZero()
    {
        var candles = new[]
        {
            CandleBuilder.ValidCandle(price: 101m, openTime: DateTime.UtcNow.AddMinutes(-5)),
            CandleBuilder.ValidCandle(price: 99m,  openTime: DateTime.UtcNow.AddMinutes(-4)),
            CandleBuilder.ValidCandle(price: 101m, openTime: DateTime.UtcNow.AddMinutes(-3)),
            CandleBuilder.ValidCandle(price: 99m,  openTime: DateTime.UtcNow.AddMinutes(-2)),
            CandleBuilder.ValidCandle(price: 101m, openTime: DateTime.UtcNow.AddMinutes(-1))
        };

        var result = _sut.Calculate(candles, Settings());

        result.ResidualStandardDeviation.Should().BeApproximately(0.9798m, 0.0001m);
    }

    [Fact]
    public void SingleCandle_ReturnsSlope0_DirectionNeutral()
    {
        var candle = new[] { CandleBuilder.ValidCandle(price: 150m) };

        var result = _sut.Calculate(candle, Settings());

        result.Slope.Should().Be(0.0m);
        result.Intercept.Should().Be(150.0m);
        result.Direction.Should().Be(TrendDirection.Neutral);
    }
}
