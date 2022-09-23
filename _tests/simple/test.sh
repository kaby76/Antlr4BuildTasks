#!/usr/bin/bash
where=`dirname -- "$0"`
unameOut="$(uname -s)"
case "${unameOut}" in
    Linux*)
	machine=Linux
	;;
    Darwin*)
	rm -rf ~/.rje
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
dotnet nuget remove source nuget-antlr4buildtasks
dotnet nuget add source $cwd/Antlr4BuildTasks/bin/Debug/ --name nuget-antlr4buildtasks
cd "$where"
dotnet build
