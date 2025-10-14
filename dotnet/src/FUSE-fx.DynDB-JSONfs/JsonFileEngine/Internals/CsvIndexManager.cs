using System.Collections.Generic;
using System.Data.ModelDescription;
using System.Globalization;
using System.IO;
using System.Linq;

namespace System.Data.Fuse.JsonFileEngine.Internals {

  internal class CsvIndexManager {

    private readonly string _Folder;

    private readonly EntitySchema _Schema;
    private readonly FilesystemIndexOptions _Options;
    private readonly FilesystemRepositoryOptions _RepoOptions;
    private readonly IndexSchema[] _Indices;

    private readonly string[] _KeyMembers;

    public CsvIndexManager(string entityFolder, EntitySchema schema, FilesystemIndexOptions options, FilesystemRepositoryOptions repoOptions) {
    
      _Folder = entityFolder;
      _Schema = schema;
      _Options = options;
      _RepoOptions = repoOptions;
      _Indices = schema.Indices == null ? new IndexSchema[0] : schema.Indices;
     
      IndexSchema pk = _Indices.FirstOrDefault((IndexSchema i) => i.Name == schema.PrimaryKeyIndexName);
    
      if (pk == null) {
        _KeyMembers = new string[0]; 
      } else { 
        _KeyMembers = pk.MemberFieldNames; 
      }

    }

    public void UpsertEntity<TEntity, TKey>(TEntity entity, EntityAccessor accessor) where TEntity : class {
      Dictionary<string, object> fields = accessor.ToDictionary(entity);
      foreach (IndexSchema index in _Indices) {
        this.UpsertIndexRow(index, fields);
      }
    }

    public void DeleteByKey<TKey>(TKey key, EntityAccessor accessor) {
      string[] keyParts = accessor.RenderKeyParts(key);
      foreach (IndexSchema index in _Indices) {
        this.DeleteIndexRow(index, keyParts);
      }
    }

    private void UpsertIndexRow(IndexSchema index, Dictionary<string, object> fields) {

      string path = this.IndexPath(index);
      string[] header = this.BuildHeader(index);
      string keyComposite = CompositeKey(fields, _KeyMembers);
      string[] row = this.BuildRow(index, fields, keyComposite);

      CsvWal.AppendOrReplace(path, header, keyComposite, row, _Options, _RepoOptions);

    }

    private void DeleteIndexRow(IndexSchema index, string[] keyParts) {
      string path = this.IndexPath(index);
      string[] header = this.BuildHeader(index);
      string keyComposite = FileNameEncoder.Join(keyParts);
      CsvWal.Delete(path, header, keyComposite, _Options, _RepoOptions);
    }

    private string[] BuildHeader(IndexSchema index) {
      List<string> cols = new List<string>();
      cols.Add("Key");
      foreach (string m in index.MemberFieldNames) { 
        cols.Add(m);
      }
      string[] arr = cols.ToArray();
      return arr;
    }

    private string[] BuildRow(IndexSchema index, Dictionary<string, object> fields, string compositeKey) {
      List<string> cols = new List<string>();
      cols.Add(compositeKey);
      foreach (string m in index.MemberFieldNames) {
        object val;
        if (!fields.TryGetValue(m, out val)) { 
          cols.Add("");
        } else {
          cols.Add(Stringify(val));
        }
      }
      return cols.ToArray();
    }

    private string IndexPath(IndexSchema index) {
      string file = "index_" + index.Name + ".csv";
      string path = Path.Combine(_Folder, file);
      return path;
    }

    private static string CompositeKey(Dictionary<string, object> fields, string[] keyMembers) {
      List<string> parts = new List<string>();

      foreach (string k in keyMembers) {
        object val;
        if (fields.TryGetValue(k, out val)) { 
          parts.Add(FileNameEncoder.NormalizePart(val));
        } else {
          parts.Add("");
        }
      }

      string composite = FileNameEncoder.Join(parts.ToArray());
      return composite;
    }

    private static string Stringify(object value) {

      if (value == null) { 
        return "";
      }

      if (value is DateTime) { 
        return ((DateTime)value).ToString("o", CultureInfo.InvariantCulture);
      }

      return Convert.ToString(value, CultureInfo.InvariantCulture);
    }

  }

}
