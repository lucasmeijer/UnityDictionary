using System.Collections;
using System.Runtime.Serialization;
using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public class UnityDictionary<TKey,TValue> : IDictionary<TKey, TValue>, IDictionary
{
    [SerializeField]
    private List<TKey> _keys = new List<TKey>();
	
    [SerializeField]
    private List<TValue> _values = new List<TValue>();

    private int _version = 0;

    public UnityDictionary()
    {
    }

    public UnityDictionary(int capacity)
    {
        _keys.Capacity = capacity;
        _values.Capacity = capacity;
    }

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

        // Add to the cache before adding to the key/value lists
        // That way, duplicate or null keys get caught before we
        // modify anything permanently.
        _cache.Add(key, _keys.Count);
        _keys.Add(key);
        _values.Add(value);
        ++_version;
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
        if (_cache != null) _cache.Remove(_keys[index]);

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

        ++_version;
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
                ++_version;
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

    ICollection IDictionary.Keys
    {
        get { return new ReadOnlyListWrapper<TKey>(_keys); }
    }

    ICollection IDictionary.Values
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

    private class Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDictionaryEnumerator
    {
        public void Dispose() { }

        private int _index = -1;
        private readonly UnityDictionary<TKey, TValue> _dict;
        private readonly int _initialVersion;

        public Enumerator(UnityDictionary<TKey, TValue> dict)
        {
            _dict = dict;
            _initialVersion = dict._version;
            Reset();
        }

        #region Implementation of IEnumerator

        public bool MoveNext()
        {
            if(_dict._version != _initialVersion)
                throw new InvalidOperationException("The dictionary was modified while enumerating.");
            ++_index;
            return _index < _dict.Count;
        }

        public void Reset()
        {
            _index = -1;
        }

        public KeyValuePair<TKey, TValue> Current
        {
            get {
                if (_dict._version != _initialVersion)
                    throw new InvalidOperationException("The dictionary was modified while enumerating.");

                if(_index < 0 || _index >= _dict.Count) throw new InvalidOperationException();
                return new KeyValuePair<TKey, TValue>(_dict._keys[_index], _dict._values[_index]);
            }
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        #endregion

        #region Implementation of IDictionaryEnumerator

        public object Key
        {
            get { return Current.Key; }
        }

        public object Value
        {
            get { return Current.Value; }
        }

        public DictionaryEntry Entry
        {
            get { return new DictionaryEntry(Current.Key, Current.Value); }
        }

        #endregion
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return new Enumerator(this);
    }

    public void Remove(object key)
    {
        if(key == null) throw new ArgumentNullException("key");

        BuildCacheIfNeeded();
        if (!((IDictionary)_cache).Contains(key))
            return;

        int index = (int) ((IDictionary) _cache)[key];
        RemoveAt(index);
    }

    object IDictionary.this[object key]
    {
        get {
            if (key == null) throw new ArgumentNullException("key");
            BuildCacheIfNeeded();
            if (!((IDictionary)_cache).Contains(key)) return null;
            int index = (int) ((IDictionary) _cache)[key];
            return _values[index];
        }
        set {
            if (key == null) throw new ArgumentNullException("key");
            BuildCacheIfNeeded();
            if (!((IDictionary)_cache).Contains(key))
            {
                Add(key, value);
            }
            else
            {
                TValue tV = ConvertObjectValHelper<TValue>(value);
                int index = (int)((IDictionary)_cache)[key];
                _values[index] = tV;
                ++_version;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    IDictionaryEnumerator IDictionary.GetEnumerator()
    {
        return new Enumerator(this);
    }

    #endregion

    #region Implementation of ICollection<KeyValuePair<TKey,TValue>>

    public void Add(KeyValuePair<TKey, TValue> item)
    {
        Add(item.Key, item.Value);
    }

    public bool Contains(object key)
    {
        if(key == null)
            throw new ArgumentNullException("key");

        BuildCacheIfNeeded();
        return ((IDictionary) _cache).Contains(key);
    }

    private static T ConvertObjectValHelper<T>(object obj)
    {
        T result;
        try
        {
            if (obj == null)
                result = default(T);
            else
                result = (T)obj;
        }
        catch (InvalidCastException)
        {
            throw new ArgumentException(string.Format("The value \"{0}\" is not of type \"{1}\" and cannot be used in this generic collection.", obj, typeof(T).FullName));
        }
        return result;
    }

    public void Add(object key, object value)
    {
        if(key == null) throw new ArgumentNullException("key");

        TKey tK = ConvertObjectValHelper<TKey>(key);
        TValue tV = ConvertObjectValHelper<TValue>(value);

        Add(tK, tV);
    }

    public void Clear()
    {
        _keys.Clear();
        _values.Clear();

        if (_cache == null)
            _cache = new Dictionary<TKey, int>();
        else
            _cache.Clear();

        ++_version;
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

    public void CopyTo(Array array, int index)
    {
        if (array == null)
            throw new ArgumentNullException("array");
        if (index < 0)
            throw new ArgumentOutOfRangeException("index");
        if (array.Length - index < Count)
            throw new ArgumentException("The provided array is too small.");
        if(!(array is KeyValuePair<TKey, TValue>[]) && !(array is DictionaryEntry[]) && !(array is object[]))
            throw new ArgumentException("The array is not of the appropriate type.");

        if(array is DictionaryEntry[])
        {
            for (int i = 0; i < _keys.Count; ++i, ++index)
            {
                array.SetValue(new DictionaryEntry(_keys[i], _values[i]), index);
            }  
        }
        else
        {
            for (int i = 0; i < _keys.Count; ++i, ++index)
            {
                array.SetValue(new KeyValuePair<TKey, TValue>(_keys[i], _values[i]), index);
            }
        }
    }

    public int Count
    {
        get { return _keys.Count; }
    }

    public object SyncRoot
    {
        get { return this; }
    }

    public bool IsSynchronized
    {
        get { return false; }
    }

    public bool IsReadOnly
    {
        get { return false; }
    }

    public bool IsFixedSize
    {
        get { return false; }
    }

    #endregion
}

//Unfortunately, Unity currently doesn't serialize UnityDictionary<int,string>, but it will serialize a dummy subclass of that.
[Serializable]
public class UnityDictionaryIntString : UnityDictionary<int,string> {}
