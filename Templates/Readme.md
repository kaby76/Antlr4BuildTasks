# Antlr4BuildTasks.Templates

This directory contains code for Antlr templates for Net Core. So far the only template it
contains is a "Hello World" console app for Antlr4. You will need to download Java, and the ANTLR 4.7.2
Java jar. See [Installation of Prerequisites](https://github.com/kaby76/Antlr4BuildTasks#installation-of-prerequisites)
for full details.

## To install:

    dotnet new -i Antlr4BuildTasks.Templates

## To create an Antlr C# program:

    mkdir Foo
    cd Foo
    dotnet new antlr
    dotnet restore
    dotnet build
    dotnet run

## To uninstall:

    dotnet new -u Antlr4BuildTasks.Templates
