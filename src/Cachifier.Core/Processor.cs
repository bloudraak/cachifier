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
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Cachifier.Annotations;
    using Microsoft.Win32;

    /// <summary>
    ///     Represents a Cashifier processor
    /// </summary>
    public class Processor
    {
        private readonly string[] _exclusions =
        {
            "\\webforms\\",
            "\\msajax\\",
            "microsoft",
            "jquery",
            "\\obj\\",
            "\\bin\\",
            "_references.js",
            "modernizr",
        };

        private readonly Regex _exclusionsRegex;
        private readonly Regex _supportedExtensions;
        private string[] _extensions;

        /// <summary>
        ///     Create a new instance of <see cref="Processor" />
        /// </summary>
        public Processor()
        {
            _extensions = new[]
            {
                ".jpg",
                ".js",
                ".css",
                ".eot",
                ".svg",
                ".ttf",
                ".woff",
                ".png",
                ".gif",
                ".pdf",
                ".doc",
                ".flv",
                ".mov",
                ".mp4",
                ".mp3"
            };
            this._supportedExtensions = new Regex(string.Join("|", this._extensions.Select(Regex.Escape)),
                RegexOptions.IgnoreCase);
            this._exclusionsRegex = new Regex(string.Join("|", this._exclusions.Select(Regex.Escape)),
                RegexOptions.IgnoreCase);
        }

        /// <summary>
        ///     Process files
        /// </summary>
        /// <param name="projectDirectory">The project directory to which everything is relative to</param>
        /// <param name="resources">The resources</param>
        /// <param name="outputPath">The output path</param>
        public void Process([NotNull] string projectDirectory, [NotNull] Resource[] resources, string outputPath)
        {
            if (projectDirectory == null)
            {
                throw new ArgumentNullException("projectDirectory");
            }
            if (resources == null)
            {
                throw new ArgumentNullException("resources");
            }
            if (outputPath == null)
            {
                this.Process(projectDirectory, resources);
            }
            else
            {
                var path = Path.Combine(projectDirectory, outputPath);
                Directory.CreateDirectory(path);

                foreach (var resource in resources)
                {
                    var relativePath = this.GetRelativePath(resource.Path, projectDirectory);
                    var fullPath = Path.Combine(projectDirectory, outputPath, relativePath);
                    var directoryName = Path.GetDirectoryName(fullPath);

                    Directory.CreateDirectory(directoryName);
                    File.Copy(resource.Path, fullPath, true);
                    resource.Path = fullPath;
                }

                this.Process(path, resources);
            }
        }

        /// <summary>
        ///     /
        /// </summary>
        /// <param name="projectDirectory"></param>
        /// <param name="resources"></param>
        public void Process(string projectDirectory, Resource[] resources)
        {
            if (string.IsNullOrWhiteSpace(projectDirectory))
            {
                throw new InvalidOperationException("The project directory is required");
            }

            IHashifier hashifier = new Hashifier();
            IEncoder encoder = new Encoder();

            foreach (var resource in resources)
            {
                if (this._exclusionsRegex.IsMatch(resource.Path))
                {
                    Console.WriteLine("Excluding '{0}'", this.GetRelativePath(resource.Path, projectDirectory));
                    continue;
                }
                Console.WriteLine("Hashifying '{0}'", this.GetRelativePath(resource.Path, projectDirectory));
                var hash = hashifier.Hashify(resource.Path);
                var filename = encoder.Encode(hash);
                var directoryName = Path.GetDirectoryName(resource.Path);
                var extension = Path.GetExtension(resource.Path);
                resource.HashedPath = Path.Combine(directoryName, filename + extension);
            }

            foreach (var resource in resources)
            {
                if (string.IsNullOrWhiteSpace(resource.HashedPath))
                {
                    // we never generated a hash file
                    continue;
                }
                using (var source = File.OpenRead(resource.Path))
                {
                    using (var destination = File.Create(resource.HashedPath))
                    {
                        source.CopyTo(destination, 4096);
                    }
                }
            }

            var map = new Dictionary<string, string>();
            foreach (var resource in resources)
            {
                if (string.IsNullOrWhiteSpace(resource.HashedPath))
                {
                    // we never generated a hash file
                    continue;
                }

                var path = this.GetRelativePath(resource.Path, projectDirectory);
                var hashedPath = this.GetRelativePath(resource.HashedPath, projectDirectory);
                map.Add(path, hashedPath);
            }

            foreach (var resource in resources)
            {
                if (!this.IsTextResource(resource))
                {
                    // ignore anything that isn't text
                    continue;
                }

                var path = resource.HashedPath;
                var text = File.ReadAllText(path);
                foreach (var r in map)
                {
                    var originalFileName = r.Key.Replace(Path.DirectorySeparatorChar, '/');
                    var newFileName = r.Value.Replace(Path.DirectorySeparatorChar, '/');

                    text = text.Replace(originalFileName, newFileName);
                }
                File.WriteAllText(path, text);
            }
        }

        private bool IsTextResource(Resource resource)
        {
            var mimeType = this.GetMimeType(resource.Path);
            switch (mimeType)
            {
                case "text/css":
                case "text/text":
                case "text/javascript":
                case "application/x-javascript":
                    return true;
            }
            return false;
        }

        private string GetMimeType(string path)
        {
            string mimeType = "application/unknown";
            var extension = Path.GetExtension(path);
            if (string.IsNullOrWhiteSpace(extension))
            {
                return mimeType;
            }

            RegistryKey regKey = Registry.ClassesRoot.OpenSubKey(extension);
            if (regKey == null)
            {
                return mimeType;
            }

            object contentType = regKey.GetValue("Content Type");
            if (contentType == null)
            {
                return mimeType;
            }

            return contentType.ToString();
        }

        private string GetRelativePath(string path, string baseFolder)
        {
            var pathUri = new Uri(path);
            if (baseFolder[baseFolder.Length - 1] != Path.DirectorySeparatorChar)
            {
                baseFolder += Path.DirectorySeparatorChar;
            }
            var folderUri = new Uri(baseFolder);
            return
                Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri)
                    .ToString()
                    .Replace('/', Path.DirectorySeparatorChar));
        }

        /// <summary>
        ///     Process a folder
        /// </summary>
        /// <param name="path">The path</param>
        /// <param name="outputPath">The output path</param>
        public void Process(string path, string outputPath)
        {
            var resources = this.GetResources(path, outputPath);
            this.Process(path, resources.ToArray(), outputPath);
        }

        private List<Resource> GetResources(string path, string outputPath)
        {
            outputPath = Path.Combine(path, outputPath);
            var binPath = Path.Combine(path, "bin");
            var objPath = Path.Combine(path, "obj");
            var resources = new List<Resource>();
            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (file.StartsWith(outputPath, StringComparison.InvariantCultureIgnoreCase))
                {
                    // skip things in the output path
                    continue;
                }
                if (file.StartsWith(binPath, StringComparison.InvariantCultureIgnoreCase))
                {
                    // skip things in the bin path
                    continue;
                }
                if (file.StartsWith(objPath, StringComparison.InvariantCultureIgnoreCase))
                {
                    // skip things in the obj path
                    continue;
                }

                // We'll be ignoring any files that isn't static content.
                string extension = Path.GetExtension(file);
                if (string.IsNullOrWhiteSpace(extension))
                {
                    // We're not doing anything to files without extensions.
                    continue;
                }

                if (!this._supportedExtensions.IsMatch(extension))
                {
                    // it was requested that we exclude this extension by request.
                    continue;
                }
                var resource = new Resource();
                resource.Path = file;
                resources.Add(resource);
            }
            return resources;
        }
    }
}