using MarketRadar.Domain.Enums;
using MarketRadar.Domain.Services;
using MarketRadar.Domain.Settings;
using MarketRadar.Domain.Tests.Helpers;

namespace MarketRadar.Domain.Tests.Services;

public class TrendBreakAnalysisServiceTests
{
    private readonly TrendBreakAnalysisService _sut = new(
        new TrendLineService(),
        new MovingAverageService(),
        new TrendScoreService());

    private static TrendAnalysisSettings DefaultSettings(
        decimal minimumBreakScore = 70m) => new()
    {
        RegressionWindowCandles      = 120,
        RequiredBreakCandles         = 3,
        DeviationMultiplier          = 1.5m,
        MinimumSlope                 = 0.001m,
        MinimumBreakScore            = minimumBreakScore,
        RecentHighLowWindowCandles   = 20,
        FalseBreakoutLookbackCandles = 5
    };

    private static MovingAverageSettings MaSettings(params int[] periods) => new()
    {
        Periods = periods
    };

    [Fact]
    public void NeutralTrend_ReturnsNeutralStatus_HasAlertFalse()
    {
        var candles = CandleBuilder.Flat(50);

        var result = _sut.Analyze(candles, DefaultSettings(), MaSettings(9, 21));

        result.Status.Should().Be(TrendAnalysisStatus.NeutralTrend);
        result.HasAlert.Should().BeFalse();
        result.Score.Should().Be(0m);
        result.AlertType.Should().BeNull();
    }

    [Fact]
    public void ValidBreakdown_ReturnsBreakoutDetected_Critical()
    {
        var candles = CandleBuilder.BreakoutDown(50, 3, 100m, 2m, 140m);

        var result = _sut.Analyze(candles, DefaultSettings(), MaSettings(9, 21));

        result.Status.Should().Be(TrendAnalysisStatus.BreakoutDetected);
        result.HasAlert.Should().BeTrue();
        result.AlertType.Should().Be(AlertType.TrendBreakDown);
        result.Severity.Should().Be(AlertSeverity.Critical);
        result.Score.Should().Be(90m);
    }

    [Fact]
    public void ValidBreakup_ReturnsBreakoutDetected_Critical()
    {
        var candles = CandleBuilder.BreakoutUp(50, 3, 200m, 2m, 160m);

        var result = _sut.Analyze(candles, DefaultSettings(), MaSettings(9, 21));

        result.Status.Should().Be(TrendAnalysisStatus.BreakoutDetected);
        result.HasAlert.Should().BeTrue();
        result.AlertType.Should().Be(AlertType.TrendBreakUp);
        result.Severity.Should().Be(AlertSeverity.Critical);
        result.Score.Should().Be(90m);
    }

    [Fact]
    public void KCandlesNotConfirmed_ReturnsNoBreakout()
    {
        // 1 break candle only → K=min(3,51)=3 checks last 3; indices 48,49 are on/above trend
        var candles = CandleBuilder.BreakoutDown(50, 1, 100m, 2m, 140m);

        var result = _sut.Analyze(candles, DefaultSettings(), MaSettings(9, 21));

        result.Status.Should().Be(TrendAnalysisStatus.NoBreakout);
        result.HasAlert.Should().BeFalse();
        result.Message.Should().Contain("consecutive");
    }

    [Fact]
    public void EMA21NotPresent_ReturnsNoBreakout()
    {
        var candles = CandleBuilder.BreakoutDown(50, 3, 100m, 2m, 140m);

        var result = _sut.Analyze(candles, DefaultSettings(), MaSettings());

        result.Status.Should().Be(TrendAnalysisStatus.NoBreakout);
        result.HasAlert.Should().BeFalse();
        result.Message.Should().Contain("EMA 21");
    }

    [Fact]
    public void ScoreBelowMinimum_ReturnsNoBreakout()
    {
        var candles = CandleBuilder.BreakoutDown(50, 3, 100m, 2m, 140m);

        var result = _sut.Analyze(candles, DefaultSettings(minimumBreakScore: 95m), MaSettings(9, 21));

        result.Status.Should().Be(TrendAnalysisStatus.NoBreakout);
        result.HasAlert.Should().BeFalse();
        result.Score.Should().Be(90m);
    }

    [Fact]
    public void SeverityWarning_WhenScore70to84()
    {
        // periods=[21] only → no EMA9 → no +10 → score=80m → Warning
        var candles = CandleBuilder.BreakoutDown(50, 3, 100m, 2m, 140m);

        var result = _sut.Analyze(candles, DefaultSettings(), MaSettings(21));

        result.Severity.Should().Be(AlertSeverity.Warning);
        result.Score.Should().Be(80m);
    }

    [Fact]
    public void SeverityCritical_WhenScoreAbove85()
    {
        var candles = CandleBuilder.BreakoutDown(50, 3, 100m, 2m, 140m);

        var result = _sut.Analyze(candles, DefaultSettings(), MaSettings(9, 21));

        result.Severity.Should().Be(AlertSeverity.Critical);
        result.Score.Should().Be(90m);
    }

    [Fact]
    public void SymbolAndTimeframe_TakenFromLastCandle()
    {
        var candles = CandleBuilder.BreakoutDown(50, 3, 100m, 2m, 140m,
            symbol: "BTCUSDT", timeframe: "H1");

        var result = _sut.Analyze(candles, DefaultSettings(), MaSettings(9, 21));

        result.Symbol.Should().Be("BTCUSDT");
        result.Timeframe.Should().Be("H1");
    }

    [Fact]
    public void TrendLine_PopulatedInResult()
    {
        var candles = CandleBuilder.BreakoutDown(50, 3, 100m, 2m, 140m);

        var result = _sut.Analyze(candles, DefaultSettings(), MaSettings(9, 21));

        result.TrendLine.Should().NotBeNull();
        result.TrendLine.Direction.Should().Be(TrendDirection.Up);
        result.TrendLine.Slope.Should().BePositive();
    }

    [Fact]
    public void MovingAverages_PopulatedInResult()
    {
        var candles = CandleBuilder.BreakoutDown(50, 3, 100m, 2m, 140m);

        var result = _sut.Analyze(candles, DefaultSettings(), MaSettings(9, 21));

        result.MovingAverages.Should().HaveCount(2);
    }
}
