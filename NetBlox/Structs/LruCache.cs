namespace NetBlox.Structs;

// original author - Atul Mishra
// https://medium.com/@atulmishra.bhumca09/basic-lru-cache-implementation-in-c-17d4a5f0d4ba

public class LRUCache<TKey, TValue> where TKey : struct
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, LinkedListNode<LRUCacheItem<TKey, TValue>>> _cacheMap = new Dictionary<TKey, LinkedListNode<LRUCacheItem<TKey, TValue>>>();
    private readonly LinkedList<LRUCacheItem<TKey, TValue>> _lruList = new LinkedList<LRUCacheItem<TKey, TValue>>();

    public LRUCache(int capacity)
    {
        _capacity = capacity;
    }

    public TValue? Get(TKey key)
    {
        if (_cacheMap.TryGetValue(key, out var node))
        {
            TValue value = node.Value.Value;
            _lruList.Remove(node);
            _lruList.AddFirst(node);
            return value;
        }

        return default;
    }
    public void Set(TKey key, TValue value)
    {
        if (_cacheMap.TryGetValue(key, out var node))
        {
            _lruList.Remove(node);
        }
        else
        {
            if (_cacheMap.Count == _capacity)
            {
                var last = _lruList.Last;
                _lruList.RemoveLast();
                if (last != null)
                    _cacheMap.Remove(last.Value.Key);
            }

            node = new LinkedListNode<LRUCacheItem<TKey, TValue>>(new LRUCacheItem<TKey, TValue>(key, value));
            _cacheMap.Add(key, node);
        }

        _lruList.AddFirst(node);
    }
}

public class LRUCacheItem<TKey, TValue>
{
    public readonly TKey Key;
    public readonly TValue Value;

    public LRUCacheItem(TKey key, TValue value)
    {
        Key = key;
        Value = value;
    }
}