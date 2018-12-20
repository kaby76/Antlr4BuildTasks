# Antlr4BuildTasks

This is a modification of Hartwell's [Antlr4cs](https://github.com/tunnelvisionlabs/antlr4cs),
containing only the Antlr4 Task wrapper program, which is needed by Msbuild. All else is removed
so it can use whatever Antlr tool chain, albeit Java based. The package is only Net Standard assembly
because the assembly, Antrl4BuildTasks.dll, is not linked with a referring assembly. It is only used by
MSBuild, and Msbuild 4.0 uses Net Standard assemblies just fine. However, when referenced, the referring
assembly must be a compatible version of Net Core, Net Framework, or Mono. I just assume you will be using
the latest. This package is in VS2017 for setting an Antlr .g4 file properties. This package supports only
Antlr4 grammars.
Prerequisites: (1) an Antlr4 Java tool must be downloaded. (2) Java 8 must be installed, and JAVA_HOME set.
The tool has been updated to check and output these errors.
 
