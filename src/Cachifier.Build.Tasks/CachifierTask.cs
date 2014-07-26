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
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using Cachifier.Build.Tasks.Annotations;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Encoder = Cachifier.Encoder;

    /// <summary>
    ///     Represents an Microsoft Build task for Cachifier
    /// </summary>
    [PublicAPI]
    public class CachifierTask : Task
    {
        private IEncoder _encoder;
        private string _extensionPattern;
        private Regex _extensionRegex;
        private IHashifier _hashifier;

        /// <summary>
        ///     Gets the content files
        /// </summary>
        [Required]
        [PublicAPI]
        public ITaskItem[] Content
        {
            get;
            set;
        }

        /// <summary>
        ///     Gets the embedded resources in the project
        /// </summary>
        [Required]
        [PublicAPI]
        public ITaskItem[] EmbeddedResources
        {
            get;
            set;
        }

        [Required]
        [PublicAPI]
        public ITaskItem[] StaticExtensions
        {
            get;
            set;
        }

        /// <summary>
        ///     Gets the output folders
        /// </summary>
        [Required]
        [PublicAPI]
        public string OutputPath
        {
            get;
            set;
        }

        /// <summary>
        ///     Gets the items the output items
        /// </summary>
        [Output]
        [PublicAPI]
        public ITaskItem[] OutputItems
        {
            get;
            private set;
        }

        [Required]
        [PublicAPI]
        public string ProjectDirectory
        {
            get;
            set;
        }

        [PublicAPI]
        public string ManifestPath
        {
            get;
            set;
        }

        [PublicAPI]
        public string StaticMappingSourcePath
        {
            get;
            set;
        }

        public string RootNamespace
        {
            get;
            set;
        }

        public string AssemblyName
        {
            get;
            set;
        }

        public string CdnBaseUri
        {
            get;
            set;
        }

        public bool ForceLowercase
        {
            get;
            set;
        }

        [DefaultValue(false)]
        [PublicAPI]
        public bool Override
        {
            get;
            set;
        }

        /// <summary>
        ///     When overridden in a derived class, executes the task.
        /// </summary>
        /// <returns>
        ///     true if the task successfully executed; otherwise, false.
        /// </returns>
        public override bool Execute()
        {
            try
            {
                if (this.Content.Length == 0)
                {
                    var message = string.Format("There are no content items to process");
                    var args = new BuildMessageEventArgs(message,
                        string.Empty,
                        "CachifierNoContent",
                        MessageImportance.Low);
                    this.BuildEngine.LogMessageEvent(args);
                }

                var resourcesElement = new XElement("resources");
                this._extensionPattern = string.Join("|", this.StaticExtensions.Select(s => Regex.Escape(s.ItemSpec)));
                this._extensionRegex = new Regex(this._extensionPattern,
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
                this._hashifier = new Hashifier();
                this._encoder = new Encoder();
                var outputItems = new List<ITaskItem>();
                var outputDirectoryName = Path.Combine(this.ProjectDirectory, this.OutputPath);
                var filesToDelete = GetFilesToDelete(outputDirectoryName);
                var files = new HashSet<string>();
                var mapping = new Dictionary<string, string>();
               
                var resources = new ResourceCollection();
                
                foreach (var contentFile in this.EmbeddedResources)
                {
                    this.ProcessTaskItem(contentFile, filesToDelete, mapping, outputDirectoryName, files, outputItems, true, resourcesElement, resources);
                }

                foreach (var contentFile in this.Content)
                {
                    this.ProcessTaskItem(contentFile, filesToDelete, mapping, outputDirectoryName, files, outputItems, false, resourcesElement, resources);
                }

                if (Directory.Exists(outputDirectoryName))
                {
                    foreach (var file in filesToDelete)
                    {
                        this.Log(MessageImportance.Low,
                            "Deleting file \"{0}\" because it is no longer referenced by the project.",
                            file);
                        File.Delete(file);
                    }
                    this.DeleteEmptyDirectories(outputDirectoryName);
                }
                this.OutputItems = outputItems.ToArray();

                foreach (var resource in resources)
                {
                    this.Log(MessageImportance.Normal, "Static Resource: {0}", resource);
                }

                foreach (var file in files)
                {
                    var relativePath = Processor.GetRelativePath(file, this.ProjectDirectory);

                    if (IsBinary(file))
                    {
                        this.Log(MessageImportance.Low,
                            "Skipping file \"{0}\" because it is not a static resource.",
                            relativePath);
                        continue;
                    }

                    var originalText = File.ReadAllText(file);
                    var text = File.ReadAllText(file);
                    foreach (var pair in mapping)
                    {
                        var oldValue = pair.Key.Replace(Path.DirectorySeparatorChar, '/');
                        var newValue = pair.Value.Replace(Path.DirectorySeparatorChar, '/');
                        text = text.Replace(oldValue, newValue);
                    }
                    if (!originalText.Equals(text))
                    {
                        File.WriteAllText(file, text);
                        this.Log(MessageImportance.Low,
                            "Updating file \"{0}\" since references to other static resources has changed.",
                            relativePath);
                    }
                    else
                    {
                        this.Log(MessageImportance.Low,
                            "Skipping file \"{0}\" since references to other static resources has not changed.",
                            relativePath);
                    }
                }

                if (ManifestPath != null)
                {
                    var manifest = new XDocument(resourcesElement);
                    manifest.Save(this.ManifestPath);
                }

                if (!string.IsNullOrWhiteSpace(this.StaticMappingSourcePath))
                {
                    this.GenerateSourceMappingSource(resources);
                }
                else
                {
                    this.Log(MessageImportance.Low, "Skipping the generation of ScriptResourceMapping in code, since no the property StaticMappingSourcePath was not set");
                }

                return true;
            }
            catch (Exception e)
            {
                this.Log(MessageImportance.High, "Exception: {0}", e);
                return false;
            }
        }

        private void GenerateSourceMappingSource(ResourceCollection resources)
        {
            using (var streamWriter = new StreamWriter(this.StaticMappingSourcePath))
            {
                using (var writer = new IndentedTextWriter(streamWriter))
                {
                    writer.WriteLine("using System;");
                    writer.WriteLine("using System.IO;");
                    writer.WriteLine("using System.Configuration;");
                    writer.WriteLine("using System.Web;");
                    writer.WriteLine("using System.Web.UI;");
                    writer.WriteLine("using System.Web.Configuration;");
                    writer.WriteLine("using {0};", this.RootNamespace);
                    writer.WriteLine();
                    writer.WriteLine("[assembly: PreApplicationStartMethod(typeof (MapScriptResources), \"Start\")]");
                    writer.WriteLine();
                    writer.WriteLine("namespace {0}", this.RootNamespace);
                    writer.WriteLine("{");
                    writer.Indent++;
                    writer.WriteLine();
                    writer.WriteLine("public static partial class MapScriptResources");
                    writer.WriteLine("{");
                    writer.WriteLine();
                    writer.Indent++;
                    writer.WriteLine("public static void Start()");
                    writer.WriteLine("{");
                    writer.Indent++;
                    writer.WriteLine("ScriptResourceDefinition definition = null;");
                    writer.WriteLine("var assembly = typeof(MapScriptResources).Assembly;");
                    foreach (var resource in resources)
                    {
                        var assemblyName = "assembly";
                        if (!resource.IsEmbedded)
                        {
                            assemblyName = "null";
                        }

                        writer.WriteLine();
                        writer.WriteLine("//");
                        writer.WriteLine("// Name: {0}", resource.Name);
                        writer.WriteLine("// Assembly: {0}", resource.Assembly);
                        writer.WriteLine("// IsEmbedded: {0}", resource.IsEmbedded);
                        writer.WriteLine("// RelativeHashifiedPath: {0}", resource.RelativeHashifiedPath);
                        writer.WriteLine("// RelativePath: {0}", resource.RelativePath);
                        writer.WriteLine("//");

                        writer.WriteLine("definition = ScriptManager.ScriptResourceMapping.GetDefinition(\"{0}\", {1});", resource.Name, assemblyName);
                        writer.WriteLine("if(definition == null)");
                        writer.WriteLine("{");
                        writer.Indent++;
                        writer.WriteLine("definition = new ScriptResourceDefinition();");
                        
                        if (!resource.IsEmbedded)
                        {
                            var relativeUri = resource.RelativePath.Replace('\\', '/');
                            if (this.ForceLowercase)
                            {
                                relativeUri = relativeUri.ToLowerInvariant();
                            }
                            writer.WriteLine("if(IsDebuggingEnabled)");
                            writer.WriteLine("{");
                            writer.Indent++;
                            writer.WriteLine("definition.Path = \"~/{0}\";", relativeUri);
                            writer.WriteLine("definition.DebugPath = \"~/{0}\";", relativeUri);
                            writer.Indent--;
                            writer.WriteLine("}");
                            writer.WriteLine("else");
                            writer.WriteLine("{");
                            writer.Indent++;
                            relativeUri = resource.RelativeHashifiedPath.Replace('\\', '/');
                            if (this.ForceLowercase)
                            {
                                relativeUri = relativeUri.ToLowerInvariant();
                            }
                            writer.WriteLine("definition.Path = \"~/{1}/{0}\";", relativeUri, this.OutputPath);
                            writer.WriteLine("definition.DebugPath = \"~/{1}/{0}\";", relativeUri, this.OutputPath);
                            writer.Indent--;
                            writer.WriteLine("}");
                        }
                        else
                        {
                            writer.WriteLine("definition.ResourceAssembly = assembly;");
                            writer.WriteLine("definition.ResourceName = \"{0}\";", resource.Name);
                        }
                        var uri = this.GetCdnUri(resource);
                        writer.WriteLine("definition.CdnPath = \"{0}\";", uri);
                        writer.WriteLine("definition.CdnDebugPath = \"{0}\";", uri);
                        writer.WriteLine("definition.CdnSupportsSecureConnection = true;");
                        writer.WriteLine("ScriptManager.ScriptResourceMapping.AddDefinition(\"{0}\", {1}, definition);", resource.Name, assemblyName);
                        writer.WriteLine();
                        
                        writer.Indent--;
                        writer.WriteLine("}");
                    }
                    writer.Indent--;
                    writer.WriteLine("}");
                    writer.WriteLine();
                    writer.WriteLine("private static bool IsDebuggingEnabled");
                    writer.WriteLine("{");
                    writer.Indent++;
                    writer.WriteLine("get");
                    writer.WriteLine("{");
                    writer.Indent++;
                    writer.WriteLine("var compilationSection = (CompilationSection) ConfigurationManager.GetSection(\"system.web/compilation\");");
                    writer.WriteLine("return compilationSection.Debug;");
                    writer.Indent--;
                    writer.WriteLine("}");
                    writer.Indent--;
                    writer.WriteLine("}");
                    writer.Indent--;
                    writer.WriteLine("}");
                    writer.Indent--;
                    writer.WriteLine("}");
                }
            }
        }

        private Uri GetCdnUri(Resource resource)
        {
            var relativeUri = resource.RelativeHashifiedPath.Replace('\\', '/');
            if (ForceLowercase)
            {
                relativeUri = relativeUri.ToLowerInvariant();
            }
            var baseUri = new Uri(this.CdnBaseUri, UriKind.Absolute);
            var uri = new Uri(baseUri, relativeUri);
            return uri;
        }

        private static HashSet<string> GetFilesToDelete(string outputDirectoryName)
        {
            HashSet<string> filesToDelete;
            if (Directory.Exists(outputDirectoryName))
            {
                var items = Directory.EnumerateFiles(outputDirectoryName, "*", SearchOption.AllDirectories);
                filesToDelete = new HashSet<string>(items);
            }
            else
            {
                filesToDelete = new HashSet<string>();
            }
            return filesToDelete;
        }

        private static bool IsBinary(string file)
        {
            bool skip = true;
            switch (Path.GetExtension(file))
            {
                case ".css":
                case ".js":
                    skip = false;
                    break;
            }
            return skip;
        }

        private void ProcessTaskItem(ITaskItem contentFile, ICollection<string> filesToDelete, IDictionary<string, string> mapping, string outputDirectoryName, ICollection<string> files, ICollection<ITaskItem> outputItems, bool IsEmbeddedResource, XElement resources, ResourceCollection resourceCollection)
        {
            var path = contentFile.ItemSpec;
            var extension = Path.GetExtension(path);
            if (extension == null)
            {
                this.Log(MessageImportance.Low,
                    "Skipping '{0}' because it doesn't represent a static resource.",
                    path);
                return;
            }

            if (!this._extensionRegex.IsMatch(extension))
            {
                this.Log(MessageImportance.Low,
                    "Skipping '{0}' because it doesn't represent a static resource.",
                    path);
                return;
            }

            var resourceElement = new XElement("resource");
            resources.Add(resourceElement);

            var fullPath = contentFile.GetMetadata("FullPath");
            var hashValue = this._hashifier.Hashify(fullPath);
            var encodedHashValue = this._encoder.Encode(hashValue);
            var relativePath = Processor.GetRelativePath(fullPath, this.ProjectDirectory);
            
            var hashedFileName = Path.GetFileNameWithoutExtension(fullPath) + "," + encodedHashValue
                                 + Path.GetExtension(fullPath);
            var outputPath = Path.Combine(this.ProjectDirectory, this.OutputPath, relativePath);
            outputPath = Path.Combine(Path.GetDirectoryName(outputPath), hashedFileName);
            var relativeOutputPath = Processor.GetRelativePath(outputPath, this.ProjectDirectory);

            var directoryName = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(directoryName))
            {
                this.Log(MessageImportance.Low, "Creating directory '{0}'...", directoryName);
                Directory.CreateDirectory(directoryName);
            }

            if (File.Exists(outputPath))
            {
                var targetWrittenTime = File.GetLastWriteTimeUtc(outputPath);
                var sourceWrittenTime = File.GetLastWriteTimeUtc(fullPath);

                if (this.Override || sourceWrittenTime.CompareTo(targetWrittenTime) > 0)
                {
                    // Copying file from "obj\Debug\Sample.WebApplication3.dll" to "bin\Sample.WebApplication3.dll".
                    this.Log(MessageImportance.Low,
                        "Copying file from \"{0}\" to \"{1}\".",
                        Processor.GetRelativePath(fullPath, this.ProjectDirectory),
                        relativeOutputPath);
                    File.Copy(fullPath, outputPath, true);
                }
                else
                {
                    //Skipping target "GenerateTargetFrameworkMonikerAttribute" because all output files are up-to-date with respect to the input files.
                    this.Log(MessageImportance.Low,
                        "Skipping file \"{0}\" because all output files are up-to-date with respect to the input files.",
                        relativeOutputPath);
                }
            }
            else
            {
                // Deleting file "S:\CachifierSamples\src\Sample.WebApplication3\bin\WebGrease.dll".
                // Copying file from "obj\Debug\Sample.WebApplication3.dll" to "bin\Sample.WebApplication3.dll".
                this.Log(MessageImportance.Low,
                    "Copying file from \"{0}\" to \"{1}\".",
                    Processor.GetRelativePath(fullPath, this.ProjectDirectory),
                    relativeOutputPath);
                File.Copy(fullPath, outputPath, true);
            }

            filesToDelete.Remove(outputPath);

            mapping.Add(relativePath, Processor.GetRelativePath(outputPath, outputDirectoryName));
            files.Add(outputPath);

            var taskItem = new TaskItem(outputPath);
            taskItem.SetMetadata("OriginalRelativePath", relativePath);
            taskItem.SetMetadata("IsEmbeddedResource", IsEmbeddedResource.ToString());
            outputItems.Add(taskItem);

            var resource = new Resource();
            resourceCollection.Add(resource);

            if (IsEmbeddedResource && !string.IsNullOrWhiteSpace(AssemblyName))
            {
                var name = RootNamespace + "." + relativePath.Replace(Path.DirectorySeparatorChar, '.');
                resourceElement.Add(new XAttribute("name", name));
                resourceElement.Add(new XElement("assembly", AssemblyName));

                resource.Name = name;
                resource.Assembly = AssemblyName;
            }
            else
            {
                var name = relativePath.Replace(Path.DirectorySeparatorChar, '/');
                resourceElement.Add(new XAttribute("name", name));
                resource.Name = Path.GetFileName(name);
            }
            resource.RelativePath = relativePath;
            var hashifiedPath = Path.Combine(Path.GetDirectoryName(relativePath), hashedFileName);
            resource.RelativeHashifiedPath = hashifiedPath;

            resourceElement.Add(new XElement("cdn-relative-path", Processor.GetRelativePath(outputPath, outputDirectoryName).Replace(Path.DirectorySeparatorChar, '/')));
            resourceElement.Add(new XElement("relative-path", Processor.GetRelativePath(outputPath, this.ProjectDirectory).Replace(Path.DirectorySeparatorChar, '/')));
        }

        private void DeleteEmptyDirectories(string directoryName)
        {
            foreach (var entry in Directory.GetDirectories(directoryName))
            {
                this.DeleteEmptyDirectories(entry);
            }

            if (!Directory.EnumerateFileSystemEntries(directoryName).Any())
            {
                this.Log(MessageImportance.Low, "Deleting directory \"{0}\" because it is empty.", directoryName);
                Directory.Delete(directoryName);
            }
        }

        private void Log(MessageImportance importance, string format, params object[] args)
        {
            var message = string.Format(format, args);
            var eventArgs = new BuildMessageEventArgs(message, string.Empty, "CachifierProcessingContent", importance);
            this.BuildEngine.LogMessageEvent(eventArgs);
        }
    }
}