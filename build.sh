#!/bin/bash
set +x
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
dotnet restore Antlr4BuildTasks.csproj
dotnet build Antlr4BuildTasks.csproj
dotnet nuget add source "$cwd/Antlr4BuildTasks/bin/Debug/" --name nuget-antlr4buildtasks 2>&1 > /dev/null
