using Logging.SmartStandards;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.Fuse;
using System.Data.Fuse.JsonFileEngine.Internals;
using System.Data.ModelDescription;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace System.Data.Fuse.JsonFileEngine {

  /// <summary>
  /// File-backed repository using one JSON file per entity and CSV index files per index.
  /// Designed for SMB/network filesystems with compatibility-first semantics.
  /// </summary>
  public class FilesystemRepository<TEntity, TKey> : IRepository<TEntity, TKey> where TEntity : class {
    
    private readonly string _RootPath;
    private readonly string _EntityFolder;
    private readonly EntitySchema _Schema;
    private readonly FilesystemRepositoryOptions _Options;
    private readonly FilesystemIndexOptions _IndexOptions;
    private readonly JsonSerializerSettings _JsonSettings;
    private readonly CsvIndexManager _IndexManager;
    private readonly EntityAccessor _Accessor;
    private readonly FileChangeTracker _ChangeTracker;
    private readonly L1Cache<string, CacheEntry> _Cache;

    public FilesystemRepository(string rootPath, EntitySchema schema, FilesystemRepositoryOptions options, FilesystemIndexOptions indexOptions, JsonSerializerSettings jsonSettings) {
      if (string.IsNullOrEmpty(rootPath)) {
        throw new ArgumentException("rootPath must not be empty");
      }
      if (schema == null) {
        throw new ArgumentNullException("schema");
      }

      _RootPath = rootPath;
      _Schema = schema;
      _Options = options == null ? new FilesystemRepositoryOptions() : options;
      _IndexOptions = indexOptions == null ? new FilesystemIndexOptions() : indexOptions;
      _JsonSettings = jsonSettings == null ? new JsonSerializerSettings() : jsonSettings;

      _EntityFolder = Path.Combine(_RootPath, _Schema.Name);
      Directory.CreateDirectory(_EntityFolder);

      _Accessor = new EntityAccessor(_Schema);
      _IndexManager = new CsvIndexManager(_EntityFolder, _Schema, _IndexOptions, _Options);
      _ChangeTracker = new FileChangeTracker(_EntityFolder, _Options);
      _Cache = new L1Cache<string, CacheEntry>(_Options.CacheEntityCountLimit, _Options.CacheTtlMs);
    }

    public string GetOriginIdentity() {
      string identity = Convert.ToBase64String(SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(_EntityFolder)));
      return identity;
    }

    public RepositoryCapabilities GetCapabilities() {
      RepositoryCapabilities caps = new RepositoryCapabilities();
      caps.CanAddNewEntities = true;
      caps.CanDeleteEntities = true;
      caps.CanReadContent = true;
      caps.CanUpdateContent = true;
      caps.SupportsKeyUpdate = true;
      caps.SupportsMassupdate = true;
      caps.SupportsStringBasedSearchExpressions = false; // keep simple and robust
      caps.RequiresExternalKeys = true; // per user
      return caps;
    }

    // -----------------------------------------------------------------------------------------
    // Queries – Refs
    // -----------------------------------------------------------------------------------------
    public EntityRef<TKey>[] GetEntityRefs(ExpressionTree filter, string[] sortedBy, int limit = 100, int skip = 0) {
      TEntity[] entities = GetEntities(filter, sortedBy, limit, skip);
      EntityRef<TKey>[] refs = entities.Select((TEntity e) => new EntityRef<TKey>() { Key = _Accessor.ReadKey<TKey>(e), Label = _Accessor.ReadIdentityLabel(e) }).ToArray();
      return refs;
    }

    public EntityRef<TKey>[] GetEntityRefsBySearchExpression(string searchExpression, string[] sortedBy, int limit = 100, int skip = 0) {
      throw new NotSupportedException("Search expressions are not supported by this repository. Use ExpressionTree.");
    }

    public EntityRef<TKey>[] GetEntityRefsByKey(TKey[] keysToLoad) {
      TEntity[] entities = GetEntitiesByKey(keysToLoad);
      EntityRef<TKey>[] refs = entities.Select((TEntity e) => new EntityRef<TKey>() { Key = _Accessor.ReadKey<TKey>(e), Label = _Accessor.ReadIdentityLabel(e) }).ToArray();
      return refs;
    }

    // -----------------------------------------------------------------------------------------
    // Queries – Entities
    // -----------------------------------------------------------------------------------------
    public TEntity[] GetEntities(ExpressionTree filter, string[] sortedBy, int limit = 100, int skip = 0) {
      IEnumerable<string> files = Directory.EnumerateFiles(_EntityFolder, "*.json", SearchOption.TopDirectoryOnly);
      List<TEntity> acc = new List<TEntity>();
      foreach (string file in files) {
        CacheEntry cached = TryGetFromCache(file);
        TEntity entity;
        if (cached != null && cached.Entity != null) {
          entity = (TEntity)cached.Entity;
        }
        else {
          entity = ReadEntityFromFile(file);
          PutCache(file, entity);
        }
        if (entity == null) {
          continue;
        }
        if (ExpressionEvaluator.Matches(entity, filter)) {
          acc.Add(entity);
        }
      }
      IEnumerable<TEntity> seq = acc.AsEnumerable();
      seq = SortingHelper.ApplySorting(seq, sortedBy, _Accessor);
      if (skip > 0) { seq = seq.Skip(skip); }
      if (limit > 0) { seq = seq.Take(limit); }
      return seq.ToArray();
    }

    public TEntity[] GetEntitiesBySearchExpression(string searchExpression, string[] sortedBy, int limit = 100, int skip = 0) {
      throw new NotSupportedException("Search expressions are not supported by this repository. Use ExpressionTree.");
    }

    public TEntity[] GetEntitiesByKey(TKey[] keysToLoad) {
      if (keysToLoad == null) { throw new ArgumentNullException("keysToLoad"); }
      List<TEntity> list = new List<TEntity>();
      string[] paths = keysToLoad.Select((TKey k) => BuildEntityPathFromKey(k)).ToArray();
      foreach (string p in paths) {
        if (!File.Exists(p)) { continue; }
        CacheEntry cached = TryGetFromCache(p);
        TEntity entity;
        if (cached != null && cached.Entity != null) {
          entity = (TEntity)cached.Entity;
        }
        else {
          entity = ReadEntityFromFile(p);
          PutCache(p, entity);
        }
        if (entity != null) { list.Add(entity); }
      }
      return list.ToArray();
    }

    // -----------------------------------------------------------------------------------------
    // Queries – Fields
    // -----------------------------------------------------------------------------------------
    public Dictionary<string, object>[] GetEntityFields(ExpressionTree filter, string[] includedFieldNames, string[] sortedBy, int limit = 100, int skip = 0) {
      TEntity[] entities = GetEntities(filter, sortedBy, limit, skip);
      return entities.Select((TEntity e) => _Accessor.Project(e, includedFieldNames)).ToArray();
    }

    public Dictionary<string, object>[] GetEntityFieldsBySearchExpression(string searchExpression, string[] includedFieldNames, string[] sortedBy, int limit = 100, int skip = 0) {
      throw new NotSupportedException("Search expressions are not supported by this repository. Use ExpressionTree.");
    }

    public Dictionary<string, object>[] GetEntityFieldsByKey(TKey[] keysToLoad, string[] includedFieldNames) {
      TEntity[] entities = GetEntitiesByKey(keysToLoad);
      return entities.Select((TEntity e) => _Accessor.Project(e, includedFieldNames)).ToArray();
    }

    // -----------------------------------------------------------------------------------------
    // Counts / Contains
    // -----------------------------------------------------------------------------------------
    public int CountAll() {
      int count = Directory.EnumerateFiles(_EntityFolder, "*.json", SearchOption.TopDirectoryOnly).Count();
      return count;
    }
    public int Count(ExpressionTree filter) {
      if (filter == null) { return CountAll(); }
      int matches = GetEntities(filter, new string[0], int.MaxValue, 0).Length;
      return matches;
    }
    public int CountBySearchExpression(string searchExpression) {
      throw new NotSupportedException("Search expressions are not supported by this repository. Use ExpressionTree.");
    }
    public bool ContainsKey(TKey key) {
      string path = BuildEntityPathFromKey(key);
      return File.Exists(path);
    }

    // -----------------------------------------------------------------------------------------
    // Mutations – AddOrUpdate / TryUpdate / Massupdate / Delete / Key update
    // -----------------------------------------------------------------------------------------
    public Dictionary<string, object> AddOrUpdateEntityFields(Dictionary<string, object> fields) {
      if (fields == null) { throw new ArgumentNullException("fields"); }
      // Merge semantics: keep unknown/old values; overwrite only provided fields
      TKey key = _Accessor.ReadKeyFromFields<TKey>(fields);
      bool hasKey = !EqualityComparer<TKey>.Default.Equals(key, default(TKey));
      if (!hasKey && GetCapabilities().RequiresExternalKeys) {
        return null;
      }
      TEntity entity = default(TEntity);
      if (hasKey && ContainsKey(key)) {
        entity = ReadEntityFromFile(BuildEntityPathFromKey(key));
        if (entity == null) { return null; }
        _Accessor.ApplyFields(entity, fields, true);
      }
      else {
        entity = _Accessor.Instantiate<TEntity>(fields);
      }
      TEntity written = AddOrUpdateEntity(entity);
      if (written == null) { return null; }
      Dictionary<string, object> diff = _Accessor.Diff(written, fields);
      return diff;
    }

    public TEntity AddOrUpdateEntity(TEntity entity) {
      if (entity == null) { throw new ArgumentNullException("entity"); }
      TKey key = _Accessor.ReadKey<TKey>(entity);
      bool hasKey = !EqualityComparer<TKey>.Default.Equals(key, default(TKey));
      if (!hasKey && GetCapabilities().RequiresExternalKeys) {
        return null;
      }
      string path = BuildEntityPathFromKey(key);
      Retry.Run(_Options, () => WriteEntityAtomically(path, entity));
      PutCache(path, entity);
      _IndexManager.UpsertEntity<TEntity, TKey>(entity, _Accessor);
      return entity;
    }

    public Dictionary<string, object> TryUpdateEntityFields(Dictionary<string, object> fields) {
      if (fields == null) { throw new ArgumentNullException("fields"); }
      TKey key = _Accessor.ReadKeyFromFields<TKey>(fields);
      if (EqualityComparer<TKey>.Default.Equals(key, default(TKey))) {
        throw new InvalidOperationException("Key fields missing for TryUpdateEntityFields().");
      }
      string path = BuildEntityPathFromKey(key);
      if (!File.Exists(path)) { return null; }
      TEntity current = ReadEntityFromFile(path);
      if (current == null) { return null; }
      _Accessor.ApplyFields(current, fields, true);
      Retry.Run(_Options, () => WriteEntityAtomically(path, current));
      PutCache(path, current);
      _IndexManager.UpsertEntity<TEntity, TKey>(current, _Accessor);
      Dictionary<string, object> diff = _Accessor.Diff(current, fields);
      return diff;
    }

    public TEntity TryUpdateEntity(TEntity entity) {
      if (entity == null) { throw new ArgumentNullException("entity"); }
      TKey key = _Accessor.ReadKey<TKey>(entity);
      if (EqualityComparer<TKey>.Default.Equals(key, default(TKey))) {
        throw new InvalidOperationException("Key fields missing for TryUpdateEntity().");
      }
      string path = BuildEntityPathFromKey(key);
      if (!File.Exists(path)) { return null; }
      Retry.Run(_Options, () => WriteEntityAtomically(path, entity));
      PutCache(path, entity);
      _IndexManager.UpsertEntity<TEntity, TKey>(entity, _Accessor);
      return entity;
    }

    public TKey TryAddEntity(TEntity entity) {
      if (entity == null) { throw new ArgumentNullException("entity"); }
      TKey key = _Accessor.ReadKey<TKey>(entity);
      if (EqualityComparer<TKey>.Default.Equals(key, default(TKey)) && GetCapabilities().RequiresExternalKeys) {
        return default(TKey);
      }
      string path = BuildEntityPathFromKey(key);
      if (File.Exists(path)) { return default(TKey); }
      Retry.Run(_Options, () => WriteEntityAtomically(path, entity));
      PutCache(path, entity);
      _IndexManager.UpsertEntity<TEntity, TKey>(entity, _Accessor);
      return key;
    }

    public TKey[] MassupdateByKeys(TKey[] keysToUpdate, Dictionary<string, object> fields) {
      if (keysToUpdate == null) { throw new ArgumentNullException("keysToUpdate"); }
      if (fields == null) { throw new ArgumentNullException("fields"); }
      if (_Accessor.KeyFieldsOverlap(fields)) {
        throw new InvalidOperationException("Fields contain key members. Massupdate is not allowed to touch keys.");
      }
      List<TKey> updated = new List<TKey>();
      foreach (TKey key in keysToUpdate) {
        string path = BuildEntityPathFromKey(key);
        if (!File.Exists(path)) { continue; }
        TEntity e = ReadEntityFromFile(path);
        if (e == null) { continue; }
        _Accessor.ApplyFields(e, fields, true);
        Retry.Run(_Options, () => WriteEntityAtomically(path, e));
        PutCache(path, e);
        _IndexManager.UpsertEntity<TEntity, TKey>(e, _Accessor);
        updated.Add(key);
      }
      return updated.ToArray();
    }

    public TKey[] Massupdate(ExpressionTree filter, Dictionary<string, object> fields) {
      if (fields == null) { throw new ArgumentNullException("fields"); }
      if (_Accessor.KeyFieldsOverlap(fields)) {
        throw new InvalidOperationException("Fields contain key members. Massupdate is not allowed to touch keys.");
      }
      TEntity[] entities = GetEntities(filter, new string[0], int.MaxValue, 0);
      List<TKey> updated = new List<TKey>();
      foreach (TEntity e in entities) {
        TKey key = _Accessor.ReadKey<TKey>(e);
        string path = BuildEntityPathFromKey(key);
        _Accessor.ApplyFields(e, fields, true);
        Retry.Run(_Options, () => WriteEntityAtomically(path, e));
        PutCache(path, e);
        _IndexManager.UpsertEntity<TEntity, TKey>(e, _Accessor);
        updated.Add(key);
      }
      return updated.ToArray();
    }

    public TKey[] MassupdateBySearchExpression(string searchExpression, Dictionary<string, object> fields) {
      throw new NotSupportedException("Search expressions are not supported by this repository. Use ExpressionTree.");
    }

    public TKey[] TryDeleteEntities(TKey[] keysToDelete) {
      if (keysToDelete == null) { throw new ArgumentNullException("keysToDelete"); }
      List<TKey> deleted = new List<TKey>();
      foreach (TKey key in keysToDelete) {
        string path = BuildEntityPathFromKey(key);
        if (!File.Exists(path)) { continue; }
        Retry.Run(_Options, () => {
          // conflict-safe delete: move to .deleted first
          string trash = path + ".deleted";
          if (File.Exists(trash)) { File.Delete(trash); }
          File.Move(path, trash);
          File.Delete(trash);
        });
        RemoveCache(path);
        _IndexManager.DeleteByKey(key, _Accessor);
        deleted.Add(key);
      }
      return deleted.ToArray();
    }

    public bool TryUpdateKey(TKey currentKey, TKey newKey) {
      string src = BuildEntityPathFromKey(currentKey);
      if (!File.Exists(src)) { return false; }
      string dst = BuildEntityPathFromKey(newKey);
      if (File.Exists(dst)) { return false; }
      Retry.Run(_Options, () => {
        string tmp = dst + ".tmp";
        File.Move(src, tmp);
        File.Move(tmp, dst);
      });
      RemoveCache(src);
      TEntity entity = ReadEntityFromFile(dst);
      if (entity != null) {
        _IndexManager.DeleteByKey(currentKey, _Accessor);
        _IndexManager.UpsertEntity<TEntity, TKey>(entity, _Accessor);
      }
      return true;
    }

    private string BuildEntityPathFromKey(TKey key) {
      string[] parts = _Accessor.RenderKeyParts(key);
      string fileSafe = FileNameEncoder.Encode(parts);
      string path = Path.Combine(_EntityFolder, fileSafe + ".json");
      return path;
    }

    private TEntity ReadEntityFromFile(string path) {
      try {
        string json = File.ReadAllText(path, Encoding.UTF8);
        JObject jo = JObject.Parse(json);
        // preserve old fields: no stripping
        TEntity e = jo.ToObject<TEntity>(JsonSerializer.Create(_JsonSettings));
        return e;
      }
      catch (IOException io) { DevLogger.LogError(io); return null; }
      catch (UnauthorizedAccessException ua) { DevLogger.LogError(ua); return null; }
      catch (JsonException jx) { DevLogger.LogError(jx); return null; }
    }

    private void WriteEntityAtomically(string path, TEntity entity) {
      string dir = Path.GetDirectoryName(path);
      Directory.CreateDirectory(dir);
      string tmp = path + ".tmp";
      string conflict = path + ".conflict-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");

      // Create merged JSON object that preserves unknown/legacy fields from existing file.
      JObject merged;
      JObject newObj = JObject.FromObject(entity, JsonSerializer.Create(_JsonSettings));
      if (File.Exists(path)) {
        try { File.Copy(path, conflict + ".json", true); }
        catch (Exception) { }
        try {
          string oldJson = File.ReadAllText(path, Encoding.UTF8);
          JObject oldObj = JObject.Parse(oldJson);
          JsonMergeSettings ms = new JsonMergeSettings();
          ms.MergeArrayHandling = MergeArrayHandling.Replace;
          ms.MergeNullValueHandling = MergeNullValueHandling.Merge;
          oldObj.Merge(newObj, ms); // overwrite only provided (new) fields, keep others
          merged = oldObj;
        }
        catch (Exception ex) {
          DevLogger.LogTrace(0, 99999, "Merge fallback due to read/parse issue: " + ex.Message);
          merged = newObj; // fallback: write just the new object
        }
      }
      else {
        merged = newObj;
      }

      string json = merged.ToString(Formatting.Indented);
      using (FileStream fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None)) {
        byte[] data = Encoding.UTF8.GetBytes(json);
        fs.Write(data, 0, data.Length);
        fs.Flush(true); // compatibility-first (SMB-friendly)
      }
      if (File.Exists(path)) { File.Delete(path); }
      File.Move(tmp, path);
    }

    // -----------------------------------------------------------------------------------------
    // Cache & change tracking
    // -----------------------------------------------------------------------------------------
    private CacheEntry TryGetFromCache(string path) {
      FileChangeInfo ch = _ChangeTracker.Get(path);
      CacheEntry entry;
      if (_Cache.TryGet(path, out entry)) {
        if (entry.LastWriteTicks == ch.LastWriteTicks && entry.Length == ch.Length) {
          return entry;
        }
        else {
          RemoveCache(path);
        }
      }
      return null;
    }
    private void PutCache(string path, TEntity entity) {
      FileChangeInfo ch = _ChangeTracker.Get(path);
      CacheEntry entry = new CacheEntry();
      entry.Entity = entity;
      entry.LastWriteTicks = ch.LastWriteTicks;
      entry.Length = ch.Length;
      _Cache.Put(path, entry);
    }
    private void RemoveCache(string path) {
      _Cache.Remove(path);
    }

    private class CacheEntry {
      public object Entity { get; set; }
      public long LastWriteTicks { get; set; }
      public long Length { get; set; }
    }

  }

}
