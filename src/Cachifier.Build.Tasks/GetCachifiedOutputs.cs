namespace Cachifier.Build.Tasks
{
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
    public class GetCachifiedOutputs : Task
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

        [Required]
        public string ProjectDirectory
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
            if (this.Content.Length == 0)
            {
                string message = string.Format("There are no content items to process");
                var args = new BuildMessageEventArgs(message, string.Empty, "CachifierNoContent", MessageImportance.Normal);
                this.BuildEngine.LogMessageEvent(args);
            }

            var extensionPattern = string.Join("|", this.StaticExtensions.Select(s => Regex.Escape(s.ItemSpec)));
            var extensionRegex = new Regex(extensionPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            
            foreach (var contentFile in this.Content)
            {
                var path = contentFile.ItemSpec;
                var extension = Path.GetExtension(path);
                if (extension == null)
                {
                    this.Log("Skipping '{0}' since it has no extension", path);
                    continue;
                }
                
                if (!extensionRegex.IsMatch(extension))
                {
                    this.Log("Skipping '{0}'. It doesn't represent a static file.", path);
                    continue;
                }

                var fullPath = contentFile.GetMetadata("FullPath");
                var relativePath = Processor.GetRelativePath(fullPath, this.ProjectDirectory);
                var ouputPath = Path.Combine(this.ProjectDirectory, this.OutputPath, relativePath);

                this.Log(string.Format("Adding '{0}", ouputPath));
                contentFile.SetMetadata("HashedFullPath", ouputPath);
            }
            return true;
        }

        private void Log(string format, params object[] args)
        {
            var message = string.Format(format, args);
            var eventArgs = new BuildMessageEventArgs(message, string.Empty, "CachifierProcessingContent", MessageImportance.Normal);
            this.BuildEngine.LogMessageEvent(eventArgs);
        }

    }
}