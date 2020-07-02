using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Antlr4.Build.Tasks
{
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using System;
    using System.Net;
    using System.Text;
    using Directory = System.IO.Directory;
    using File = System.IO.File;
    using Path = System.IO.Path;

    public class GetRuntime : Task
    {
        private ITaskItem[] _result = null;

        [Required]
        public string IntermediateOutputPath
        {
            get;
            set;
        }

        [Output]
        public ITaskItem[] Runtime
        {
            get
            {
                return _result;
            }
            set
            {
                _result = null;
            }
        }

        public override bool Execute()
        {
            // Download from web.
            // https://github.com/antlr/antlr4/archive/4.8.zip
            //string zip = @"https://github.com/antlr/antlr4/archive/4.8.zip";
            string zip = "antlr4-4.8.zip";
            if (System.Environment.OSVersion.Platform == PlatformID.Win32NT
                || System.Environment.OSVersion.Platform == PlatformID.Win32S
                || System.Environment.OSVersion.Platform == PlatformID.Win32Windows
            )
            {
                System.IO.Directory.CreateDirectory(IntermediateOutputPath);
                string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string archive_path = Path.GetFullPath(assemblyPath
                           + System.IO.Path.DirectorySeparatorChar
                           + ".."
                           + System.IO.Path.DirectorySeparatorChar
                           + ".."
                           + System.IO.Path.DirectorySeparatorChar
                           + "build"
                           + System.IO.Path.DirectorySeparatorChar
                           + System.IO.Path.GetFileName(zip)
                );
                var file_name = Path.GetFileName(archive_path);
                var file_name_without_suffix = file_name.Replace(".zip", "");
                var runtime_dir = IntermediateOutputPath
                                  + System.IO.Path.DirectorySeparatorChar
                                  + "Runtime";
                System.IO.Directory.CreateDirectory(runtime_dir);
                var sub_dir = runtime_dir
                              + System.IO.Path.DirectorySeparatorChar
                              + file_name_without_suffix;
                if (!Directory.Exists(sub_dir))
                {
                    System.IO.Compression.ZipFile.ExtractToDirectory(archive_path, runtime_dir);
                }

                var hintpath = sub_dir
                             + System.IO.Path.DirectorySeparatorChar
                             + "runtime"
                             + System.IO.Path.DirectorySeparatorChar
                             + "CSharp"
                             + System.IO.Path.DirectorySeparatorChar
                             + "runtime"
                             + System.IO.Path.DirectorySeparatorChar
                             + "CSharp"
                             + System.IO.Path.DirectorySeparatorChar
                             + "Antlr4.Runtime"
                             + System.IO.Path.DirectorySeparatorChar
                             + "lib"
                             + System.IO.Path.DirectorySeparatorChar
                             + "Debug"
                             + System.IO.Path.DirectorySeparatorChar
                             + "netstandard1.3"
                             + System.IO.Path.DirectorySeparatorChar
                             + "Antlr4.Runtime.Standard.dll"
                    ;

                if (!System.IO.File.Exists(hintpath))
                {
                    var sln = sub_dir
                              + System.IO.Path.DirectorySeparatorChar
                              + "runtime"
                              + System.IO.Path.DirectorySeparatorChar
                              + "CSharp"
                              + System.IO.Path.DirectorySeparatorChar
                              + "runtime"
                              + System.IO.Path.DirectorySeparatorChar
                              + "CSharp"
                              + System.IO.Path.DirectorySeparatorChar
                              + "Antlr4.dotnet.sln"
                        ;

                    // Execute dotnet.
                    List<string> arguments = new List<string>();
                    arguments.Add("build");
                    arguments.Add(sln);
                    arguments.Add("-f");
                    arguments.Add("netstandard1.3");
                    ProcessStartInfo startInfo = new ProcessStartInfo("dotnet", JoinArguments(arguments))
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    };
                    this.Log.LogMessage(MessageImportance.Normal, "Building runtime " + hintpath);
                    Process process = new Process();
                    process.StartInfo = startInfo;
                    process.Start();
                    process.BeginErrorReadLine();
                    process.BeginOutputReadLine();
                    process.StandardInput.Dispose();
                    process.WaitForExit();
                    if (process.ExitCode != 0) return false;
                }

                _result = new ITaskItem[] {new TaskItem(hintpath) };
            }
            else throw new Exception("Which OS??");

            return true;
        }

        private static string JoinArguments(IEnumerable<string> arguments)
        {
            if (arguments == null)
                throw new ArgumentNullException("arguments");

            StringBuilder builder = new StringBuilder();
            foreach (string argument in arguments)
            {
                if (builder.Length > 0)
                    builder.Append(' ');

                if (argument.IndexOfAny(new[] { '"', ' ' }) < 0)
                {
                    builder.Append(argument);
                    continue;
                }

                // escape a backslash appearing before a quote
                string arg = argument.Replace("\\\"", "\\\\\"");
                // escape double quotes
                arg = arg.Replace("\"", "\\\"");

                // wrap the argument in outer quotes
                builder.Append('"').Append(arg).Append('"');
            }

            return builder.ToString();
        }
    }
}