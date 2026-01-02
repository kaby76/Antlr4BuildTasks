using System.Collections.Generic;

namespace Antlr4.Build.Tasks.Tools
{
    /// <summary>
    /// Interface for ANTLR tool implementations (Java-based ANTLR4, antlr-ng, etc.)
    /// </summary>
    public interface IAntlrTool
    {
        /// <summary>
        /// Gets the name of the tool (e.g., "java", "antlr-ng")
        /// </summary>
        string ToolName { get; }

        /// <summary>
        /// Discovers the list of files that will be generated without actually generating them.
        /// This is used for incremental build support.
        /// </summary>
        /// <param name="grammarFiles">List of grammar files to process</param>
        /// <param name="generatedFiles">Output list of files that will be generated</param>
        /// <param name="generatedCodeFiles">Output list of code files (.cs) that will be generated</param>
        /// <returns>True if successful, false otherwise</returns>
        bool DiscoverGeneratedFiles(
            IEnumerable<string> grammarFiles,
            out List<string> generatedFiles,
            out List<string> generatedCodeFiles);

        /// <summary>
        /// Generates parser/lexer files from grammar files
        /// </summary>
        /// <param name="grammarFiles">List of grammar files to process</param>
        /// <param name="success">Output parameter indicating if generation was successful</param>
        /// <returns>True if the process executed, false if there was an error starting the process</returns>
        bool GenerateFiles(IEnumerable<string> grammarFiles, out bool success);

        /// <summary>
        /// Sets up the tool environment (downloads required runtime, tool, etc.)
        /// </summary>
        /// <returns>True if setup was successful, false otherwise</returns>
        bool Setup();

        /// <summary>
        /// Gets or sets the output directory for generated files
        /// </summary>
        string OutputDirectory { get; set; }

        /// <summary>
        /// Gets or sets the library search paths for imported grammars
        /// </summary>
        string LibPath { get; set; }

        /// <summary>
        /// Gets or sets whether to generate ATN diagrams
        /// </summary>
        bool GenerateATN { get; set; }

        /// <summary>
        /// Gets or sets whether to enable logging
        /// </summary>
        bool EnableLogging { get; set; }

        /// <summary>
        /// Gets or sets whether to use long error messages
        /// </summary>
        bool LongMessages { get; set; }

        /// <summary>
        /// Gets or sets the encoding for grammar files
        /// </summary>
        string Encoding { get; set; }

        /// <summary>
        /// Gets or sets whether to generate listener classes
        /// </summary>
        bool GenerateListener { get; set; }

        /// <summary>
        /// Gets or sets whether to generate visitor classes
        /// </summary>
        bool GenerateVisitor { get; set; }

        /// <summary>
        /// Gets or sets the package/namespace for generated code
        /// </summary>
        string Package { get; set; }

        /// <summary>
        /// Gets or sets grammar-level options (semicolon-separated key=value pairs)
        /// </summary>
        string DOptions { get; set; }

        /// <summary>
        /// Gets or sets whether to treat warnings as errors
        /// </summary>
        bool TreatWarningsAsErrors { get; set; }

        /// <summary>
        /// Gets or sets whether to force ATN for all decisions
        /// </summary>
        bool ForceATN { get; set; }
    }
}
