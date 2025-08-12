using System.Globalization;
using Synack.Cookies;

namespace Synack.Tests.Collection;

public sealed class CookieTests
{
    [Fact]
    public void Ctor_Sets_Name_And_Value_And_Defaults()
    {
        var c = new Cookie("sid", "abc");
        c.Name.ShouldBe("sid");
        c.Value.ShouldBe("abc");
        c.Path.ShouldBe("/");
        c.Domain.ShouldBeNull();
        c.Expires.ShouldBeNull();
        c.MaxAge.ShouldBeNull();
        c.Secure.ShouldBeFalse();
        c.HttpOnly.ShouldBeFalse();
        c.SameSite.ShouldBeNull();
        c.Priority.ShouldBeNull();
        c.Partitioned.ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Name_Invalid_Throws(string? name)
    {
        Should.Throw<ArgumentException>(() => new Cookie(name!, "v"));
    }

    [Theory]
    [InlineData("bad name")]
    [InlineData("na=me")]
    [InlineData("na;me")]
    public void Name_With_Invalid_Token_Chars_Throws(string name)
    {
        var c = new Cookie("x", "y");
        Should.Throw<ArgumentException>(() => c.Name = name);
    }

    [Fact]
    public void Value_Null_Throws()
    {
        var c = new Cookie("x", "y");
        Should.Throw<ArgumentNullException>(() => c.Value = null!);
    }

    [Theory]
    [InlineData("has;semi")]
    [InlineData("line1\r\nline2")]
    public void Value_With_Semicolon_Or_CrLf_Throws(string value)
    {
        var c = new Cookie("x", "y");
        Should.Throw<ArgumentException>(() => c.Value = value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Example.COM")]
    [InlineData(".sub.Example.com")]
    public void Domain_Allows_Null_And_Normalizes(string? domain)
    {
        var c = new Cookie("x", "y");
        c.Domain = domain;
        if (domain is null)
            c.Domain.ShouldBeNull();
        else
            c.Domain.ShouldBe(domain.Trim().TrimStart('.').ToLowerInvariant());
    }

    [Fact]
    public void Domain_Idn_To_Ascii()
    {
        var input = "münich.de";
        var expected = new IdnMapping().GetAscii(input.Trim().TrimStart('.').ToLowerInvariant());
        var c = new Cookie("x", "y") { Domain = input };
        c.Domain.ShouldBe(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("not/starting/with/slash")]
    public void Path_Invalid_Throws(string path)
    {
        var c = new Cookie("x", "y");
        Should.Throw<ArgumentException>(() => c.Path = path);
    }

    [Theory]
    [InlineData("/ok")]
    [InlineData("/")]
    [InlineData("/with/sub")]
    public void Path_Valid_Succeeds(string path)
    {
        var c = new Cookie("x", "y") { Path = path };
        c.Path.ShouldBe(path);
    }

    [Fact]
    public void Expires_Must_Be_Utc()
    {
        var c = new Cookie("x", "y");
        Should.Throw<ArgumentException>(() => c.Expires = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Local));
        c.Expires = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        c.Expires?.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public void ToString_Contains_Basics_And_Attributes()
    {
        var expires = new DateTime(2030, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var c = new Cookie("id", "42")
        {
            Domain = "EXAMPLE.com",
            Path = "/p",
            MaxAge = 3600,
            Expires = expires,
            Secure = true,
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Priority = CookiePriority.High,
            Partitioned = true
        };

        var s = c.ToString();

        s.ShouldStartWith("id=42");
        s.ShouldContain("Domain=example.com");
        s.ShouldContain("Path=/p");
        s.ShouldContain("Max-Age=3600");
        s.ShouldContain("Expires=" + expires.ToString("R", CultureInfo.InvariantCulture));
        s.ShouldContain("Secure");
        s.ShouldContain("HttpOnly");
        s.ShouldContain("SameSite=Lax");
        s.ShouldContain("Priority=High");
        s.ShouldContain("Partitioned");
    }

    [Fact]
    public void ToString_Throws_When_SameSiteNone_And_Not_Secure()
    {
        var c = new Cookie("n", "v") { SameSite = SameSiteMode.None, Secure = false };
        Should.Throw<InvalidOperationException>(() => _ = c.ToString());
    }

    [Fact]
    public void ToString_Allows_SameSiteNone_When_Secure()
    {
        var c = new Cookie("n", "v") { SameSite = SameSiteMode.None, Secure = true };
        var s = c.ToString();
        s.ShouldContain("SameSite=None");
        s.ShouldContain("Secure");
    }

    [Fact]
    public void Delete_Factory_Sets_Expected_Attributes()
    {
        var d = Cookie.Delete("gone", "/x", "example.com");
        d.Name.ShouldBe("gone");
        d.Value.ShouldBe(string.Empty);
        d.Path.ShouldBe("/x");
        d.Domain.ShouldBe("example.com");
        d.MaxAge.ShouldBe(0);
        d.Expires.ShouldNotBeNull();
        d.Expires!.Value.Kind.ShouldBe(DateTimeKind.Utc);

        var s = d.ToString();
        s.ShouldStartWith("gone=");
        s.ShouldContain("Max-Age=0");
        s.ShouldContain("Expires=");
        s.ShouldContain("Path=/x");
        s.ShouldContain("Domain=example.com");
    }

    [Fact]
    public void Delete_Factory_Default_Path_And_No_Domain()
    {
        var d = Cookie.Delete("gone");
        d.Path.ShouldBe("/");
        d.Domain.ShouldBeNull();
    }

    [Fact]
    public void Enum_Tokens_Appear_As_Expected()
    {
        var c1 = new Cookie("a", "b") { Priority = CookiePriority.Low };
        c1.ToString().ShouldContain("Priority=Low");

        var c2 = new Cookie("a", "b") { SameSite = SameSiteMode.Strict };
        c2.ToString().ShouldContain("SameSite=Strict");
    }

    [Fact]
    public void ExpireIn_PositiveTtl_Sets_MaxAge_And_Expires_Utc()
    {
        var ttl = TimeSpan.FromMinutes(5);
        var c = new Cookie("k", "v");

        var before = DateTime.UtcNow;
        c.ExpireIn(ttl);
        var after = DateTime.UtcNow;

        c.MaxAge.ShouldNotBeNull();
        // Allow 1s wiggle because of wall-clock between reads
        c.MaxAge!.Value.ShouldBeInRange((int)ttl.TotalSeconds - 1, (int)ttl.TotalSeconds);

        c.Expires.ShouldNotBeNull();
        c.Expires!.Value.Kind.ShouldBe(DateTimeKind.Utc);
        c.Expires!.Value.ShouldBeGreaterThanOrEqualTo(before + ttl - TimeSpan.FromSeconds(1));
        c.Expires!.Value.ShouldBeLessThanOrEqualTo(after + ttl + TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ExpireIn_Zero_Sets_Immediate_Expiry()
    {
        var c = new Cookie("k", "v");
        var before = DateTime.UtcNow;

        c.ExpireIn(TimeSpan.Zero);

        c.MaxAge.ShouldBe(0);
        c.Expires.ShouldNotBeNull();
        c.Expires!.Value.Kind.ShouldBe(DateTimeKind.Utc);
        c.Expires!.Value.ShouldBeGreaterThanOrEqualTo(before - TimeSpan.FromSeconds(1));
        c.Expires!.Value.ShouldBeLessThanOrEqualTo(DateTime.UtcNow + TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ExpireIn_Negative_Sets_Immediate_Expiry()
    {
        var c = new Cookie("k", "v");
        var before = DateTime.UtcNow;

        c.ExpireIn(TimeSpan.FromSeconds(-10));

        c.MaxAge.ShouldBe(0);
        c.Expires.ShouldNotBeNull();
        c.Expires!.Value.Kind.ShouldBe(DateTimeKind.Utc);
        c.Expires!.Value.ShouldBeGreaterThanOrEqualTo(before - TimeSpan.FromSeconds(1));
        c.Expires!.Value.ShouldBeLessThanOrEqualTo(DateTime.UtcNow + TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ExpireIn_DoesNot_Change_Other_Attributes()
    {
        var c = new Cookie("k", "v")
        {
            Domain = "example.com",
            Path = "/x",
            Secure = true,
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Priority = CookiePriority.High,
            Partitioned = true
        };

        c.ExpireIn(TimeSpan.FromSeconds(10));

        c.Domain.ShouldBe("example.com");
        c.Path.ShouldBe("/x");
        c.Secure.ShouldBeTrue();
        c.HttpOnly.ShouldBeTrue();
        c.SameSite.ShouldBe(SameSiteMode.Lax);
        c.Priority.ShouldBe(CookiePriority.High);
        c.Partitioned.ShouldBeTrue();
    }
}
