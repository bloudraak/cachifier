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

        private string _tempPath;

        /// <summary>
        ///     Verifies that the static web resources were processed correctly.
        /// </summary>
        /// <remarks>
        ///     This tets verifies that
        ///     <ul>
        ///         <li>The filenames are renamed accordingly</li>
        ///         <li>The internal references are fixed</li>
        ///     </ul>
        /// </remarks>
        [Test]
        public void Process()
        {
            // Arrange
            var imagePath = Path.Combine(this._tempPath, "Image1.jpg");
            File.WriteAllBytes(imagePath, Resources.Image1);
            var expectedHashedImagePath = Path.Combine(this._tempPath,
                "kna98mczkhzth33v1zzy10in814243cflqzjf7dlezrgmcddg1.jpg");

            var stylesheetPath = Path.Combine(this._tempPath, "Styles.css");
            File.WriteAllText(stylesheetPath, ".image1 {background-image:url('Image1.jpg');}");
            var expectedHashedStylesheetPath = Path.Combine(this._tempPath,
                "vgtnhq3mduz5kf356dr0ohej9uxzjgu1eykdcfppzabso6obj.css");

            var target = new Processor();
            target.ProjectDirectory = _tempPath;

            var resources = new[]
            {
                new Resource
                {
                    Path = imagePath
                },
                new Resource
                {
                    Path = stylesheetPath
                }
            };

            // Act
            target.Process(resources);

            // Assert
            Assert.AreEqual(expectedHashedImagePath, resources[0].HashedPath, "The hashed path is incorrect");
            Assert.IsTrue(File.Exists(resources[0].HashedPath),
                "Expected the hashed path '{0}' to exists",
                resources[0].HashedPath);

            Assert.AreEqual(expectedHashedStylesheetPath, resources[1].HashedPath, "The hashed path is incorrect");
            Assert.IsTrue(File.Exists(resources[1].HashedPath),
                "Expected the hashed path '{0}' to exists",
                resources[0].HashedPath);

            var stylesheetContents = File.ReadAllText(resources[1].HashedPath);
            Console.WriteLine("=== {0} ===", resources[1].HashedPath);
            Console.WriteLine(stylesheetContents);
            Console.WriteLine("======");
            var exists =
                stylesheetContents.Contains(".image1 {background-image:url('"
                                            + Path.GetFileName(resources[0].HashedPath + "');}"));
            Assert.IsTrue(exists,
                "Expects the hashed file '{0}' to have a reference to '{1}' rather than 'Image1.jpg'",
                resources[1].HashedPath,
                resources[0].HashedPath);
        }

        /// <summary>
        ///     Verifies that the static web resources were processed correctly.
        /// </summary>
        /// <remarks>
        ///     This tets verifies that
        ///     <ul>
        ///         <li>The filenames are renamed accordingly</li>
        ///         <li>The internal references are fixed</li>
        ///     </ul>
        /// </remarks>
        [Test]
        public void ProcessWithSameFileNames()
        {
            // Arrange
            var imageXPath = Path.Combine(this._tempPath, "x", "Image1.jpg");
            Directory.CreateDirectory(Path.GetDirectoryName(imageXPath));
            File.WriteAllBytes(imageXPath, Resources.Image1);
            var expectedHashedImageXPath = Path.Combine(this._tempPath, "x",
                "kna98mczkhzth33v1zzy10in814243cflqzjf7dlezrgmcddg1.jpg");

            var imageYPath = Path.Combine(this._tempPath, "y", "Image1.jpg");
            Directory.CreateDirectory(Path.GetDirectoryName(imageYPath));
            File.WriteAllBytes(imageYPath, Resources.Image2);
            var expectedHashedImageYPath = Path.Combine(this._tempPath, "y",
                "zwlj9fjh5thm9l15nykdf1aapr3i9rtq1m40u8ppvyecxrxmw1.jpg");

            var stylesheetPath = Path.Combine(this._tempPath, "Styles.css");
            File.WriteAllText(stylesheetPath, ".image1 {background-image:url('x/Image1.jpg');}\n\n.image2 {background-image:url('y/Image1.jpg');}");
            var expectedHashedStylesheetPath = Path.Combine(this._tempPath,
                "ldv77axkmmch62vr1i239j3its23z9xhvfr0gi8pnm4c2qcs02.css");

            var target = new Processor();
            target.ProjectDirectory = _tempPath;

            var resources = new[]
            {
                new Resource
                {
                    Path = imageXPath
                },
                new Resource
                {
                    Path = imageYPath
                },
                new Resource
                {
                    Path = stylesheetPath
                }
            };

            // Act
            target.Process(resources);

            // Assert
            Assert.AreEqual(expectedHashedImageXPath, resources[0].HashedPath, "The hashed path is incorrect");
            Assert.IsTrue(File.Exists(resources[0].HashedPath),
                "Expected the hashed path '{0}' to exists",
                resources[0].HashedPath);

            Assert.AreEqual(expectedHashedImageYPath, resources[1].HashedPath, "The hashed path is incorrect");
            Assert.IsTrue(File.Exists(resources[1].HashedPath),
                "Expected the hashed path '{0}' to exists",
                resources[1].HashedPath);

            Assert.AreEqual(expectedHashedStylesheetPath, resources[2].HashedPath, "The hashed path is incorrect");
            Assert.IsTrue(File.Exists(resources[2].HashedPath),
                "Expected the hashed path '{0}' to exists",
                resources[2].HashedPath);

            var stylesheetContents = File.ReadAllText(resources[2].HashedPath);
            Console.WriteLine("=== {0} ===", resources[2].HashedPath);
            Console.WriteLine(stylesheetContents);
            Console.WriteLine("======");
            var exists =
                stylesheetContents.Contains(".image1 {background-image:url('x/"
                                            + Path.GetFileName(resources[0].HashedPath + "');}"));
            Assert.IsTrue(exists,
                "Expects the hashed file '{0}' to have a reference to '{1}' rather than 'x/Image1.jpg'",
                resources[2].HashedPath,
                resources[0].HashedPath);

            exists = stylesheetContents.Contains(".image2 {background-image:url('y/"+ Path.GetFileName(resources[1].HashedPath + "');}"));
            Assert.IsTrue(exists,
                "Expects the hashed file '{0}' to have a reference to '{1}' rather than 'y/Image1.jpg'",
                resources[2].HashedPath,
                resources[1].HashedPath);
        }

        /// <summary>
        ///     Verifies that the static web resources were processed correctly.
        /// </summary>
        /// <remarks>
        ///     This tets verifies that
        ///     <ul>
        ///         <li>The filenames are renamed accordingly</li>
        ///         <li>The internal references are fixed</li>
        ///     </ul>
        /// </remarks>
        [Test]
        public void ProcessWithRelativeReferences()
        {
            // Arrange
            var imageXPath = Path.Combine(this._tempPath, "x", "Image1.jpg");
            Directory.CreateDirectory(Path.GetDirectoryName(imageXPath));
            File.WriteAllBytes(imageXPath, Resources.Image1);
            var expectedHashedImageXPath = Path.Combine(this._tempPath, "x",
                "kna98mczkhzth33v1zzy10in814243cflqzjf7dlezrgmcddg1.jpg");

            var imageYPath = Path.Combine(this._tempPath, "y", "Image1.jpg");
            Directory.CreateDirectory(Path.GetDirectoryName(imageYPath));
            File.WriteAllBytes(imageYPath, Resources.Image2);
            var expectedHashedImageYPath = Path.Combine(this._tempPath, "y",
                "zwlj9fjh5thm9l15nykdf1aapr3i9rtq1m40u8ppvyecxrxmw1.jpg");

            var stylesheetPath = Path.Combine(this._tempPath, "z", "Styles.css");
            Directory.CreateDirectory(Path.GetDirectoryName(stylesheetPath));
            File.WriteAllText(stylesheetPath, ".image1 {background-image:url('../x/Image1.jpg');}\n\n.image2 {background-image:url('../y/Image1.jpg');}");
            var expectedHashedStylesheetPath = Path.Combine(this._tempPath, "z",
                "valwumxgedp6r7aoas4zwv9o60qljex9orjzc8e1dr5itb9bf1.css");

            var target = new Processor();
            target.ProjectDirectory = _tempPath;

            var resources = new[]
            {
                new Resource
                {
                    Path = imageXPath
                },
                new Resource
                {
                    Path = imageYPath
                },
                new Resource
                {
                    Path = stylesheetPath
                }
            };

            // Act
            target.Process(resources);

            // Assert
            Assert.AreEqual(expectedHashedImageXPath, resources[0].HashedPath, "The hashed path is incorrect");
            Assert.IsTrue(File.Exists(resources[0].HashedPath),
                "Expected the hashed path '{0}' to exists",
                resources[0].HashedPath);

            Assert.AreEqual(expectedHashedImageYPath, resources[1].HashedPath, "The hashed path is incorrect");
            Assert.IsTrue(File.Exists(resources[1].HashedPath),
                "Expected the hashed path '{0}' to exists",
                resources[1].HashedPath);

            Assert.AreEqual(expectedHashedStylesheetPath, resources[2].HashedPath, "The hashed path is incorrect");
            Assert.IsTrue(File.Exists(resources[2].HashedPath),
                "Expected the hashed path '{0}' to exists",
                resources[2].HashedPath);

            var stylesheetContents = File.ReadAllText(resources[2].HashedPath);
            Console.WriteLine("=== {0} ===", resources[2].HashedPath);
            Console.WriteLine(stylesheetContents);
            Console.WriteLine("======");
            var exists =
                stylesheetContents.Contains(".image1 {background-image:url('../x/"
                                            + Path.GetFileName(resources[0].HashedPath + "');}"));
            Assert.IsTrue(exists,
                "Expects the hashed file '{0}' to have a reference to '../x/{1}' rather than '../x/Image1.jpg'",
                resources[2].HashedPath,
                Path.GetFileName(resources[0].HashedPath));

            exists = stylesheetContents.Contains(".image2 {background-image:url('../y/" + Path.GetFileName(resources[1].HashedPath + "');}"));
            Assert.IsTrue(exists,
                "Expects the hashed file '{0}' to have a reference to '../y/{1}' rather than '../y/Image1.jpg'",
                resources[2].HashedPath,
                Path.GetFileName(resources[1].HashedPath));
        }
    }
}