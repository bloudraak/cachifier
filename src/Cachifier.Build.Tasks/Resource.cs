namespace Cachifier.Build.Tasks
{
    /// <summary>
    /// Represents a resource
    /// </summary>
    public class Resource
    {
        public string Name
        {
            get;
            set;
        }

        public string Assembly
        {
            get;
            set;
        }

        public string RelativePath
        {
            get;
            set;
        }

        public string RelativeHashifiedPath
        {
            get;
            set;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return string.Format("Name: {0}, Assembly: {1}, RelativePath: {2}", this.Name, this.Assembly, this.RelativePath);
        }

        public bool IsEmbedded
        {
            get
            {
                return !string.IsNullOrWhiteSpace(this.Assembly);
            }
        }
    }
}