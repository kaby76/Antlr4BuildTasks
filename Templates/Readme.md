# Antlr4BuildTasks.Templates

This directory contains code for Antlr templates for Net Core. The template provided contains an expression
grammar example, code to print out the Antlr parse trees and token stream. Also included is an error
reporter that augments the error messages provided by the Antlr runtime that improves on the suggested input
that it is looking for when a parse error occurs. You will need to install Net Core.

## To install:

    dotnet new -i Antlr4BuildTasks.Templates

Note, if you are working from a cloned local copy,
you need to use the full path of the "AntlrCAProject" directory.
(The dotnet tool always gives "Usage: new [options]"
in the output even if the command succeeds, so you don't know if the
command worked or not,
and [the instructions in poor English](https://docs.microsoft.com/en-us/dotnet/core/tools/custom-templates#installing-a-template).)

    dotnet new -i <absolute-path-to-AntlrCAProject>

e.g.,

    dotnet new -i 'C:\Users\kenne\Documents\Antlr4BuildTasks\Templates\templates\AntlrCAProject'


## To create an Antlr C# program:

    mkdir Foo
    cd Foo
    dotnet new antlr
    dotnet restore
    dotnet build
    dotnet run

## To uninstall:

    dotnet new -u Antlr4BuildTasks.Templates

Note, if you are working from a cloned local copy,
you need to use the full path of the "AntlrCAProject" directory.

    dotnet new -u <absolute-path-to-AntlrCAProject>

e.g.,

    dotnet new -u 'C:\Users\kenne\Documents\Antlr4BuildTasks\Templates\templates\AntlrCAProject'

