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
    using System;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Text;
    using Cachifier.Build.Tasks.Annotations;

    /// <summary>
    ///     Represents extension methods for byte arrays
    /// </summary>
    public static class ByteArrayExtensions
    {
        /// <summary>
        ///     Converts the byte array to a string
        /// </summary>
        /// <param name="bytes">The bytes to convert</param>
        /// <param name="seperator">The seperator</param>
        /// <returns>A string</returns>
        public static string ToString([NotNull] this byte[] bytes, [NotNull] string seperator)
        {
            Contract.Requires(bytes != null);
            Contract.Requires(seperator != null); 
            Contract.Requires((bytes.Length * 2 + seperator.Length * (bytes.Length - 1)) >= 0);

            var seperatorTotalLength = (seperator.Length * (bytes.Length - 1));
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