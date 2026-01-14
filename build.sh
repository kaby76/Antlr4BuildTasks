#!/usr/bin/env bash
set -x
config=Release
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
    cwd=`pwd | sed 's%/c%c:%' | sed 's%/d%d:%' | sed 's%/%\\\\%g'`
else
    cwd=`pwd`
fi
echo $cwd
echo $machine
cd Antlr4BuildTasks
dotnet nuget remove source nuget-antlr4buildtasks > /dev/null 2>&1
dotnet restore Antlr4BuildTasks.csproj
dotnet build Antlr4BuildTasks.csproj -c $config
dotnet nuget add source "$cwd/Antlr4BuildTasks/bin/$config/" --name nuget-antlr4buildtasks > /dev/null 2>&1
dotnet nuget list source | grep nuget-antlr4buildtasks
