# Antlr4BuildTasks

This is a modification of Hartwell's [Antlr4cs](https://github.com/tunnelvisionlabs/antlr4cs),
containing the Antlr4 Task wrapper program,
the targets and props files for Antlr files with MSBuild,
and a file properties schema for Antlr files with Visual Studio IDE.
The purpose of the package is to integrate the Antlr tool
into the build, generating the source code for the parser from grammar,
compile and link using MSBuild or Dotnet. Using this package, you don't have to manually
run the Antlr tool outside the IDE, then go back to the IDE to complete the build. It is
all done seamlessly in the IDE. This project also assumes you are compiling for C# NET,
using the Antlr runtime library for NET, [Antlr4.Runtime.Standard](https://www.nuget.org/packages/Antlr4.Runtime.Standard).

This package uses a separately installed
Java tool chain and Antlr tool chain (a jar file), which you
will have to install yourself. The advantage of
decoupling the Antlr tool from the package allows one to work
with the latest version of Antlr, instead of using
a version that is older.
The environment variable JAVA_HOME must be set for the Java installation.
The environment variable Antlr4BuildTasks set to the path of the jar file.
Both of these variables are checked by the targets file for MSBuild/Dotnet build.

This package is a Net Standard assembly and works on Linux or Windows. This package supports only Antlr4 grammars.

To set grammar specific options for the Antlr tool, use VS2017 file properties or set the options in the CSPROJ file.

Language support in Visual Studio 2017 itself--
e.g. colorized tagging of the Antlr grammar, go to definition, reformat, etc.--
is a separate product, not part of the build rules for Antlr grammar files,
which is what this package supports. You can use Hartwell’s [Antlr Language Support](https://marketplace.visualstudio.com/items?itemName=SamHarwell.ANTLRLanguageSupport)
extension, my own [AntlrVSIX](https://marketplace.visualstudio.com/items?itemName=KenDomino.AntlrVSIX) extension, or another.

You can see the NuGet package in action [here on Youtube](https://www.youtube.com/watch?v=Flfequp_Dy4).
The Net Core code for the Antlr Hello World example is [here in Github](https://github.com/kaby76/AntlrHW).
