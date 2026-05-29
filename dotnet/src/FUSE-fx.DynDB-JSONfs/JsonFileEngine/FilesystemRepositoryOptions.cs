using System.Runtime.InteropServices;

namespace System.Data.Fuse.JsonFileEngine {

  public class FilesystemRepositoryOptions {

    public FilesystemRepositoryOptions() {

      this.UseWindowsCsvBom = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
      this.MaxRetries = 3;
      this.RetryInitialDelayMs = 60;
      this.EnableFsyncCompatibilityMode = true; // Compatibility-first as requested
      this.ScanChangeDetectionIntervalMs = 3000; // Fallback when watchers are unreliable
      this.CacheEntityCountLimit = 2000;
      this.CacheTtlMs = 15000;

    }

    public bool UseWindowsCsvBom { get; set; }
    public int MaxRetries { get; set; }
    public int RetryInitialDelayMs { get; set; }
    public bool EnableFsyncCompatibilityMode { get; set; }
    public int ScanChangeDetectionIntervalMs { get; set; }
    public int CacheEntityCountLimit { get; set; }
    public int CacheTtlMs { get; set; }

  }

}
