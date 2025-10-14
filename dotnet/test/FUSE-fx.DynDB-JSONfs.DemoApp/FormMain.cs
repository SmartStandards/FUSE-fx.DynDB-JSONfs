using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Fuse.JsonFileEngine;
using System.Data.ModelDescription;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace System.Data.Fuse.WinFormsDemo {

  public partial class FormMain : Form {

    private SchemaRoot _SchemaRoot = null;
    private IRepository<DemoEntity, int> _Repository = null;

    public FormMain() {
      this.InitializeComponent();
    }


    private void FormMain_Load(object sender, EventArgs e) {

      _SchemaRoot = ModelReader.GetSchema(new Type[] { typeof(DemoEntity) }, true);

      string rootFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SmartStandards", "FUSE-fx.DynDB-JSONfs.TestApp"
      );
      Directory.CreateDirectory(rootFolder);

      FilesystemAccessContext context = new FilesystemAccessContext(rootFolder);

      _Repository = context.Repo<DemoEntity, int>();

      _FuseTable.EntitySchema = _SchemaRoot.Entities.Where((e) => e.Name == nameof(DemoEntity)).Single();
      _FuseTable.ExpressionTree = new ExpressionTree();
      _FuseTable.SortedBy = new string[] { nameof(DemoEntity.Id) };
      _FuseTable.Skip = 0;
      _FuseTable.Limit = 100;

      _FuseTable.BindToRepository(_Repository);

    }  
    
    private void _BtnAddNewEntity_Click(object sender, EventArgs e) {

      int randomId = Random.Shared.Next();
      _Repository.TryAddEntity(
        new DemoEntity {
          Id = randomId,
          FirstName = $"Max-{randomId}",
          Last = $"Mustermann-{randomId}",
          ChangedUtc = DateTime.UtcNow
        }
      );

      //HACK: es gibt noch kein Reload, also triggen wir das so
      _FuseTable.EntitySchema = _FuseTable.EntitySchema;
      //_FuseTable.Reload();


    }

    private void FormMain_FormClosing(object sender, FormClosingEventArgs e) {
    }

  }

  [PrimaryIdentity("PK")]
  [PropertyGroup("PK", nameof(DemoEntity.Id))]
  public class DemoEntity {

    public int Id { get; set; }

    public string FirstName { get; set; }

    public string Last { get; set; }

    public DateTime ChangedUtc { get; set; }

  }

}
