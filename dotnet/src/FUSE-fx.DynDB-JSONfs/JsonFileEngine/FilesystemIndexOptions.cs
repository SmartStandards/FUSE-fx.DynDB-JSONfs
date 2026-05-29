
namespace System.Data.Fuse.JsonFileEngine {

  public class FilesystemIndexOptions {

    public FilesystemIndexOptions() {

      this.Delimiter = ",";
      this.Quote = '"';
      this.Escape = '"';
      this.UseHeader = true;
      this.EnableWal = true;
      this.EnableCrc32 = true;

    }
    public string Delimiter { get; set; }
    public char Quote { get; set; }
    public char Escape { get; set; }
    public bool UseHeader { get; set; }
    public bool EnableWal { get; set; }
    public bool EnableCrc32 { get; set; }

  }

}
