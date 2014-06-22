namespace Cachifier
{
    using System.IO;
    using System.Text;

    /// <summary>
    /// Represents extension methods for byte arrays
    /// </summary>
    public static class ByteArrayExtensions
    {
        /// <summary>
        /// Converts the byte array to a string
        /// </summary>
        /// <param name="bytes">The bytes to convert</param>
        /// <param name="seperator">The seperator</param>
        /// <returns>A string</returns>
        public static string ToString(this byte[] bytes, string seperator)
        {
            var seperatorTotalLength = (seperator.Length * (bytes.Length-1));
            var bytesTotalLength = (bytes.Length * 2);
            var stringBuilder = new StringBuilder(bytesTotalLength + seperatorTotalLength);
            var writer = new StringWriter(stringBuilder);
            bool first = true;
            foreach (var b in bytes)
            {
                if (!first)
                {
                    writer.Write(seperator);
                }
                writer.Write("0x{0:x2}", b);
                first = false;
            }
            return writer.ToString();
        }
    }
}