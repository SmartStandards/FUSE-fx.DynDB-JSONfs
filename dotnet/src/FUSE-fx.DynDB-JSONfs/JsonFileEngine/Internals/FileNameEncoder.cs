using System.Globalization;
using System.Linq;
using System.Text;

namespace System.Data.Fuse.JsonFileEngine.Internals {

  internal static class FileNameEncoder {

    public static string Encode(string[] keyParts) {

      string[] safe = keyParts.Select((string p) => NormalizePart(p)).ToArray();

      string file = string.Join("_", safe);

      return file;
    }

    public static string Join(string[] parts) {
      return string.Join("_", parts);
    }

    public static string NormalizePart(object value) {

      if (value == null) {
        return string.Empty;
      }

      string s = Convert.ToString(value, CultureInfo.InvariantCulture);

      // Escape non-filename-safe characters -> percent-encoding
      StringBuilder sb = new StringBuilder(s.Length + 8);

      for (int i = 0; i < s.Length; i++) {

        char ch = s[i];
        if (IsSafe(ch)) {
          sb.Append(ch);
        }
        else {
          sb.Append('%');
          sb.Append(((int)ch).ToString("X2"));
        }

      }
      return sb.ToString();
    }

    private static bool IsSafe(char ch) {

      if (char.IsLetterOrDigit(ch)) {
        return true;
      }

      if (ch == '-' || ch == '_' || ch == '.') {
        return true;
      }

      return false;
    }

  }

}
