using System.Runtime.CompilerServices;
#if NET6_0_OR_GREATER
using System.Runtime.InteropServices; // CollectionsMarshal
#endif
#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#endif

namespace Synack.Collections;

/// <summary>
/// Base class for immutable, allocation-minimal multi-value collections.
/// Keys map to IReadOnlyList&lt;string&gt; values backed by arrays.
/// </summary>
public abstract class ReadOnlyMultiMap : IReadOnlyDictionary<string, IReadOnlyList<string>>
{
#if NET8_0_OR_GREATER
    private readonly FrozenDictionary<string, IReadOnlyList<string>> _map;
#else
    private readonly Dictionary<string, IReadOnlyList<string>> _map;
#endif
    private readonly string[] _keys;
    private readonly IReadOnlyList<string>[] _values;
    private readonly KeyValuePair<string, IReadOnlyList<string>>[] _entries;

    public int Count => _keys.Length;
    public IEnumerable<string> Keys => _keys;
    public IEnumerable<IReadOnlyList<string>> Values => _values;
    public IReadOnlyList<string> this[string key] => _map[key];

    protected ReadOnlyMultiMap(Dictionary<string, List<string>> source, StringComparer comparer, bool sortKeys = false)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(comparer);

        var count = source.Count;
        if (count == 0)
        {
#if NET8_0_OR_GREATER
            _map = FrozenDictionary.ToFrozenDictionary(
                Array.Empty<KeyValuePair<string, IReadOnlyList<string>>>(),
                kv => kv.Key, kv => kv.Value, comparer);
#else
            _map = new Dictionary<string, IReadOnlyList<string>>(0, comparer);
#endif
            _keys = Array.Empty<string>();
            _values = Array.Empty<IReadOnlyList<string>>();
            _entries = Array.Empty<KeyValuePair<string, IReadOnlyList<string>>>();
            return;
        }

        var keys = new string[count];
        if (sortKeys)
        {
            var i = 0;
            foreach (var k in source.Keys) keys[i++] = k;
            Array.Sort(keys, comparer);
        }
        else
        {
            source.Keys.CopyTo(keys, 0);
        }

        var values = new IReadOnlyList<string>[count];
        var entries = new KeyValuePair<string, IReadOnlyList<string>>[count];
        var map = new Dictionary<string, IReadOnlyList<string>>(count, comparer);

        for (var i = 0; i < keys.Length; i++)
        {
            var key = keys[i];
            var list = source[key];

            string[] arr;
            if (list is null || list.Count == 0)
            {
                arr = [];
            }
            else
            {
                arr = new string[list.Count];
#if NET6_0_OR_GREATER
                CollectionsMarshal.AsSpan(list).CopyTo(arr);
#else
                list.CopyTo(arr, 0);
#endif
            }

            values[i] = arr;
            entries[i] = new KeyValuePair<string, IReadOnlyList<string>>(key, arr);
            map[key] = arr;
        }

#if NET8_0_OR_GREATER
        _map = map.ToFrozenDictionary(comparer);
#else
        _map = map;
#endif
        _keys = keys;
        _values = values;
        _entries = entries;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(string key) => _map.ContainsKey(key);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(string key, out IReadOnlyList<string> value) => _map.TryGetValue(key, out value!);

    public IEnumerator<KeyValuePair<string, IReadOnlyList<string>>> GetEnumerator()
    {
        for (var i = 0; i < _entries.Length; i++) yield return _entries[i];
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
