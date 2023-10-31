#!/usr/bin/bash
where=`dirname -- "$0"`
echo $where
cd $where
where=`pwd`
unameOut="$(uname -s)"
case "${unameOut}" in
    Linux*)
	echo Linux
	machine=Linux
	rm -rf ~/.jre
	rm -rf ~/.nuget/packages/antlr4buildtasks	
	;;
    Darwin*)
	echo Mac
	machine=Mac
	rm -rf ~/.jre
	rm -rf ~/.nuget/packages/antlr4buildtasks	
	;;
    CYGWIN*)
	echo Cygwin
	machine=Cygwin
	echo USERPROFILE = $USERPROFILE
	rm -rf $USERPROFILE/.jre
	rm -rf $USERPROFILE/.m2
	rm -rf $USERPROFILE/.nuget/packages/antlr4buildtasks	
	;;
    MINGW*)
	echo Mingw
	machine=MinGw
	echo USERPROFILE = $USERPROFILE
	rm -rf $USERPROFILE/.jre
	rm -rf $USERPROFILE/.m2
	rm -rf $USERPROFILE/.nuget/packages/antlr4buildtasks	
	;;
    *)          machine="UNKNOWN:${unameOut}"
esac
ls -ld $USERPROFILE/.nuget/packages/antlr4buildtasks
ls -ld $USERPROFILE/.jre
ls -ld $USERPROFILE/.m2
cd "$where/../.."
if [[ "$machine" == "MinGw" || "$machine" == "Msys" ]]
then
    cwd=`pwd -W | sed 's%/%\\\\%g'`
    location="$cwd\\Antlr4BuildTasks\\bin\\Release\\"
else
    cwd=`pwd`
    location="$cwd/Antlr4BuildTasks/bin/Release/"
fi
echo machine is "$machine"
echo cwd is $cwd
cd "$where"
pwd
dotnet nuget remove source nuget-a4bt > /dev/null 2>&1
dotnet nuget list source | grep nuget-a4bt
if [ "$?" = "0" ]
then
	echo Found antlr4buildtasks.
	exit 1
fi
echo dotnet nuget add source $location --name nuget-a4bt
dotnet nuget add source $location --name nuget-a4bt > /dev/null 2>&1
rm -rf bin obj
