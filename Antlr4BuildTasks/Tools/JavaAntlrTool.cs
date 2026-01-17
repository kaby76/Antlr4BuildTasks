using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Antlr4.Build.Tasks;
using Antlr4.Build.Tasks.Util;

namespace Antlr4.Build.Tasks.Tools
{
    /// <summary>
    /// Implementation for the Java-based ANTLR4 tool
    /// </summary>
    public class JavaAntlrTool : AntlrToolBase
    {
        private readonly List<string> _generatedCodeFiles = new List<string>();
        private readonly List<string> _generatedFiles = new List<string>();
        private bool _start = false;
        private readonly System.Text.StringBuilder _sb = new System.Text.StringBuilder();

        public string JavaExecutable { get; set; }
        public string AntlrJarPath { get; set; }

        public JavaAntlrTool() : base()
        {
        }

        public override string ToolName => "java";

        public override bool Setup()
        {
            // Setup is expected to be done by the caller (SetupJava, SetupAntlrToolJar)
            // This method just validates that the required components are available
            if (string.IsNullOrEmpty(JavaExecutable))
            {
                MessageQueue.EnqueueMessage(Message.BuildErrorMessage(
                    "Java executable path not set"));
                return false;
            }

            if (!File.Exists(JavaExecutable))
            {
                MessageQueue.EnqueueMessage(Message.BuildErrorMessage(
                    $"Java executable not found at: {JavaExecutable}"));
                return false;
            }

            if (string.IsNullOrEmpty(AntlrJarPath))
            {
                MessageQueue.EnqueueMessage(Message.BuildErrorMessage(
                    "ANTLR JAR path not set"));
                return false;
            }

            if (!File.Exists(AntlrJarPath))
            {
                MessageQueue.EnqueueMessage(Message.BuildErrorMessage(
                    $"ANTLR JAR not found at: {AntlrJarPath}"));
                return false;
            }

            return true;
        }

        public override bool DiscoverGeneratedFiles(
            IEnumerable<string> grammarFiles,
            out List<string> generatedFiles,
            out List<string> generatedCodeFiles)
        {
            _generatedCodeFiles.Clear();
            _generatedFiles.Clear();
            _start = false;
            _sb.Clear();

            foreach (var grammar_file in grammarFiles)
            {
                var arguments = BuildDependencyArguments(grammar_file);

                bool executed = ExecuteProcess(
                    JavaExecutable,
                    arguments,
                    HandleOutputDataReceivedFirstTime,
                    HandleStderrDataReceived,
                    out int exitCode);

                if (!executed || exitCode != 0)
                {
                    generatedFiles = new List<string>();
                    generatedCodeFiles = new List<string>();
                    return false;
                }

                MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                    $"The generated file list contains {_generatedCodeFiles.Count} items."));

                // Add tokens files since ANTLR Tool doesn't list them in -depend output
                var lexerFiles = _generatedCodeFiles.Where(s => s.EndsWith("Lexer.cs")).ToList();
                foreach (var lexerFile in lexerFiles)
                {
                    var directory = Path.GetDirectoryName(lexerFile);
                    var stem = Path.GetFileNameWithoutExtension(lexerFile);
                    var tokensFile = Path.Combine(directory, stem + ".tokens");
                    _generatedFiles.Add(tokensFile);
                }
            }

