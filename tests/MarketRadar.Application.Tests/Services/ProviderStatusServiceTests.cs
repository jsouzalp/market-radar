using MarketRadar.Infrastructure.Services;

namespace MarketRadar.Application.Tests.Services;

public class ProviderStatusServiceTests
{
    private readonly ProviderStatusService _sut = new();

    [Fact]
    public void Default_IsNotClosed()
    {
        _sut.IsMarketClosed("XAUUSD").Should().BeFalse();
    }

    [Fact]
    public void SetMarketClosed_IsMarketClosed_ReturnsTrue()
    {
        _sut.SetMarketClosed("XAUUSD");

        _sut.IsMarketClosed("XAUUSD").Should().BeTrue();
    }

    [Fact]
    public void SetMarketOpen_AfterClosed_ReturnsFalse()
    {
        _sut.SetMarketClosed("XAUUSD");
        _sut.SetMarketOpen("XAUUSD");

        _sut.IsMarketClosed("XAUUSD").Should().BeFalse();
    }
}
