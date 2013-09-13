#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public class UnityDictionary<TKey,TValue>
{
	[SerializeField]
	private	List<TKey> _keys = new List<TKey>();
	
	[SerializeField]
	private	List<TValue> _values = new List<TValue>();

	private Dictionary<TKey,TValue> _cache;

	public void Add(TKey key, TValue value)
	{
		if (_cache == null)
			BuildCache();

		_cache.Add(key,value);
		_keys.Add(key);
		_values.Add(value);
	}

	public TValue this[TKey key]
	{
		get {
			if (_cache == null)
				BuildCache();
			
			return _cache[key];
		}
	}

	void BuildCache()
	{
		_cache = new Dictionary<TKey,TValue>();
		for (int i=0; i!=_keys.Count; i++)
		{
			_cache.Add(_keys[i],_values[i]);
		}
	}
}

//Unfortunately, Unity currently doesn't serialize UnityDictionary<int,string>, but it will serialize a dummy subclass of that.
[Serializable]
public class UnityDictionaryIntString : UnityDictionary<int,string> {}


/*
//TODO: implement the propertydrawer, and figure out how to make it so you dont need to have one per dummy subclass.
#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(UnityDictionaryIntString))]
public class UnityDictionaryDrawer : PropertyDrawer {
	
	override public void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		EditorGUI.BeginProperty (position, label, property);
		
		//todo: put nice drawing code here
		
		EditorGUI.EndProperty ();
	}
}
#endif
*/
