using MarketRadar.Domain.Results;
using MarketRadar.Domain.Services;
using MarketRadar.Domain.Settings;
using MarketRadar.Domain.Tests.Helpers;

namespace MarketRadar.Domain.Tests.Services;

public class TrendScoreServiceTests
{
    private readonly TrendScoreService _sut = new();

    private static TrendAnalysisSettings ScoreSettings() => new()
    {
        RegressionWindowCandles      = 120,
        RequiredBreakCandles         = 3,
        DeviationMultiplier          = 1.5m,
        MinimumSlope                 = 0.001m,
        MinimumBreakScore            = 70m,
        RecentHighLowWindowCandles   = 20,
        FalseBreakoutLookbackCandles = 5
    };

    private static (TrendLineResult Trend, IReadOnlyCollection<MovingAverageResult> Emas) Compute(
        IReadOnlyList<Entities.MarketCandle> candles, int[] periods)
    {
        var trend = new TrendLineService().Calculate(candles, ScoreSettings());
        var emas  = new MovingAverageService().CalculateEma(candles, periods);
        return (trend, emas);
    }

    [Fact]
    public void NeutralTrend_ReturnsZero()
    {
        var candles = CandleBuilder.Flat(50, 100m);
        var (trend, emas) = Compute(candles, [9, 21]);

        var score = _sut.CalculateScore(trend, emas, candles, ScoreSettings());

        score.Should().Be(0m);
    }

    [Fact]
    public void FullBreakdown_Returns90()
    {
        var candles = CandleBuilder.BreakoutDown(50, 3, 100m, 2m, 140m);
        var (trend, emas) = Compute(candles, [9, 21]);

        var score = _sut.CalculateScore(trend, emas, candles, ScoreSettings());

        score.Should().Be(90m);
    }

    [Fact]
    public void FullBreakup_Returns90()
    {
        var candles = CandleBuilder.BreakoutUp(50, 3, 200m, 2m, 160m);
        var (trend, emas) = Compute(candles, [9, 21]);

        var score = _sut.CalculateScore(trend, emas, candles, ScoreSettings());

        score.Should().Be(90m);
    }

    [Fact]
    public void NoEmaPeriods_Returns65()
    {
        var candles = CandleBuilder.BreakoutDown(50, 3, 100m, 2m, 140m);
        var (trend, emas) = Compute(candles, []);

        var score = _sut.CalculateScore(trend, emas, candles, ScoreSettings());

        score.Should().Be(65m);
    }

    [Fact]
    public void OnlyEma21Period_Returns80()
    {
        var candles = CandleBuilder.BreakoutDown(50, 3, 100m, 2m, 140m);
        var (trend, emas) = Compute(candles, [21]);

        var score = _sut.CalculateScore(trend, emas, candles, ScoreSettings());

        score.Should().Be(80m);
    }
}
