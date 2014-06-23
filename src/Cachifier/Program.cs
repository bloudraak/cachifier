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
    using System.Diagnostics;
    using System.IO;

    internal class CommandLineArgs
    {
        public static CommandLineArgs Parse(string[] args)
        {
            var commandLineArgs = new CommandLineArgs();
            switch (args.Length)
            {
                case 2:
                    commandLineArgs.OutputPath = args[1];
                    commandLineArgs.ProjectPath = args[0];
                    break;

                default:
                    throw new InvalidOperationException(Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName) + " path outputPath");
            }
            
            return commandLineArgs;
        }

        public string OutputPath
        {
            get;
            set;
        }

        public string ProjectPath
        {
            get;
            set;
        }
    }

    internal class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                var commandLineArgs = CommandLineArgs.Parse(args);

                if (!Directory.Exists(commandLineArgs.ProjectPath))
                {
                    Console.Error.WriteLine("The directory '{0}' does not exist");
                    return 1;
                }

                var processor = new Processor();
                processor.Process(commandLineArgs.ProjectPath, commandLineArgs.OutputPath);
                return 0;
            }
            catch (IndexOutOfRangeException)
            {
                var foregroundColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("Usage:\n\n\t{0} <path>", Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName));
                Console.ForegroundColor = foregroundColor;
                return 2;
                
            }
            catch (Exception e)
            {
                var foregroundColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(e);
                Console.ForegroundColor = foregroundColor;
                return 2;
            }
        }
    }
}