# Antlr4BuildTasks

This is a modification of Hartwell's [Antlr4cs](https://github.com/tunnelvisionlabs/antlr4cs),
containing only the Antlr4 Task wrapper program, which is needed by Msbuild. All else is removed
so it can use whatever Antlr tool chain, albeit Java based. The package is only Net Standard assembly
because the assembly, Antrl4BuildTasks.dll, is not linked with a referring assembly. It is only used by
MSBuild abd Dotnet, and these work with Net Standard assemblies just fine--both Linux and Windows.
However, when referenced, the referring assembly must be a compatible version of Net Core, Net Framework, or Mono.
To set grammar specific options for the Antlr tool, use VS2017 file properties or set the options in the CSPROJ file.
This package supports only Antlr4 grammars.
Java 8 must be installed, and environment variable JAVA_HOME set. Antlr4 Java tool must be downloaded, and the
environment variable Antlr4BuildTasks set to the path of the jar file. 
