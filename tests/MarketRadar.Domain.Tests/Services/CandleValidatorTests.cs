using MarketRadar.Domain.Entities;
using MarketRadar.Domain.Services;
using MarketRadar.Domain.Tests.Helpers;

namespace MarketRadar.Domain.Tests.Services;

public class CandleValidatorTests
{
    private readonly CandleValidator _sut = new();

    [Fact]
    public void Valid_ReturnsIsValid_True()
    {
        var result = _sut.Validate(CandleBuilder.ValidCandle());

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void EmptySymbol_ReturnsError()
    {
        var candle = CandleBuilder.ValidCandle();
        candle.Symbol = "";

        var result = _sut.Validate(candle);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Symbol cannot be empty.");
    }

    [Fact]
    public void WhitespaceSymbol_ReturnsError()
    {
        var candle = CandleBuilder.ValidCandle();
        candle.Symbol = " ";

        var result = _sut.Validate(candle);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Symbol cannot be empty.");
    }

    [Fact]
    public void EmptyTimeframe_ReturnsError()
    {
        var candle = CandleBuilder.ValidCandle();
        candle.Timeframe = "";

        var result = _sut.Validate(candle);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Timeframe cannot be empty.");
    }

    [Fact]
    public void OpenTimeNotUtc_ReturnsError()
    {
        var candle = CandleBuilder.ValidCandle();
        candle.OpenTime = DateTime.Now;

        var result = _sut.Validate(candle);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("OpenTime must be in UTC.");
    }

    [Fact]
    public void OpenTimeInFuture_ReturnsError()
    {
        var candle = CandleBuilder.ValidCandle();
        candle.OpenTime = DateTime.UtcNow.AddMinutes(1);

        var result = _sut.Validate(candle);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("OpenTime cannot be in the future.");
    }

    [Fact]
    public void OpenPriceZero_ReturnsError()
    {
        var candle = CandleBuilder.ValidCandle();
        candle.OpenPrice = 0m;

        var result = _sut.Validate(candle);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("OpenPrice must be greater than zero.");
    }

    [Fact]
    public void HighPriceZero_ReturnsError()
    {
        var candle = CandleBuilder.ValidCandle();
        candle.HighPrice = 0m;

        var result = _sut.Validate(candle);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("HighPrice must be greater than zero.");
    }

    [Fact]
    public void LowPriceZero_ReturnsError()
    {
        var candle = CandleBuilder.ValidCandle();
        candle.LowPrice = 0m;

        var result = _sut.Validate(candle);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("LowPrice must be greater than zero.");
    }

    [Fact]
    public void ClosePriceZero_ReturnsError()
    {
        var candle = CandleBuilder.ValidCandle();
        candle.ClosePrice = 0m;

        var result = _sut.Validate(candle);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("ClosePrice must be greater than zero.");
    }

    [Fact]
    public void HighLessThanLow_ReturnsError()
    {
        var candle = CandleBuilder.ValidCandle();
        candle.HighPrice = 99m;
        candle.LowPrice  = 100m;

        var result = _sut.Validate(candle);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("HighPrice must be greater than or equal to LowPrice.");
    }

    [Fact]
    public void OpenBelowLow_ReturnsError()
    {
        var candle = CandleBuilder.ValidCandle();
        candle.OpenPrice  = 98m;
        candle.HighPrice  = 101m;
        candle.LowPrice   = 99m;
        candle.ClosePrice = 100m;

        var result = _sut.Validate(candle);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("OpenPrice must be between LowPrice and HighPrice.");
    }

    [Fact]
    public void CloseAboveHigh_ReturnsError()
    {
        var candle = CandleBuilder.ValidCandle();
        candle.OpenPrice  = 100m;
        candle.HighPrice  = 101m;
        candle.LowPrice   = 99m;
        candle.ClosePrice = 102m;

        var result = _sut.Validate(candle);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("ClosePrice must be between LowPrice and HighPrice.");
    }

    [Fact]
    public void NegativeVolume_ReturnsError()
    {
        var candle = CandleBuilder.ValidCandle();
        candle.Volume = -1m;

        var result = _sut.Validate(candle);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Volume cannot be negative.");
    }

    [Fact]
    public void NullVolume_ReturnsValid()
    {
        var candle = CandleBuilder.ValidCandle(volume: null);

        var result = _sut.Validate(candle);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ZeroVolume_ReturnsValid()
    {
        var candle = CandleBuilder.ValidCandle(volume: 0m);

        var result = _sut.Validate(candle);

        result.IsValid.Should().BeTrue();
    }
}
