#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public class UnityDictionary<TKey,TValue> : IDictionary<TKey, TValue>
{
    [SerializeField]
    private List<TKey> _keys = new List<TKey>();
	
    [SerializeField]
    private List<TValue> _values = new List<TValue>();

    // _cache maps keys to list indices; this allows us to do stuff
    // like Remove() as O(1), instead of O(n)
	private Dictionary<TKey, int> _cache;

    public bool ContainsKey(TKey key)
    {
        BuildCacheIfNeeded();
        return _cache.ContainsKey(key);
    }

    public void Add(TKey key, TValue value)
    {
        BuildCacheIfNeeded();

        _cache.Add(key, _keys.Count);
        _keys.Add(key);
        _values.Add(value);
    }

    public bool Remove(TKey key)
    {
        BuildCacheIfNeeded();

        int index;
        if (!_cache.TryGetValue(key, out index))
            return false;

        RemoveAt(index);

        return true;
    }

    // Private method for removing the key/value pair at a particular index
    // This should never be public; dictionaries aren't supposed to have any
    // ordering on their elements, so the idea of an element at a particular
    // index isn't valid in the outside world. That we're using indexable
    // lists for storing keys/values is an implementation detail.
    private void RemoveAt(int index)
    {
        if(_keys.Count > 1)
        {
            // Copy the final key/value into this index and update the cache if it exists
            _keys[index] = _keys[_keys.Count - 1];
            _values[index] = _values[_values.Count - 1];
            if(_cache != null) _cache[_keys[index]] = index;
        }

        // Truncate the lists
        _keys.RemoveAt(_keys.Count - 1);
        _values.RemoveAt(_values.Count - 1);
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        BuildCacheIfNeeded();

        int index;
        if(!_cache.TryGetValue(key, out index))
        {
            value = default(TValue);
            return false;
        }

        value = _values[index];
        return true;
    }

    TValue IDictionary<TKey, TValue>.this[TKey key]
    {
        get { return this[key]; }
        set { this[key] = value; }
    }

    public TValue this[TKey key]
    {
        get
        {
            BuildCacheIfNeeded();
            return _values[_cache[key]];
        }
        set
        {
            BuildCacheIfNeeded();
            int index;
            if (!_cache.TryGetValue(key, out index))
            {
                // This key isn't presently in the dictionary, so add it
                Add(key, value);
            }
            else
            {
                // The key is already in the dictionary, just update it
                _values[index] = value;
            }
        }
    }

    public ICollection<TKey> Keys
    {
        get { return new ReadOnlyListWrapper<TKey>(_keys); }
    }

    public ICollection<TValue> Values
    {
        get { return new ReadOnlyListWrapper<TValue>(_values); }
    }

    void BuildCacheIfNeeded()
    {
        if(_cache == null) BuildCache();
    }

	void BuildCache()
	{
		_cache = new Dictionary<TKey,int>();
		for (int i=0; i!=_keys.Count; i++)
		{
			_cache.Add(_keys[i], i);
		}
	}

    #region Implementation of IEnumerable

    private class Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        public void Dispose() { }

        private int _index = -1;
        private readonly UnityDictionary<TKey, TValue> _dict;

        public Enumerator(UnityDictionary<TKey, TValue> dict)
        {
            _dict = dict;
            Reset();
        }

        #region Implementation of IEnumerator

        public bool MoveNext()
        {
            ++_index;
            return _index <= _dict.Count;
        }

        public void Reset()
        {
            _index = -1;
        }

        public KeyValuePair<TKey, TValue> Current
        {
            get { 
                if(_index < 0 || _index >= _dict.Count) throw new InvalidOperationException();
                return new KeyValuePair<TKey, TValue>(_dict._keys[_index], _dict._values[_index]);
            }
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        #endregion
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return new Enumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    #endregion

    #region Implementation of ICollection<KeyValuePair<TKey,TValue>>

    public void Add(KeyValuePair<TKey, TValue> item)
    {
        Add(item.Key, item.Value);
    }

    public void Clear()
    {
        _keys.Clear();
        _values.Clear();

        if (_cache == null)
            _cache = new Dictionary<TKey, int>();
        else
            _cache.Clear();
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        BuildCacheIfNeeded();
        int index;
        if (!_cache.TryGetValue(item.Key, out index))
            return false;

        return EqualityComparer<TValue>.Default.Equals(_values[index], item.Value);
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        if(array == null)
            throw new ArgumentNullException("array");
        if(arrayIndex < 0)
            throw new ArgumentOutOfRangeException("arrayIndex");
        if(array.Length - arrayIndex < Count)
            throw new ArgumentException("The provided array is too small.");

        for(int i = 0; i < _keys.Count; ++i, ++arrayIndex)
        {
            array[arrayIndex] = new KeyValuePair<TKey, TValue>(_keys[i], _values[i]);
        }
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        BuildCacheIfNeeded();
        int index;
        if (!_cache.TryGetValue(item.Key, out index))
            return false;

        if (!EqualityComparer<TValue>.Default.Equals(_values[index], item.Value))
            return false;

        RemoveAt(index);
        return true;
    }

    public int Count
    {
        get { return _keys.Count; }
    }

    public bool IsReadOnly
    {
        get { return false; }
    }

    #endregion
}

//Unfortunately, Unity currently doesn't serialize UnityDictionary<int,string>, but it will serialize a dummy subclass of that.
[Serializable]
public class UnityDictionaryIntString : UnityDictionary<int,string> {}
