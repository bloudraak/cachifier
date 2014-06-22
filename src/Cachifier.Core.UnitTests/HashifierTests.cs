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
    using System.IO;
    using NUnit.Framework;

    /// <summary>
    ///     Represents the tests for <see cref="Hashifier" />
    /// </summary>
    [TestFixture]
    public sealed class HashifierTests
    {
        /// <summary>
        ///     Verifies <see cref="Hashifier.Hashify(byte[])" />with a byte array
        /// </summary>
        [Test]
        public void HashifyWithByteArray()
        {
            // Arrange
            var target = new Hashifier();
            var bytes = new byte[]
            {
                0x01,
                0x02,
                0x03
            };
            var expected = new byte[]
            {
                0x03,
                0x90,
                0x58,
                0xc6,
                0xf2,
                0xc0,
                0xcb,
                0x49,
                0x2c,
                0x53,
                0x3b,
                0x0a,
                0x4d,
                0x14,
                0xef,
                0x77,
                0xcc,
                0x0f,
                0x78,
                0xab,
                0xcc,
                0xce,
                0xd5,
                0x28,
                0x7d,
                0x84,
                0xa1,
                0xa2,
                0x01,
                0x1c,
                0xfb,
                0x81
            };

            // Act
            byte[] actual = target.Hashify(bytes);

            // Assert
            Assert.AreEqual(expected, actual, "The hash is incorrect");
        }

        /// <summary>
        ///     Verifies <see cref="Hashifier.Hashify(Stream)" />with a byte array
        /// </summary>
        [Test]
        public void HashifyWithStream()
        {
            // Arrange
            var target = new Hashifier();
            var bytes = new byte[]
            {
                0x01,
                0x02,
                0x03
            };
            var stream = new MemoryStream(bytes);
            var expected = new byte[]
            {
                0x03,
                0x90,
                0x58,
                0xc6,
                0xf2,
                0xc0,
                0xcb,
                0x49,
                0x2c,
                0x53,
                0x3b,
                0x0a,
                0x4d,
                0x14,
                0xef,
                0x77,
                0xcc,
                0x0f,
                0x78,
                0xab,
                0xcc,
                0xce,
                0xd5,
                0x28,
                0x7d,
                0x84,
                0xa1,
                0xa2,
                0x01,
                0x1c,
                0xfb,
                0x81
            };

            // Act
            byte[] actual = target.Hashify(stream);

            // Assert
            Assert.AreEqual(expected, actual, "The hash is incorrect");
        }
    }
}