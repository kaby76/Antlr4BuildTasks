// Derived from Terence Parr, Sam Harwell.

namespace Antlr4.Build.Tasks
{
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using Directory = System.IO.Directory;
    using File = System.IO.File;
    using FileAttributes = System.IO.FileAttributes;
    using Path = System.IO.Path;
    using StringBuilder = System.Text.StringBuilder;

    public class RunAntlrTool : Task
    {
        private const string DefaultGeneratedSourceExtension = "g4";
        private List<string> _generatedCodeFiles = new List<string>();
        private List<string> _allGeneratedFiles = new List<string>();

        public RunAntlrTool()
        {
            this.GeneratedSourceExtension = DefaultGeneratedSourceExtension;
        }

        [Output] public ITaskItem[] AllGeneratedFiles
        {
            get { return this._allGeneratedFiles.Select(t => new TaskItem(t)).ToArray(); }
            set { this._allGeneratedFiles = value.Select(t => t.ItemSpec).ToList(); }
        }
        public string AntOutDir { get; set; }
        public string BuildTaskPath { get; set; }
        public string DOptions { get; set; }
        public string Encoding { get; set; }
        public bool Error { get; set; }
        public bool ForceAtn { get; set; }
        public bool GAtn { get; set; }
        [Output] public ITaskItem[] GeneratedCodeFiles
        {
            get
            {
                return this._generatedCodeFiles.Select(t => new TaskItem(t)).ToArray();
            }
        }
        public string GeneratedSourceExtension { get; set; }
        [Required] public string IntermediateOutputPath { get; set; }
        public string JavaExec { get; set; }
        public string LibPath { get; set; }
        public bool Listener { get; set; }
        public string Package { get; set; }
        [Required] public ITaskItem[] SourceCodeFiles { get; set; }
        public string TargetFrameworkVersion { get; set; }
        public ITaskItem[] TokensFiles { get; set; }
        public string ToolPath { get; set; }
        public bool Visitor { get; set; }

