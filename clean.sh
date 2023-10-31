#!/usr/bin/bash
rm -rf obj bin
rm -rf _tests/*/bin _tests/*/obj
rm -rf .vs
rm -rf Antlr4BuildTasks/.vs
rm -rf packages
rm -rf "$LOCALAPPDATA/Microsoft/VisualStudio/16.0"*"Exp/Extensions"
rm -rf c:/Users/Kenne/.nuget/packages/antlr4buildtasks/
rm -rf Antlr4BuildTasks/bin
rm -rf Antlr4BuildTasks/obj
rm -f antlr4-*-complete.jar
rm -rf c:/Users/Kenne/.m2
rm -rf c:/Users/Kenne/.jre
dotnet nuget remove source nuget-antlr4buildtasks 2>&1 > /dev/null
exit 0
