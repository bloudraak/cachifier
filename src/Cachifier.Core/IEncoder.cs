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
    using Cachifier.Annotations;

    /// <summary>
    ///     Represents an encoder
    /// </summary>
    /// <remarks>
    ///     There are cases when you'd like to perform custom encoding of a string, for example to generate a valid filename.
    /// </remarks>
    public interface IEncoder
    {
        /// <summary>
        ///     Encodes a byte array to base36
        /// </summary>
        /// <param name="bytes">The bytes to convert to</param>
        /// <returns>The base36 encoded version of a byte array</returns>
        [PublicAPI]
        string Encode(byte[] bytes);

        /// <summary>
        ///     Encodes a byte array to a particular alphabet
        /// </summary>
        /// <param name="bytes">The bytes to convert</param>
        /// <param name="alphabet">The alphabet to convert too</param>
        /// <returns>The encoded byte array</returns>
        [PublicAPI]
        string Encode(byte[] bytes, string alphabet);

        /// <summary>
        ///     Encodes a byte array to a particular alphabet
        /// </summary>
        /// <param name="bytes">The bytes to convert</param>
        /// <param name="alphabet">The alphabet to convert too</param>
        /// <returns>The encoded byte array</returns>
        [PublicAPI]
        string Encode(byte[] bytes, char[] alphabet);
    }
}