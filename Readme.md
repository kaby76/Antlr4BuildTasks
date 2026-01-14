# Antlr4BuildTasks

[![Build](https://github.com/kaby76/Antlr4BuildTasks/workflows/.NET/badge.svg)](https://github.com/kaby76/Antlr4BuildTasks/actions?query=workflow%3A.NET)

### Purpose
The purpose of Antlr4BuildTasks is to simplify the builds for Antlr4 parser applications.

***The requirement for Antlr4BuildTasks is simple: require nothing, download everything.***


### History
Building Antlr4 programs is a laborious task. To build a program, you must:
* Install Java. You need to use Java version 11 or newer because the
tool requires it.
* Download the Antlr4 Tool .jar from the download area.
* Run the tool on you grammar. Many manually run the tool, but there
are plenty of questions how to do that because it is different over the OS
you are running. If you modify the grammar, but forget to regenerate the parser
and lexer, you end up with a version skew.
* For C#, you need to set up a `<PackageReference>` for the runtime Antlr4.Runtime.Standard.
If you are not careful, you may generate the parser and lexer with a different
version of the tool from the runtime.

Harwell's excellent [Antlr4cs](https://github.com/tunnelvisionlabs/antlr4cs),
was published in NuGet to simplify the build of C# Antlr programs. It was
published as three packages:

* [Antlr4](https://www.nuget.org/packages/Antlr4/) (which is a thin package
that simply requires the two following packages)
* [Antlr4.CodeGenerator](https://www.nuget.org/packages/Antlr4.CodeGenerator/)
* [Antlr4.Runtime](https://www.nuget.org/packages/Antlr4.Runtime/).

They were the official items for C# Antlr programs until [Antlr4.Runtime.Standard](https://www.nuget.org/packages/Antlr4.Runtime.Standard/)
became the official runtime for C#, which completely
replaces Antlr4.Runtime since ANTLR 4.7.

Unfortunately, since then, there is no "official" build package for C# Antlr programs.
Antlr4BuildTasks replaces the old Antlr4cs support with an updated and cleaned up
build tool.

> More details can be found in [this file](https://github.com/antlr/antlr4/blob/4.7.1/runtime/CSharp/README.md).

**You shouldn't use Harwell's packages any more if you want to use ANTLR 4.7 and above.**

### New Approach
Antlr4BuildTasks is a third-party set of build rules
[published in Nuget](https://www.nuget.org/packages/Antlr4BuildTasks/) as a package
for builds of C# projects containing Antlr4 grammars,
using the official Antl4.Runtime.Standard package.

When added as a `<PackageReference>` to your C# project,
Antlr4BuildTasks provides the rules to compile .g4's into parser code
via the Antlr4 Tool using Java, and compiled by the C# compiler,
for a seemless build for Antlr4 grammars using C#.

Antlr4BuildTasks automatically downloads a Java Runtime Environment (JRE)
and the Antlr tool jar file to generate the parser and lexer. You do not
need to set up anything. It can be used either at the command line
or within Visual Studio, and on Windows, Linux or Mac. The JRE is only required
to build your project, and never required or used after the build.

To use this package, add the Antlr4BuildTasks and Antlr4.Runtime.Standard packages
to your project. csproj file as shown below, otherwise you can use the "NuGet Package Manager":

````xml
<ItemGroup>
    <PackageReference Include="Antlr4.Runtime.Standard" Version="4.13.1" />
    <PackageReference Include="Antlr4BuildTasks" Version="12.13" PrivateAssets="all" IncludeAssets="build" />
</ItemGroup>
````
    
Then, you will need to tag each Antlr4 grammar file you want the Antlr tool to process. You can change the
"Build Action" property for the .g4 file from "None" to "ANTLR 4 grammar". Or, you can add the following lines
to your .csproj file for your "MyGrammar.g4" file:

````xml
<ItemGroup>
    <Antlr4 Include="MyGrammar.g4" />
</ItemGroup>
````

Notes:
* `<PrivateAssets>all</PrivateAssets>` is added to the package reference
so that when you run `dotnet publish`, the Antlr4BuildTasks.dll is not included
in your app; it's only used to build your application.
Antlr4BuildTasks.dll is not currently digitally signed.
* Antlr4BuildTasks downloads a JRE for your OS and places it in `$USERPROFILE/.jre/`
on Windows and `$HOME/.jre/` on Linux.
The package also downloads the Antlr4 tool JAR file and places it in `$USERPROFILE/.m2/`
on Windows and `$HOME/.m2/` on Linux.
* You can use a pattern for the `Include` attribute, e.g., `<Antlr4 Include="*.g4"/>`.

### Setting arguments to the Antlr tool

You can set the arguments to the Antlr Tool in Visual Studio by modifying the properties
for each grammar file. Or, you can modify the .csproj file to include the parameters you are
interested in setting:

````xml
<ItemGroup>
    <Antlr4 Include="MyGrammar.g4">
        <Listener>false</Listener>
        <Visitor>false</Visitor>
        <GAtn>true</GAtn>
        <Package>foo</Package>
        <Error>true</Error>
    </Antlr4>
</ItemGroup>
````

The `<Antlr4>` item supports the following parameters that are passed onto the Antlr tool:

* `<Listener>` -- A bool that specifies whether you want an
Antlr Listener class generated by the tool.
* `<Visitor>` -- A bool that specifies whether you want an
Antlr Visitor class generated by the tool.
* `<GAtn>` -- A bool that specifies whether you want
Antlr to generate the ATN Dot diagrams.
* `<Package>` -- An C# identifier that specifies the namespace to wrap
the generated classes in.
* `<Error>` -- Use this to specify whether you want the tool to
flag warnings as errors and stop a build.
* `<LibPath>` -- A string that specifies the path for token and grammar files
for the Antlr tool.
* `<Encoding>` -- A string that specifies the encoding of the input grammars.
* `<DOptions>` -- A list of `<option>=<value>`, passed to the Antlr tool. E.g.,
language=Java. Multiple options can be specified using a semi-colon separating each.
* `<JavaExec>` -- A semi-colon separated list of one of the following: Full path name of the Java executable, or PATH indicating
to search PATH for an executable, or DOWNLOAD to download and use the `<JavaDownloadDirectory>`.
* `<AllowAntlr4cs>` -- Allow both `<PackageReference>` to Antlr4.Runtime and Antlr4.Runtime.Standard. (NB, you will need to handle aliasing of one package. See https://github.com/kaby76/Antlr4BuildTasks/issues/32)
* `<JavaDownloadDirectory>` -- Full path of directory for downloaded JRE compressed and uncompressed files.
* `<AntlrToolJarDownloadDir>` -- Full path of the directory for download and use Antlr4 tool jar. If not set the default value is `$USERPROFILE/.m2/` and `$HOME/.m2/` to Windows and Linux respectively.
* `<Log>` -- Adds'-Xlog' to Antlr4 Tool call, which turns on logging.
* `<LongMessages>` -- Add '-long-messages' to Antlr4 Tool call, which turns on long messages.
* `<ToolType>` -- Specifies which ANTLR tool to use: `java` (default) for the standard Java-based ANTLR4 tool, or `antlr-ng` for the Node.js-based antlr-ng tool.
* `<NodeExec>` -- Path to Node.js executable (only used when `ToolType` is `antlr-ng`). Default is `PATH` to search system PATH.
* `<AntlrNgPath>` -- Full path to antlr-ng CLI script (only used when `ToolType` is `antlr-ng`). If blank, searches common npm installation locations.

### Using antlr-ng (Node.js-based ANTLR tool)

As an alternative to the Java-based ANTLR tool, you can use [antlr-ng](https://github.com/antlr-ng/antlr-ng),
a Node.js-based reimplementation of ANTLR that doesn't require Java.

**Prerequisites:**
- Node.js 20.x or later must be installed
- antlr-ng must be installed globally (`npm install -g antlr-ng`) or locally in your project (`npm install --save-dev antlr-ng`)

**To use antlr-ng, set the `ToolType` property:**

````xml
<ItemGroup>
    <Antlr4 Include="MyGrammar.g4">
        <ToolType>antlr-ng</ToolType>
        <Visitor>true</Visitor>
        <Package>MyNamespace</Package>
    </Antlr4>
</ItemGroup>
````

**Benefits of using antlr-ng:**
- No Java runtime required
- Faster startup time (no JVM initialization)
- Native Node.js integration
- Smaller footprint

**Note:** antlr-ng automatically generates C# code when used with Antlr4BuildTasks. Most ANTLR options are supported,
though some advanced Java-specific options (like `-Xlog`, `-Werror`) may not have antlr-ng equivalents.

### Customizing the Antlr4 tool JAR download location

By default, Antlr4BuildTasks downloads the ANTLR tool JAR from `https://www.antlr.org/download/`, falling back
to Maven Central if the ANTLR website is unavailable. You can customize this behavior using the `AntlrProbePath` property.

The `AntlrProbePath` property is a semicolon-separated list of URIs to search for the ANTLR tool JAR.

You can use `file://` URIs to point to a local directory containing the JAR:

````xml
<PropertyGroup>
    <AntlrProbePath>file:///$(MSBuildProjectDirectory)/external/</AntlrProbePath>
</PropertyGroup>
````

The JAR file should be named `antlr4-{version}-complete.jar` (e.g., `antlr4-4.13.1-complete.jar`).

The Antlr4 tool generates files that produce a lot of compiler warnings for code
set with `CLSCompliant=false`. This package adds in code to ignore these warnings
so you don't need to modify your .csproj file.

### Conversion from Antlr4.CodeGenerator/Antlr4.Runtime to Antlr4BuildTasks/Antlr4.Runtime.Standard

In the .csproj file, change items from

````xml
<ItemGroup>
    <Antlr4 Update="arithmetic.g4">
        etc etc etc
    </Antlr4>
</ItemGroup>
````

to

````xml
<ItemGroup>
    <Antlr4 Include="arithmetic.g4" />
</ItemGroup>
````

Change package references from

````xml
<ItemGroup>
    <PackageReference Include="Antlr4.CodeGenerator" Version="4.6.6">
        <PrivateAssets>all</PrivateAssets>
        <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Antlr4.Runtime" Version="4.6.6" />
</ItemGroup>
````

to

````xml
<ItemGroup>
    <PackageReference Include="Antlr4BuildTasks" Version="12.13" PrivateAssets="all" />
    <PackageReference Include="Antlr4.Runtime.Standard" Version="4.13.1" />
</ItemGroup>
````

Antlr4BuildTasks examines PropertyGroup `AntlrProbePath`, a string of URI
separated by semicolon, to find the version
of the Antlr JAR file that you are looking for. Antlr4BuildTasks will look for a .jar
with version as specified in the Antlr4.Runtime.Standard PackageReference (e.g., 4.13.1).
_I recommend that you just use the PackageReference's defined above._

### Visual Studio IDE or Visual Studio Code integration

The package here has little to do with VS other than builds under VS can use Antlr4BuildTasks
for a seemless build. You will just need to add the package references and other items to
the .csproj file as outlined above.

If you are looking for a set of templates to create a console application that uses Antlr4,
then see [Antlr4Templates](https://github.com/kaby76/Antlr4Templates).

### Latest release, v12.13

### Released 12.13 (Jan 26)
* Fixes #110.
* Adds https://github.com/kaby76/Antlr4BuildTasks/pull/108.
* Fixes #99: https://github.com/kaby76/Antlr4BuildTasks/pull/107.

### Released 12.10 (Mar 25)
* Fix JRE download link for Linux x86.

### Release 12.9 (Nov '24)
* Fix for #90.

### Release 12.8 (16 Jan '24)
* Fix for #77.

### Release 12.7 (29 Nov '23)
* Fix for Eclipse Temurin OpenJDK.

### Release 12.6 (27 Nov '23)
* Fix for https://github.com/kaby76/Antlr4BuildTasks/issues/75. WebClient, obsolete, has been replaced.

### Release 12.5 (22 Nov '23)
* Fix for https://github.com/kaby76/Antlr4BuildTasks/issues/49. The pattern recognition for valid versions of Java has been updated.

### Release v12.4 (31 Oct 2023)

* Security update. Clean up builds. Published now in Release configuration.

### Release v12.2 (22 Dec 2022)

* Added new parameter `<AntlrToolJarDownloadDir>` to set the location to download the Antlr4 tool jar.
