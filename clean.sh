#!/bin/sh

rm -rf obj bin
rm -rf .vs
rm -rf packages
rm -rf "$LOCALAPPDATA/Microsoft/VisualStudio/16.0"*"Exp/Extensions"
rm -rf c:/Users/Kenne/.nuget/packages/antlr4buildtasks/
rm -f antlr4-4.10-complete.jar
dotnet nuget remove source nuget-antlr4buildtasks 2>&1 > /dev/null
