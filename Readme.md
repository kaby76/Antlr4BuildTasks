# Antlr4BuildTasks

This is a modification of Hartwell's [Antlr4cs](https://github.com/tunnelvisionlabs/antlr4cs),
containing only the Antlr4 Task wrapper program, and is for compiling Antlr grammars into CS
source code during an MSBuild or Dotnet build. This package performs Antlr parser generation using a separately installed
Java tool chain and separately installed Antlr tool chain (a jar file).
This package is a Net Standard assembly that is
used by MSBuild/Dotnet with the target and props files contained in this package. It has been tested on Linux and Windows.
To set grammar specific options for the Antlr tool, use VS2017 file properties or set the options in the CSPROJ file.
This package supports only Antlr4 grammars.
Java 8 must be installed, and environment variable JAVA_HOME set. Antlr4 Java tool must be downloaded, and the
environment variable Antlr4BuildTasks set to the path of the jar file. 

Language support in Visual Studio 2017 itself is a separate product, not part of the build rules for Antlr grammar files,
which is what this package supports. You can use Hartwell’s [Antlr Language Support](https://marketplace.visualstudio.com/items?itemName=SamHarwell.ANTLRLanguageSupport)
extension, my own [AntlrVSIX](https://marketplace.visualstudio.com/items?itemName=KenDomino.AntlrVSIX) extension, or another.

You can see the NuGet package in action [here on Youtube](https://www.youtube.com/watch?v=Flfequp_Dy4).
The Net Core code for the Antlr Hello World example is [here in Github](https://github.com/kaby76/AntlrHW).
