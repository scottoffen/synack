namespace Synack.Collections;

/// <summary>
/// Small, allocation-aware builder for multi-value maps. Use a type-specific builder to get the right comparer.
/// </summary>
internal abstract class MultiMapBuilder
{
    protected readonly Dictionary<string, List<string>> Inner;

    protected MultiMapBuilder(StringComparer comparer, int? initialKeyCapacity = null)
    {
        if (comparer is null) throw new ArgumentNullException(nameof(comparer));
        Inner = initialKeyCapacity.HasValue
            ? new Dictionary<string, List<string>>(initialKeyCapacity.Value, comparer)
            : new Dictionary<string, List<string>>(comparer);
    }

    public void EnsureCapacity(int keyCapacity) => Inner.EnsureCapacity(keyCapacity);

    public void Add(string key, string value)
    {
        if (key is null) throw new ArgumentNullException(nameof(key));
        if (value is null) throw new ArgumentNullException(nameof(value));

        if (!Inner.TryGetValue(key, out var list))
        {
            list = new List<string>(2);
            Inner[key] = list;
        }
        list.Add(value);
    }

    public void AddRange(string key, IEnumerable<string> values)
    {
        if (key is null) throw new ArgumentNullException(nameof(key));
        if (values is null) throw new ArgumentNullException(nameof(values));

        if (!Inner.TryGetValue(key, out var list))
        {
            list = [];
            Inner[key] = list;
        }
        foreach (var v in values) list.Add(v);
    }

    public bool TryGetValues(string key, out List<string>? values) => Inner.TryGetValue(key, out values);

    public void Clear() => Inner.Clear();
}
