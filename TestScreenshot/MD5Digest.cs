
namespace TestScreenshot
{
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;

    static class MD5Helper
    {
        private static string ByteArrayToString (byte[] ba)
        {
            // From https://stackoverflow.com/a/311179
            var hexString = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hexString.AppendFormat("{0:x2}", b);
            return hexString.ToString();
        }

        public static string GetMD5String (string fileName)
        {
            using (var stream = File.OpenRead(fileName))
            {
                return GetMD5String(stream);
            }
        }

        public static string GetMD5String (Stream stream)
        {
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(stream);
                return ByteArrayToString(hash);
            }
        }
    }
}
