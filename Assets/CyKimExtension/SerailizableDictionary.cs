using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{
    [SerializeField]
    private List<KeyValuePair> keyValuePairs = new();

    [Serializable]
    private struct KeyValuePair
    {
        public TKey key;
        public TValue value;
    }

    public new void Add(TKey key, TValue value)
    {
        if (ContainsKey(key))
        {
            throw new ArgumentException($"Key '{key}' already exists in the dictionary.");
        }
        base.Add(key, value);
    }

    public void OnBeforeSerialize()
    {
        keyValuePairs.Clear();
        foreach (var pair in this)
        {
            keyValuePairs.Add(new KeyValuePair { key = pair.Key, value = pair.Value });
        }
    }

    public void OnAfterDeserialize()
    {
        Clear();
        foreach (var pair in keyValuePairs)
        {
            if (pair.key != null && !ContainsKey(pair.key))
            {
                this[pair.key] = pair.value;
            }
        }
    }
}