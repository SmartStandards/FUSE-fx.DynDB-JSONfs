using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Fuse.JsonFileEngine;
using System.Data.ModelDescription;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Logging.SmartStandards;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace System.Data.Fuse.Files {

  [TestClass]
  public class FilesystemRepositoryTests {

    private EntitySchema BuildSchema() {
      EntitySchema e = new EntitySchema();
      e.Name = nameof(SampleEntity);
      e.PrimaryKeyIndexName = "PK";
      e.Fields = new FieldSchema[] {
                new FieldSchema(){ Name = nameof(SampleEntity.Id), Type = "Int32" },
                new FieldSchema(){ Name = nameof(SampleEntity.Name), Type = "String", IdentityLabel = true },
                new FieldSchema(){ Name = nameof(SampleEntity.ChangedUtc), Type = "DateTime" },
            };
      e.Indices = new IndexSchema[] {
                new IndexSchema(){ Name = "PK", Unique = true, MemberFieldNames = new string[]{ nameof(SampleEntity.Id) } },
                new IndexSchema(){ Name = "ByName", Unique = false, MemberFieldNames = new string[]{ nameof(SampleEntity.Name) } },
            };
      return e;
    }

    [TestMethod]
    public void AddReadUpdateDelete_Roundtrip() {
      string root = Path.Combine(Path.GetTempPath(), "FuseFsRepo_Tests");
      if (Directory.Exists(root)) { Directory.Delete(root, true); }
      FilesystemAccessContext ctx = new FilesystemAccessContext(root);
      FilesystemRepositoryOptions ro = new FilesystemRepositoryOptions();
      FilesystemIndexOptions io = new FilesystemIndexOptions();
      JsonSerializerSettings js = new JsonSerializerSettings();
      FilesystemRepository<SampleEntity, int> repo = new FilesystemRepository<SampleEntity, int>(root, BuildSchema(), ro, io, js);

      SampleEntity e1 = new SampleEntity() { Id = 1, Name = "Alpha", ChangedUtc = DateTime.UtcNow };
      int key = repo.TryAddEntity(e1);
      Assert.AreEqual(1, key);
      Assert.IsTrue(repo.ContainsKey(1));

      SampleEntity[] found = repo.GetEntities(ExpressionTree.Empty(), new string[] { "Name" }, 100, 0);
      Assert.AreEqual(1, found.Length);
      Assert.AreEqual("Alpha", found[0].Name);

      Dictionary<string, object> change = new Dictionary<string, object>();
      change[nameof(SampleEntity.Id)] = 1; // key required for TryUpdateEntityFields
      change[nameof(SampleEntity.Name)] = "Beta";
      Dictionary<string, object> diff = repo.TryUpdateEntityFields(change);
      Assert.IsNotNull(diff);

      SampleEntity[] after = repo.GetEntities(ExpressionTree.Empty(), new string[] { }, 100, 0);
      Assert.AreEqual("Beta", after[0].Name);

      int[] del = repo.TryDeleteEntities(new int[] { 1 });
      Assert.AreEqual(1, del.Length);
    }

    [TestMethod]
    public void Measure_Read_1000() {
      string root = Path.Combine(Path.GetTempPath(), "FuseFsRepo_Tests_ReadPerf");
      if (Directory.Exists(root)) { Directory.Delete(root, true); }
      FilesystemRepository<SampleEntity, int> repo = new FilesystemRepository<SampleEntity, int>(root, BuildSchema(), new FilesystemRepositoryOptions(), new FilesystemIndexOptions(), new JsonSerializerSettings());
      for (int i = 1; i <= 1000; i++) {
        SampleEntity e = new SampleEntity() { Id = i, Name = "E" + i, ChangedUtc = DateTime.UtcNow };
        repo.TryAddEntity(e);
      }
      Stopwatch sw = Stopwatch.StartNew();
      SampleEntity[] all = repo.GetEntities(ExpressionTree.Empty(), new string[] { }, int.MaxValue, 0);
      sw.Stop();
      DevLogger.LogTrace(0, 99999, "Loaded " + all.Length + " entities in " + sw.ElapsedMilliseconds + " ms");
      Assert.AreEqual(1000, all.Length);
    }

  }

  public class SampleEntity {
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime ChangedUtc { get; set; }
  }

}
