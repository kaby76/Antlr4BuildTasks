#!/usr/bin/bash
where=`dirname -- "$0"`
unameOut="$(uname -s)"
case "${unameOut}" in
    Linux*)
	rm -rf ~/.rje
	machine=Linux
	;;
    Darwin*)
	rm -rf ~/.rje
	machine=Mac
	;;
    CYGWIN*)
	rm -rf $USERPROFILE/.rje
	rm -rf $USERPROFILE/.m2
	machine=Cygwin
	;;
    MINGW*)
	rm -rf $USERPROFILE/.rje
	rm -rf $USERPROFILE/.m2
	machine=MinGw
	;;
    *)          machine="UNKNOWN:${unameOut}"
esac
cd "$where/../.."
if [[ "$machine" == "MinGw" || "$machine" == "Msys" ]]
then
    cwd=`pwd`
    cwd=`cygpath -w $cwd`
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
dotnet build
