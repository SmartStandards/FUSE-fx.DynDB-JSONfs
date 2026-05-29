using Logging.SmartStandards;
using System.IO;

namespace System.Data.Fuse.JsonFileEngine.Internals {

  internal static class Retry {

    public static void Run(FilesystemRepositoryOptions options, Action action) {
      int attempts = 0;
      Exception last = null;
      while (attempts < options.MaxRetries) {
        try { action(); return; }
        catch (IOException io) { last = io; }
        catch (UnauthorizedAccessException ua) { last = ua; }
        catch (Exception ex) { last = ex; break; }
        attempts++;
        System.Threading.Thread.Sleep(options.RetryInitialDelayMs * attempts);
      }
      if (last != null) { DevLogger.LogError(last); throw last; }
    }

  }

}
