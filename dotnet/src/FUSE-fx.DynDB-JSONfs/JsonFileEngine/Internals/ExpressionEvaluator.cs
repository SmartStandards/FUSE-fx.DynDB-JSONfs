using System.Globalization;
using System.Reflection;

namespace System.Data.Fuse.JsonFileEngine.Internals {

  internal static class ExpressionEvaluator {

    public static bool Matches<TEntity>(TEntity entity, ExpressionTree filter) where TEntity : class {
      if (filter == null) { return true; }
      bool result = true;
      if (filter.Predicates != null && filter.Predicates.Count > 0) {
        if (filter.MatchAll) { result = true; }
        else { result = false; }
        foreach (FieldPredicate p in filter.Predicates) {
          bool m = MatchesPredicate(entity, p);
          if (filter.MatchAll) {
            if (!m) { result = false; break; }
          }
          else {
            if (m) { result = true; break; }
          }
        }
      }
      if (filter.SubTree != null && filter.SubTree.Count > 0) {
        foreach (ExpressionTree sub in filter.SubTree) {
          bool m = Matches(entity, sub);
          if (filter.MatchAll) {
            if (!m) { result = false; break; }
          }
          else {
            if (m) { result = true; break; }
          }
        }
      }
      if (filter.Negate) { result = !result; }
      return result;
    }

    private static bool MatchesPredicate<TEntity>(TEntity entity, FieldPredicate p) where TEntity : class {
      object left = Read(entity, p.FieldName);
      object right = p.Value;
      string op = p.Operator;
      if (left == null && right == null) { return op == FieldOperators.Equal; }
      if (left == null || right == null) { return op == FieldOperators.NotEqual; }
      if (op == FieldOperators.Equal) { return object.Equals(left, right); }
      if (op == FieldOperators.NotEqual) { return !object.Equals(left, right); }
      IComparable lc = left as IComparable;
      IComparable rc = right as IComparable;
      if (op == FieldOperators.Greater || op == FieldOperators.GreaterOrEqual || op == FieldOperators.Less || op == FieldOperators.LessOrEqual) {
        if (lc == null || rc == null) { return false; }
        int cmp = lc.CompareTo(rc);
        if (op == FieldOperators.Greater) { return cmp > 0; }
        if (op == FieldOperators.GreaterOrEqual) { return cmp >= 0; }
        if (op == FieldOperators.Less) { return cmp < 0; }
        if (op == FieldOperators.LessOrEqual) { return cmp <= 0; }
      }
      string ls = Convert.ToString(left, CultureInfo.InvariantCulture);
      string rs = Convert.ToString(right, CultureInfo.InvariantCulture);
      if (op == FieldOperators.StartsWith) { return ls.StartsWith(rs, StringComparison.Ordinal); }
      if (op == FieldOperators.EndsWith) { return ls.EndsWith(rs, StringComparison.Ordinal); }
      if (op == FieldOperators.Contains) { return ls.IndexOf(rs, StringComparison.Ordinal) >= 0; }
      if (op == FieldOperators.SubstringOf) { return rs.IndexOf(ls, StringComparison.Ordinal) >= 0; }
      if (op == FieldOperators.In) {
        if (right is Array) {
          foreach (object item in (Array)right) {
            if (object.Equals(left, item)) { return true; }
          }
          return false;
        }
      }
      return false;
    }

    private static object Read(object entity, string field) {
      PropertyInfo pi = entity.GetType().GetProperty(field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
      if (pi == null) { return null; }
      return pi.GetValue(entity);
    }
  }

}
