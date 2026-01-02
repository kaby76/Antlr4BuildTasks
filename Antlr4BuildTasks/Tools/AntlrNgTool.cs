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
    /// Implementation for the Node.js-based antlr-ng tool
    /// </summary>
    public class AntlrNgTool : AntlrToolBase
    {
        public string NodeExecutable { get; set; }
        public string AntlrNgPath { get; set; }

        public AntlrNgTool() : base()
        {
        }

        public override string ToolName => "antlr-ng";

        public override bool Setup()
        {
            // Validate Node.js is available
            if (string.IsNullOrEmpty(NodeExecutable))
            {
                MessageQueue.EnqueueMessage(Message.BuildErrorMessage(
                    "Node.js executable path not set"));
                return false;
            }

            if (!File.Exists(NodeExecutable))
            {
                MessageQueue.EnqueueMessage(Message.BuildErrorMessage(
                    $"Node.js executable not found at: {NodeExecutable}"));
                return false;
            }

            // Validate antlr-ng is available
            if (string.IsNullOrEmpty(AntlrNgPath))
            {
                MessageQueue.EnqueueMessage(Message.BuildErrorMessage(
                    "antlr-ng path not set"));
                return false;
            }

            if (!File.Exists(AntlrNgPath))
            {
                MessageQueue.EnqueueMessage(Message.BuildErrorMessage(
                    $"antlr-ng not found at: {AntlrNgPath}"));
                return false;
            }

            return true;
        }

        public override bool DiscoverGeneratedFiles(
            IEnumerable<string> grammarFiles,
            out List<string> generatedFiles,
            out List<string> generatedCodeFiles)
        {
            // antlr-ng doesn't have a -depend mode, so we predict the generated files
            // based on grammar file analysis
            generatedFiles = new List<string>();
            generatedCodeFiles = new List<string>();

            foreach (var grammarFile in grammarFiles)
            {
                try
                {
                    var predictedFiles = PredictGeneratedFiles(grammarFile);
                    generatedFiles.AddRange(predictedFiles);
                    generatedCodeFiles.AddRange(predictedFiles.Where(f => f.EndsWith(".cs")));
                }
                catch (Exception ex)
                {
                    MessageQueue.EnqueueMessage(Message.BuildWarningMessage(
                        $"Failed to predict generated files for {grammarFile}: {ex.Message}"));
                }
            }

            MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                $"Predicted {generatedCodeFiles.Count} code files from {grammarFiles.Count()} grammar files."));

            return true;
        }

        public override bool GenerateFiles(IEnumerable<string> grammarFiles, out bool success)
        {
            var arguments = BuildGenerationArguments(grammarFiles);

            bool executed = ExecuteProcess(
                NodeExecutable,
                arguments,
                HandleStdoutDataReceived,
                HandleStderrDataReceived,
                out int exitCode);

            success = executed && exitCode == 0;
            return executed;
        }

        private List<string> BuildGenerationArguments(IEnumerable<string> grammarFiles)
        {
            var arguments = new List<string>();

            // antlr-ng script
            arguments.Add(NormalizePath(AntlrNgPath));

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
                arguments.Add("--atn");

            // Encoding
            if (!string.IsNullOrEmpty(Encoding))
            {
                arguments.Add("-e");
                arguments.Add(Encoding);
            }

            // Listener/Visitor generation
            // Note: antlr-ng uses boolean values instead of -no-listener/-no-visitor
            if (GenerateListener)
            {
                arguments.Add("-l");
            }
            else
            {
                arguments.Add("-l");
                arguments.Add("false");
            }

            if (GenerateVisitor)
            {
                arguments.Add("-v");
            }
            else
            {
                arguments.Add("-v");
                arguments.Add("false");
            }

            // Package/namespace
            if (!string.IsNullOrEmpty(Package) && !string.IsNullOrWhiteSpace(Package))
            {
                arguments.Add("-p");
                arguments.Add(Package);
            }

            // Grammar options
            // Always ensure language=CSharp is set for antlr-ng
            var hasLanguageOption = false;
            if (!string.IsNullOrEmpty(DOptions))
            {
                foreach (var option in SplitAndFilterList(DOptions))
                {
                    if (option.StartsWith("language=", StringComparison.OrdinalIgnoreCase))
                        hasLanguageOption = true;
                    arguments.Add("-D");
                    arguments.Add(option);
                }
            }

            // Add language=CSharp if not already specified
            if (!hasLanguageOption)
            {
                arguments.Add("-D");
                arguments.Add("language=CSharp");
            }

            // Add separator to prevent grammar files from being interpreted as option values
            arguments.Add("--");

            // Grammar files
            arguments.AddRange(grammarFiles.Select(NormalizePath));

            return arguments;
        }

        private List<string> PredictGeneratedFiles(string grammarFile)
        {
            var result = new List<string>();
            var grammarName = Path.GetFileNameWithoutExtension(grammarFile);
            var outputDir = NormalizePath(OutputDirectory);

            // Read grammar file to determine if it's a lexer, parser, or combined grammar
            var grammarContent = File.ReadAllText(grammarFile);

            bool isLexer = Regex.IsMatch(grammarContent, @"^\s*lexer\s+grammar\s+", RegexOptions.Multiline);
            bool isParser = Regex.IsMatch(grammarContent, @"^\s*parser\s+grammar\s+", RegexOptions.Multiline);
            bool isCombined = !isLexer && !isParser; // Combined grammar if no explicit type

            // Predict generated files based on grammar type
            if (isLexer || isCombined)
            {
                // Lexer files
                result.Add(Path.Combine(outputDir, $"{grammarName}Lexer.cs"));
                result.Add(Path.Combine(outputDir, $"{grammarName}.tokens"));

                if (GenerateListener)
                {
                    // Lexers don't generate listeners in standard ANTLR
                }
            }

            if (isParser || isCombined)
            {
                // Parser files
                result.Add(Path.Combine(outputDir, $"{grammarName}Parser.cs"));

                if (GenerateListener)
                {
                    result.Add(Path.Combine(outputDir, $"{grammarName}Listener.cs"));
                    result.Add(Path.Combine(outputDir, $"{grammarName}BaseListener.cs"));
                }

                if (GenerateVisitor)
                {
                    result.Add(Path.Combine(outputDir, $"{grammarName}Visitor.cs"));
                    result.Add(Path.Combine(outputDir, $"{grammarName}BaseVisitor.cs"));
                }
            }

            // Combined grammars also generate .interp files
            if (isCombined)
            {
                result.Add(Path.Combine(outputDir, $"{grammarName}Lexer.interp"));
                result.Add(Path.Combine(outputDir, $"{grammarName}Parser.interp"));
            }
            else if (isLexer)
            {
                result.Add(Path.Combine(outputDir, $"{grammarName}Lexer.interp"));
            }
            else if (isParser)
            {
                result.Add(Path.Combine(outputDir, $"{grammarName}Parser.interp"));
            }

            // ATN files
            if (GenerateATN)
            {
                if (isLexer || isCombined)
                    result.Add(Path.Combine(outputDir, $"{grammarName}Lexer.dot"));
                if (isParser || isCombined)
                    result.Add(Path.Combine(outputDir, $"{grammarName}Parser.dot"));
            }

            return result;
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

            var line = e.Data;

            // antlr-ng error/warning format may differ from Java ANTLR
            // Adjust parsing as needed based on actual output
            if (line.ToLower().Contains("error"))
            {
                MessageQueue.EnqueueMessage(Message.BuildErrorMessage(line));
            }
            else if (line.ToLower().Contains("warning"))
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
