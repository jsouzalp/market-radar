using MarketRadar.Domain.Enums;
using MarketRadar.Domain.Services;
using MarketRadar.Domain.Settings;
using MarketRadar.Domain.Tests.Helpers;

namespace MarketRadar.Domain.Tests.Services;

public class MarketDataQualityServiceTests
{
    private readonly MarketDataQualityService _sut = new();

    private static MarketDataQualitySettings Settings(int min = 10, int staleMinutes = 30) => new()
    {
        MinimumRequiredCandles    = min,
        StaleDataToleranceMinutes = staleMinutes
    };

    [Fact]
    public void EmptyCollection_ReturnsWaitingForEnoughData()
    {
        var result = _sut.Evaluate([], Settings());

        result.Status.Should().Be(MarketDataQualityStatus.WaitingForEnoughData);
        result.CanAnalyze.Should().BeFalse();
    }

    [Fact]
    public void CountBelowMinimum_ReturnsWaitingForEnoughData()
    {
        var candles = CandleBuilder.LinearSequence(5, 100m, 1m);

        var result = _sut.Evaluate(candles, Settings(min: 10));

        result.Status.Should().Be(MarketDataQualityStatus.WaitingForEnoughData);
        result.CanAnalyze.Should().BeFalse();
    }

    [Fact]
    public void CountEqualsMinimum_FreshData_ReturnsValid()
    {
        var baseTime = DateTime.UtcNow.AddMinutes(-14);
        var candles  = CandleBuilder.LinearSequence(10, 100m, 1m, baseTime: baseTime);

        var result = _sut.Evaluate(candles, Settings(min: 10, staleMinutes: 30));

        result.Status.Should().Be(MarketDataQualityStatus.Valid);
        result.CanAnalyze.Should().BeTrue();
    }

    [Fact]
    public void CountExceedsMinimum_FreshData_ReturnsValid()
    {
        var baseTime = DateTime.UtcNow.AddMinutes(-24);
        var candles  = CandleBuilder.LinearSequence(15, 100m, 1m, baseTime: baseTime);

        var result = _sut.Evaluate(candles, Settings(min: 10, staleMinutes: 30));

        result.Status.Should().Be(MarketDataQualityStatus.Valid);
        result.CanAnalyze.Should().BeTrue();
    }

    [Fact]
    public void StaleData_ReturnsStaleData()
    {
        var baseTime = DateTime.UtcNow.AddMinutes(-45);
        var candles  = CandleBuilder.LinearSequence(10, 100m, 1m, baseTime: baseTime);

        var result = _sut.Evaluate(candles, Settings(staleMinutes: 30));

        result.Status.Should().Be(MarketDataQualityStatus.StaleData);
        result.CanAnalyze.Should().BeFalse();
    }

    [Fact]
    public void DataJustBelowThreshold_ReturnsValid()
    {
        var baseTime = DateTime.UtcNow.AddMinutes(-38);
        var candles  = CandleBuilder.LinearSequence(10, 100m, 1m, baseTime: baseTime);

        var result = _sut.Evaluate(candles, Settings(staleMinutes: 30));

        result.Status.Should().Be(MarketDataQualityStatus.Valid);
        result.CanAnalyze.Should().BeTrue();
    }

    [Fact]
    public void UsesMaxOpenTime_NotInsertionOrder()
    {
        var staleBase  = DateTime.UtcNow.AddMinutes(-50);
        var freshBase  = DateTime.UtcNow.AddMinutes(-5);
        var staleGroup = CandleBuilder.LinearSequence(9, 100m, 1m, baseTime: staleBase);
        var freshCandle = new Entities.MarketCandle
        {
            Symbol    = "XAUUSD",
            Timeframe = "M1",
            OpenTime  = freshBase,
            OpenPrice  = 200m,
            HighPrice  = 201m,
            LowPrice   = 199m,
            ClosePrice = 200m,
            Volume     = 1000m,
            CreatedAt  = DateTime.UtcNow
        };

        var candles = staleGroup.Append(freshCandle).ToList().AsReadOnly();

        var result = _sut.Evaluate(candles, Settings(min: 10, staleMinutes: 30));

        result.Status.Should().Be(MarketDataQualityStatus.Valid);
        result.CanAnalyze.Should().BeTrue();
    }
}
