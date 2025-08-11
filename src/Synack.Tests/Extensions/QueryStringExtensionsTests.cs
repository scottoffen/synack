using Synack.Exceptions;
using Synack.Extensions;

namespace Synack.Tests.Extensions;

public sealed class QueryStringExtensionsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("?")]
    public void NullOrEmpty_ReturnsEmptyMap(string? input)
    {
        var limits = new RequestParsingLimits();
        var map = input.MapToQueryString(limits);
        map.Count.ShouldBe(0);
    }

    [Fact]
    public void Parses_SinglePair()
    {
        var q = "a=1";
        var map = q.MapToQueryString(new RequestParsingLimits());
        map["a"].ShouldBe(new[] { "1" }, ignoreOrder: false);
    }

    [Theory]
    [InlineData("a", "a", "")]
    [InlineData("a=", "a", "")]
    [InlineData("=x", "", "x")]
    public void KeyWithoutValue_TreatedAsEmpty(string input, string expectedKey, string expectedValue)
    {
        var map = input.MapToQueryString(new RequestParsingLimits());
        map[expectedKey].ShouldBe(new[] { expectedValue }, ignoreOrder: false);
    }

    [Fact]
    public void LeadingQuestionMark_IsIgnored()
    {
        var q = "?a=1&b=2";
        var map = q.MapToQueryString(new RequestParsingLimits());
        map["a"].ShouldBe(new[] { "1" });
        map["b"].ShouldBe(new[] { "2" });
        map.Count.ShouldBe(2);
    }

    [Fact]
    public void Decodes_PlusAndPercent()
    {
        var q = "q=hello+world&check=%E2%9C%93";
        var map = q.MapToQueryString(new RequestParsingLimits());
        map["q"].ShouldBe(new[] { "hello world" });
        map["check"][0].ShouldBe("✓");
    }

    [Fact]
    public void Decodes_PlusInKey_AndValue()
    {
        var q = "a+b=c+d";
        var map = q.MapToQueryString(new RequestParsingLimits());
        map.ContainsKey("a b").ShouldBeTrue();
        map["a b"].ShouldBe(new[] { "c d" });
    }

    [Fact]
    public void Skips_EmptySegments()
    {
        var q = "a=1&&b=2&";
        var map = q.MapToQueryString(new RequestParsingLimits());
        map.Count.ShouldBe(2);
        map.ContainsKey("a").ShouldBeTrue();
        map.ContainsKey("b").ShouldBeTrue();
    }

    [Fact]
    public void Enforces_MaxQueryParameterCount_OnDistinctKeys()
    {
        var limits = new RequestParsingLimits { MaxQueryParameterCount = 2 };
        var q = "a=1&b=2&c=3";
        Should.Throw<RequestLimitExceededException>(() => q.MapToQueryString(limits));
    }

    [Fact]
    public void DistinctKeyCounting_IgnoresRepeatedKeys()
    {
        var limits = new RequestParsingLimits { MaxQueryParameterCount = 2 };
        var q = "a=1&a=2&b=3";
        var map = q.MapToQueryString(limits);
        map.Count.ShouldBe(2);
        map["a"].ShouldBe(new[] { "1", "2" }, ignoreOrder: false);
        map["b"].ShouldBe(new[] { "3" }, ignoreOrder: false);
    }

    [Fact]
    public void Enforces_MaxQueryValuesPerKey()
    {
        var limits = new RequestParsingLimits { MaxQueryValuesPerKey = 2 };
        var q = "id=1&id=2&id=3";
        Should.Throw<RequestLimitExceededException>(() => q.MapToQueryString(limits));
    }
}
