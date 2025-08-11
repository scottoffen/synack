using Synack.Collections;

namespace Synack.Tests.Collection;

file sealed class TestBuilder : MultiMapBuilder
{
    public TestBuilder(StringComparer comparer, int? initialKeyCapacity = null)
        : base(comparer, initialKeyCapacity) { }

    public Dictionary<string, List<string>> ExposeInner() => Inner;
}

public sealed class MultiMapBuilderTests
{
    [Fact]
    public void Ctor_Throws_WhenComparerIsNull()
    {
        Should.Throw<ArgumentNullException>(() => new TestBuilder(null!));
    }

    [Fact]
    public void Ctor_InitializesInner_WhenInitialKeyCapacityNotProvided()
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var builder = new TestBuilder(comparer);
        var inner = builder.ExposeInner();

        inner.ShouldNotBeNull();
        (inner.Comparer as StringComparer).ShouldBeSameAs(comparer);

        builder.Add("Key", "v1");
        inner.ContainsKey("key").ShouldBeTrue();
    }

    [Fact]
    public void Add_Throws_WhenKeyIsNull()
    {
        var builder = new TestBuilder(StringComparer.Ordinal);
        Should.Throw<ArgumentNullException>(() => builder.Add(null!, "value"));
    }

    [Fact]
    public void Add_Throws_WhenValueIsNull()
    {
        var builder = new TestBuilder(StringComparer.Ordinal);
        Should.Throw<ArgumentNullException>(() => builder.Add("key", null!));
    }

    [Fact]
    public void AddRange_AddsAllValues_ForNewKey()
    {
        var builder = new TestBuilder(StringComparer.Ordinal);
        builder.AddRange("k", new[] { "a", "b", "c" });

        var inner = builder.ExposeInner();
        inner.TryGetValue("k", out var list).ShouldBeTrue();
        list.ShouldBe(new[] { "a", "b", "c" }, ignoreOrder: false);
    }

    [Fact]
    public void AddRange_AppendsValues_ForExistingKey()
    {
        var builder = new TestBuilder(StringComparer.Ordinal);
        builder.Add("k", "x");
        builder.AddRange("k", new[] { "y", "z" });

        var inner = builder.ExposeInner();
        inner["k"].ShouldBe(new[] { "x", "y", "z" }, ignoreOrder: false);
    }

    [Fact]
    public void AddRange_Throws_WhenKeyIsNull()
    {
        var builder = new TestBuilder(StringComparer.Ordinal);
        Should.Throw<ArgumentNullException>(() => builder.AddRange(null!, new[] { "a" }));
    }

    [Fact]
    public void AddRange_Throws_WhenValuesIsNull()
    {
        var builder = new TestBuilder(StringComparer.Ordinal);
        Should.Throw<ArgumentNullException>(() => builder.AddRange("k", null!));
    }

    [Fact]
    public void EnsureCapacity_Preallocates_BuilderAcceptsSubsequentAdds()
    {
        var builder = new TestBuilder(StringComparer.Ordinal);
        builder.EnsureCapacity(32);

        for (int i = 0; i < 20; i++)
            builder.Add("k" + i, "v" + i);

        var inner = builder.ExposeInner();
        inner.Count.ShouldBe(20);
        for (int i = 0; i < 20; i++)
            inner["k" + i].ShouldBe(new[] { "v" + i }, ignoreOrder: false);
    }

    [Fact]
    public void TryGetValues_ReturnsTrueWithList_WhenPresent_AndFalseWhenMissing()
    {
        var builder = new TestBuilder(StringComparer.Ordinal);
        builder.Add("k", "a");
        builder.Add("k", "b");

        builder.TryGetValues("k", out var list).ShouldBeTrue();
        list.ShouldNotBeNull();
        list!.ShouldBe(new[] { "a", "b" }, ignoreOrder: false);

        builder.TryGetValues("missing", out var none).ShouldBeFalse();
        none.ShouldBeNull();
    }

    [Fact]
    public void Clear_EmptiesAllKeys_AndAllowsReuse()
    {
        var builder = new TestBuilder(StringComparer.Ordinal);
        builder.Add("a", "1");
        builder.Add("b", "2");

        builder.Clear();

        var inner = builder.ExposeInner();
        inner.Count.ShouldBe(0);

        builder.Add("c", "3");
        inner.Count.ShouldBe(1);
        inner["c"].ShouldBe(new[] { "3" }, ignoreOrder: false);
    }
}
