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

namespace Cachifier.UnitTests
{
    using NUnit.Framework;

    [TestFixture]
    public class EncoderTests
    {
        [TestFixture]
        public class Encode
        {
            [Test]
            public void WithByteArray()
            {
                // Arrange
                var target = new Encoder();
                var bytes = new byte[]
                {
                    0xeb,
                    0x0d,
                    0xb6,
                    0xe2,
                    0x60,
                    0xe2,
                    0x5c,
                    0xf0,
                    0x40,
                    0xeb,
                    0xb2,
                    0x5b,
                    0x87,
                    0x47,
                    0x78,
                    0xc7,
                    0x76,
                    0x46,
                    0x69,
                    0xd8,
                    0x21,
                    0x86,
                    0x5f,
                    0xd1,
                    0x7d,
                    0x90,
                    0x51,
                    0x18,
                    0x73,
                    0xeb,
                    0x55,
                    0xbe
                };
                const string expected = "hxeuj2ddrgcp52lorkxgbeef19kcb2vjunh1qm2ab335qu1xm1";

                // Act
                var actual = target.Encode(bytes);

                // Assert
                Assert.AreEqual(expected, actual);
            }
        }
    }
}