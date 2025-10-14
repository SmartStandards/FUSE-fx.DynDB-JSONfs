using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data.Fuse;
using System.Data.ModelDescription;
using Logging.SmartStandards;

namespace System.Data.Fuse.JsonFileEngine.Internals {

  internal class L1Cache<TKey, TValue> {

    private readonly int _Capacity;
    private readonly int _TtlMs;
    private readonly LinkedList<TKey> _Lru;
    private readonly Dictionary<TKey, Tuple<TValue, long>> _Map;
    private readonly object _Sync;

    public L1Cache(int capacity, int ttlMs) {
      _Capacity = capacity;
      _TtlMs = ttlMs;
      _Lru = new LinkedList<TKey>();
      _Map = new Dictionary<TKey, Tuple<TValue, long>>();
      _Sync = new object();
    }
    public bool TryGet(TKey key, out TValue value) {
      lock (_Sync) {
        if (_Map.ContainsKey(key)) {
          Tuple<TValue, long> entry = _Map[key];
          if ((DateTime.UtcNow.Ticks - entry.Item2) / TimeSpan.TicksPerMillisecond <= _TtlMs) {
            _Lru.Remove(key);
            _Lru.AddFirst(key);
            value = entry.Item1;
            return true;
          }
          _Map.Remove(key);
          _Lru.Remove(key);
        }
      }
      value = default(TValue);
      return false;
    }
    public void Put(TKey key, TValue value) {
      lock (_Sync) {
        if (_Map.ContainsKey(key)) { _Lru.Remove(key); _Map.Remove(key); }
        _Map[key] = new Tuple<TValue, long>(value, DateTime.UtcNow.Ticks);
        _Lru.AddFirst(key);
        Trim();
      }
    }
    public void Remove(TKey key) {
      lock (_Sync) {
        if (_Map.ContainsKey(key)) { _Map.Remove(key); }
        _Lru.Remove(key);
      }
    }
    private void Trim() {
      while (_Lru.Count > _Capacity) {
        TKey last = _Lru.Last.Value;
        _Lru.RemoveLast();
        if (_Map.ContainsKey(last)) { _Map.Remove(last); }
      }
    }
  }

}

//namespace System.Data.Fuse {
//  /// <summary>
//  /// Lightweight entity reference with key and human-readable label.
//  /// </summary>
//  public class EntityRef {
//    public object Key { get; set; }
//    public string Label { get; set; }
//  }
//}

