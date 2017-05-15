using System.Text;

namespace BmdViewer
{
    static class Utils
    {
        public static string toUnicodeString(byte[] str)
        {
            return Encoding.Unicode.GetString(str).Split('\0')[0];
        }

        public static string toGBKString(byte[] bytes)
        {
            return Encoding.GetEncoding(936).GetString(bytes).Split('\0')[0];
        }

        public static string toString(byte[] bytes)
        {
            return Encoding.Default.GetString(bytes).Split('\0')[0];
        }
    }
}
