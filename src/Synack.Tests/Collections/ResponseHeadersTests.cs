using System.Collections.ObjectModel;
using Synack.Collections;

namespace Synack.Tests.Collection;

public sealed class ResponseHeadersTests
{
    [Fact]
    public void Set_Adds_And_Replaces_Values()
    {
        var h = new ResponseHeaders();
        h.Set("X-Id", "1");
        h.ContainsKey("X-Id").ShouldBeTrue();

        h.TryGetValues("X-Id", out var values1).ShouldBeTrue();
        values1.ShouldBe(["1"], ignoreOrder: false);

        h.Set("X-Id", "2");
        h.TryGetValues("X-Id", out var values2).ShouldBeTrue();
        values2.ShouldBe(["2"], ignoreOrder: false);
    }

    [Fact]
    public void Append_Allows_Multiple_Values_For_NonSingleton()
    {
        var h = new ResponseHeaders();
        h.Append("Accept", "text/plain");
        h.Append("Accept", "application/json");

        h.TryGetValues("Accept", out var values).ShouldBeTrue();
        values.ShouldBe(["text/plain", "application/json"], ignoreOrder: false);
    }

    [Fact]
    public void Append_Throws_For_Singleton_Header()
    {
        var h = new ResponseHeaders();
        Should.Throw<InvalidOperationException>(() => h.Append("Content-Length", "100"));
    }

    [Theory]
    [InlineData("Set-Cookie")]
    [InlineData("set-cookie")]
    public void SetCookie_Is_Blocked_In_Set_And_Append(string name)
    {
        var h = new ResponseHeaders();
        var ex1 = Should.Throw<InvalidOperationException>(() => h.Set(name, "a=b"));
        ex1.Message.ShouldBe(ResponseHeaders.MessageCookieHeaderNotAllowed);

        var ex2 = Should.Throw<InvalidOperationException>(() => h.Append(name, "a=b"));
        ex2.Message.ShouldBe(ResponseHeaders.MessageCookieHeaderNotAllowed);
    }

    [Fact]
    public void Validation_Throws_For_Invalid_Header_Name()
    {
        var h = new ResponseHeaders();
        Should.Throw<ArgumentException>(() => h.Set("Bad Name", "v"));
        Should.Throw<ArgumentException>(() => h.Append("Bad\tName", "v"));
    }

    [Fact]
    public void Validation_Throws_For_CrLf_In_Value_And_Control_Chars()
    {
        var h = new ResponseHeaders();
        Should.Throw<ArgumentException>(() => h.Set("X", "bad\r\nvalue"));
        Should.Throw<ArgumentException>(() => h.Append("X", "\u0001control"));
    }

    [Fact]
    public void TryGetValues_PreSeal_Returns_Detached_Array_Copy()
    {
        var h = new ResponseHeaders();
        h.Append("X", "1");
        h.Append("X", "2");

        h.TryGetValues("X", out var first).ShouldBeTrue();
        first.ShouldBeOfType<string[]>();
        var arr = (string[])first;
        arr[0] = "mutated";

        h.TryGetValues("X", out var second).ShouldBeTrue();
        second.ShouldBe(["1", "2"], ignoreOrder: false);
    }

    [Fact]
    public void TryGetValues_PostSeal_Returns_ReadOnly_Wrapper()
    {
        var h = new ResponseHeaders();
        h.Set("X", "v");
        h.Seal();

        h.TryGetValues("X", out var values).ShouldBeTrue();
        values.ShouldBeOfType<ReadOnlyCollection<string>>();
        (values as string[]).ShouldBeNull();
        values.ShouldBe(["v"], ignoreOrder: false);
    }

    [Fact]
    public void Mutations_Throw_After_Seal()
    {
        var h = new ResponseHeaders();
        h.Set("X", "1");
        h.Seal();

        var m1 = Should.Throw<InvalidOperationException>(() => h.Set("X", "2"));
        m1.Message.ShouldBe(ResponseHeaders.MessageHeadersAreReadOnly);
        var m2 = Should.Throw<InvalidOperationException>(() => h.Append("Y", "z"));
        m2.Message.ShouldBe(ResponseHeaders.MessageHeadersAreReadOnly);
        var m3 = Should.Throw<InvalidOperationException>(() => h.Remove("X"));
        m3.Message.ShouldBe(ResponseHeaders.MessageHeadersAreReadOnly);
        var m4 = Should.Throw<InvalidOperationException>(() => h.Clear());
        m4.Message.ShouldBe(ResponseHeaders.MessageHeadersAreReadOnly);
    }

    [Fact]
    public void ContainsKey_Works_Pre_And_Post_Seal()
    {
        var h = new ResponseHeaders();
        h.Set("Server", "synack");
        h.ContainsKey("server").ShouldBeTrue();

        h.Seal();
        h.ContainsKey("SERVER").ShouldBeTrue();
    }

    [Fact]
    public void Enumeration_PreSeal_Produces_Arrays()
    {
        var h = new ResponseHeaders();
        h.Append("A", "1");
        h.Append("A", "2");
        h.Set("B", "x");

        var seen = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in h)
        {
            kv.Value.ShouldBeOfType<string[]>();
            seen[kv.Key] = kv.Value;
        }

        seen["A"].ShouldBe(["1", "2"], ignoreOrder: false);
        seen["B"].ShouldBe(["x"], ignoreOrder: false);
    }

    [Fact]
    public void Enumeration_PostSeal_Produces_ReadOnly_Collections()
    {
        var h = new ResponseHeaders();
        h.Append("A", "1");
        h.Set("B", "x");
        h.Seal();

        var seen = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in h)
        {
            kv.Value.ShouldBeOfType<ReadOnlyCollection<string>>();
            seen[kv.Key] = kv.Value;
        }

        seen["A"].ShouldBe(["1"], ignoreOrder: false);
        seen["B"].ShouldBe(["x"], ignoreOrder: false);
    }

    [Fact]
    public void Clear_Before_Seal_Empties_Collection()
    {
        var h = new ResponseHeaders();
        h.Append("A", "1");
        h.Set("B", "x");

        h.Clear();
        h.Count.ShouldBe(0);

        h.Seal();
        foreach (var _ in h)
            true.ShouldBeFalse(); // should not iterate any items
    }

    [Fact]
    public void CaseInsensitive_Keys()
    {
        var h = new ResponseHeaders();
        h.Set("X-Test", "1");
        h.Append("x-test", "2");

        h.TryGetValues("X-TEST", out var vals).ShouldBeTrue();
        vals.ShouldBe(["1", "2"], ignoreOrder: false);
    }
}
