using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Antlr4.Build.Tasks;
using Antlr4.Build.Tasks.Util;

namespace Antlr4.Build.Tasks.Tools
{
    /// <summary>
    /// Base class providing common functionality for ANTLR tool implementations
    /// </summary>
    public abstract class AntlrToolBase : IAntlrTool
    {
        protected AntlrToolBase()
        {
        }

        public abstract string ToolName { get; }

        public string OutputDirectory { get; set; }
        public string LibPath { get; set; }
        public bool GenerateATN { get; set; }
        public bool EnableLogging { get; set; }
        public bool LongMessages { get; set; }
        public string Encoding { get; set; } = "UTF-8";
        public bool GenerateListener { get; set; } = true;
        public bool GenerateVisitor { get; set; } = true;
        public string Package { get; set; }
        public string DOptions { get; set; }
        public bool TreatWarningsAsErrors { get; set; }
        public bool ForceATN { get; set; }

        public abstract bool Setup();

        public abstract bool DiscoverGeneratedFiles(
            IEnumerable<string> grammarFiles,
            out List<string> generatedFiles,
            out List<string> generatedCodeFiles);

        public abstract bool GenerateFiles(IEnumerable<string> grammarFiles, out bool success);

        /// <summary>
        /// Joins command-line arguments, quoting those with spaces or special characters
        /// </summary>
        protected string JoinArguments(List<string> arguments)
        {
            var sb = new StringBuilder();
            foreach (var arg in arguments)
            {
                if (sb.Length > 0)
                    sb.Append(' ');

                // Check if argument needs quoting
                if (arg.Contains(' ') || arg.Contains('"'))
                {
                    sb.Append('"');
                    // Escape any quotes in the argument
                    sb.Append(arg.Replace("\"", "\\\""));
                    sb.Append('"');
                }
                else
                {
                    sb.Append(arg);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Executes a process and waits for it to complete
        /// </summary>
        protected bool ExecuteProcess(
            string executable,
            List<string> arguments,
            DataReceivedEventHandler outputHandler,
            DataReceivedEventHandler errorHandler,
            out int exitCode)
        {
            try
            {
                var startInfo = new ProcessStartInfo(executable, JoinArguments(arguments))
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };

                MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                    $"Executing command: \"{startInfo.FileName}\" {startInfo.Arguments}"));

                using (var process = new Process())
                {
                    process.StartInfo = startInfo;

                    if (errorHandler != null)
                        process.ErrorDataReceived += errorHandler;

                    if (outputHandler != null)
                        process.OutputDataReceived += outputHandler;

                    process.Start();
                    process.BeginErrorReadLine();
                    process.BeginOutputReadLine();
                    process.StandardInput.Dispose();
                    process.WaitForExit();

                    exitCode = process.ExitCode;

                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                        $"Finished executing {ToolName} command with exit code {exitCode}."));

                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageQueue.EnqueueMessage(Message.BuildErrorMessage(
                    $"Failed to execute {ToolName}: {ex.Message}"));
                exitCode = -1;
                return false;
            }
        }

        /// <summary>
        /// Normalizes path separators for the tool (forward slashes)
        /// </summary>
        protected string NormalizePath(string path)
        {
            return path?.Replace("\\", "/");
        }

        /// <summary>
        /// Splits a semicolon-separated list and filters empty entries
        /// </summary>
        protected IEnumerable<string> SplitAndFilterList(string list)
        {
            if (string.IsNullOrEmpty(list))
                return Enumerable.Empty<string>();

            return list.Split(';')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s));
        }
    }
}