            generatedFiles = _generatedFiles;
            generatedCodeFiles = _generatedCodeFiles;
            return true;
        }

        public override bool GenerateFiles(IEnumerable<string> grammarFiles, out bool success)
        {
            bool executed = false;
            success = false;
            foreach (var grammar_file in grammarFiles)
            {
                var arguments = BuildGenerationArguments(grammar_file);

                executed = ExecuteProcess(
                    JavaExecutable,
                    arguments,
                    HandleStdoutDataReceived,
                    HandleStderrDataReceived,
                    out int exitCode);

                success = executed && exitCode == 0;
            }
            return executed;
        }

        private List<string> BuildDependencyArguments(string grammarFile)
        {
            var arguments = new List<string>();

            // Java classpath and main class
            arguments.Add("-cp");
            arguments.Add(NormalizePath(AntlrJarPath));
            arguments.Add("org.antlr.v4.Tool");

            // Dependency mode
            arguments.Add("-depend");

            // Common arguments
            AddCommonArguments(grammarFile.EndsWith("Parser.g4"), arguments);

            // Grammar files
            arguments.Add(grammarFile);

            return arguments;
        }

        private List<string> BuildGenerationArguments(string grammarFile)
        {
            var arguments = new List<string>();

            // Java classpath and main class
            arguments.Add("-cp");
            arguments.Add(NormalizePath(AntlrJarPath));
            arguments.Add("org.antlr.v4.Tool");

            // Common arguments
            AddCommonArguments(grammarFile.EndsWith("Parser.g4"), arguments);

            // Logging (only for generation, not dependency discovery)
            if (EnableLogging)
                arguments.Add("-Xlog");

            // Grammar files
            arguments.Add(grammarFile);

            return arguments;
        }

        private void AddCommonArguments(bool parser, List<string> arguments)
        {
            // Output directory
            arguments.Add("-o");
            arguments.Add(NormalizePath(OutputDirectory));

            // Library paths
            if (!string.IsNullOrEmpty(LibPath))
            {
                foreach (var path in SplitAndFilterList(LibPath))
                {
                    arguments.Add("-lib");
                    arguments.Add(NormalizePath(path));
                }
            }

            // ATN generation
            if (GenerateATN)
                arguments.Add("-atn");

            // Long messages
            if (LongMessages)
                arguments.Add("-long-messages");

            // Encoding
            if (!string.IsNullOrEmpty(Encoding))
            {
                arguments.Add("-encoding");
                arguments.Add(Encoding);
            }

            // Listener/Visitor generation
            arguments.Add(GenerateListener ? "-listener" : "-no-listener");
            arguments.Add(GenerateVisitor ? "-visitor" : "-no-visitor");

            // Package/namespace
            if (!string.IsNullOrEmpty(Package) && !string.IsNullOrWhiteSpace(Package))
            {
                arguments.Add("-package");
                arguments.Add(Package);
            }

            // Grammar options
            if (!string.IsNullOrEmpty(DOptions))
            {
                foreach (var option in SplitAndFilterList(DOptions))
                {
                    arguments.Add("-D" + option);
                }
            }

            // Treat warnings as errors
            if (TreatWarningsAsErrors)
                arguments.Add("-Werror");

            // Force ATN
            if (ForceATN)
                arguments.Add("-Xforce-atn");
        }

        private void HandleOutputDataReceivedFirstTime(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
                return;

            // Parse dependency output to extract generated files
            // Format: <output-file> : <input-file1> <input-file2> ...
            Regex regex = new Regex(@"^(?<OUTPUT>[^\n\r]+)\s{0,}[:]\s{1,}");
            var match = regex.Match(e.Data);

            if (match.Success && match.Groups["OUTPUT"].Length > 0)
            {
                var outputFile = match.Groups["OUTPUT"].Value.Trim();

                if (!_start)
                {
                    _start = true;
                    _sb.Clear();
                }
                if (outputFile.EndsWith(".g4"))
		    return;

		if (outputFile.IndexOf("Listener") >= 0 && outputFile.IndexOf("Lexer") >= 0)
			return;
		if (outputFile.IndexOf("BaseListener") >= 0 && outputFile.IndexOf("Lexer") >= 0)
			return;
		if (outputFile.IndexOf("Visitor") >= 0 && outputFile.IndexOf("Lexer") >= 0)
			return;
		if (outputFile.IndexOf("BaseVisitor") >= 0 && outputFile.IndexOf("Lexer") >= 0)
			return;

                _sb.Append(outputFile);
                _generatedFiles.Add(outputFile);

                if (outputFile.EndsWith(".cs"))
                {
                    _generatedCodeFiles.Add(outputFile);
                }
            }
        }

        private void HandleStdoutDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
                return;

            MessageQueue.EnqueueMessage(Message.BuildInfoMessage(e.Data));
        }

        private void HandleStderrDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
                return;

            // Parse ANTLR error/warning messages
            // Format: error(code): message
            // Format: warning(code): message
            var line = e.Data;

            if (line.Contains("error(") || line.ToLower().StartsWith("error"))
            {
                MessageQueue.EnqueueMessage(Message.BuildErrorMessage(line));
            }
            else if (line.Contains("warning(") || line.ToLower().StartsWith("warning"))
            {
                MessageQueue.EnqueueMessage(Message.BuildWarningMessage(line));
            }
            else
            {
                MessageQueue.EnqueueMessage(Message.BuildInfoMessage(line));
            }
        }
    }
}
