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
    using System.IO;
    using Cachifier.Properties;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ProcessorTests
    {
        private string _tempPath;

        [SetUp]
        public void SetUp()
        {
            this._tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(this._tempPath);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(this._tempPath, true);
        }

        [Test]
        public void Process()
        {
            // Arrange
            var imagePath = Path.Combine(_tempPath, "Image1.jpg");
            File.WriteAllBytes(imagePath, Resources.Image1);

            var expectedHashedImagePath = Path.Combine(_tempPath, "kna98mczkhzth33v1zzy10in814243cflqzjf7dlezrgmcddg1.jpg");
            var target = new Processor();

            var resources = new[]
            {
                new Resource()
                {
                    Path = imagePath
                }
            };

            // Act
            target.Process(resources);

            // Assert
            Assert.AreEqual(expectedHashedImagePath, resources[0].HashedPath, "The hashed path is incorrect");
            Assert.IsTrue(File.Exists(resources[0].HashedPath), "Expected the hashed path '{0}' to exists", resources[0].HashedPath);
        }
    }
}