        public override bool Execute()
        {
            bool success = false;
            //System.Threading.Thread.Sleep(20000);
            try
            {
                MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                    "Starting Antlr4 Build Tasks. ToolPath is \""
                    + ToolPath + "\""));
                if (AntOutDir == null || AntOutDir == "")
                {
                    Log.LogMessage("Note AntOutDir is empty. Placing generated files in IntermediateOutputPath " + IntermediateOutputPath);
                    AntOutDir = IntermediateOutputPath;
                }
                Directory.CreateDirectory(AntOutDir);
                MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                    "JavaExec is \"" + JavaExec + "\""));
                if (JavaExec == null || JavaExec == "")
                {
                    if (System.Environment.OSVersion.Platform == PlatformID.Win32NT
                        || System.Environment.OSVersion.Platform == PlatformID.Win32S
                        || System.Environment.OSVersion.Platform == PlatformID.Win32Windows
                    )
                    {
                        MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                            "IntermediateOutputPath is \"" + IntermediateOutputPath + "\""));
                        var java_dir = IntermediateOutputPath
                                       + System.IO.Path.DirectorySeparatorChar +
                                       "Java";
                        JavaExec = java_dir
                            + System.IO.Path.DirectorySeparatorChar
                            + "jre"
                            + System.IO.Path.DirectorySeparatorChar
                            + "bin"
                            + System.IO.Path.DirectorySeparatorChar
                            + "java.exe";
                        MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                            "From now on, JavaExec is \"" + JavaExec + "\""));
                    }
                    else if (System.Environment.OSVersion.Platform == PlatformID.Unix
                        || System.Environment.OSVersion.Platform == PlatformID.MacOSX
                        )
                    {
                        MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                            "IntermediateOutputPath is \"" + IntermediateOutputPath + "\""));
                        var JavaExec = "/usr/bin/java";
                        MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                            "From now on, JavaExec is \"" + JavaExec + "\""));
                    }
                    else throw new Exception("Which OS??");
                }
                if (!File.Exists(JavaExec))
                    throw new Exception("Cannot find Java executable, currently set to "
                                        + "'" + JavaExec + "'");
                // Next find Java.
                string java_executable = null;
                if (!string.IsNullOrEmpty(JavaExec))
                {
                    java_executable = JavaExec;
                }
                else
                {
                    java_executable = JavaExec;
                    if (!File.Exists(java_executable))
                        throw new Exception("I haven't a clue where the Java executable is on this system. Quiting...");
                }
                if (ToolPath == null || ToolPath == "")
                {
                    if (System.Environment.OSVersion.Platform == PlatformID.Win32NT
                        || System.Environment.OSVersion.Platform == PlatformID.Win32S
                        || System.Environment.OSVersion.Platform == PlatformID.Win32Windows
                        || System.Environment.OSVersion.Platform == PlatformID.Unix
                        || System.Environment.OSVersion.Platform == PlatformID.MacOSX
                    )
                    {
                        var jar =
                            @"https://www.antlr.org/download/antlr-4.9-complete.jar";
                        string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                        string archive_path = Path.GetFullPath(assemblyPath
                           + System.IO.Path.DirectorySeparatorChar
                           + ".."
                           + System.IO.Path.DirectorySeparatorChar
                           + ".."
                           + System.IO.Path.DirectorySeparatorChar
                           + "build"
                           + System.IO.Path.DirectorySeparatorChar
                           + System.IO.Path.GetFileName(jar)
                        );
                        ToolPath = archive_path;
                    }
                    else throw new Exception("Which OS??");
                }
                _ = Path.IsPathRooted(ToolPath);
                if (!File.Exists(ToolPath))
                    throw new Exception("Cannot find Antlr4 jar file, currently set to "
                                        + "'" + ToolPath + "'");
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
                    arguments.Add("-cp");
                    arguments.Add(ToolPath);
                    arguments.Add("org.antlr.v4.Tool");
                    arguments.Add("-depend");
                    arguments.Add("-o");
                    arguments.Add(AntOutDir);
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
                    if (GAtn) arguments.Add("-atn");
                    if (!string.IsNullOrEmpty(Encoding))
                    {
                        arguments.Add("-encoding");
                        arguments.Add(Encoding);
                    }
                    arguments.Add(Listener ? "-listener" : "-no-listener");
                    arguments.Add(Visitor ? "-visitor" : "-no-visitor");
                    if (!(string.IsNullOrEmpty(Package) || string.IsNullOrWhiteSpace(Package)))
                    {
                        arguments.Add("-package");
                        arguments.Add(Package);
                    }
                    if (!string.IsNullOrEmpty(DOptions))
                    {
                        // The Antlr tool can take multiple -D options, but
                        // DOptions is just a string. We allow for multiple
                        // options by separating each with a semi-colon. At this
                        // point, convert each option into separate "-D" option
                        // arguments.
                        var split = DOptions.Split(';');
                        foreach (var p in split)
                        {
                            var q = p.Trim();
                            if (string.IsNullOrEmpty(q))
                                continue;
                            if (string.IsNullOrWhiteSpace(q))
                                continue;
                            arguments.Add("-D" + q);
                        }
                    }
                    if (Error) arguments.Add("-Werror");
                    if (ForceAtn) arguments.Add("-Xforce-atn");
                    arguments.AddRange(SourceCodeFiles?.Select(s => s.ItemSpec));
                    ProcessStartInfo startInfo = new ProcessStartInfo(java_executable, JoinArguments(arguments))
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    };
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                        "Executing command: \"" + startInfo.FileName + "\" " + startInfo.Arguments));
                    Process process = new Process();
                    process.StartInfo = startInfo;
                    process.ErrorDataReceived += HandleStderrDataReceived;
                    process.OutputDataReceived += HandleOutputDataReceivedFirstTime;
                    process.Start();
                    process.BeginErrorReadLine();
                    process.BeginOutputReadLine();
                    process.StandardInput.Dispose();
                    process.WaitForExit();
                    MessageQueue.EnqueueMessage( Message.BuildInfoMessage(
                        "Finished executing Antlr jar command."));
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                        "The generated file list contains " + _generatedCodeFiles.Count() + " items."));
                    if (process.ExitCode != 0)
                    {
                        return false;
                    }
                    // Add in tokens and interp files since Antlr Tool does not do that.
                    var old_list = _generatedCodeFiles.ToList();
                    var new_list = new List<string>();
                    foreach (var s in old_list)
                    {
                        if (Path.GetExtension(s) == ".tokens")
                        {
                            var interp = s.Replace(Path.GetExtension(s), ".interp");
                            new_list.Append(interp);
                        }
                        else
                            new_list.Add(s);
                    }
                    _generatedCodeFiles = new_list.ToList();
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
                    arguments.Add(AntOutDir);
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
                    if (GAtn) arguments.Add("-atn");
                    if (!string.IsNullOrEmpty(Encoding))
                    {
                        arguments.Add("-encoding");
                        arguments.Add(Encoding);
                    }
                    arguments.Add(Listener ? "-listener" : "-no-listener");
                    arguments.Add(Visitor ? "-visitor" : "-no-visitor");
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
                    if (Error) arguments.Add("-Werror");
                    if (ForceAtn) arguments.Add("-Xforce-atn");
                    arguments.AddRange(SourceCodeFiles.Select(s => s.ItemSpec));
                    ProcessStartInfo startInfo = new ProcessStartInfo(java_executable, JoinArguments(arguments))
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    };
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                        "Executing command: \"" + startInfo.FileName + "\" " + startInfo.Arguments));
                    Process process = new Process();
                    process.StartInfo = startInfo;
                    process.ErrorDataReceived += HandleStderrDataReceived;
                    process.OutputDataReceived += HandleStdoutDataReceived;
                    process.Start();
                    process.BeginErrorReadLine();
                    process.BeginOutputReadLine();
                    process.StandardInput.Dispose();
                    process.WaitForExit();
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                        "Finished executing Antlr jar command."));
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                        "The generated file list contains " + _generatedCodeFiles.Count() + " items."));
                    foreach (var fn in _generatedCodeFiles)
                        MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Generated file " + fn));
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                        "Executing command: \"" + startInfo.FileName + "\" " + startInfo.Arguments));
                    // At this point, regenerate the entire GeneratedCodeFiles list.
                    // This is because (1) it contains duplicates; (2) it contains
                    // files that really actually weren't generated. This can happen
                    // if the grammar was a Lexer grammar. (Note, I don't think it
                    // wise to look at the grammar file to figure out what it is, nor
                    // do I think it wise to expose a switch to the user for him to
                    // indicate what type of grammar it is.)
                    var new_code_list = new List<string>();
                    var new_all_list = new List<string>();
                    foreach (var fn in _generatedCodeFiles.Distinct().ToList())
                    {
                        var ext = Path.GetExtension(fn);
                        if (File.Exists(fn) && !(ext == ".g4" && ext == ".g4"))
                            new_all_list.Add(fn);
                        if ((ext == ".cs" || ext == ".java" || ext == ".cpp" ||
                             ext == ".php" || ext == ".js") && File.Exists(fn))
                            new_code_list.Add(fn);
                    }
                    foreach (var fn in _allGeneratedFiles.Distinct().ToList())
                    {
                        var ext = Path.GetExtension(fn);
                        if (File.Exists(fn) && !(ext == ".g4" && ext == ".g4"))
                            new_all_list.Add(fn);
                        if ((ext == ".cs" || ext == ".java" || ext == ".cpp" ||
                             ext == ".php" || ext == ".js") && File.Exists(fn))
                            new_code_list.Add(fn);
                    }
                    _allGeneratedFiles = new_all_list.ToList();
                    _generatedCodeFiles = new_code_list.ToList();
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("List of generated files "
                                   + String.Join(" ", _allGeneratedFiles)));
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("List of generated code files "
                                   + String.Join(" ", _generatedCodeFiles)));
                    success = process.ExitCode == 0;
                }
            }
            catch (Exception exception)
            {
                ProcessExceptionAsBuildMessage(exception);
                success = false;
            }
            finally
            {
                if (!success)
                {
                    MessageQueue.EnqueueMessage(Message.BuildErrorMessage("The Antlr4 tool failed."));
                    MessageQueue.MutateToError();
                    _generatedCodeFiles.Clear();
                    _allGeneratedFiles.Clear();
                }
                MessageQueue.EmptyMessageQueue(Log);
            }
            return success;
        }

        private void ProcessExceptionAsBuildMessage(Exception exception)
        {
            MessageQueue.EnqueueMessage(Message.BuildCrashMessage(exception.Message
                + exception.StackTrace));
        }

        internal static bool IsFatalException(Exception exception)
        {
            while (exception != null)
            {
                if (exception is OutOfMemoryException)
                {
                    return true;
                }

                if (!(exception is TypeInitializationException) && !(exception is TargetInvocationException))
                {
                    break;
                }

                exception = exception.InnerException;
            }

            return false;
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

        private void HandleStderrDataReceived(object sender, DataReceivedEventArgs e)
        {
            HandleStderrDataReceived(e.Data);
        }

        private void HandleStderrDataReceived(string data)
        {
            //System.Console.Error.WriteLine("XXX3 " + data);
            if (string.IsNullOrEmpty(data))
                return;

            try
            {
                MessageQueue.EnqueueMessage(Message.BuildDefaultMessage(data));
            }
            catch (Exception ex)
            {
                if (RunAntlrTool.IsFatalException(ex))
                    throw;

                MessageQueue.EnqueueMessage(Message.BuildCrashMessage(ex.Message));
            }
        }

        private void HandleOutputDataReceivedFirstTime(object sender, DataReceivedEventArgs e)
        {
            string str = e.Data as string;
            if (string.IsNullOrEmpty(str))
                return;

            MessageQueue.EnqueueMessage(new Message("Yo got " + str + " from Antlr Tool."));

            // There could all kinds of shit coming out of the Antlr Tool, so we need to
            // take care of what to record.
            // Parse the dep string as "file-name1 : file-name2". Strip off the name
            // file-name1 and save it away.
            try
            {
                Regex regex = new Regex(@"^(?<OUTPUT>\S+)\s*:");
                Match match = regex.Match(str);
                if (!match.Success)
                {
                    MessageQueue.EnqueueMessage(new Message("Yo didn't fit pattern!"));
                    return;
                }
                string fn = match.Groups["OUTPUT"].Value;
                var ext = Path.GetExtension(fn);
                if (ext == ".cs" || ext == ".java" || ext == ".cpp" ||
                    ext == ".php" || ext == ".js" || ext == ".tokens" || ext == ".interp" ||
                    ext == ".dot")
                    _generatedCodeFiles.Add(fn);
            }
            catch (Exception ex)
            {
                if (RunAntlrTool.IsFatalException(ex))
                    throw;

                MessageQueue.EnqueueMessage(new Message(ex.Message));
            }
        }

        private void HandleStdoutDataReceived(object sender, DataReceivedEventArgs e)
        {
            HandleStdoutDataReceived(e.Data);
        }

        private void HandleStdoutDataReceived(string data)
        {
            if (string.IsNullOrEmpty(data))
                return;

            MessageQueue.EnqueueMessage(new Message("Yo got " + data + " from Antlr Tool."));

            try
            {
                Match match = GeneratedFileMessageFormat.Match(data);
                if (!match.Success)
                {
                    MessageQueue.EnqueueMessage(new Message(data));
                    return;
                }

                string fileName = match.Groups["OUTPUT"].Value;
                _generatedCodeFiles.Add(match.Groups["OUTPUT"].Value);
            }
            catch (Exception ex)
            {
                MessageQueue.EnqueueMessage(Message.BuildErrorMessage(ex.Message
                                                            + ex.StackTrace));

                if (RunAntlrTool.IsFatalException(ex))
                    throw;
            }
        }
    }
}
