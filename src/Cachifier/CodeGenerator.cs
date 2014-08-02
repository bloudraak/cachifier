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
    using System;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.IO;
    using Cachifier.Build.Tasks;
    using Cachifier.Build.Tasks.Annotations;

    public class CodeGenerator
    {
        public void GenerateSourceMappingSource([NotNull] IEnumerable<Resource> resources, string ns, string path, bool forceLowercase, string outputPath, string cdnBaseUri)
        {
            if (resources == null)
            {
                throw new ArgumentNullException("resources");
            }
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new InvalidOperationException(
                    "The static mapping source path is null, empty or consists solely of whitespace");
            }
            using (var streamWriter = new StreamWriter(path))
            {
                using (var writer = new IndentedTextWriter(streamWriter))
                {
                    writer.WriteLine("using System;");
                    writer.WriteLine("using System.IO;");
                    writer.WriteLine("using System.Configuration;");
                    writer.WriteLine("using System.Web;");
                    writer.WriteLine("using System.Web.UI;");
                    writer.WriteLine("using System.Web.Configuration;");
                    writer.WriteLine("using {0};", ns);
                    writer.WriteLine();
                    writer.WriteLine("[assembly: PreApplicationStartMethod(typeof (MapScriptResources), \"Start\")]");
                    writer.WriteLine();
                    writer.WriteLine("namespace {0}", ns);
                    writer.WriteLine("{");
                    writer.Indent++;
                    writer.WriteLine();
                    writer.WriteLine("public static partial class MapScriptResources");
                    writer.WriteLine("{");
                    writer.WriteLine();
                    writer.Indent++;
                    writer.WriteLine("public static void Start()");
                    writer.WriteLine("{");
                    writer.Indent++;
                    writer.WriteLine("ScriptResourceDefinition definition = null;");
                    writer.WriteLine("var assembly = typeof(MapScriptResources).Assembly;");
                    foreach (var resource in resources)
                    {
                        var assemblyName = "assembly";
                        if (!resource.IsEmbedded)
                        {
                            assemblyName = "null";
                        }

                        writer.WriteLine();
                        writer.WriteLine("//");
                        writer.WriteLine("// Name: {0}", resource.Name);
                        writer.WriteLine("// Assembly: {0}", resource.Assembly);
                        writer.WriteLine("// IsEmbedded: {0}", resource.IsEmbedded);
                        writer.WriteLine("// RelativeHashifiedPath: {0}", resource.RelativeHashifiedPath);
                        writer.WriteLine("// RelativePath: {0}", resource.RelativePath);
                        writer.WriteLine("//");

                        writer.WriteLine(
                                         "definition = ScriptManager.ScriptResourceMapping.GetDefinition(\"{0}\", {1});",
                            resource.Name,
                            assemblyName);
                        writer.WriteLine("if(definition == null)");
                        writer.WriteLine("{");
                        writer.Indent++;
                        writer.WriteLine("definition = new ScriptResourceDefinition();");

                        string relativeUri;
                        if (!resource.IsEmbedded)
                        {
                            relativeUri = NormalizeRelativeUri(resource.RelativePath, forceLowercase);
                            writer.WriteLine("if(IsDebuggingEnabled)");
                            writer.WriteLine("{");
                            writer.Indent++;
                            writer.WriteLine("definition.Path = \"~/{0}\";", relativeUri);
                            writer.WriteLine("definition.DebugPath = \"~/{0}\";", relativeUri);
                            writer.Indent--;
                            writer.WriteLine("}");
                            writer.WriteLine("else");
                            writer.WriteLine("{");
                            writer.Indent++;
                            relativeUri = NormalizeRelativeUri(resource.RelativeHashifiedPath, forceLowercase);
                            writer.WriteLine("definition.Path = \"~/{1}/{0}\";", relativeUri, outputPath);
                            writer.WriteLine("definition.DebugPath = \"~/{1}/{0}\";", relativeUri, outputPath);
                            writer.Indent--;
                            writer.WriteLine("}");
                        }
                        else
                        {
                            writer.WriteLine("definition.ResourceAssembly = assembly;");
                            writer.WriteLine("definition.ResourceName = \"{0}\";", resource.Name);
                        }

                        if (resource.RelativeHashifiedPath != null)
                        {
                            if (!string.IsNullOrWhiteSpace(cdnBaseUri))
                            {
                                Uri baseUri;
                                if (Uri.TryCreate(cdnBaseUri, UriKind.Absolute, out baseUri))
                                {
                                    relativeUri = NormalizeRelativeUri(resource.RelativeHashifiedPath, forceLowercase);
                                    // Double escape the damn string because creating a Uri will unescape it... Go figure.  
                                    relativeUri = NormalizeRelativeUri(relativeUri, forceLowercase);
                                    Uri uri;
                                    if (Uri.TryCreate(baseUri, relativeUri, out uri))
                                    {
                                        writer.WriteLine("definition.CdnPath = \"{0}\";", uri);
                                        writer.WriteLine("definition.CdnDebugPath = \"{0}\";", uri);
                                        writer.WriteLine("definition.CdnSupportsSecureConnection = true;");
                                    }
                                }
                            }
                        }

                        writer.WriteLine("ScriptManager.ScriptResourceMapping.AddDefinition(\"{0}\", {1}, definition);",
                            resource.Name,
                            assemblyName);
                        writer.WriteLine();

                        writer.Indent--;
                        writer.WriteLine("}");
                    }
                    writer.Indent--;
                    writer.WriteLine("}");
                    writer.WriteLine();
                    writer.WriteLine("private static bool IsDebuggingEnabled");
                    writer.WriteLine("{");
                    writer.Indent++;
                    writer.WriteLine("get");
                    writer.WriteLine("{");
                    writer.Indent++;
                    writer.WriteLine(
                                     "var compilationSection = (CompilationSection) ConfigurationManager.GetSection(\"system.web/compilation\");");
                    writer.WriteLine("return compilationSection.Debug;");
                    writer.Indent--;
                    writer.WriteLine("}");
                    writer.Indent--;
                    writer.WriteLine("}");
                    writer.Indent--;
                    writer.WriteLine("}");
                    writer.Indent--;
                    writer.WriteLine("}");
                }
            }
        }

        [NotNull]
        private static string NormalizeRelativeUri([NotNull] string relativePath, bool forceLowercase)
        {
            if (relativePath == null)
            {
                throw new ArgumentNullException("relativePath");
            }
            var relativeUri = relativePath.Replace('\\', '/');
            if (forceLowercase)
            {
                relativeUri = relativeUri.ToLowerInvariant();
            }
            return Uri.EscapeUriString(relativeUri);
        }
    }
}