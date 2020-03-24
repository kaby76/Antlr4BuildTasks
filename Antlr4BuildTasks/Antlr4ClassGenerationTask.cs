// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

namespace Antlr4.Build.Tasks
{
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

    public class Antlr4ClassGenerationTask
        : Task
    {
        private const string DefaultGeneratedSourceExtension = "g4";
        private List<ITaskItem> _generatedCodeFiles = new List<ITaskItem>();

        public Antlr4ClassGenerationTask()
        {
            this.GeneratedSourceExtension = DefaultGeneratedSourceExtension;
        }

        [Required]
        public string ToolPath
        {
            get;
            set;
        }

        [Required]
        public string JavaHome
        {
            get;
            set;
        }

        [Required]
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

        public string TargetFrameworkVersion
        {
            get;
            set;
        }

        public string BuildTaskPath
        {
            get;
            set;
        }

        public ITaskItem[] SourceCodeFiles
        {
            get;
            set;
        }

        public ITaskItem[] TokensFiles
        {
            get;
            set;
        }

        public string GeneratedSourceExtension
        {
            get;
            set;
        }

        public string[] LanguageSourceExtensions
        {
            get;
            set;
        }

        public bool ForceAtn
        {
            get;
            set;
        }

        [Required]
        public string JavaVendor
        {
            get;
            set;
        }

        [Required]
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

        [Output]
        public ITaskItem[] GeneratedCodeFiles
        {
            get
            {
                return this._generatedCodeFiles.ToArray();
            }
            set
            {
                this._generatedCodeFiles = new List<ITaskItem>(value);
            }
        }

        public override bool Execute()
        {
            bool success;

            //System.Threading.Thread.Sleep(20000);

            if (!Path.IsPathRooted(ToolPath))
                ToolPath = Path.Combine(Path.GetDirectoryName(BuildEngine.ProjectFileOfTaskNode), ToolPath);

            if (!Path.IsPathRooted(BuildTaskPath))
                BuildTaskPath = Path.Combine(Path.GetDirectoryName(BuildEngine.ProjectFileOfTaskNode), BuildTaskPath);

            try
            {
                AntlrClassGenerationTaskInternal wrapper = CreateBuildTaskWrapper();
                success = wrapper.Execute();

                if (success)
                {
                    _generatedCodeFiles.AddRange(wrapper.GeneratedCodeFiles.Select(file => (ITaskItem)new TaskItem(file)));
                }

                foreach (BuildMessage message in wrapper.BuildMessages)
                {
                    ProcessBuildMessage(message);
                }
            }
            catch (Exception exception)
            {
                if (IsFatalException(exception))
                    throw;

                ProcessExceptionAsBuildMessage(exception);
                success = false;
            }

            return success;
        }

        private void ProcessExceptionAsBuildMessage(Exception exception)
        {
            ProcessBuildMessage(new BuildMessage(exception.Message));
        }

        private void ProcessBuildMessage(BuildMessage message)
        {
            string logMessage;
            string errorCode;
            errorCode = Log.ExtractMessageCode(message.Message, out logMessage);
            if (string.IsNullOrEmpty(errorCode))
            {
                if (message.Message.StartsWith("Executing command:", StringComparison.Ordinal) && message.Severity == TraceLevel.Info)
                {
                    // This is a known informational message
                    logMessage = message.Message;
                }
                else
                {
                    errorCode = "AC1000";
                    logMessage = "Unknown build error: " + message.Message;
                }
            }

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

        private AntlrClassGenerationTaskInternal CreateBuildTaskWrapper()
        {
            AntlrClassGenerationTaskInternal wrapper = new AntlrClassGenerationTaskInternal();

            IList<string> sourceCodeFiles = null;
            if (this.SourceCodeFiles != null)
            {
                sourceCodeFiles = new List<string>(SourceCodeFiles.Length);
                foreach (ITaskItem taskItem in SourceCodeFiles)
                    sourceCodeFiles.Add(taskItem.ItemSpec);
            }

            if (this.TokensFiles != null && this.TokensFiles.Length > 0)
            {
                Directory.CreateDirectory(OutputPath);

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

                    string target = Path.Combine(OutputPath, Path.GetFileName(fileName));
                    if (!Path.GetExtension(target).Equals(".tokens", StringComparison.OrdinalIgnoreCase))
                    {
                        Log.LogError("The destination for the tokens file '{0}' did not have the correct extension '.tokens'.", target);
                        continue;
                    }

                    File.Copy(fileName, target, true);
                    File.SetAttributes(target, File.GetAttributes(target) & ~FileAttributes.ReadOnly);
                }
            }

            wrapper.ToolPath = ToolPath;
            wrapper.SourceCodeFiles = sourceCodeFiles;
            wrapper.TargetFrameworkVersion = TargetFrameworkVersion;
            wrapper.OutputPath = OutputPath;
            wrapper.LanguageSourceExtensions = LanguageSourceExtensions;
            wrapper.LibPath = LibPath;
            wrapper.GAtn = GAtn;
            wrapper.Encoding = Encoding;
            wrapper.Listener = Listener;
            wrapper.Visitor = Visitor;
            wrapper.Package = Package;
            wrapper.DOptions = DOptions;
            wrapper.Error = Error;
            wrapper.ForceAtn = ForceAtn;
            wrapper.JavaVendor = JavaVendor;
            wrapper.JavaInstallation = JavaInstallation;
            wrapper.JavaExecutable = JavaExecutable;
            wrapper.JavaHome = JavaHome;
            return wrapper;
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
    }
}
