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

namespace Cachifier.Build.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using Cachifier.Build.Tasks.Annotations;

    /// <summary>
    ///     Represents a resource collector
    /// </summary>
    public class ResourceCollector
    {
        private readonly string _projectDirectory;
        private readonly ResourceFilter _resourceFilter;

        /// <summary>
        ///     Initializes a new instance of the <see cref="T:System.Object" /> class.
        /// </summary>
        /// <exception cref="ArgumentNullException">resourceFilter or projectDirectory is null</exception>
        public ResourceCollector([NotNull] string projectDirectory, [NotNull] ResourceFilter resourceFilter)
        {
            if (projectDirectory == null)
            {
                throw new ArgumentNullException("projectDirectory");
            }
            if (resourceFilter == null)
            {
                throw new ArgumentNullException("resourceFilter");
            }

            this._projectDirectory = projectDirectory;
            this._resourceFilter = resourceFilter;
        }
        
        /// <summary>
        /// Verifies the never changing fields
        /// </summary>
        [ContractInvariantMethod]
        private void ContractInvariantMethod()
        {
            Contract.Invariant(this._projectDirectory != null);
            Contract.Invariant(this._resourceFilter != null);
        }

        /// <summary>
        ///     Collects embedded resources
        /// </summary>
        /// <param name="assemblyName">The assembly name</param>
        /// <param name="rootNamespace">The root name space of the assembly</param>
        /// <param name="files">The files that will be embedded in <see cref="assemblyName" /></param>
        /// <returns>An enumeration of resources</returns>
        public IEnumerable<Resource> CollectEmbeddedResources([NotNull] string assemblyName,
                                                              [NotNull] string rootNamespace,
                                                              [NotNull] IEnumerable<string> files)
        {
            if (assemblyName == null)
            {
                throw new ArgumentNullException("assemblyName");
            }
            if (rootNamespace == null)
            {
                throw new ArgumentNullException("rootNamespace");
            }
            if (files == null)
            {
                throw new ArgumentNullException("files");
            }

            foreach (var item in this._resourceFilter.Filter(files))
            {
                var relativePath = FileManager.GetRelativePath(item, this._projectDirectory);
                var name = rootNamespace + "." + relativePath.Replace(Path.DirectorySeparatorChar, '.');

                var resource = new Resource
                {
                    Assembly = assemblyName,
                    Path = item,
                    Name = name,
                    RelativePath = relativePath
                };
                yield return resource;
            }
        }

        /// <summary>
        ///     Returns a enumeration of content
        /// </summary>
        /// <param name="files">The static content</param>
        /// <returns>an enumeration of <see cref="Resource" /></returns>
        public IEnumerable<Resource> CollectContent(IEnumerable<string> files)
        {
            if (files == null)
            {
                throw new ArgumentNullException("files");
            }

            foreach (var item in this._resourceFilter.Filter(files))
            {
                var relativePath = FileManager.GetRelativePath(item, this._projectDirectory);
                var name = Path.GetFileName(item);
                var resource = new Resource
                {
                    Path = item,
                    Name = name,
                    RelativePath = relativePath
                };
                yield return resource;
            }
        }
    }
}