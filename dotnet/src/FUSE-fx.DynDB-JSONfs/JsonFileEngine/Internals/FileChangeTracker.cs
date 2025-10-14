using Logging.SmartStandards;
using System.Collections.Generic;
using System.IO;

namespace System.Data.Fuse.JsonFileEngine.Internals {

  internal class FileChangeTracker {

    private readonly string _Folder;
    private readonly FilesystemRepositoryOptions _Options;
    private readonly FileSystemWatcher _Watcher;
    private readonly Dictionary<string, FileChangeInfo> _Map;
    private readonly object _Sync;

    public FileChangeTracker(string folder, FilesystemRepositoryOptions options) {
      _Folder = folder;
      _Options = options;
      _Map = new Dictionary<string, FileChangeInfo>(StringComparer.OrdinalIgnoreCase);
      _Sync = new object();
      try {
        _Watcher = new FileSystemWatcher(_Folder, "*.json");
        _Watcher.IncludeSubdirectories = false;
        _Watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size;
        _Watcher.Changed += (object s, FileSystemEventArgs e) => { Touch(e.FullPath); };
        _Watcher.Created += (object s, FileSystemEventArgs e) => { Touch(e.FullPath); };
        _Watcher.Deleted += (object s, FileSystemEventArgs e) => { Remove(e.FullPath); };
        _Watcher.Renamed += (object s, RenamedEventArgs e) => { Remove(e.OldFullPath); Touch(e.FullPath); };
        _Watcher.EnableRaisingEvents = true;
      }
      catch (Exception ex) {
        DevLogger.LogTrace(0, 99999, "FileSystemWatcher disabled: " + ex.Message);
      }
    }

    public FileChangeInfo Get(string path) {
      FileInfo fi = new FileInfo(path);
      FileChangeInfo info = new FileChangeInfo();
      info.Length = fi.Exists ? fi.Length : 0;
      info.LastWriteTicks = fi.Exists ? fi.LastWriteTimeUtc.Ticks : 0;
      lock (_Sync) {
        _Map[path] = info;
      }
      return info;
    }
    private void Touch(string path) {
      FileInfo fi = new FileInfo(path);
      FileChangeInfo info = new FileChangeInfo();
      info.Length = fi.Exists ? fi.Length : 0;
      info.LastWriteTicks = fi.Exists ? fi.LastWriteTimeUtc.Ticks : 0;
      lock (_Sync) {
        _Map[path] = info;
      }
    }
    private void Remove(string path) {
      lock (_Sync) {
        if (_Map.ContainsKey(path)) { _Map.Remove(path); }
      }
    }
  }

}
