namespace Cachifier.Build.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using Cachifier.Build.Tasks.Annotations;

    /// <summary>
    /// Represents a collection of resources
    /// </summary>
    public class ResourceCollection : HashSet<Resource>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Collections.Generic.HashSet`1"/> class that is empty and uses the default equality comparer for the set type.
        /// </summary>
        public ResourceCollection()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Collections.Generic.HashSet`1"/> class that is empty and uses the specified equality comparer for the set type.
        /// </summary>
        /// <param name="comparer">The <see cref="T:System.Collections.Generic.IEqualityComparer`1"/> implementation to use when comparing values in the set, or null to use the default <see cref="T:System.Collections.Generic.EqualityComparer`1"/> implementation for the set type.</param>
        public ResourceCollection(IEqualityComparer<Resource> comparer)
            : base(comparer)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Collections.Generic.HashSet`1"/> class that uses the default equality comparer for the set type, contains elements copied from the specified collection, and has sufficient capacity to accommodate the number of elements copied.
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new set.</param><exception cref="T:System.ArgumentNullException"><paramref name="collection"/> is null.</exception>
        public ResourceCollection([NotNull] IEnumerable<Resource> collection)
            : base(collection)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Collections.Generic.HashSet`1"/> class that uses the specified equality comparer for the set type, contains elements copied from the specified collection, and has sufficient capacity to accommodate the number of elements copied.
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new set.</param><param name="comparer">The <see cref="T:System.Collections.Generic.IEqualityComparer`1"/> implementation to use when comparing values in the set, or null to use the default <see cref="T:System.Collections.Generic.EqualityComparer`1"/> implementation for the set type.</param><exception cref="T:System.ArgumentNullException"><paramref name="collection"/> is null.</exception>
        public ResourceCollection([NotNull] IEnumerable<Resource> collection, IEqualityComparer<Resource> comparer)
            : base(collection, comparer)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Collections.Generic.HashSet`1"/> class with serialized data.
        /// </summary>
        /// <param name="info">A <see cref="T:System.Runtime.Serialization.SerializationInfo"/> object that contains the information required to serialize the <see cref="T:System.Collections.Generic.HashSet`1"/> object.</param><param name="context">A <see cref="T:System.Runtime.Serialization.StreamingContext"/> structure that contains the source and destination of the serialized stream associated with the <see cref="T:System.Collections.Generic.HashSet`1"/> object.</param>
        protected ResourceCollection(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Adds the elements of the specified collection
        /// </summary>
        /// <param name="collection">The collection</param>
        /// <exception cref="ArgumentNullException">collection is null</exception>
        public void AddRange([NotNull] IEnumerable<Resource> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }
            foreach (var resource in collection)
            {
                this.Add(resource);
            }
        }

        [NotNull]
        public IEnumerable<string> GetHashifiedPaths()
        {
            return this.Where(item => item != null)
                .Select(item => item.HashifiedPath)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.ToLower());
        }
    }
}