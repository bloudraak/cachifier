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
    using System.Diagnostics.Contracts;
    using Cachifier.Build.Tasks.Annotations;
    using Microsoft.Build.Framework;
    using ILogger = Cachifier.ILogger;
    using MessageImportance = Cachifier.MessageImportance;

    public class BuidLogger : ILogger
    {
        private readonly IBuildEngine _buildEngine;

        public BuidLogger([NotNull] IBuildEngine buildEngine)
        {
            if (buildEngine == null)
            {
                throw new ArgumentNullException("buildEngine");
            }
            this._buildEngine = buildEngine;
        }

        #region Implementation of ILogger

        public void Log(MessageImportance importance, string format, params object[] args)
        {
            if (format == null)
            {
                throw new ArgumentNullException("format");
            }
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }
            var i = Microsoft.Build.Framework.MessageImportance.High;
            switch (importance)
            {
                case MessageImportance.High:
                    i = Microsoft.Build.Framework.MessageImportance.High;
                    break;

                case MessageImportance.Normal:
                    i = Microsoft.Build.Framework.MessageImportance.Normal;
                    break;

                case MessageImportance.Low:
                    i = Microsoft.Build.Framework.MessageImportance.Low;
                    break;
            }
            var message = string.Format(format, args);
            var eventArgs = new BuildMessageEventArgs(message, string.Empty, "CachifierProcessingContent", i);
            this._buildEngine.LogMessageEvent(eventArgs);
        }

        [ContractInvariantMethod]
        private void ContractInvariantMethod()
        {
            Contract.Invariant(this._buildEngine != null);
        }


        #endregion
    }
}