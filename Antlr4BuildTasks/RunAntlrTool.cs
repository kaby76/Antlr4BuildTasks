// Derived from Terence Parr, Sam Harwell.

namespace Antlr4.Build.Tasks
{
    using System.Diagnostics;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Net;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using StringBuilder = System.Text.StringBuilder;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Directory = System.IO.Directory;
    using File = System.IO.File;
    using FileAttributes = System.IO.FileAttributes;
    using Path = System.IO.Path;

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
            get
            {
                return this._allGeneratedFiles.Select(t => new TaskItem(t)).ToArray();
            }
            set
            {
                this._allGeneratedFiles = value.Select(t => t.ItemSpec).ToList();
            }
        }

        public string AntOutDir
        {
            get;
            set;
        }

        public string BuildTaskPath
        {
            get;
            set;
        }

        public string DOptions
        {
            get;
            set;
        }

        public string Encoding
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

        public bool GAtn
        {
            get;
            set;
        }

        [Output] public ITaskItem[] GeneratedCodeFiles
        {
            get
            {
                return this._generatedCodeFiles.Select(t => new TaskItem(t)).ToArray();
            }
        }

        public string GeneratedSourceExtension
        {
            get;
            set;
        }

        [Required] public string IntermediateOutputPath
        {
            get;
            set;
        }

        public string JavaExec
        {
            get;
            set;
        }

        public string LibPath
        {
            get;
            set;
        }

        public bool Listener
        {
            get;
            set;
        }

        public string Package
        {
            get;
            set;
        }

        [Required] public ITaskItem[] SourceCodeFiles
        {
            get;
            set;
        }

        public string TargetFrameworkVersion
        {
            get;
            set;
        }

        public ITaskItem[] TokensFiles
        {
            get;
            set;
        }

        public string ToolPath
        {
            get;
            set;
        }

        public bool Visitor
        {
            get;
            set;
        }



        public override bool Execute()
        {
            bool success = false;

            //System.Threading.Thread.Sleep(20000);

            try
            {
                ProcessBuildMessage(new BuildMessage(
                    TraceLevel.Info,
                    "Starting Antlr4 Build Tasks. ToolPath is \""
                    + ToolPath + "\"", "", 0, 0));

                if (AntOutDir == null || AntOutDir == "")
                {
                    Log.LogMessage("Note AntOutDir is empty. Placing generated files in IntermediateOutputPath " + IntermediateOutputPath);
                    AntOutDir = IntermediateOutputPath;
                }
                Directory.CreateDirectory(AntOutDir);

                // First, find JAVA_EXE. This could throw an exception with error message.
                ProcessBuildMessage(new BuildMessage(
                    TraceLevel.Info, "JavaExec is \"" + JavaExec + "\"", "", 0, 0));
                
                if (JavaExec == null || JavaExec == "")
                {
                    if (System.Environment.OSVersion.Platform == PlatformID.Win32NT
                        || System.Environment.OSVersion.Platform == PlatformID.Win32S
                        || System.Environment.OSVersion.Platform == PlatformID.Win32Windows
                    )
                    {
                        ProcessBuildMessage(new BuildMessage(
                            TraceLevel.Info, "IntermediateOutputPath is \"" + IntermediateOutputPath + "\"", "", 0, 0));
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
                        ProcessBuildMessage(new BuildMessage(
                            TraceLevel.Info, "From now on, JavaExec is \"" + JavaExec + "\"", "", 0, 0));
                    }
                    else throw new Exception("Which OS??");
                }

                if (!File.Exists(JavaExec))
                    throw new Exception("Cannot find Java executable, currently set to "
                                        + "'" + JavaExec + "'"
                                        + " Please set either the JAVA_EXEC environment variable, "
                                        + "or set a property for JAVA_EXEC in your CSPROJ file."
                                        + " The variable must be the full path name of the executable for Java."
                                        + " E.g., on Linux, export JAVA_EXEC=\"/usr/bin/java\". On Windows,"
                                        + " JAVA_EXEC=\"C:\\Program Files\\Java\\jdk-11.0.4\"");

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
                        throw new Exception("Yo, I haven't a clue where the Java executable is on this system. Crashing...");
                }

                if (ToolPath == null || ToolPath == "")
                {
                    if (System.Environment.OSVersion.Platform == PlatformID.Win32NT
                        || System.Environment.OSVersion.Platform == PlatformID.Win32S
                        || System.Environment.OSVersion.Platform == PlatformID.Win32Windows
                    )
                    {
                        var jar =
                            @"https://www.antlr.org/download/antlr-4.8-complete.jar";
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

                    arguments.AddRange(SourceCodeFiles?.Select(s => s.ItemSpec));

                    ProcessStartInfo startInfo = new ProcessStartInfo(java_executable, JoinArguments(arguments))
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    };

                    ProcessBuildMessage(new BuildMessage(TraceLevel.Info,
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

                    ProcessBuildMessage(new BuildMessage(TraceLevel.Info,
                        "Finished command", "", 0, 0));
                    ProcessBuildMessage(new BuildMessage(TraceLevel.Info,
                        "The generated file list contains " + _generatedCodeFiles.Count() + " items.", "", 0, 0));

                    if (process.ExitCode != 0) return false;

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

                    arguments.AddRange(SourceCodeFiles.Select(s => s.ItemSpec));

                    ProcessStartInfo startInfo = new ProcessStartInfo(java_executable, JoinArguments(arguments))
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    };

                    ProcessBuildMessage(new BuildMessage(TraceLevel.Info,
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

                    ProcessBuildMessage(new BuildMessage(TraceLevel.Info,
                        "Finished command", "", 0, 0));
                    ProcessBuildMessage(new BuildMessage(TraceLevel.Info,
                        "The generated file list contains " + _generatedCodeFiles.Count() + " items.", "", 0, 0));

                    foreach (var fn in _generatedCodeFiles)
                        ProcessBuildMessage(new BuildMessage("Generated file " + fn));
                    ProcessBuildMessage(new BuildMessage(TraceLevel.Info,
                        "Executing command: \"" + startInfo.FileName + "\" " + startInfo.Arguments, "", 0, 0));

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
                    Log.LogMessage("List of generated files "
                                   + String.Join(" ", _allGeneratedFiles));
                    Log.LogMessage("List of generated code files "
                                   + String.Join(" ", _generatedCodeFiles));
                    success = process.ExitCode == 0;
                }
            }
            catch (Exception exception)
            {
                ProcessExceptionAsBuildMessage(exception);
                success = false;
            }

            if (!success)
            {
                _generatedCodeFiles.Clear();
                _allGeneratedFiles.Clear();
            }

            return success;
        }

        private void ProcessExceptionAsBuildMessage(Exception exception)
        {
            ProcessBuildMessage(new BuildMessage(exception.Message
                + exception.StackTrace));
        }

        private void ProcessBuildMessage(BuildMessage message)
        {
            string errorCode;
            switch (message.Severity)
            {
                case TraceLevel.Error:
                    errorCode = "ANT02";
                    break;
                case TraceLevel.Warning:
                    errorCode = "ANT01";
                    break;
                case TraceLevel.Info:
                    errorCode = "ANT00";
                    break;
                default:
                    errorCode = "ANT00";
                    break;
            }
            var logMessage = message.Message;

            string subcategory = null;
            string helpKeyword = null;

            switch (message.Severity)
            {
            case TraceLevel.Error:
                this.Log.LogError(subcategory, errorCode, helpKeyword, message.FileName, message.LineNumber, message.ColumnNumber, 0, 0, logMessage);
                break;
            case TraceLevel.Warning:
                this.Log.LogWarning(subcategory, errorCode, helpKeyword, message.FileName, message.LineNumber, message.ColumnNumber, 0, 0, logMessage);
                break;
            case TraceLevel.Info:
                this.Log.LogMessage(MessageImportance.Normal, logMessage);
                break;
            case TraceLevel.Verbose:
                this.Log.LogMessage(MessageImportance.Low, logMessage);
                break;
            }
        }

        private void CreateBuildTaskWrapper()
        {

            if (false && this.TokensFiles != null && this.TokensFiles.Length > 0)
            {

                HashSet<string> copied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (ITaskItem taskItem in TokensFiles)
                {
                    string fileName = taskItem.ItemSpec;
                    if (!File.Exists(fileName))
                    {
                        Log.LogError("The tokens file '{0}' does not exist.", fileName);
                        continue;
                    }

                    string vocabName = Path.GetFileNameWithoutExtension(fileName);
                    if (!copied.Add(vocabName))
                    {
                        Log.LogWarning("The tokens file '{0}' conflicts with another tokens file in the same project.", fileName);
                        continue;
                    }

                    string target = Path.Combine(AntOutDir, Path.GetFileName(fileName));
                    if (!Path.GetExtension(target).Equals(".tokens", StringComparison.OrdinalIgnoreCase))
                    {
                        Log.LogError("The destination for the tokens file '{0}' did not have the correct extension '.tokens'.", target);
                        continue;
                    }

                    File.Copy(fileName, target, true);
                    File.SetAttributes(target, File.GetAttributes(target) & ~FileAttributes.ReadOnly);
                }
            }
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
                ProcessBuildMessage(new BuildMessage(data));
            }
            catch (Exception ex)
            {
                if (RunAntlrTool.IsFatalException(ex))
                    throw;

                ProcessBuildMessage(new BuildMessage(ex.Message));
            }
        }

        private void HandleOutputDataReceivedFirstTime(object sender, DataReceivedEventArgs e)
        {
            string str = e.Data as string;
            if (string.IsNullOrEmpty(str))
                return;

            ProcessBuildMessage(new BuildMessage("Yo got " + str + " from Antlr Tool."));

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
                    ProcessBuildMessage(new BuildMessage("Yo didn't fit pattern!"));
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

                ProcessBuildMessage(new BuildMessage(ex.Message));
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

            ProcessBuildMessage(new BuildMessage("Yo got " + data + " from Antlr Tool."));

            try
            {
                Match match = GeneratedFileMessageFormat.Match(data);
                if (!match.Success)
                {
                    ProcessBuildMessage(new BuildMessage(data));
                    return;
                }

                string fileName = match.Groups["OUTPUT"].Value;
                _generatedCodeFiles.Add(match.Groups["OUTPUT"].Value);
            }
            catch (Exception ex)
            {
                ProcessBuildMessage(new BuildMessage(ex.Message
                                                            + ex.StackTrace));

                if (RunAntlrTool.IsFatalException(ex))
                    throw;
            }
        }
    }
}
