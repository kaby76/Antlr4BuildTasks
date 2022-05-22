#!/bin/bash

wget https://repo1.maven.org/maven2/org/antlr/antlr4/4.10.1/antlr4-4.10.1-complete.jar
dotnet restore
dotnet build
unameOut="$(uname -s)"
case "${unameOut}" in
    Linux*)     machine=Linux;;
    Darwin*)    machine=Mac;;
    CYGWIN*)    machine=Cygwin;;
    MINGW*)     machine=MinGw;;
    MSYS_NT*)   machine=Msys;;
    *)          machine="UNKNOWN:${unameOut}"
esac
if [[ "$machine" == "MinGw" || "$machine" == "Msys" ]]
then
    cwd=`pwd | sed 's%/c%c:%' | sed 's%/%\\\\%g'`
else
    cwd=`pwd`
fi
echo "$machine"
echo "$cwd"
dotnet nuget add source $cwd/Antlr4BuildTasks/bin/Debug/ --name nuget-antlr4buildtasks > /dev/null 2>&1
dotnet nuget list source
