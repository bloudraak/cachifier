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
    using System.IO;

    /// <summary>
    /// Represents a Cashifier processor
    /// </summary>
    public class Processor
    {
        /// <summary>
        /// Process files
        /// </summary>
        /// <param name="resources"></param>
        public void Process(Resource[] resources)
        {
            IHashifier hashifier = new Hashifier();
            IEncoder encoder = new Encoder();

            foreach (var resource in resources)
            {
                var hash = hashifier.Hashify(resource.Path);
                var filename = encoder.Encode(hash);
                resource.HashedPath = Path.Combine(Path.GetDirectoryName(resource.Path), filename + Path.GetExtension(resource.Path));
            }

            foreach (var resource in resources)
            {
                using (var source = File.OpenRead(resource.Path))
                {
                    using (var destination = File.Open(resource.HashedPath, FileMode.Create))
                    {
                        source.CopyTo(destination, 4096);
                    }
                }
            }
        }
    }
}