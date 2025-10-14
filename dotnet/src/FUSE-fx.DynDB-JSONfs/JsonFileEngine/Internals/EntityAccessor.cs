using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.ModelDescription;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace System.Data.Fuse.JsonFileEngine.Internals {

  internal class EntityAccessor {

    private readonly EntitySchema _Schema;
    private readonly IndexSchema _Pk;

    public EntityAccessor(EntitySchema schema) {
      _Schema = schema;
      _Pk = schema.Indices.FirstOrDefault((IndexSchema i) => i.Name == schema.PrimaryKeyIndexName);
    }

    public TKey ReadKey<TKey>(object entity) {

      if (_Pk == null) {
        return default(TKey);
      }

      string[] names = _Pk.MemberFieldNames;

      if (names == null || names.Length == 0) {
        return default(TKey); 
      }

      if (names.Length == 1) {
        object value = ReadProperty(entity, names[0]);
        return ConvertTo<TKey>(value);
      }

      // composite key: build dictionary or tuple-equivalent bag
      Dictionary<string, object> bag = new Dictionary<string, object>();
      foreach (string n in names) {
        bag[n] = ReadProperty(entity, n);
      }

      if (typeof(TKey) == typeof(string)) {
        string composite = FileNameEncoder.Join(bag.Select((KeyValuePair<string, object> kv) => FileNameEncoder.NormalizePart(kv.Value)).ToArray());
        object boxed = composite;
        return (TKey)boxed;
      }

      // For non-string bags, try to map object -> TKey via JSON roundtrip
      string json = JsonConvert.SerializeObject(bag);

      TKey key = JsonConvert.DeserializeObject<TKey>(json);
      return key;
    }

    public TKey ReadKeyFromFields<TKey>(Dictionary<string, object> fields) {
      if (_Pk == null) { 
        return default(TKey);
      }

      if (_Pk.MemberFieldNames.Length == 1) {
        object v;
        if (!fields.TryGetValue(_Pk.MemberFieldNames[0], out v)) { return default(TKey); }
        return ConvertTo<TKey>(v);
      }

      // composite
      if (typeof(TKey) == typeof(string)) {
        List<string> parts = new List<string>();
        foreach (string n in _Pk.MemberFieldNames) {
          object v;
          if (!fields.TryGetValue(n, out v)) { parts.Add(""); }
          else { parts.Add(FileNameEncoder.NormalizePart(v)); }
        }
        string s = FileNameEncoder.Join(parts.ToArray());
        object boxed = s;
        return (TKey)boxed;
      }
      string json = JsonConvert.SerializeObject(fields.Where((KeyValuePair<string, object> kv) => _Pk.MemberFieldNames.Contains(kv.Key)).ToDictionary((KeyValuePair<string, object> kv) => kv.Key, (KeyValuePair<string, object> kv) => kv.Value));
      TKey key = JsonConvert.DeserializeObject<TKey>(json);
      return key;
    }

    public string[] RenderKeyParts<TKey>(TKey key) {
      if (_Pk == null) { return new string[0]; }
      if (_Pk.MemberFieldNames.Length == 1) {
        string p = FileNameEncoder.NormalizePart(key);
        return new string[] { p };
      }
      if (key is string) {
        string[] parts = ((string)(object)key).Split(new string[] { "_" }, StringSplitOptions.None);
        return parts;
      }
      // bag: reflect into dictionary
      JObject jo = JObject.FromObject(key);
      List<string> arr = new List<string>();
      foreach (string n in _Pk.MemberFieldNames) {
        JToken token = jo[n];
        if (token == null) { arr.Add(""); }
        else { arr.Add(FileNameEncoder.NormalizePart(token.ToObject<object>())); }
      }
      return arr.ToArray();
    }

    public string ReadIdentityLabel(object entity) {
      FieldSchema labelField = _Schema.Fields.FirstOrDefault((FieldSchema f) => f.IdentityLabel);
      if (labelField == null) { return string.Empty; }
      object v = ReadProperty(entity, labelField.Name);
      if (v == null) { return string.Empty; }
      return Convert.ToString(v, CultureInfo.InvariantCulture);
    }

    public Dictionary<string, object> ToDictionary(object entity) {
      Dictionary<string, object> dict = new Dictionary<string, object>();
      foreach (FieldSchema f in _Schema.Fields) {
        object v = ReadProperty(entity, f.Name);
        dict[f.Name] = v;
      }
      return dict;
    }

    public Dictionary<string, object> Project(object entity, string[] included) {
      Dictionary<string, object> dict = new Dictionary<string, object>();
      if (included == null || included.Length == 0) {
        return this.ToDictionary(entity);
      }
      foreach (string name in included) {
        object v = ReadProperty(entity, name);
        dict[name] = v;
      }
      return dict;
    }

    public void ApplyFields(object entity, Dictionary<string, object> fields, bool overwriteExisting) {
      foreach (KeyValuePair<string, object> kv in fields) {
        PropertyInfo pi = entity.GetType().GetProperty(kv.Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        if (pi == null) { continue; }
        if (!pi.CanWrite) { continue; }
        if (!overwriteExisting) {
          object current = pi.GetValue(entity);
          if (current != null) { continue; }
        }
        object v = ConvertValue(kv.Value, pi.PropertyType);
        pi.SetValue(entity, v);
      }
    }

    public bool KeyFieldsOverlap(Dictionary<string, object> fields) {
      if (_Pk == null) { return false; }
      foreach (string name in _Pk.MemberFieldNames) {
        if (fields.ContainsKey(name)) { return true; }
      }
      return false;
    }

    public Dictionary<string, object> Diff(object entity, Dictionary<string, object> provided) {
      Dictionary<string, object> diff = new Dictionary<string, object>();
      foreach (KeyValuePair<string, object> kv in provided) {
        object current = ReadProperty(entity, kv.Key);
        if (current == null && kv.Value == null) { continue; }
        if (current == null && kv.Value != null) { diff[kv.Key] = current; continue; }
        if (!object.Equals(current, kv.Value)) { diff[kv.Key] = current; }
      }
      return diff;
    }

    private static object ReadProperty(object entity, string name) {
      PropertyInfo pi = entity.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
      if (pi == null) { return null; }
      object v = pi.GetValue(entity);
      return v;
    }

    private static T ConvertTo<T>(object value) {
      if (value == null) { return default(T); }
      if (value is T) { return (T)value; }
      return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Instantiate a new entity instance of TEntity and apply provided fields.
    /// Only provided fields are set; everything else remains default/null.
    /// </summary>
    public TEntity Instantiate<TEntity>(Dictionary<string, object> fields) where TEntity : class {
      if (fields == null) {
        throw new ArgumentNullException("fields");
      }
      object instance = Activator.CreateInstance(typeof(TEntity));
      ApplyFields(instance, fields, true);
      return (TEntity)instance;
    }

    private static object ConvertValue(object value, Type targetType) {
      if (value == null) { return null; }
      Type t = Nullable.GetUnderlyingType(targetType) == null ? targetType : Nullable.GetUnderlyingType(targetType);
      if (t.IsEnum) {
        object parsed = Enum.Parse(t, value.ToString());
        return parsed;
      }
      if (t == typeof(Guid)) {
        Guid g = Guid.Parse(Convert.ToString(value, CultureInfo.InvariantCulture));
        return g;
      }
      if (t == typeof(DateTime)) {
        DateTime dt = DateTime.Parse(Convert.ToString(value, CultureInfo.InvariantCulture), null, DateTimeStyles.RoundtripKind);
        return dt;
      }
      object converted = Convert.ChangeType(value, t, CultureInfo.InvariantCulture);
      return converted;
    }

  }

}
