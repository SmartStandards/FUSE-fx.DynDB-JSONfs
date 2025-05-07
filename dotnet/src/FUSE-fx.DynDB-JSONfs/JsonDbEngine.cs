using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.Data.ModelDescription;
using System.Diagnostics;
using System.IO;
using System.IO.Abstraction;
using System.IO.Abstraction.FsConvenience;
using System.Linq;
using System.Net;
using System.Reflection;

namespace System.Data.Fuse {

  public class JsonDbEngine : IUniversalRepository {

    private IAfsRepository _FsStore;
    private SchemaRoot _Schema;

    public JsonDbEngine(IAfsRepository fsStore, SchemaRoot schema) {
      _FsStore = fsStore;
      _Schema = schema;
    }
  
    public string GetOriginIdentity() {
      return _FsStore.GetOriginIdentity();
    }

    public RepositoryCapabilities GetCapabilities() {
      var fsCap = _FsStore.GetCapabilities();

      return new RepositoryCapabilities {
        CanAddNewEntities = fsCap.CanAppendContent,
        CanDeleteEntities = true,
        CanReadContent = true,
        CanUpdateContent = true,
        RequiresExternalKeys = true, 
        SupportsKeyUpdate = true,
        SupportsMassupdate = true,
        SupportsStringBasedSearchExpressions = false // << TODO: hier LinqDynamic nutzen
      };

    }

    public string[] GetEntityNames() {
      return _Schema.Entities.Select((e) => e.Name).ToArray();
    }

    public int CountAll(string entityName) {
      EntitySchema entitySchema = this.GetSchema(entityName);
      string entityDir = this.GetEntityDirectoryFullName(entitySchema);
      string[] fileFullNames = _FsStore.ListFilesOfDirectory(entityDir, limit: int.MaxValue);
      return fileFullNames.Length;
    }

    public int Count(string entityName, ExpressionTree filter) {
      throw new NotImplementedException();
    }

    public EntityRef[] GetEntityRefs(string entityName, ExpressionTree filter, string[] sortedBy, int limit = 100, int skip = 0) {
      throw new NotImplementedException();
    }

    public EntityRef[] GetEntityRefsBySearchExpression(string entityName, string searchExpression, string[] sortedBy, int limit = 100, int skip = 0) {
      throw new NotImplementedException();
    }

    public EntityRef[] GetEntityRefsByKey(string entityName, object[] keysToLoad) {
      throw new NotImplementedException();
    }

    public object[] GetEntities(string entityName, ExpressionTree filter, string[] sortedBy, int limit = 100, int skip = 0) {
      throw new NotImplementedException();
    }

    public object[] GetEntitiesBySearchExpression(string entityName, string searchExpression, string[] sortedBy, int limit = 100, int skip = 0) {
      throw new NotImplementedException();
    }

    public object[] GetEntitiesByKey(string entityName, object[] keysToLoad) {
      throw new NotImplementedException();
    }

    public Dictionary<string, object>[] GetEntityFields(string entityName, ExpressionTree filter, string[] includedFieldNames, string[] sortedBy, int limit = 100, int skip = 0) {
      throw new NotImplementedException();
    }

    public Dictionary<string, object>[] GetEntityFieldsBySearchExpression(string entityName, string searchExpression, string[] includedFieldNames, string[] sortedBy, int limit = 100, int skip = 0) {
      throw new NotImplementedException();
    }

    public Dictionary<string, object>[] GetEntityFieldsByKey(string entityName, object[] keysToLoad, string[] includedFieldNames) {
      throw new NotImplementedException();
    }

    public int CountBySearchExpression(string entityName, string searchExpression) {
      throw new NotImplementedException();
    }

    public bool ContainsKey(string entityName, object key) {
      throw new NotImplementedException();
    }

    public Dictionary<string, object> AddOrUpdateEntityFields(string entityName, Dictionary<string, object> fields) {
      throw new NotImplementedException();
    }

    public object AddOrUpdateEntity(string entityName, object entity) {
      throw new NotImplementedException();
    }

    public Dictionary<string, object> TryUpdateEntityFields(string entityName, Dictionary<string, object> fields) {
      throw new NotImplementedException();
    }

    public object TryUpdateEntity(string entityName, object entity) {
      throw new NotImplementedException();
    }

    public object TryAddEntity(string entityName, object entity) {
      throw new NotImplementedException();
    }

    public object[] MassupdateByKeys(string entityName, object[] keysToUpdate, Dictionary<string, object> fields) {
      throw new NotImplementedException();
    }

    public object[] Massupdate(string entityName, ExpressionTree filter, Dictionary<string, object> fields) {
      throw new NotImplementedException();
    }

    public object[] MassupdateBySearchExpression(string entityName, string searchExpression, Dictionary<string, object> fields) {
      throw new NotImplementedException();
    }

    public object[] TryDeleteEntities(string entityName, object[] keysToDelete) {
      throw new NotImplementedException();
    }

    public bool TryUpdateKey(string entityName, object currentKey, object newKey) {
      throw new NotImplementedException();
    }

    ///// INTERNAL /////////////////////////////

    private EntitySchema GetSchema(string entityName) {
      EntitySchema entitySchema = _Schema.Entities.Where((e) => e.Name == entityName).FirstOrDefault();
      if (entitySchema == null) {
        throw new Exception("Unknown Schema for -Entities named '{entityName}'!");
      }
      return entitySchema;
    }

    private string GetEntityDirectoryFullName(EntitySchema entitySchema) {
      return "/" + entitySchema.NamePlural;
    }


  }

}
