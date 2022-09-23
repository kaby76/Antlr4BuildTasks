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
    cwd=`pwd -W | sed 's%/%\\\\%g'`
    location="$cwd\\Antlr4BuildTasks\\bin\\Debug\\"
else
    cwd=`pwd`
    location="$cwd/Antlr4BuildTasks/bin/Debug/"
fi
echo "$machine"
echo $cwd
echo dotnet nuget add source $location --name nuget-a4bt
dotnet nuget add source $location --name nuget-a4bt > /dev/null 2>&1
cd "$where"
dotnet build -v diag
