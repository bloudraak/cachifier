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
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Cachifier.Build.Tasks.Annotations;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

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
                        MessageImportance.Normal);
                    this.BuildEngine.LogMessageEvent(args);
                }

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

                foreach (var contentFile in this.EmbeddedResources)
                {
                    this.ProcessTaskItem(contentFile, filesToDelete, mapping, outputDirectoryName, files, outputItems);
                }

                foreach (var contentFile in this.Content)
                {
                    this.ProcessTaskItem(contentFile, filesToDelete, mapping, outputDirectoryName, files, outputItems);
                }

                if (Directory.Exists(outputDirectoryName))
                {
                    foreach (var file in filesToDelete)
                    {
                        this.Log(MessageImportance.Normal,
                            "Deleting file \"{0}\" because it is no longer referenced by the project.",
                            file);
                        File.Delete(file);
                    }
                    this.DeleteEmptyDirectories(outputDirectoryName);
                }

                this.OutputItems = outputItems.ToArray();

                foreach (var file in files)
                {
                    var relativePath = Processor.GetRelativePath(file, this.ProjectDirectory);

                    if (IsBinary(file))
                    {
                        this.Log(MessageImportance.Normal,
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
                        this.Log(MessageImportance.Normal,
                            "Updating file \"{0}\" since references to other static resources has changed.",
                            relativePath);
                    }
                    else
                    {
                        this.Log(MessageImportance.Normal,
                            "Skipping file \"{0}\" since references to other static resources has not changed.",
                            relativePath);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                this.Log(MessageImportance.High, "Exception: {0}", e);
                return false;
            }
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

        private void ProcessTaskItem(
            ITaskItem contentFile,
            ICollection<string> filesToDelete,
            IDictionary<string, string> mapping,
            string outputDirectoryName,
            ICollection<string> files,
            ICollection<ITaskItem> outputItems)
        {
            var path = contentFile.ItemSpec;
            var extension = Path.GetExtension(path);
            if (extension == null)
            {
                this.Log(MessageImportance.Normal,
                    "Skipping '{0}' because it doesn't represent a static resource.",
                    path);
                return;
            }

            if (!this._extensionRegex.IsMatch(extension))
            {
                this.Log(MessageImportance.Normal,
                    "Skipping '{0}' because it doesn't represent a static resource.",
                    path);
                return;
            }

            var fullPath = contentFile.GetMetadata("FullPath");
            var hashValue = this._hashifier.Hashify(fullPath);
            var encodedHashValue = this._encoder.Encode(hashValue);
            var relativePath = Processor.GetRelativePath(fullPath, this.ProjectDirectory);
            var hashedFileName = Path.GetFileNameWithoutExtension(fullPath) + "," + encodedHashValue
                                 + Path.GetExtension(fullPath);
            var outputPath = Path.Combine(outputDirectoryName, hashedFileName);
            var relativeOutputPath = Processor.GetRelativePath(outputPath, this.ProjectDirectory);

            var directoryName = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(directoryName))
            {
                this.Log(MessageImportance.Normal, "Creating directory '{0}'...", directoryName);
                Directory.CreateDirectory(directoryName);
            }

            if (File.Exists(outputPath))
            {
                var targetWrittenTime = File.GetLastWriteTimeUtc(outputPath);
                var sourceWrittenTime = File.GetLastWriteTimeUtc(fullPath);

                if (this.Override || sourceWrittenTime.CompareTo(targetWrittenTime) > 0)
                {
                    // Copying file from "obj\Debug\Sample.WebApplication3.dll" to "bin\Sample.WebApplication3.dll".
                    this.Log(MessageImportance.Normal,
                        "Copying file from \"{0}\" to \"{1}\".",
                        Processor.GetRelativePath(fullPath, this.ProjectDirectory),
                        relativeOutputPath);
                    File.Copy(fullPath, outputPath, true);
                }
                else
                {
                    //Skipping target "GenerateTargetFrameworkMonikerAttribute" because all output files are up-to-date with respect to the input files.
                    this.Log(MessageImportance.Normal,
                        "Skipping file \"{0}\" because all output files are up-to-date with respect to the input files.",
                        relativeOutputPath);
                }
            }
            else
            {
                // Deleting file "S:\CachifierSamples\src\Sample.WebApplication3\bin\WebGrease.dll".
                // Copying file from "obj\Debug\Sample.WebApplication3.dll" to "bin\Sample.WebApplication3.dll".
                this.Log(MessageImportance.Normal,
                    "Copying file from \"{0}\" to \"{1}\".",
                    Processor.GetRelativePath(fullPath, this.ProjectDirectory),
                    relativeOutputPath);
                File.Copy(fullPath, outputPath, true);
            }

            filesToDelete.Remove(outputPath);

            mapping.Add(relativePath, Processor.GetRelativePath(outputPath, outputDirectoryName));
            files.Add(outputPath);
            var taskItem = new TaskItem(outputPath);
            outputItems.Add(taskItem);
        }

        private void DeleteEmptyDirectories(string directoryName)
        {
            foreach (var entry in Directory.GetDirectories(directoryName))
            {
                this.DeleteEmptyDirectories(entry);
            }

            if (!Directory.EnumerateFileSystemEntries(directoryName).Any())
            {
                this.Log(MessageImportance.Normal, "Deleting directory \"{0}\" because it is empty.", directoryName);
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