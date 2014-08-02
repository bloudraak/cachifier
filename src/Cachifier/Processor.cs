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
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using Cachifier.Build.Tasks;
    using Cachifier.Build.Tasks.Annotations;

    public class Processor
    {
        private readonly string _assemblyName;
        private readonly string _cdnBaseUri;
        private readonly CodeGenerator _codeGenerator;
        private readonly string[] _content;
        private readonly string[] _embeddedResources;
        private readonly Encoder _encoder;
        private readonly string[] _exclusions;
        private readonly ICollection<string> _extensions;
        private readonly bool _forceLowercase;
        private readonly Hashifier _hashifier;
        private readonly string _outputPath2;
        private readonly string _projectDirectory;
        private readonly ResourceNamingPolicy _resourceNamingPolicy;
        private readonly string _rootNamespace;
        private readonly string _staticMappingSourcePath;
        private ILogger _logger;

        public Processor([NotNull] string[] embeddedResources,
                         [NotNull] string[] content,
                         [NotNull] string[] exclusions,
                         [NotNull] string projectDirectory,
                         [NotNull] string assemblyName,
                         [NotNull] string rootNamespace,
                         [NotNull] IEnumerable<string> extensions,
                         [NotNull] string staticMappingSourcePath,
                         bool forceLowercase,
                         [NotNull] string outputPath2,
                         [NotNull] string cdnBaseUri)
        {
            if (embeddedResources == null)
            {
                throw new ArgumentNullException("embeddedResources");
            }
            if (content == null)
            {
                throw new ArgumentNullException("content");
            }
            if (exclusions == null)
            {
                throw new ArgumentNullException("exclusions");
            }
            if (projectDirectory == null)
            {
                throw new ArgumentNullException("projectDirectory");
            }
            if (assemblyName == null)
            {
                throw new ArgumentNullException("assemblyName");
            }
            if (rootNamespace == null)
            {
                throw new ArgumentNullException("rootNamespace");
            }
            if (extensions == null)
            {
                throw new ArgumentNullException("extensions");
            }
            if (staticMappingSourcePath == null)
            {
                throw new ArgumentNullException("staticMappingSourcePath");
            }
            if (outputPath2 == null)
            {
                throw new ArgumentNullException("outputPath2");
            }
            
            this._embeddedResources = embeddedResources;
            this._content = content;
            this._exclusions = exclusions;
            this._projectDirectory = projectDirectory;
            this._assemblyName = assemblyName;
            this._rootNamespace = rootNamespace;
            this._extensions = new List<string>(extensions);
            this._staticMappingSourcePath = staticMappingSourcePath;
            this._forceLowercase = forceLowercase;
            this._outputPath2 = outputPath2;
            this._cdnBaseUri = cdnBaseUri;
            this._logger = new NullLogger();
            this._hashifier = new Hashifier();
            this._encoder = new Encoder();
            this._resourceNamingPolicy = new ResourceNamingPolicy();
            this._codeGenerator = new CodeGenerator();
        }

        [NotNull]
        public ILogger Logger
        {
            get
            {
                Contract.Ensures(Contract.Result<ILogger>() != null);
                return this._logger;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                this._logger = value;
            }
        }

        public void Process()
        {
            var resources = new ResourceCollection();
            this.CollectResources(resources);
            this.HashifyResources(resources);
            this.UpdateHashifiedPaths(resources);
            this.CreateDirectories(resources);
            this.CopyFiles(resources);
            this.DeleteOrphanFiles(resources);
            this.GenerateScriptManagerMapping(resources);
        }

        private void GenerateScriptManagerMapping([NotNull] IEnumerable<Resource> resources)
        {
            if (resources == null)
            {
                throw new ArgumentNullException("resources");
            }

            if (string.IsNullOrWhiteSpace(this._staticMappingSourcePath))
            {
                return;
            }

            if (this._codeGenerator == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(this._staticMappingSourcePath))
            {
                this._codeGenerator.GenerateSourceMappingSource(resources,
                    this._rootNamespace,
                    this._staticMappingSourcePath,
                    this._forceLowercase,
                    this._outputPath2,
                    this._cdnBaseUri);
            }
        }

        private void DeleteOrphanFiles([NotNull] ResourceCollection resources)
        {
            if (resources == null)
            {
                throw new ArgumentNullException("resources");
            }
            var set1 = new HashSet<string>(resources.GetHashifiedPaths());
            var set2 = new HashSet<string>(this.GetFiles());
            set2.ExceptWith(set1);
            foreach (var s in set2)
            {
                this.Logger.Log(MessageImportance.High, string.Format("Deleting \"{0}\" because it is an orphan.", s));
                File.Delete(s);
            }
        }

        private void CopyFiles([NotNull] IEnumerable<Resource> resources)
        {
            if (resources == null)
            {
                throw new ArgumentNullException("resources");
            }
            foreach (var resource in resources)
            {
                var sourceFileName = resource.Path;
                var destFileName = resource.HashifiedPath;
                this.Logger.Log(MessageImportance.High,
                    string.Format("Copying directory \"{0}\" to \"{1}\".", sourceFileName, destFileName));
                File.Copy(sourceFileName, destFileName, true);
            }
        }

        private void CreateDirectories([NotNull] IEnumerable<Resource> resources)
        {
            if (resources == null)
            {
                throw new ArgumentNullException("resources");
            }
            var directories = resources.Where(item => item != null)
                .Select(item => item.HashifiedPath)
                .Select(Path.GetDirectoryName)
                .Where(item => item != null);

            foreach (var directory in directories)
            {
                if (directory == null)
                {
                    continue;
                }

                if (!Directory.Exists(directory))
                {
                    this.Logger.Log(MessageImportance.High,
                        string.Format("Creating directory \"{0}\" because it does not exist.", directory));
                    Directory.CreateDirectory(directory);
                }
            }
        }

        private void UpdateHashifiedPaths([NotNull] IEnumerable<Resource> resources)
        {
            if (resources == null)
            {
                throw new ArgumentNullException("resources");
            }
            foreach (var resource in resources)
            {
                Contract.Assume(resource != null);
                this.Logger.Log(MessageImportance.High,
                    string.Format("Computing the new filename of \"{0}\".", resource.Path));
                var directoryName = Path.GetDirectoryName(resource.RelativePath);
                var outputPath = this._outputPath2;
                var hashifiedFileName = this._resourceNamingPolicy.GetFileName(resource);

                if (string.IsNullOrWhiteSpace(directoryName))
                {
                    if (outputPath != null)
                    {
                        resource.HashifiedPath = Path.Combine(this._projectDirectory, outputPath, hashifiedFileName);
                    }
                    else
                    {
                        resource.HashifiedPath = Path.Combine(this._projectDirectory, hashifiedFileName);
                    }
                }
                else
                {
                    if (outputPath != null)
                    {
                        resource.HashifiedPath = Path.Combine(this._projectDirectory,
                            outputPath,
                            directoryName,
                            hashifiedFileName);
                    }
                    else
                    {
                        resource.HashifiedPath = Path.Combine(this._projectDirectory, directoryName, hashifiedFileName);
                    }
                }

                resource.RelativeHashifiedPath = FileManager.GetRelativePath(resource.HashifiedPath,
                    this._projectDirectory);
            }
        }

        private void HashifyResources([NotNull] IEnumerable<Resource> resources)
        {
            if (resources == null)
            {
                throw new ArgumentNullException("resources");
            }
            foreach (var resource in resources)
            {
                this.Logger.Log(MessageImportance.High,
                    string.Format("Computing the SHA256 hash of \"{0}\".", resource.Path));
                var contentHash = this._hashifier.Hashify(resource.Path);

                var bytes = contentHash;
                if (bytes != null && bytes.Length > 0)
                {
                    this.Logger.Log(MessageImportance.High,
                        string.Format("Encoding the SHA256 hash of \"{0}\" [{1}].", resource.Path, bytes.ToString(",")));
                }
                resource.ContentHash = this._encoder.Encode(contentHash);
            }
        }

        private void CollectResources([NotNull] ResourceCollection resources)
        {
            if (resources == null)
            {
                throw new ArgumentNullException("resources");
            }
            this.Logger.Log(MessageImportance.High, "Collecting static resources from \"{0}\"", this._projectDirectory);
            var resourceFilter = new ResourceFilter(this._extensions, this._exclusions);
            var collector = new ResourceCollector(this._projectDirectory, resourceFilter);

            resources.AddRange(collector.CollectEmbeddedResources(this._assemblyName,
                this._rootNamespace,
                this._embeddedResources));

            resources.AddRange(collector.CollectContent(this._content));
            this.Logger.Log(MessageImportance.High,
                string.Format("Collected \"{0}\" static resources.", resources.Count));
        }
        
        [NotNull]
        private IEnumerable<string> GetFiles()
        {
            Contract.Ensures(Contract.Result<IEnumerable<string>>() != null);

            var path = Path.Combine(this._projectDirectory, this._outputPath2);
            return Directory.EnumerateFiles(path,
                "*",
                SearchOption.AllDirectories)
                .Select(item => item.ToLower());
        }

        [ContractInvariantMethod]
        private void ContractInvariantMethod()
        {
            Contract.Invariant(this._outputPath2 != null);
            Contract.Invariant(this._projectDirectory != null);
            Contract.Invariant(this._logger != null);
            Contract.Invariant(this._rootNamespace != null);
        }
    }
}