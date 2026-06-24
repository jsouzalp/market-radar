using MarketRadar.Application.Settings;
using MarketRadar.Infrastructure.Services;

namespace MarketRadar.Application.Tests.Services;

public class AlertCooldownServiceTests
{
    private readonly AlertCooldownService _sut = new();
    private readonly AlertCooldownSettings _settings = new() { MaxConsecutiveAlerts = 3, MinCyclesToReset = 2 };

    [Fact]
    public void FirstBreakout_AllowsDispatch()
    {
        var result = _sut.Evaluate("XAUUSD", "TrendBreakDown", hasBreakout: true, _settings);

        result.CanDispatch.Should().BeTrue();
        result.ConsecutiveCount.Should().Be(1);
    }

    [Fact]
    public void AtMaxConsecutive_StillAllowsDispatch()
    {
        for (int i = 0; i < 3; i++)
            _sut.Evaluate("XAUUSD", "TrendBreakDown", hasBreakout: true, _settings);

        var result = _sut.GetCurrentState("XAUUSD", "TrendBreakDown", _settings);

        result.ConsecutiveCount.Should().Be(3);
        result.CanDispatch.Should().BeTrue();
        result.IsBlocked.Should().BeFalse();
    }

    [Fact]
    public void ExceedsMax_BlocksDispatch()
    {
        for (int i = 0; i <= 3; i++) // 4 breakouts with max=3
            _sut.Evaluate("XAUUSD", "TrendBreakDown", hasBreakout: true, _settings);

        var result = _sut.GetCurrentState("XAUUSD", "TrendBreakDown", _settings);

        result.CanDispatch.Should().BeFalse();
        result.IsBlocked.Should().BeTrue();
        result.ConsecutiveCount.Should().Be(4);
    }

    [Fact]
    public void QuietBelowThreshold_DoesNotReset()
    {
        for (int i = 0; i <= 3; i++)
            _sut.Evaluate("XAUUSD", "TrendBreakDown", hasBreakout: true, _settings);

        _sut.Evaluate("XAUUSD", "TrendBreakDown", hasBreakout: false, _settings); // quiet=1 < min=2

        var result = _sut.GetCurrentState("XAUUSD", "TrendBreakDown", _settings);

        result.IsBlocked.Should().BeTrue();
        result.ConsecutiveCount.Should().Be(4);
    }

    [Fact]
    public void QuietAtThreshold_Resets()
    {
        for (int i = 0; i <= 3; i++)
            _sut.Evaluate("XAUUSD", "TrendBreakDown", hasBreakout: true, _settings);

        for (int i = 0; i < 2; i++) // 2 quiet cycles = MinCyclesToReset
            _sut.Evaluate("XAUUSD", "TrendBreakDown", hasBreakout: false, _settings);

        var result = _sut.Evaluate("XAUUSD", "TrendBreakDown", hasBreakout: true, _settings);

        result.ConsecutiveCount.Should().Be(1);
        result.CanDispatch.Should().BeTrue();
    }

    [Fact]
    public void GetCurrentState_DoesNotModifyState()
    {
        _sut.Evaluate("XAUUSD", "TrendBreakDown", hasBreakout: true, _settings);

        var before = _sut.GetCurrentState("XAUUSD", "TrendBreakDown", _settings);
        var after  = _sut.GetCurrentState("XAUUSD", "TrendBreakDown", _settings);

        before.ConsecutiveCount.Should().Be(after.ConsecutiveCount);
    }
}
