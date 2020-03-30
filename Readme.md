# Antlr4BuildTasks

This is a modification of Harwell's [Antlr4cs build tool](https://github.com/tunnelvisionlabs/antlr4cs/tree/master/runtime/CSharp/Antlr4BuildTasks),
which is a Task wrapper assembly for [Antlr4cs](https://github.com/tunnelvisionlabs/antlr4cs).
This modification includes cleaned up and simplified target and prop files for Antlr files,
a file properties schema for Antlr grammars processed with the official Antlr parser generator
for Visual Studio 2019, and the tool wrapper.
The purpose of the package is to integrate the official
[Java-based Antlr tool](https://www.antlr.org/download.html) and
[Antlr4.Runtime.Standard runtime](https://www.nuget.org/packages/Antlr4.Runtime.Standard/)
into the building of NET programs that reference Antlr. During a build,
it calls the Antlr tool to generate the source code for the parser from grammar,
then compiles and links. Using this package, you don't have to manually
run the Antlr tool outside the IDE, then go back to the IDE to complete the build. It is
all done seamlessly in the IDE. This project also assumes you are compiling for C# NET,
using the Antlr runtime library for NET, [Antlr4.Runtime.Standard](https://www.nuget.org/packages/Antlr4.Runtime.Standard),
(and which doesn't include any wrapper for the Java-based Antlr tool, nor build rules).
This allows you to use the latest any version of Antlr.

# Installation of Prerequisites

* Install Java tool chain, either [OpenJDK](https://openjdk.java.net/) or [Oracle JDK SE](https://www.oracle.com/technetwork/java/javase/downloads/index.html).

* Downloaded the Java-based Antlr tool chain. [Complete ANTLR 4.8 Java binaries jar](https://www.antlr.org/download/antlr-4.8-complete.jar).

* Set the environment variable "JAVA_HOME" to the directory of the java installation. See [this](https://confluence.atlassian.com/doc/setting-the-java_home-variable-in-windows-8895.html) for some instructions on how to do this
on Windows.

* Set the environment variable "Antlr4ToolPath" to the path of the downloaded Antlr jar file.

* Do not include the generated .cs Antlr parser files in the CSPROJ file for your program. The generated parser code is placed in the build temp output directory and automatically included.

* Make sure you do not have a version skew between the Java Antlr tool and the runtime versions.

# Verify Prerequisites

Please verify that you have these variables set up as expected. Try
*"$JAVA_HOME/bin/java.exe" -jar "$Antlr4ToolPath"*
from a Git Bash or
*"%JAVA_HOME%\bin\java.exe" -jar "%Antlr4ToolPath%"*
from a Cmd.exe.
That should execute the Antlr tool and print out the options expected
for the command. If it doesn't
work, adjust JAVA_HOME and Antlr4ToolPath. JAVA_HOME should be the full
path of the JDK; Antlr4ToolPath should be the full path of the Antlr
tool jar file. If you look at the generated .csproj file for the Antlr
Console program generated, you should see what it defaults if they
aren't set.

This package is a Net Standard assembly and works on Linux or Windows, and works only for Antlr4 grammars.

To set grammar specific options for the Antlr tool, use VS2019 file properties or set the options in the CSPROJ file.

Language support in Visual Studio 2019 itself--
e.g. colorized tagging of the Antlr grammar, go to definition, reformat, etc.--
is a separate product, not part of the build rules for Antlr grammar files,
which is what this package supports. You can use Harwell’s [Antlr Language Support](https://marketplace.visualstudio.com/items?itemName=SamHarwell.ANTLRLanguageSupport)
extension, my own [AntlrVSIX](https://marketplace.visualstudio.com/items?itemName=KenDomino.AntlrVSIX) extension, or another.

You can see the NuGet package in action [here on Youtube](https://www.youtube.com/watch?v=Flfequp_Dy4).
The Net Core code for the Antlr Hello World example is [here in Github](https://github.com/kaby76/AntlrHW).

# Modifying .csproj files for Antlr

You can use the build plug-in independently of Visual Studio. Modify directly
your .csproj with these attributes and elements. The following example
will help you get started. Note, the &lt;Antlr4&gt; element is similar to that used
in Harwell's Antlr4BuildTasks, but uses different child elements as the wrapper programs
have different options.

    <ItemGroup>
        <Antlr4 Include="ExpressionParser.g4">
            <Listener>false</Listener>
            <Visitor>false</Visitor>
            <GAtn>true</GAtn>
            <Package>foo</Package>
            <Error>true</Error>
        </Antlr4>
    </ItemGroup>
    <ItemGroup>
        <PackageReference Include="Antlr4.Runtime.Standard" Version="4.8.0" />
        <PackageReference Include="Antlr4BuildTasks" Version="3.0" />
    </ItemGroup>

You must include a reference to Antlr4BuildTasks and Antlr4.Runtime.Standard.
Remove references to Antlr4 (Harwell's obsolete code).

For every Antlr grammar file you want to have the Antlr Tool run on, list
each as an element in an &lt;ItemGroup&gt; with the attribute Include set to the
name of the file. The tool takes the following parameters:

* &lt;Listener&gt; -- A bool that specifies whether you want an
Antlr Listener class generated by the tool.
* &lt;Visitor&gt; -- A bool that specifies whether you want an
Antlr Visitor class generated by the tool.
* &lt;GAtn&gt; -- A bool that specifies whether you want
Antlr to generate the ATN Dot diagrams.
* &lt;Package&gt; -- An C# identifier that specifies the namespace to wrap
the generated classes in.
* &lt;Error&gt; -- Use this to specify whether you want the tool to
flag warnings as errors and stop a build.
* &lt;LibPath&gt; -- A string that specifies the path for token and grammar files
for the Antlr tool.
* &lt;Encoding&gt; -- A string that specifies the encoding of the input grammars.
* &lt;DOptions&gt; -- A list of <option>=<value>, passed to the Antlr tool. E.g.,
language=Java.

Antlr generates files that may produce a lot of compiler warnings. To ignore those,
add the following &lt;PropertyGroup&gt; to you .csproj file.

    <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|AnyCPU'">
        <NoWarn>3021;1701;1702</NoWarn>
    </PropertyGroup>
