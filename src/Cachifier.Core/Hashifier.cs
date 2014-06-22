#region Copyright

// The MIT License (MIT)
// 
// Copyright (c) 2014 Werner Strydom
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

#endregion

namespace Cachifier
{
    using System.Diagnostics;
    using System.IO;
    using System.Security.Cryptography;

    /// <summary>
    ///     Represents a class that generates a hash
    /// </summary>
    public sealed class Hashifier : IHashifier
    {
        /// <summary>
        ///     Generates a SHA256 hash from a byte array
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public byte[] Hashify(byte[] bytes)
        {
            using (var algorithm = HashAlgorithm.Create("SHA256"))
            {
                Debug.Assert(algorithm != null, "algorithm != null");
                return algorithm.ComputeHash(bytes);
            }
        }

        /// <summary>
        ///     Generates a SHA256 hash from a byte array
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public byte[] Hashify(Stream stream)
        {
            using (var algorithm = HashAlgorithm.Create("SHA256"))
            {
                Debug.Assert(algorithm != null, "algorithm != null");
                return algorithm.ComputeHash(stream);
            }
        }
    }
}