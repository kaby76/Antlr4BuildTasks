#!/usr/bin/bash
where=`dirname -- "$0"`
unameOut="$(uname -s)"
case "${unameOut}" in
    Linux*)
	machine=Linux
	;;
    Darwin*)
	machine=Mac
	;;
    CYGWIN*)
	machine=Cygwin
	;;
    MINGW*)
	machine=MinGw
	;;
    *)          machine="UNKNOWN:${unameOut}"
esac
cd "$where/../.."
if [[ "$machine" == "MinGw" || "$machine" == "Msys" ]]
then
    cwd=`pwd | sed 's%/c%c:%' | sed 's%/%\\\\%g'`
else
    cwd=`pwd`
fi
echo "$machine"
echo $cwd
echo dotnet nuget add source "$cwd/Antlr4BuildTasks/bin/Debug/" --name nuget-antlr4buildtasks
dotnet nuget add source "$cwd/Antlr4BuildTasks/bin/Debug/" --name nuget-antlr4buildtasks > /dev/null 2>&1
cd "$where"
dotnet build
