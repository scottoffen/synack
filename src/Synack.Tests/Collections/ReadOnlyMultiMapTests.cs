using Synack.Collections;

namespace Synack.Tests.Collection;

file sealed class TestReadOnlyMultiMap : ReadOnlyMultiMap
{
    public TestReadOnlyMultiMap(Dictionary<string, List<string>> source, StringComparer comparer, bool sortKeys = false)
        : base(source, comparer, sortKeys) { }
}

file sealed class TestBuilder : MultiMapBuilder
{
    public TestBuilder(StringComparer comparer, int? initialKeyCapacity = null)
        : base(comparer, initialKeyCapacity) { }

    public TestReadOnlyMultiMap Build(bool sortKeys = false)
    {
        var comparer = Inner.Comparer as StringComparer ?? StringComparer.Ordinal;
        return new TestReadOnlyMultiMap(Inner, comparer, sortKeys);
    }
}

public sealed class ReadOnlyMultiMapTests
{
    [Fact]
    public void Indexer_ReturnsValues_WhenKeyExists()
    {
        var b = new TestBuilder(StringComparer.OrdinalIgnoreCase);
        b.Add("Content-Type", "application/json");
        b.Add("X-Id", "1");
        b.Add("X-Id", "2");

        var map = b.Build(sortKeys: true);

        map["content-type"].ShouldBe(new[] { "application/json" });
        map["X-ID"].ShouldBe(new[] { "1", "2" });
    }

    [Fact]
    public void Indexer_Throws_WhenKeyMissing()
    {
        var b = new TestBuilder(StringComparer.Ordinal);
        var map = b.Build();

        Should.Throw<KeyNotFoundException>(() => _ = map["missing"]);
    }

    [Fact]
    public void TryGetValue_Works_ForPresentAndMissingKeys()
    {
        var b = new TestBuilder(StringComparer.Ordinal);
        b.Add("k", "v");
        var map = b.Build();

        map.TryGetValue("k", out var v).ShouldBeTrue();
        v.ShouldBe(new[] { "v" });

        map.TryGetValue("nope", out var _).ShouldBeFalse();
    }

    [Fact]
    public void ContainsKey_RespectsComparer_CaseInsensitive()
    {
        var b = new TestBuilder(StringComparer.OrdinalIgnoreCase);
        b.Add("Server", "synack");
        var map = b.Build();

        map.ContainsKey("server").ShouldBeTrue();
        map.ContainsKey("SeRvEr").ShouldBeTrue();
        map.ContainsKey("not-server").ShouldBeFalse();
    }

    [Fact]
    public void ContainsKey_RespectsComparer_CaseSensitive()
    {
        var b = new TestBuilder(StringComparer.Ordinal);
        b.Add("id", "1");
        var map = b.Build();

        map.ContainsKey("id").ShouldBeTrue();
        map.ContainsKey("ID").ShouldBeFalse();
    }

    [Fact]
    public void Snapshot_IsIndependent_Of_Source_AfterConstruction()
    {
        var b = new TestBuilder(StringComparer.Ordinal);
        var list = new List<string> { "initial" };
        b.Add("K", "initial");
        var map = b.Build();

        list.Add("mutated");
        b.Add("K", "added-later");
        b.Add("New", "value");

        map["K"].ShouldBe(new[] { "initial" });
        map.ContainsKey("New").ShouldBeFalse();
    }

    [Fact]
    public void Enumeration_IsSorted_WhenRequested()
    {
        var b = new TestBuilder(StringComparer.OrdinalIgnoreCase);
        b.Add("z-last", "1");
        b.Add("A-First", "2");
        b.Add("m-Mid", "3");

        var map = b.Build(sortKeys: true);

        var keys = new List<string>();
        foreach (var kv in map) keys.Add(kv.Key);

        keys.ShouldBe(new[] { "A-First", "m-Mid", "z-last" }, ignoreOrder: false);
    }

    [Fact]
    public void Enumeration_PreservesInsertionOrder_WhenNotSorted()
    {
        var b = new TestBuilder(StringComparer.Ordinal);
        b.Add("k1", "1");
        b.Add("k2", "2");
        b.Add("k3", "3");

        var map = b.Build(sortKeys: false);

        var keys = new List<string>();
        foreach (var kv in map) keys.Add(kv.Key);

        keys.ShouldBe(new[] { "k1", "k2", "k3" }, ignoreOrder: false);
    }

    [Fact]
    public void Keys_Values_Count_AreConsistent()
    {
        var b = new TestBuilder(StringComparer.Ordinal);
        b.Add("a", "1");
        b.Add("b", "2");
        b.Add("b", "3");

        var map = b.Build();

        map.Count.ShouldBe(2);
        map.Keys.ShouldBe(new[] { "a", "b" }, ignoreOrder: true);
        foreach (var vals in map.Values)
            vals.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void EmptyOrNullList_ResultsInEmptyArrayInSnapshot()
    {
        // Case 1: null list value
        var srcNull = new Dictionary<string, List<string>>(StringComparer.Ordinal)
        {
            ["null-list"] = null!
        };
        var nullMap = new TestReadOnlyMultiMap(srcNull, StringComparer.Ordinal);
        nullMap["null-list"].ShouldBeSameAs(Array.Empty<string>());

        // Case 2: empty list value
        var srcEmpty = new Dictionary<string, List<string>>(StringComparer.Ordinal)
        {
            ["empty-list"] = new List<string>()
        };
        var emptyMap = new TestReadOnlyMultiMap(srcEmpty, StringComparer.Ordinal);
        emptyMap["empty-list"].ShouldBeSameAs(Array.Empty<string>());
    }

}
