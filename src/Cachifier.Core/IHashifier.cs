namespace Cachifier
{
    using System.IO;

    /// <summary>
    /// Represents a hashifier
    /// </summary>
    public interface IHashifier
    {
        /// <summary>
        ///     Generates a SHA256 hash from a byte array
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        byte[] Hashify(byte[] bytes);

        /// <summary>
        ///     Generates a SHA256 hash from a byte array
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        byte[] Hashify(Stream stream);
    }
}