using System.Collections.Generic;

namespace System.IO.Abstraction.FsConvenience {

  internal static class AfsExtensions {

    public static string[] ListSubDirsOfDirectory(this IAfsRepository repo, string parentDirectoryFullName = "/") {
      if (!parentDirectoryFullName.EndsWith("/")) {
        parentDirectoryFullName += "/";
      }
      return repo.GetValueRange(AfsWellknownAttributeNames.Directory, parentDirectoryFullName + "*", out bool isReadOnly);
    }

    public static bool TryCreateDirectory(this IAfsRepository repo, string directoryFullName) {
      if (directoryFullName.EndsWith("/")) {
        directoryFullName = directoryFullName.Substring(0, directoryFullName.Length - 1);
      }
      return repo.TryAddToValueRange(AfsWellknownAttributeNames.Directory, directoryFullName);
    }

    /// <summary>
    /// Returns full names
    /// </summary>
    /// <param name="repo"></param>
    /// <param name="parentDirectoryFullName"></param>
    /// <param name="sortingAttributeName"></param>
    /// <param name="limit"></param>
    /// <param name="skip"></param>
    /// <returns></returns>
    public static string[] ListFilesOfDirectory(
      this IAfsRepository repo, string parentDirectoryFullName = "/",
      string sortingAttributeName = AfsWellknownAttributeNames.FileName, int limit = 100, int skip = 0
    ) {
      var attributesToFilter = new Dictionary<string, string>();
      attributesToFilter[AfsWellknownAttributeNames.Directory] = parentDirectoryFullName;
      return repo.SearchFilesByAttribute(attributesToFilter, sortingAttributeName, limit, skip);
    }

  }

}
