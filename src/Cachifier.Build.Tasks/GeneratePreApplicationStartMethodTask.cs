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
    using System.CodeDom.Compiler;
    using System.Diagnostics;
    using System.IO;
    using Cachifier.Build.Tasks.Annotations;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    ///     Represents a class that generates a PreApplicationStartMethod
    /// </summary>
    [PublicAPI]
    public class GeneratePreApplicationStartMethodTask : Task
    {
        private string _className;

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
        ///     The file to generate
        /// </summary>
        [PublicAPI]
        [Required]
        public string SourcePath
        {
            get;
            set;
        }

        /// <summary>
        ///     The file to generate
        /// </summary>
        [PublicAPI]
        [Required]
        public string ProjectDirectory
        {
            get;
            set;
        }

        /// <summary>
        ///     The file to generate
        /// </summary>
        [PublicAPI]
        [Required]
        public string OutputPath
        {
            get;
            set;
        }

        /// <summary>
        ///     The file to generate
        /// </summary>
        [PublicAPI]
        public string Namespace
        {
            get;
            set;
        }

        /// <summary>
        ///     The file to generate
        /// </summary>
        [PublicAPI]
        public string ClassName
        {
            get
            {
                return this._className ?? "PreApplicationStart";
            }
            set
            {
                this._className = value;
            }
        }

        #region Overrides of Task

        /// <summary>
        ///     When overridden in a derived class, executes the task.
        /// </summary>
        /// <returns>
        ///     true if the task successfully executed; otherwise, false.
        /// </returns>
        public override bool Execute()
        {
            string source;
            using (var stringWriter = new StringWriter())
            {
                using (var writer = new IndentedTextWriter(stringWriter))
                {
                    this.Generate(writer);
                }
                source = stringWriter.ToString();
            }

            if (File.Exists(this.SourcePath))
            {
                var originalSource = File.ReadAllText(this.SourcePath);
                if (!originalSource.Equals(source))
                {
                    this.Log(MessageImportance.Normal, "Writing '{0}'", this.SourcePath);
                    File.WriteAllText(this.SourcePath, source);
                }
                else
                {
                    this.Log(MessageImportance.Normal,
                        "Skipping file \"{0}\" because all output files are up-to-date with respect to the input files.",
                        this.SourcePath);
                }
            }
            else
            {
                this.Log(MessageImportance.Normal, "Writing '{0}'", this.SourcePath);
                File.WriteAllText(this.SourcePath, source);
            }

            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="writer"></param>
        private void Generate(IndentedTextWriter writer)
        {
            writer.WriteLine("using System.Web;");
            writer.WriteLine("using MyNamespace;");
            writer.WriteLine();
            writer.WriteLine("[assembly: PreApplicationStartMethod(typeof (MyClassName), \"Start\")]");
            writer.WriteLine();
            writer.WriteLine("namespace MyNamespace");
            writer.WriteLine("{");
            writer.Indent++;
            writer.WriteLine("using System;");
            writer.WriteLine("using System.Configuration;");
            writer.WriteLine("using System.Web.UI;");
            writer.WriteLine();
            writer.WriteLine("/// <summary>");
            writer.WriteLine("/// Represents a routine that registers static resources with ScriptManager before the application starts");
            writer.WriteLine("/// </summary>");
            writer.WriteLine("public static partial class MyClassName");
            writer.WriteLine("{");
            writer.Indent++;
            writer.WriteLine("/// <summary>");
            writer.WriteLine("/// Registers script mappings with ScriptManager");
            writer.WriteLine("/// </summary>");
            writer.WriteLine("public static void Start()");
            writer.WriteLine("{");
            writer.Indent++;
            writer.WriteLine("Uri baseUri = null;");
            writer.WriteLine("var setting = ConfigurationManager.AppSettings[\"UseContentDeliveryNetwork\"];");
            writer.WriteLine("bool useContentDeliveryNetwork;");
            writer.WriteLine("if (bool.TryParse(setting, out useContentDeliveryNetwork))");
            writer.WriteLine("{");
            writer.Indent++;
            writer.WriteLine("setting = ConfigurationManager.AppSettings[\"ContentDeliveryNetworkUri\"];");
            writer.WriteLine("if (!Uri.TryCreate(setting, UriKind.Absolute, out baseUri))");
            writer.WriteLine("{");
            writer.Indent++;
            writer.WriteLine("// The URI is invalid, so we will not use the CDN.");
            writer.WriteLine("useContentDeliveryNetwork = false;");
            writer.Indent--;
            writer.WriteLine("}");
            writer.Indent--;
            writer.WriteLine("}");
            writer.WriteLine();
            writer.WriteLine("setting = ConfigurationManager.AppSettings[\"MapStaticResourcesToPublicFolder\"];");
            writer.WriteLine("bool mapResourcesToPublicFolder;");
            writer.WriteLine();
            writer.WriteLine("// we don't really care whether it parsed or not. If the configuration is empty or it consisted ");
            writer.WriteLine("// of an invalid boolean, we'll simply assume false.");
            writer.WriteLine("bool.TryParse(setting, out mapResourcesToPublicFolder);");
            writer.WriteLine();
            writer.WriteLine("if(!mapResourcesToPublicFolder && !useContentDeliveryNetwork)");
            writer.WriteLine("{");
            writer.Indent++;
            writer.WriteLine("// There is nothing to do, nothing to map.");
            writer.WriteLine("return;");
            writer.Indent--;
            writer.WriteLine("}");
            writer.WriteLine();
            writer.WriteLine("var mapping = ScriptManager.ScriptResourceMapping;");
            writer.WriteLine("ScriptResourceDefinition resourceDefinition;");
            foreach (var item in this.Content)
            {
                if (!isEmbeddedResource(item))
                {
                    // if its not an embedded resource, we will not be generating a definition.
                    continue;
                }

                string relativePath = item.GetMetadata("OriginalRelativePath");
                string debugPath = Processor.GetRelativePath(item.ItemSpec, this.ProjectDirectory);
                debugPath = debugPath.Replace("\\", "/");
                var relativeUri = relativePath.Replace("\\", "/");
                writer.WriteLine("resourceDefinition = new ScriptResourceDefinition();");
                writer.WriteLine("resourceDefinition.CdnPath = new Uri(baseUri, new Uri(\"{0}\", UriKind.Relative)).ToString();", relativeUri);
                writer.WriteLine("resourceDefinition.CdnDebugPath = new Uri(baseUri, new Uri(\"{0}\", UriKind.Relative)).ToString();", relativeUri);
                writer.WriteLine("resourceDefinition.CdnSupportsSecureConnection = true;");
                writer.WriteLine("resourceDefinition.Path = \"~/{0}\";", debugPath);
                writer.WriteLine("resourceDefinition.DebugPath = \"~/{0}\";", debugPath);
                writer.WriteLine("mapping.AddDefinition(\"{0}.{1}\",",
                    this.Namespace,
                    relativePath.Replace('\\', '.'));
                writer.Indent++;
                writer.WriteLine("typeof(MyClassName).Assembly,");
                writer.WriteLine("resourceDefinition);");
                writer.WriteLine();
                writer.Indent--;
            }

            writer.Indent--;
            writer.WriteLine("}");
            writer.Indent--;
            writer.WriteLine("}");
            writer.Indent--;
            writer.WriteLine("}");
        }

        private static bool isEmbeddedResource(ITaskItem item)
        {
            bool isEmbeddedResource;
            string value = item.GetMetadata("IsEmbeddedResource");
            return value != null && bool.TryParse(value, out isEmbeddedResource) && isEmbeddedResource;
        }

        private void Log(MessageImportance importance, string format, params object[] args)
        {
            var message = string.Format(format, args);
            var eventArgs = new BuildMessageEventArgs(message, string.Empty, "CachifierProcessingContent", importance);
            this.BuildEngine.LogMessageEvent(eventArgs);
        }

        #endregion
    }
}