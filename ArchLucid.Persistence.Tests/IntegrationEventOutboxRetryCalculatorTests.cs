namespace ArchLucid.Persistence.Tests;

public sealed class IntegrationEventOutboxRetryCalculatorTests
{
    [Fact]
    public void DelayUntilNextAttempt_first_failure_uses_two_seconds()
    {
        TimeSpan d = IntegrationEventOutboxRetryCalculator.DelayUntilNextAttempt(1, 300);

        d.Should().Be(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void DelayUntilNextAttempt_respects_cap()
    {
        TimeSpan d = IntegrationEventOutboxRetryCalculator.DelayUntilNextAttempt(20, 30);

        d.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void DelayUntilNextAttempt_rejects_non_positive(int n)
    {
        Action act = () => IntegrationEventOutboxRetryCalculator.DelayUntilNextAttempt(n, 10);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
