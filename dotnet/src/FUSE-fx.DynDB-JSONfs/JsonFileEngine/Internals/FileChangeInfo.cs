using System.Globalization;
using System.Linq;
using System.Text;

namespace System.Data.Fuse.JsonFileEngine.Internals {

  internal class FileChangeInfo { 
    public long LastWriteTicks { get; set; }
    public long Length { get; set; }
  }

}
