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
    using System.Diagnostics;
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
        /// <summary>
        /// Gets the content files
        /// </summary>
        [Required]
        public ITaskItem[] Content
        {
            get; 
            set;
        }

        [Required]
        public ITaskItem[] StaticExtensions
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the output folders
        /// </summary>
        [Required]
        public string OutputPath
        {
            get; 
            set;
        }

        /// <summary>
        /// Gets the items the output items
        /// </summary>
        [Output]
        public ITaskItem[] OutputItems
        {
            get; 
            private set;
        }

        [Required]
        public string ProjectDirectory
        {
            get;
            set;
        }

        [DefaultValue(false)]
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
                if (Content.Length == 0)
                {
                    string message = string.Format("There are no content items to process");
                    var args = new BuildMessageEventArgs(message, string.Empty, "CachifierNoContent", MessageImportance.Normal);
                    BuildEngine.LogMessageEvent(args);
                }

                var extensionPattern = string.Join("|", StaticExtensions.Select(s => Regex.Escape(s.ItemSpec)));
                var extensionRegex = new Regex(extensionPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

                var outputItems = new List<ITaskItem>();

                IHashifier hashifier = new Hashifier();
                IEncoder encoder = new Encoder();

                var filesToDelete = new List<string>();

                var outputDirectoryName = Path.Combine(ProjectDirectory, OutputPath);
                if (Directory.Exists(outputDirectoryName))
                {
                    filesToDelete.AddRange(Directory.GetFiles(outputDirectoryName, "*", SearchOption.AllDirectories));
                }

                var files = new List<string>();
                var mapping = new Dictionary<string, string>();

                foreach (var contentFile in Content)
                {
                    var path = contentFile.ItemSpec;
                    var extension = Path.GetExtension(path);
                    if (extension == null)
                    {
                        //this.Log("Skipping '{0}' since it has no extension", path);
                        continue;
                    }
                
                    if (!extensionRegex.IsMatch(extension))
                    {
                        this.Log(MessageImportance.Low, "Skipping '{0}'. It doesn't represent a static file.", path);
                        continue;
                    }

                    var fullPath = contentFile.GetMetadata("FullPath");
                    var hashValue = hashifier.Hashify(fullPath);
                    var encodedHashValue = encoder.Encode(hashValue);
                    var relativePath = Processor.GetRelativePath(fullPath, ProjectDirectory);
                    var hashedFileName = Path.GetFileNameWithoutExtension(fullPath) + "," + encodedHashValue + Path.GetExtension(fullPath);
                    var outputPath = Path.Combine(ProjectDirectory, OutputPath, relativePath);
                    outputPath = Path.Combine(Path.GetDirectoryName(outputPath), hashedFileName);
                    var relativeOutputPath = Processor.GetRelativePath(outputPath, ProjectDirectory);
                
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

                        if (Override || sourceWrittenTime.CompareTo(targetWrittenTime) > 0)
                        {
                            // Copying file from "obj\Debug\Sample.WebApplication3.dll" to "bin\Sample.WebApplication3.dll".
                            this.Log(MessageImportance.Normal, "Copying file from \"{0}\" to \"{1}\".", Processor.GetRelativePath(fullPath, ProjectDirectory), relativeOutputPath);
                            File.Copy(fullPath, outputPath, true);
                        }
                        else
                        {
                            //Skipping target "GenerateTargetFrameworkMonikerAttribute" because all output files are up-to-date with respect to the input files.
                            this.Log(MessageImportance.Normal, "Skipping file \"{0}\" because all output files are up-to-date with respect to the input files.", relativeOutputPath);
                        }
                    }
                    else
                    {
                        // Deleting file "S:\CachifierSamples\src\Sample.WebApplication3\bin\WebGrease.dll".
                        // Copying file from "obj\Debug\Sample.WebApplication3.dll" to "bin\Sample.WebApplication3.dll".
                        this.Log(MessageImportance.Normal, "Copying file from \"{0}\" to \"{1}\".", Processor.GetRelativePath(fullPath, ProjectDirectory), relativeOutputPath);
                        File.Copy(fullPath, outputPath, true);
                    }

                    filesToDelete.Remove(outputPath);

                    mapping.Add(relativePath, Processor.GetRelativePath(outputPath, outputDirectoryName));
                    files.Add(outputPath);
                    var taskItem = new TaskItem(outputPath);
                    outputItems.Add(taskItem);

                    // TODO: Replace relative paths
                }

                if (Directory.Exists(outputDirectoryName))
                {
                    foreach (var file in filesToDelete)
                    {
                        Log(MessageImportance.Normal, "Deleting file \"{0}\".", file);
                        File.Delete(file);
                    }
                    this.DeleteEmptyDirectories(outputDirectoryName);
                }


                OutputItems = outputItems.ToArray();
                if (files.Count > 0)
                {
                    this.Log(MessageImportance.High, "Fixing references between static content");
                }
            
                foreach (var file in files)
                {
                    var relativePath = Processor.GetRelativePath(file, ProjectDirectory);

                    bool skip = true;
                    switch (Path.GetExtension(file))
                    {
                        case ".css":
                        case ".js":
                            skip = false;
                            break;
                    }

                    if (skip)
                    {
                        Log(MessageImportance.Normal, "Skipping file \"{0}\" because its not a text file.", relativePath);
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
                        this.Log(MessageImportance.Normal, "Modified references in \"{0}\" to the hashified paths", relativePath);
                    }
                    else
                    {
                        Log(MessageImportance.Normal, "Skipping file \"{0}\" it has not been modified.", relativePath);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Log(MessageImportance.High, "Exception: {0}", e);
                return false;
            }
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