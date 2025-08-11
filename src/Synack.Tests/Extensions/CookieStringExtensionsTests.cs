using Synack.Exceptions;
using Synack.Extensions;

namespace Synack.Tests.Extensions;

public sealed class CookieStringExtensionsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void NullOrEmpty_ReturnsEmptyMap(string? input)
    {
        var limits = new RequestParsingLimits();
        var map = input.ToReadOnlyCookies(limits);
        map.Count.ShouldBe(0);
    }

    [Fact]
    public void Parses_SingleCookie()
    {
        var header = "sid=abc123";
        var map = header.ToReadOnlyCookies(new RequestParsingLimits());
        map["sid"].ShouldBe(["abc123"], ignoreOrder: false);
    }

    [Fact]
    public void Trims_OWS_Around_Name_And_Value()
    {
        var header = " \t name \t = \t value \t ";
        var map = header.ToReadOnlyCookies(new RequestParsingLimits());
        map.ContainsKey("name").ShouldBeTrue();
        map["name"].ShouldBe(["value"]);
    }

    [Fact]
    public void Handles_QuotedValue_Strips_Only_OuterPair()
    {
        var header = "q=\"hello world\"";
        var map = header.ToReadOnlyCookies(new RequestParsingLimits());
        map["q"].ShouldBe(["hello world"]);

        header = "q=\"leading only";
        map = header.ToReadOnlyCookies(new RequestParsingLimits());
        map["q"].ShouldBe(["\"leading only"]);

        header = "q=trailing only\"";
        map = header.ToReadOnlyCookies(new RequestParsingLimits());
        map["q"].ShouldBe(["trailing only\""]);
    }

    [Fact]
    public void MultipleCookies_WithSemicolons_And_Whitespace()
    {
        var header = "a=1;  \tb=2;\t c=3 ";
        var map = header.ToReadOnlyCookies(new RequestParsingLimits());
        map.Count.ShouldBe(3);
        map["a"].ShouldBe(["1"]);
        map["b"].ShouldBe(["2"]);
        map["c"].ShouldBe(["3"]);
    }

    [Fact]
    public void DuplicateNames_LastWins()
    {
        var header = "id=1; id=2; id=3";
        var map = header.ToReadOnlyCookies(new RequestParsingLimits());
        map.Count.ShouldBe(1);
        map["id"].ShouldBe(["3"]);
    }

    [Fact]
    public void DoesNotDecode_Plus_Or_Percent()
    {
        var header = "x=a+b%2Fz";
        var map = header.ToReadOnlyCookies(new RequestParsingLimits());
        map["x"].ShouldBe(["a+b%2Fz"]);
    }

    [Fact]
    public void EmptyValue_WhenMissingOrEmptyAfterEquals()
    {
        var map = "a".ToReadOnlyCookies(new RequestParsingLimits());
        map["a"].ShouldBe([string.Empty]);

        map = "b=".ToReadOnlyCookies(new RequestParsingLimits());
        map["b"].ShouldBe([string.Empty]);
    }

    [Fact]
    public void Ignores_EmptySegments_And_SegmentsWithNoName()
    {
        var header = ";; =x ; ; valid=ok ;";
        var map = header.ToReadOnlyCookies(new RequestParsingLimits());
        map.Count.ShouldBe(1);
        map.ContainsKey("valid").ShouldBeTrue();
        map["valid"].ShouldBe(["ok"]);
    }

    [Fact]
    public void Enforces_MaxCookieCount_OnDistinctNames()
    {
        var limits = new RequestParsingLimits { MaxCookieCount = 2 };
        var header = "a=1; b=2; c=3";
        Should.Throw<RequestLimitExceededException>(() => header.ToReadOnlyCookies(limits));
    }

    [Fact]
    public void Enforces_MaxCookieBytesPerName_Ascii()
    {
        var limits = new RequestParsingLimits { MaxCookieBytesPerName = 1 }; // name(1) + value(1) = 2 -> exceeds
        var header = "a=1";
        Should.Throw<RequestLimitExceededException>(() => header.ToReadOnlyCookies(limits));
    }

    [Fact]
    public void Enforces_MaxCookieBytesPerName_NonAscii_UTF8()
    {
        var limits = new RequestParsingLimits { MaxCookieBytesPerName = 3 }; // name(1) + value(3 for ✓) = 4 -> exceeds
        var header = "k=✓";
        Should.Throw<RequestLimitExceededException>(() => header.ToReadOnlyCookies(limits));
    }

    [Fact]
    public void Enforces_MaxCookiesBytesTotal_SimpleSum()
    {
        var limits = new RequestParsingLimits { MaxCookiesBytesTotal = 3 }; // a=1 (2 bytes) + b=2 (2 bytes) -> 4 > 3
        var header = "a=1; b=2";
        Should.Throw<RequestLimitExceededException>(() => header.ToReadOnlyCookies(limits));
    }

    [Fact]
    public void TotalBytes_Adjusts_On_LastWins_Replacement()
    {
        var header = "a=1; a=22";
        var allow = new RequestParsingLimits { MaxCookiesBytesTotal = 3 }; // last wins: name(1)+value(2)=3 -> OK
        var map = header.ToReadOnlyCookies(allow);
        map["a"].ShouldBe(["22"]);

        var deny = new RequestParsingLimits { MaxCookiesBytesTotal = 2 }; // 3 > 2 after last-wins -> throw
        Should.Throw<RequestLimitExceededException>(() => header.ToReadOnlyCookies(deny));
    }

    [Fact]
    public void CaseSensitive_Names_TreatedAsDistinct()
    {
        var header = "SessionId=abc; sessionid=def";
        var map = header.ToReadOnlyCookies(new RequestParsingLimits());
        map.Count.ShouldBe(2);
        map["SessionId"].ShouldBe(["abc"]);
        map["sessionid"].ShouldBe(["def"]);
    }
}
