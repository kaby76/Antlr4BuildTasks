# Antlr4BuildTasks

This is a modification of Hartwell's [Antlr4cs](https://github.com/tunnelvisionlabs/antlr4cs),
containing only the Antlr4 Task wrapper program, which is needed by Msbuild. All else is removed
so it can use whatever Antlr tool chain, albeit Java based. The package is only Net Standard assembly;
Msbuild doesn't care what the assembly is. But, when referenced, the refering assembly must be
a compatible Net Core, Net Framework, or Mono. 
This project is for VS2017, Antlr4 grammars only.
Prerequisites: (1) an Antlr4 Java tool must be downloaded. (2) Java 8 must be installed, and JAVA_HOME set. 
 
