namespace Cachifier.Build.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Cachifier.Build.Tasks.Annotations;

    public class ResourceFilter
    {
        private readonly Regex _extensionRegex;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public ResourceFilter([NotNull] IEnumerable<string> extensions)
        {
            if (extensions == null)
            {
                throw new ArgumentNullException("extensions");
            }
            var extensionPattern = string.Join("|",
                extensions.Where(item => !string.IsNullOrWhiteSpace(item)).Select(Regex.Escape));
            this._extensionRegex = new Regex(extensionPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }

        /// <summary>
        ///     Returns whether the given path is a resource or not
        /// </summary>
        /// <param name="path">The path</param>
        /// <returns>true if its a resource, false otherwise</returns>
        private bool IsResource(string path)
        {
            if (path == null)
            {
                return true;
            }

            var extension = Path.GetExtension(path);
            if (string.IsNullOrWhiteSpace(extension))
            {
                return true;
            }

            Debug.Assert(this._extensionRegex != null, "_extensionRegex != null");
            return this._extensionRegex.IsMatch(extension);
        }

        /// <summary>
        /// Filters out unwanted files
        /// </summary>
        /// <param name="files">The files to filter</param>
        /// <returns>An enumeration of filters</returns>
        public IEnumerable<string> Filter([NotNull] IEnumerable<string> files)
        {
            if (files == null)
            {
                throw new ArgumentNullException("files");
            }
            return files.Where(this.IsResource);
        }

    }
}