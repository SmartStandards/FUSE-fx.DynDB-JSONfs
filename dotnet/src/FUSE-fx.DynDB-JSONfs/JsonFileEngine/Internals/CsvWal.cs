using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace System.Data.Fuse.JsonFileEngine.Internals {

  internal static class CsvWal {

    public static void AppendOrReplace(string path, string[] header, string keyComposite, string[] row, FilesystemIndexOptions options, FilesystemRepositoryOptions repoOptions) {
    
      string wal = path + ".wal";
      string bak = path + ".bak";
      string tmp = path + ".tmp";

      Directory.CreateDirectory(Path.GetDirectoryName(path));

      // Ensure header exists
      if (!File.Exists(path)) {
        WriteNew(path, header, repoOptions, options);
      }

      // Append into WAL (idempotent: we write full file later)
      using (FileStream fs = new FileStream(wal, FileMode.Append, FileAccess.Write, FileShare.Read)) {
        string line = CsvLine(header.Length, row, options);
        WriteWithBomIfNeeded(fs, line, repoOptions);
        fs.Flush(true);
      }

      // Rebuild compacted file from existing + WAL
      RebuildFromWal(path, header, wal, bak, tmp, options, repoOptions, keyComposite, row);
    }

    public static void Delete(string path, string[] header, string keyComposite, FilesystemIndexOptions options, FilesystemRepositoryOptions repoOptions) {
     
      string wal = path + ".wal";
      string bak = path + ".bak";
      string tmp = path + ".tmp";

      Directory.CreateDirectory(Path.GetDirectoryName(path));
      if (!File.Exists(path)) {
        WriteNew(path, header, repoOptions, options);
      }

      using (FileStream fs = new FileStream(wal, FileMode.Append, FileAccess.Write, FileShare.Read)) {
        string tomb = "#DEL:" + keyComposite + "\n";
        WriteWithBomIfNeeded(fs, tomb, repoOptions);
        fs.Flush(true);
      }

      RebuildFromWal(path, header, wal, bak, tmp, options, repoOptions, null, null);
    }

    private static void RebuildFromWal(string path, string[] header, string wal, string bak, string tmp, FilesystemIndexOptions options, FilesystemRepositoryOptions repoOptions, string changedKey, string[] changedRow) {
    
      Dictionary<string, string[]> rows = new Dictionary<string, string[]>();
     
      // Load current
      foreach (string line in File.ReadAllLines(path, Encoding.UTF8)) {
        if (line.StartsWith("#")) { continue; }
        if (line.Trim().Length == 0) { continue; }
        string[] parts = SplitCsv(line, options);
        if (parts.Length == 0) { continue; }
        if (parts[0] == header[0]) { continue; } // header
        if (!rows.ContainsKey(parts[0])) { rows[parts[0]] = parts; }
        else { rows[parts[0]] = parts; }
      }
    
      // Apply WAL
      if (File.Exists(wal)) {
        foreach (string line in File.ReadAllLines(wal, Encoding.UTF8)) {
          if (line.StartsWith("#DEL:")) {
            string key = line.Substring(5).Trim();
            if (rows.ContainsKey(key)) { rows.Remove(key); }
            continue;
          }
          if (line.StartsWith("#")) { continue; }
          if (line.Trim().Length == 0) { continue; }
          string[] parts = SplitCsv(line, options);
          if (parts.Length == 0) { continue; }
          rows[parts[0]] = parts;
        }
      }

      if (changedKey != null && changedRow != null) {
        rows[changedKey] = changedRow;
      }

      // Write tmp new file atomically
      using (FileStream fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None)) {
        string headerLine = CsvLine(header.Length, header, options);
        WriteWithBomIfNeeded(fs, headerLine, repoOptions);
        foreach (KeyValuePair<string, string[]> kv in rows.OrderBy((KeyValuePair<string, string[]> p) => p.Key)) {
          string line = CsvLine(header.Length, kv.Value, options);
          WriteWithBomIfNeeded(fs, line, repoOptions);
        }
        fs.Flush(true);
      }

      if (File.Exists(bak)) {
        File.Delete(bak);
      }

      if (File.Exists(path)) {
        File.Move(path, bak); 
      }

      File.Move(tmp, path);

      if (File.Exists(wal)) {
        File.Delete(wal); 
      }
      if (File.Exists(bak)) {
        File.Delete(bak); 
      }
    }

    private static void WriteNew(string path, string[] header, FilesystemRepositoryOptions repoOptions, FilesystemIndexOptions options) {
      using (FileStream fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read)) {
        string headerLine = CsvLine(header.Length, header, options);
        WriteWithBomIfNeeded(fs, headerLine, repoOptions);
        fs.Flush(true);
      }
    }

    private static void WriteWithBomIfNeeded(FileStream fs, string text, FilesystemRepositoryOptions repoOptions) {
      byte[] data = Encoding.UTF8.GetBytes(text);
      if (repoOptions.UseWindowsCsvBom && fs.Position == 0) {
        byte[] bom = new byte[] { 0xEF, 0xBB, 0xBF };
        fs.Write(bom, 0, bom.Length);
      }
      fs.Write(data, 0, data.Length);
      byte[] nl = Encoding.UTF8.GetBytes("\n");
      fs.Write(nl, 0, nl.Length);
    }

    private static string CsvLine(int headerLen, string[] cells, FilesystemIndexOptions options) {
      StringBuilder sb = new StringBuilder(256);
      for (int i = 0; i < headerLen && i < cells.Length; i++) {
        if (i > 0) { sb.Append(options.Delimiter); }
        string c = cells[i] == null ? string.Empty : cells[i];
        string q = c.Replace("\"", "\"\"");
        sb.Append(options.Quote);
        sb.Append(q);
        sb.Append(options.Quote);
      }
      return sb.ToString();
    }

    private static string[] SplitCsv(string line, FilesystemIndexOptions options) {
      List<string> result = new List<string>();
      bool inQuotes = false;
      StringBuilder cur = new StringBuilder();

      for (int i = 0; i < line.Length; i++) {
        char ch = line[i];
        if (ch == options.Quote) {
          if (inQuotes && i + 1 < line.Length && line[i + 1] == options.Quote) {
            cur.Append(options.Quote);
            i++;
          }
          else {
            inQuotes = !inQuotes;
          }
          continue;
        }
        if (!inQuotes && ch.ToString() == options.Delimiter) {
          result.Add(cur.ToString());
          cur.Clear();
          continue;
        }
        cur.Append(ch);
      }

      result.Add(cur.ToString());
      return result.ToArray();
    }

  }

}
