namespace Cachifier
{
    using System.IO;
    using Cachifier.Annotations;

    /// <summary>
    /// Represents a hashifier
    /// </summary>
    public interface IHashifier
    {
        /// <summary>
        ///     Generates a SHA256 hash from a byte array
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns>A byte array</returns>
        [PublicAPI]
        byte[] Hashify(byte[] bytes);

        /// <summary>
        ///     Generates a SHA256 hash from a stream
        /// </summary>
        /// <param name="stream"></param>
        /// <returns>A byte array</returns>
        [PublicAPI]
        byte[] Hashify(Stream stream);

        /// <summary>
        ///     Generates a SHA256 hash from a path
        /// </summary>
        /// <param name="path">The path of a file to generated a hash from</param>
        /// <returns>A byte array</returns>
        [PublicAPI]
        byte[] Hashify(string path);
    }
}