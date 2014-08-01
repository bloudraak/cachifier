namespace Cachifier.Build.Tasks
{
    using System;
    using System.IO;
    using Cachifier.Build.Tasks.Annotations;

    /// <summary>
    /// Represents a naming policy of resources
    /// </summary>
    public class ResourceNamingPolicy
    {
        /// <summary>
        /// Gets the filename of a resource
        /// </summary>
        /// <param name="resource">The resource</param>
        /// <returns>The filename of the resource</returns>
        [NotNull]
        [PublicAPI]
        public string GetFileName([NotNull] Resource resource)
        {
            if (resource == null)
            {
                throw new ArgumentNullException("resource");
            }
            var extension = Path.GetExtension(resource.Path);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(resource.Path);
            var hashifiedFileName = string.Format("{0},{1}{2}",
                fileNameWithoutExtension,
                resource.ContentHash,
                extension);
            return hashifiedFileName;
        }
    }
}