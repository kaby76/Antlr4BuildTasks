#
where=`dirname -- "$0"`
echo $where
cd $where
where=`pwd`
unameOut="$(uname -s)"
case "${unameOut}" in
    Linux*)
	machine=Linux
	rm -rf ~/.jre
	rm -rf ~/.nuget/packages/antlr4buildtasks	
	;;
    Darwin*)
	machine=Mac
	rm -rf ~/.jre
	rm -rf ~/.nuget/packages/antlr4buildtasks	
	;;
    CYGWIN*)
	machine=Cygwin
	rm -rf $USERPROFILE/.jre
	rm -rf $USERPROFILE/.m2
	rm -rf $USERPROFILE/.nuget/packages/antlr4buildtasks	
	;;
    MINGW*)
	machine=MinGw
	rm -rf $USERPROFILE/.jre
	rm -rf $USERPROFILE/.m2
	rm -rf $USERPROFILE/.nuget/packages/antlr4buildtasks	
	;;
    *)          machine="UNKNOWN:${unameOut}"
esac
output=`dotnet build -v diag | tee output | grep '2 Error'`
echo $?
output=`echo "$output" | sed 's/ *$//g' | sed 's/^ *//g'`
cat output
if [ "$output" != "2 Error(s)" ]
then
	echo Test failed.
	exit 1
else
	echo Test passed.
	exit 0
fi
