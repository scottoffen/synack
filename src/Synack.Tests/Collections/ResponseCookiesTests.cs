using Synack.Collections;
using Synack.Cookies;

namespace Synack.Tests.Collection;

public sealed class ResponseCookiesTests
{
    [Fact]
    public void Add_Cookie_Instance()
    {
        var rc = new ResponseCookies();
        var c = new Cookie("sid", "abc") { HttpOnly = true };
        rc.Add(c);

        rc.Count.ShouldBe(1);
        foreach (var x in rc) x.ShouldBeSameAs(c);
    }

    [Fact]
    public void Add_ByName_Returns_Configurable_Instance()
    {
        var rc = new ResponseCookies();
        var c = rc.Add("id", "42");
        c.Path = "/x";
        c.Secure = true;

        rc.Count.ShouldBe(1);
        foreach (var x in rc) x.ShouldBeSameAs(c);
    }

    [Fact]
    public void Add_Null_Throws()
    {
        var rc = new ResponseCookies();
        Should.Throw<ArgumentNullException>(() => rc.Add(cookie: null!));
    }

    [Fact]
    public void Delete_Adds_Deletion_Cookie()
    {
        var rc = new ResponseCookies();
        rc.Delete("gone", "/p", "example.com");

        rc.Count.ShouldBe(1);
        var c = Assert.Single(rc);
        c.Name.ShouldBe("gone");
        c.Value.ShouldBe(string.Empty);
        c.Path.ShouldBe("/p");
        c.Domain.ShouldBe("example.com");
        c.MaxAge.ShouldBe(0);
        c.Expires.ShouldNotBeNull();
        c.Expires!.Value.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public void Remove_Works()
    {
        var rc = new ResponseCookies();
        var c = new Cookie("a", "b");
        rc.Add(c);

        rc.Remove(c).ShouldBeTrue();
        rc.Count.ShouldBe(0);
        rc.Remove(c).ShouldBeFalse();
    }

    [Fact]
    public void Clear_Removes_All_Before_Seal()
    {
        var rc = new ResponseCookies();
        rc.Add("a", "1");
        rc.Add("b", "2");
        rc.Clear();

        rc.Count.ShouldBe(0);
        foreach (var _ in rc) false.ShouldBeTrue();
    }

    [Fact]
    public void Seal_Snapshots_To_SetCookie_Lines()
    {
        var rc = new ResponseCookies();
        var c = rc.Add("id", "42");
        c.HttpOnly = true;
        c.Secure = true;

        rc.Seal();
        var lines = rc.GetSetCookieHeaderValues();

        lines.Count.ShouldBe(1);
        lines[0].ShouldStartWith("id=42");
        lines[0].ShouldContain("HttpOnly");
        lines[0].ShouldContain("Secure");
    }

    [Fact]
    public void GetSetCookieHeaderValues_Throws_Before_Seal()
    {
        var rc = new ResponseCookies();
        rc.Add("x", "y");
        var ex = Should.Throw<InvalidOperationException>(() => rc.GetSetCookieHeaderValues());
        ex.Message.ShouldContain("not started");
    }

    [Fact]
    public void Mutations_Throw_After_Seal()
    {
        var rc = new ResponseCookies();
        rc.Add("x", "y");
        rc.Seal();

        var ex1 = Should.Throw<InvalidOperationException>(() => rc.Add("z", "1"));
        ex1.Message.ShouldBe(ResponseCookies.MessageCookiesAreReadOnly);
        var ex2 = Should.Throw<InvalidOperationException>(() => rc.Add(new Cookie("a", "b")));
        ex2.Message.ShouldBe(ResponseCookies.MessageCookiesAreReadOnly);
        var ex3 = Should.Throw<InvalidOperationException>(() => rc.Delete("gone"));
        ex3.Message.ShouldBe(ResponseCookies.MessageCookiesAreReadOnly);
        var ex4 = Should.Throw<InvalidOperationException>(() => rc.Clear());
        ex4.Message.ShouldBe(ResponseCookies.MessageCookiesAreReadOnly);
        var ex5 = Should.Throw<InvalidOperationException>(() => rc.Remove(new Cookie("x", "y")));
        ex5.Message.ShouldBe(ResponseCookies.MessageCookiesAreReadOnly);
    }

    [Fact]
    public void Changes_To_Cookie_After_Seal_Do_Not_Affect_Snapshot()
    {
        var rc = new ResponseCookies();
        var c = rc.Add("id", "42");
        rc.Seal();

        c.Value = "mutated";
        c.HttpOnly = true;

        var lines = rc.GetSetCookieHeaderValues();
        lines[0].ShouldStartWith("id=42");
        lines[0].ShouldNotContain("HttpOnly");
    }

    [Fact]
    public void Seal_With_Invalid_Cookie_Bubbles_Exception()
    {
        var rc = new ResponseCookies();
        var c = rc.Add("cross", "site");
        c.SameSite = SameSiteMode.None;
        c.Secure = false;

        Should.Throw<InvalidOperationException>(() => rc.Seal());
    }

    [Fact]
    public void Seal_With_No_Cookies_Produces_Empty_List()
    {
        var rc = new ResponseCookies();
        rc.Seal();

        var lines = rc.GetSetCookieHeaderValues();
        lines.ShouldBeEmpty();
    }

    [Fact]
    public void Enumeration_Returns_Original_Cookie_Objects()
    {
        var rc = new ResponseCookies();
        var c1 = rc.Add("a", "1");
        var c2 = rc.Add("b", "2");

        var seen = new List<Cookie>();
        foreach (var c in rc) seen.Add(c);

        seen.ShouldContain(c1);
        seen.ShouldContain(c2);

        rc.Seal();

        seen.Clear();
        foreach (var c in rc) seen.Add(c);

        seen.ShouldContain(c1);
        seen.ShouldContain(c2);
    }
}
