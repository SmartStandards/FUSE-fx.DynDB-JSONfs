using Newtonsoft.Json;
using System.Data.ModelDescription;
using System.IO;
using System.Linq;
using System.Reflection;

namespace System.Data.Fuse.JsonFileEngine {

  /// <summary>
  /// Dynamic model aware context that supplies repositories on demand.
  /// Uses ModelReader at runtime to acquire entity schemas when needed.
  /// </summary>
  public class FilesystemAccessContext {

    private readonly string _RootPath;
    private readonly FilesystemRepositoryOptions _RepoOptions;
    private readonly FilesystemIndexOptions _IndexOptions;
    private readonly JsonSerializerSettings _JsonSettings;
    private readonly SchemaRoot _Schema;

    public FilesystemAccessContext(string rootPath)
        : this(rootPath, new FilesystemRepositoryOptions(), new FilesystemIndexOptions(), CreateDefaultJsonSettings()) {
    }

    public FilesystemAccessContext(string rootPath, FilesystemRepositoryOptions repoOptions, FilesystemIndexOptions indexOptions, JsonSerializerSettings jsonSettings) {
      if (string.IsNullOrEmpty(rootPath)) {
        throw new ArgumentException("rootPath must not be empty");
      }
      _RootPath = rootPath;
      _RepoOptions = repoOptions == null ? new FilesystemRepositoryOptions() : repoOptions;
      _IndexOptions = indexOptions == null ? new FilesystemIndexOptions() : indexOptions;
      _JsonSettings = jsonSettings == null ? CreateDefaultJsonSettings() : jsonSettings;
      Directory.CreateDirectory(_RootPath);

      // Dynamic model: discover at runtime from current AppDomain types
      Type[] modelTypes = AppDomain.CurrentDomain.GetAssemblies()
          .SelectMany((Assembly a) => {
            try { return a.GetTypes(); }
            catch { return new Type[0]; }
          })
          .Where((Type t) => t.IsClass && !t.IsAbstract)
          .ToArray();
      _Schema = ModelReader.GetSchema(modelTypes, true);
    }

    /// <summary>
    /// Get a repository for the given entity and key type. Schema is resolved lazily via ModelReader.
    /// </summary>
    public IRepository<TEntity, TKey> Repo<TEntity, TKey>() where TEntity : class {
      EntitySchema entitySchema = ResolveEntitySchema(typeof(TEntity));
      if (entitySchema == null) {
        throw new InvalidOperationException("No EntitySchema found for type '" + typeof(TEntity).Name + "'.");
      }
      return new FilesystemRepository<TEntity, TKey>(_RootPath, entitySchema, _RepoOptions, _IndexOptions, _JsonSettings);
    }

    private EntitySchema ResolveEntitySchema(Type entityType) {
      EntitySchema match = _Schema.Entities.FirstOrDefault((EntitySchema e) => e.Name == entityType.Name);
      return match;
    }

    private static JsonSerializerSettings CreateDefaultJsonSettings() {

      JsonSerializerSettings settings = new JsonSerializerSettings();

      settings.Formatting = Formatting.Indented;
      settings.NullValueHandling = NullValueHandling.Include; // Keep historic/old fields
      settings.DateParseHandling = DateParseHandling.DateTime;
      settings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
      settings.MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead;
      settings.MissingMemberHandling = MissingMemberHandling.Ignore; // Keep unknown fields when merging
     
      return settings;
    }

  }

}
