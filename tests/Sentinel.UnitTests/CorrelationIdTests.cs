using Sentinel.Domain.Common;

namespace Sentinel.UnitTests.Common;

public class CorrelationIdTests
{
    [Theory]
    [InlineData("0123456789abcdef0123456789abcdef")]
    [InlineData("3f2504e0-4f89-11d3-9a0c-0305e82c3301")]
    [InlineData("req_42.retry:2")]
    [InlineData("A")]
    public void Normalize_WellFormedId_ReturnsItUnchanged(string id)
    {
        Assert.Equal(id, CorrelationId.Normalize(id));
    }

    [Fact]
    public void Normalize_IdAtTheLengthLimit_ReturnsItUnchanged()
    {
        var id = new string('a', CorrelationId.MaxLength);

        Assert.Equal(id, CorrelationId.Normalize(id));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc\r\n2026-01-01 ERROR forged entry")]
    [InlineData("abc\ndef")]
    [InlineData("abc\rdef")]
    [InlineData("tab\there")]
    [InlineData("has space")]
    [InlineData("semi;colon")]
    [InlineData("quote\"d")]
    public void Normalize_MalformedId_ReturnsAFreshId(string? id)
    {
        var normalized = CorrelationId.Normalize(id);

        Assert.NotEqual(id, normalized);
        Assert.Matches("^[0-9a-f]{32}$", normalized);
    }

    [Fact]
    public void Normalize_IdLongerThanTheLimit_ReturnsAFreshId()
    {
        var id = new string('a', CorrelationId.MaxLength + 1);

        var normalized = CorrelationId.Normalize(id);

        Assert.NotEqual(id, normalized);
        Assert.Matches("^[0-9a-f]{32}$", normalized);
    }
}
