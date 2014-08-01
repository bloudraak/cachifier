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
    using System.Diagnostics.Contracts;
    using System.Linq;
    using Cachifier.Build.Tasks.Annotations;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    ///     Represents an Microsoft Build task for Cachifier
    /// </summary>
    [PublicAPI]
    public class CachifierTask : Task
    {
        private ITaskItem[] _content;
        private ITaskItem[] _embeddedResources;
        private ITaskItem[] _staticExtensions;
        private ITaskItem[] _exclusions;

        /// <summary>
        ///     Gets the content files
        /// </summary>
        [Required]
        [PublicAPI]
        [NotNull]
        public ITaskItem[] Content
        {
            get
            {
                return this._content ?? new ITaskItem[0];
            }
            set
            {
                this._content = value;
            }
        }

        /// <summary>
        ///     Gets the embedded resources in the project
        /// </summary>
        [Required]
        [PublicAPI]
        [NotNull]
        public ITaskItem[] EmbeddedResources
        {
            get
            {
                return this._embeddedResources ?? new ITaskItem[0];
            }
            set
            {
                this._embeddedResources = value;
            }
        }

        [PublicAPI]
        [NotNull]
        public ITaskItem[] Exclusions
        {
            get
            {
                return this._exclusions ?? new ITaskItem[0];
            }
            set
            {
                this._exclusions = value;
            }
        }

        [Required]
        [PublicAPI]
        [NotNull]
        public ITaskItem[] StaticExtensions
        {
            get
            {
                return this._staticExtensions ?? new ITaskItem[0];
            }
            set
            {
                this._staticExtensions = value;
            }
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
                Contract.Assume((this.Content.Length != 0 || this.BuildEngine != null));

                if (this.Content.Length == 0)
                {
                    var message = string.Format("There are no content items to process");
                    this.Log(MessageImportance.High, message);
                }

                var embeddedResources = this.SelectFullPath(this.EmbeddedResources).ToArray();
                var content = this.SelectFullPath(this.Content).ToArray();
                var exclusions = this.SelectFullPath(this.Exclusions).ToArray(); 

                var processor = new Processor(embeddedResources,
                    content,
                    exclusions,
                    this.ProjectDirectory,
                    this.AssemblyName,
                    this.RootNamespace,
                    this.StaticExtensions.Where(item => item != null).Select(item => item.ItemSpec),
                    this.StaticMappingSourcePath,
                    this.ForceLowercase,
                    this.OutputPath,
                    this.CdnBaseUri);
                processor.Logger = new BuidLogger(this.BuildEngine);
                processor.Process();

                return true;
            }
            catch (Exception e)
            {
                this.Log(MessageImportance.High, "Exception: {0}", e);
                return false;
            }
        }

        private IEnumerable<string> SelectFullPath([NotNull] IEnumerable<ITaskItem> taskItems)
        {
            if (taskItems == null)
            {
                throw new ArgumentNullException("taskItems");
            }
            return taskItems.Where(item => item != null).Select(item => item.GetMetadata("FullPath"));
        }

        [StringFormatMethod("args")]
        private void Log(MessageImportance importance, [NotNull] string format, [NotNull] params object[] args)
        {
            if (format == null)
            {
                throw new ArgumentNullException("format");
            }
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }
            var message = string.Format(format, args);
            var eventArgs = new BuildMessageEventArgs(message, string.Empty, "CachifierProcessingContent", importance);
            IBuildEngine buildEngine = this.BuildEngine;
            buildEngine.LogMessageEvent(eventArgs);
        }
    }
}