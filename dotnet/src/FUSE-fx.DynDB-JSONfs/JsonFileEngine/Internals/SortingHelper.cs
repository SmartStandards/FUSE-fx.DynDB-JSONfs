using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace System.Data.Fuse.JsonFileEngine.Internals {

  internal static class SortingHelper {

    public static IEnumerable<TEntity> ApplySorting<TEntity>(IEnumerable<TEntity> seq, string[] sortedBy, EntityAccessor accessor) where TEntity : class {
      if (sortedBy == null || sortedBy.Length == 0) { return seq; }
      IOrderedEnumerable<TEntity> ordered = null;
      for (int i = 0; i < sortedBy.Length; i++) {
        string field = sortedBy[i];
        bool desc = false;
        if (field.StartsWith("^")) { desc = true; field = field.Substring(1); }
        Func<TEntity, object> keySel = (TEntity e) => { return GetField(e, field); };
        if (i == 0) {
          if (desc) { ordered = seq.OrderByDescending((TEntity e) => keySel(e)); }
          else { ordered = seq.OrderBy((TEntity e) => keySel(e)); }
        }
        else {
          if (desc) { ordered = ordered.ThenByDescending((TEntity e) => keySel(e)); }
          else { ordered = ordered.ThenBy((TEntity e) => keySel(e)); }
        }
      }
      return ordered == null ? seq : ordered;
    }

    private static object GetField(object entity, string field) {
      PropertyInfo pi = entity.GetType().GetProperty(field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
      if (pi == null) { return null; }
      return pi.GetValue(entity);
    }

  }

}
