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
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using Cachifier.Build.Tasks;

    public class Processor
    {
        public ILogger Logger
        {
            get;
            set;
        }

        private readonly string _assemblyName;
        private readonly string _cdnBaseUri;
        private readonly string[] _content;
        private readonly string[] _embeddedResources;
        private readonly ICollection<string> _extensions;
        private readonly bool _forceLowercase;
        private readonly string _outputPath2;
        private readonly string _projectDirectory;
        private readonly string _rootNamespace;
        private readonly string _staticMappingSourcePath;

        public Processor(string[] embeddedResources,
                         string[] content,
                         string projectDirectory,
                         string assemblyName,
                         string rootNamespace,
                         IEnumerable<string> extensions,
                         string staticMappingSourcePath,
                         bool forceLowercase,
                         string outputPath2,
                         string cdnBaseUri)
        {
            this._embeddedResources = embeddedResources;
            this._content = content;
            this._projectDirectory = projectDirectory;
            this._assemblyName = assemblyName;
            this._rootNamespace = rootNamespace;
            this._extensions = new List<string>(extensions);
            this._staticMappingSourcePath = staticMappingSourcePath;
            this._forceLowercase = forceLowercase;
            this._outputPath2 = outputPath2;
            this._cdnBaseUri = cdnBaseUri;
            this.Logger = new NullLogger();
        }

        public string[] EmbeddedResources
        {
            get
            {
                return this._embeddedResources;
            }
        }

        public string[] Content
        {
            get
            {
                return this._content;
            }
        }

        public string ProjectDirectory
        {
            get
            {
                return this._projectDirectory;
            }
        }

        public string AssemblyName
        {
            get
            {
                return this._assemblyName;
            }
        }

        public string RootNamespace
        {
            get
            {
                return this._rootNamespace;
            }
        }

        public IEnumerable<string> Extensions
        {
            get
            {
                return this._extensions;
            }
        }

        public string StaticMappingSourcePath
        {
            get
            {
                return this._staticMappingSourcePath;
            }
        }

        public bool ForceLowercase
        {
            get
            {
                return this._forceLowercase;
            }
        }

        public string OutputPath2
        {
            get
            {
                return this._outputPath2;
            }
        }

        public string CdnBaseUri
        {
            get
            {
                return this._cdnBaseUri;
            }
        }

        public void Process()
        {
            Contract.Requires(((Cachifier.Processor)this).Logger != null);
            var hashifier = new Hashifier();
            var encoder = new Encoder();

            // 
            // Resources to process
            //
            var resources = new ResourceCollection();

            //
            // Collect Resources
            //
            Logger.Log(MessageImportance.High, "Collecting static resources from \"{0}\"", _projectDirectory);
            var resourceFilter = new ResourceFilter(this.Extensions);
            var collector = new ResourceCollector(this.ProjectDirectory, resourceFilter);

            resources.AddRange(collector.CollectEmbeddedResources(this.AssemblyName,
                this.RootNamespace,
                this.EmbeddedResources));

            resources.AddRange(collector.CollectContent(this.Content));
            Logger.Log(MessageImportance.High, string.Format("Collected \"{0}\" static resources.", resources.Count));

            //
            // Hashify Resources
            //
            foreach (var resource in resources)
            {
                Logger.Log(MessageImportance.High, string.Format("Computing the SHA256 hash of \"{0}\".", resource.Path));
                var contentHash = hashifier.Hashify(resource.Path);

                var bytes = contentHash;
                if (bytes != null && bytes.Length > 0)
                {
                    Logger.Log(MessageImportance.High, string.Format("Encoding the SHA256 hash of \"{0}\" [{1}].", resource.Path, bytes.ToString(",")));
                }
                resource.ContentHash = encoder.Encode(contentHash);
            }

            //
            // Update Paths
            //
            var resourceNamingPolicy = new ResourceNamingPolicy();
            foreach (var resource in resources)
            {
                Contract.Assume(resource != null);
                Logger.Log(MessageImportance.High, string.Format("Computing the new filename of \"{0}\".", resource.Path));
                var directoryName = Path.GetDirectoryName(resource.RelativePath);
                var outputPath = this.OutputPath2;
                var hashifiedFileName = resourceNamingPolicy.GetFileName(resource);

                Contract.Assume(this.ProjectDirectory != null);
                if (string.IsNullOrWhiteSpace(directoryName))
                {
                    if (outputPath != null)
                    {
                        resource.HashifiedPath = Path.Combine(this.ProjectDirectory, outputPath, hashifiedFileName);
                    }
                    else
                    {
                        resource.HashifiedPath = Path.Combine(this.ProjectDirectory, hashifiedFileName);
                    }
                }
                else
                {
                    if (outputPath != null)
                    {
                        resource.HashifiedPath = Path.Combine(this.ProjectDirectory,
                            outputPath,
                            directoryName,
                            hashifiedFileName);
                    }
                    else
                    {
                        resource.HashifiedPath = Path.Combine(this.ProjectDirectory, directoryName, hashifiedFileName);
                    }
                }

                resource.RelativeHashifiedPath = FileManager.GetRelativePath(resource.HashifiedPath,
                    this.ProjectDirectory);
            }

            //
            // Create the directories
            // 
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
                    Logger.Log(MessageImportance.High, string.Format("Creating directory \"{0}\" because it does not exist.", directory));
                    Directory.CreateDirectory(directory);
                }
            }

            //
            // Copy files
            //
            foreach (var resource in resources)
            {
                var sourceFileName = resource.Path;
                var destFileName = resource.HashifiedPath;
                Logger.Log(MessageImportance.High, string.Format("Copying directory \"{0}\" to \"{1}\".", sourceFileName, destFileName));
                File.Copy(sourceFileName, destFileName, true);
            }

            //
            // Delete Orphan Files
            //
            var set1 = new HashSet<string>(resources.GetHashifiedPaths());
            var set2 = new HashSet<string>(this.GetFiles());
            set2.ExceptWith(set1);
            foreach (var s in set2)
            {
                Logger.Log(MessageImportance.High, string.Format("Deleting \"{0}\" because it is an orphan.", s));
                File.Delete(s);
            }

            //
            // Generate Script Mapping Source
            // 
            if (!string.IsNullOrWhiteSpace(this.StaticMappingSourcePath))
            {
                new CodeGenerator().GenerateSourceMappingSource(resources,
                    this.RootNamespace,
                    this.StaticMappingSourcePath,
                    this.ForceLowercase,
                    this.OutputPath2,
                    this.CdnBaseUri);
            }
        }

        private IEnumerable<string> GetFiles()
        {
            Contract.Requires(((Cachifier.Processor) this).ProjectDirectory != null);
            Contract.Requires(this.OutputPath2 != null);

            return Directory.EnumerateFiles(Path.Combine(this.ProjectDirectory, this.OutputPath2),
                "*",
                SearchOption.AllDirectories)
                .Select(item => item.ToLower());
        }
    }
}