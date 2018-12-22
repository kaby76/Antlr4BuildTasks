# Antlr4BuildTasks

This is a modification of Hartwell's [Antlr4cs](https://github.com/tunnelvisionlabs/antlr4cs),
containing only the Antlr4 Task wrapper program, and is for compiling Antlr grammars into CS
source code during an MSBuild or Dotnet build. This package performs Antlr parser generation using a separately installed
Java tool chain and separately installed Antlr tool chain (a jar file).
This package is a Net Standard assembly, works like any NuGet package, except that it is only
used by MSBuild and Dotnet. It has been tested on Linux and Windows.
To set grammar specific options for the Antlr tool, use VS2017 file properties or set the options in the CSPROJ file.
This package supports only Antlr4 grammars.
Java 8 must be installed, and environment variable JAVA_HOME set. Antlr4 Java tool must be downloaded, and the
environment variable Antlr4BuildTasks set to the path of the jar file. 
