// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

namespace Antlr4.Build.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using StringBuilder = System.Text.StringBuilder;

    internal class AntlrClassGenerationTaskInternal
    {
        private List<string> _generatedCodeFiles = new List<string>();
        private IList<string> _sourceCodeFiles = new List<string>();
        private List<BuildMessage> _buildMessages = new List<BuildMessage>();

        public IList<string> GeneratedCodeFiles
        {
            get
            {
                return this._generatedCodeFiles;
            }
        }

        public string ToolPath
        {
            get;
            set;
        }

        public string TargetFrameworkVersion
        {
            get;
            set;
        }

        public string OutputPath
        {
            get;
            set;
        }

        public string LibPath
        {
            get;
            set;
        }

        public bool GAtn
        {
            get;
            set;
        }

        public string Encoding
        {
            get;
            set;
        }

        public bool Listener
        {
            get;
            set;
        }

        public bool Visitor
        {
            get;
            set;
        }

        public string Package
        {
            get;
            set;
        }

        public string DOptions
        {
            get;
            set;
        }

        public bool Error
        {
            get;
            set;
        }

        public bool ForceAtn
        {
            get;
            set;
        }

        public string[] LanguageSourceExtensions
        {
            get;
            set;
        }


        public string JavaVendor
        {
            get;
            set;
        }

        public string JavaInstallation
        {
            get;
            set;
        }

        public string JavaExecutable
        {
            get;
            set;
        }

        public IList<string> SourceCodeFiles
        {
            get
            {
                return this._sourceCodeFiles;
            }
            set
            {
                this._sourceCodeFiles = value;
            }
        }

        public IList<BuildMessage> BuildMessages
        {
            get
            {
                return _buildMessages;
            }
        }

        public string JavaExec
        {
            get;
            set;
        }



        public bool Execute()
        {
            try
            {
                this.BuildMessages.Add(new BuildMessage(
                    TraceLevel.Info,
                    "Starting Antlr4 Build Tasks. ToolPath is \""
                        + ToolPath + "\"", "", 0, 0));

                _ = Path.IsPathRooted(ToolPath);

                // First, find JAVA_EXE. This could throw an exception with error message.
                string javaExec = JavaExec;
                if (!File.Exists(javaExec))
                    throw new Exception("Cannot find Java executable, currently set to "
                                        + "'" + javaExec + "'"
                                        + " Please set either the JAVA_EXEC environment variable, "
                                        + "or set a property for JAVA_EXEC in your CSPROJ file."
                                        + " The variable must be the full path name of the executable for Java."
                                        + " E.g., on Linux, export JAVA_EXEC=\"/usr/bin/java\". On Windows,"
                                        + " JAVA_EXEC=\"C:\\Program Files\\Java\\jdk-11.0.4\"");

                // Next find Java.
                string java_executable = null;
                if (!string.IsNullOrEmpty(JavaExecutable))
                {
                    java_executable = JavaExecutable;
                }
                else
                {
                    java_executable = javaExec;
                    if (!File.Exists(java_executable))
                        throw new Exception("Yo, I haven't a clue where the Java executable is on this system. Crashing...");
                }

                if (!File.Exists(ToolPath))
                    throw new Exception("Cannot find Antlr4 jar file, currently set to "
                                        + "'" + ToolPath + "'"
                                        + " Please set either the Antlr4ToolPath environment variable, "
                                        + "or set a property for Antlr4ToolPath in your CSPROJ file.");

                // Because we're using the Java version of the Antlr tool,
                // we're going to execute this command twice: first with the
                // -depend option so as to get the list of generated files,
                // then a second time to actually generate the files.
                // The code that was here probably worked, but only for the C#
                // version of the Antlr tool chain.
                //
                // After collecting the output of the first command, convert the
                // output so as to get a clean list of files generated.
                {
                    List<string> arguments = new List<string>();

                    {
                        arguments.Add("-cp");
                        arguments.Add(ToolPath);
                        arguments.Add("org.antlr.v4.Tool");
                    }

                    arguments.Add("-depend");

                    arguments.Add("-o");
                    arguments.Add(OutputPath);

                    if (!string.IsNullOrEmpty(LibPath))
                    {
                        var split = LibPath.Split(';');
                        foreach (var p in split)
                        {
                            if (string.IsNullOrEmpty(p))
                                continue;
                            if (string.IsNullOrWhiteSpace(p))
                                continue;
                            arguments.Add("-lib");
                            arguments.Add(p);
                        }
                    }

                    if (GAtn)
                        arguments.Add("-atn");

                    if (!string.IsNullOrEmpty(Encoding))
                    {
                        arguments.Add("-encoding");
                        arguments.Add(Encoding);
                    }

                    if (Listener)
                        arguments.Add("-listener");
                    else
                        arguments.Add("-no-listener");

                    if (Visitor)
                        arguments.Add("-visitor");
                    else
                        arguments.Add("-no-visitor");

                    if (!(string.IsNullOrEmpty(Package) || string.IsNullOrWhiteSpace(Package)))
                    {
                        arguments.Add("-package");
                        arguments.Add(Package);
                    }

                    if (!string.IsNullOrEmpty(DOptions))
                    {
                        // Since the C# target currently produces the same code for all target framework versions, we can
                        // avoid bugs with support for newer frameworks by just passing CSharp as the language and allowing
                        // the tool to use a default.
                        var split = DOptions.Split(';');
                        foreach (var p in split)
                        {
                            if (string.IsNullOrEmpty(p))
                                continue;
                            if (string.IsNullOrWhiteSpace(p))
                                continue;
                            arguments.Add("-D" + p);
                        }
                    }

                    if (Error)
                        arguments.Add("-Werror");

                    if (ForceAtn)
                        arguments.Add("-Xforce-atn");

                    arguments.AddRange(SourceCodeFiles);

                    ProcessStartInfo startInfo = new ProcessStartInfo(java_executable, JoinArguments(arguments))
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    };

                    this.BuildMessages.Add(new BuildMessage(TraceLevel.Info,
                        "Executing command: \"" + startInfo.FileName + "\" " + startInfo.Arguments, "", 0, 0));

                    Process process = new Process();
                    process.StartInfo = startInfo;
                    process.ErrorDataReceived += HandleErrorDataReceived;
                    process.OutputDataReceived += HandleOutputDataReceivedFirstTime;
                    process.Start();
                    process.BeginErrorReadLine();
                    process.BeginOutputReadLine();
                    process.StandardInput.Dispose();
                    process.WaitForExit();

                    if (process.ExitCode != 0) return false;
                }
                // Second call.
                {
                    List<string> arguments = new List<string>();

                    {
                        arguments.Add("-cp");
                        arguments.Add(ToolPath);
                        //arguments.Add("org.antlr.v4.CSharpTool");
                        arguments.Add("org.antlr.v4.Tool");
                    }

                    arguments.Add("-o");
                    arguments.Add(OutputPath);

                    if (!string.IsNullOrEmpty(LibPath))
                    {
                        var split = LibPath.Split(';');
                        foreach (var p in split)
                        {
                            if (string.IsNullOrEmpty(p))
                                continue;
                            if (string.IsNullOrWhiteSpace(p))
                                continue;
                            arguments.Add("-lib");
                            arguments.Add(p);
                        }
                    }

                    if (GAtn)
                        arguments.Add("-atn");

                    if (!string.IsNullOrEmpty(Encoding))
                    {
                        arguments.Add("-encoding");
                        arguments.Add(Encoding);
                    }

                    if (Listener)
                        arguments.Add("-listener");
                    else
                        arguments.Add("-no-listener");

                    if (Visitor)
                        arguments.Add("-visitor");
                    else
                        arguments.Add("-no-visitor");

                    if (!(string.IsNullOrEmpty(Package) || string.IsNullOrWhiteSpace(Package)))
                    {
                        arguments.Add("-package");
                        arguments.Add(Package);
                    }

                    if (!string.IsNullOrEmpty(DOptions))
                    {
                        // Since the C# target currently produces the same code for all target framework versions, we can
                        // avoid bugs with support for newer frameworks by just passing CSharp as the language and allowing
                        // the tool to use a default.
                        var split = DOptions.Split(';');
                        foreach (var p in split)
                        {
                            if (string.IsNullOrEmpty(p))
                                continue;
                            if (string.IsNullOrWhiteSpace(p))
                                continue;
                            arguments.Add("-D" + p);
                        }
                    }

                    if (Error)
                        arguments.Add("-Werror");

                    if (ForceAtn)
                        arguments.Add("-Xforce-atn");

                    arguments.AddRange(SourceCodeFiles);

                    ProcessStartInfo startInfo = new ProcessStartInfo(java_executable, JoinArguments(arguments))
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    };

                    this.BuildMessages.Add(new BuildMessage(TraceLevel.Info,
                        "Executing command: \"" + startInfo.FileName + "\" " + startInfo.Arguments, "", 0, 0));

                    Process process = new Process();
                    process.StartInfo = startInfo;
                    process.ErrorDataReceived += HandleErrorDataReceived;
                    process.OutputDataReceived += HandleOutputDataReceived;
                    process.Start();
                    process.BeginErrorReadLine();
                    process.BeginOutputReadLine();
                    process.StandardInput.Dispose();
                    process.WaitForExit();

                    // At this point, regenerate the entire GeneratedCodeFiles list.
                    // This is because (1) it contains duplicates; (2) it contains
                    // files that really actually weren't generated. This can happen
                    // if the grammar was a Lexer grammar. (Note, I don't think it
                    // wise to look at the grammar file to figure out what it is, nor
                    // do I think it wise to expose a switch to the user for him to
                    // indicate what type of grammar it is.)
                    var gen_files = GeneratedCodeFiles.Distinct().ToList();
                    var clean_list = GeneratedCodeFiles.Distinct().ToList();
                    foreach (var fn in gen_files)
                    {
                        if (!File.Exists(fn))
                        {
                            clean_list.Remove(fn);
                        }
                    }
                    GeneratedCodeFiles.Clear();
                    foreach (var fn in clean_list) GeneratedCodeFiles.Add(fn);

                    return process.ExitCode == 0;
                }
            }
            catch (Exception e)
            {
                if (e is TargetInvocationException && e.InnerException != null)
                    e = e.InnerException;

                _buildMessages.Add(new BuildMessage(e.Message));
                throw;
            }
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

        private static readonly Regex GeneratedFileMessageFormat = new Regex(@"^Generating file '(?<OUTPUT>.*?)' for grammar '(?<GRAMMAR>.*?)'$", RegexOptions.Compiled);
        private static readonly Regex GeneratedFileMessageFormatJavaTool = new Regex(@"^(?<OUTPUT>[^:]*?)\s*:", RegexOptions.Compiled);

        private void HandleErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            HandleErrorDataReceived(e.Data);
        }

        private void HandleErrorDataReceived(string data)
        {
            if (string.IsNullOrEmpty(data))
                return;

            try
            {
                _buildMessages.Add(new BuildMessage(data));
            }
            catch (Exception ex)
            {
                if (Antlr4ClassGenerationTask.IsFatalException(ex))
                    throw;

                _buildMessages.Add(new BuildMessage(ex.Message));
            }
        }

        private void HandleOutputDataReceivedFirstTime(object sender, DataReceivedEventArgs e)
        {
            string dep = e.Data as string;
            if (string.IsNullOrEmpty(dep))
                return;
            // Parse the dep string as "file-name1 : file-name2". Strip off the name
            // file-name1 and cache it.
            try
            {
                Match match = GeneratedFileMessageFormatJavaTool.Match(dep);
                if (!match.Success)
                {
                    return;
                }
                string fileName = match.Groups["OUTPUT"].Value;
                if (LanguageSourceExtensions.Contains(Path.GetExtension(fileName), StringComparer.OrdinalIgnoreCase))
                    GeneratedCodeFiles.Add(match.Groups["OUTPUT"].Value);
            }
            catch (Exception ex)
            {
                if (Antlr4ClassGenerationTask.IsFatalException(ex))
                    throw;

                _buildMessages.Add(new BuildMessage(ex.Message));
            }
        }

        private void HandleOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            HandleOutputDataReceived(e.Data);
        }

        private void HandleOutputDataReceived(string data)
        {
            if (string.IsNullOrEmpty(data))
                return;

            try
            {
                Match match = GeneratedFileMessageFormat.Match(data);
                if (!match.Success)
                {
                    _buildMessages.Add(new BuildMessage(data));
                    return;
                }

                string fileName = match.Groups["OUTPUT"].Value;
                if (LanguageSourceExtensions.Contains(Path.GetExtension(fileName), StringComparer.OrdinalIgnoreCase))
                    GeneratedCodeFiles.Add(match.Groups["OUTPUT"].Value);
            }
            catch (Exception ex)
            {
                _buildMessages.Add(new BuildMessage(ex.Message
                    + ex.StackTrace));

                if (Antlr4ClassGenerationTask.IsFatalException(ex))
                    throw;
            }
        }
    }
}
