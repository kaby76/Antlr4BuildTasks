#!/usr/bin/env bash
where=`dirname -- "$0"`
echo $where
cd $where
where=`pwd`

# Check if Node.js is available
echo "Checking for Node.js..."
if ! command -v node &> /dev/null
then
    echo "Node.js not found. Please install Node.js to test antlr-ng."
    exit 1
fi
node --version

# Check if antlr-ng is available
echo "Checking for antlr-ng..."
if ! command -v antlr-ng &> /dev/null
then
    echo "antlr-ng not found globally. Checking local installation..."
    if [ ! -f "node_modules/antlr-ng/cli.js" ]; then
        echo "Installing antlr-ng locally..."
        npm install --save-dev antlr-ng
    fi
fi

# Determine OS and clean cached files
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
    *)
        machine="UNKNOWN:${unameOut}"
esac

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

# Setup local NuGet source
dotnet nuget remove source nuget-a4bt > /dev/null 2>&1
dotnet nuget list source | grep nuget-a4bt
if [ "$?" = "0" ]
then
	echo Found antlr4buildtasks.
	exit 1
fi
echo dotnet nuget add source $location --name nuget-a4bt
dotnet nuget add source $location --name nuget-a4bt > /dev/null 2>&1

# Build test project
rm -rf bin obj
dotnet restore single-antlr-ng.csproj -v normal
result="$?"
if [ "$result" != "0" ]
then
	exit $result
fi

dotnet build single-antlr-ng.csproj -v normal
result="$?"
if [ "$result" != "0" ]
then
	echo Test failed.
	exit 1
else
	echo Test passed.
	exit 0
fi
