using FluentAssertions;

namespace Funds.Api.Tests.Services;

public class OrderServiceTests
{
    [Fact]
    public void Should_Run_First_Test()
    {
        var result = 1 + 1;

        result.Should().Be(2);
    }
}