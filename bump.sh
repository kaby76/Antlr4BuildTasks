#

next_version="12.3"

files=`find . -name '*.csproj'`
subset=`grep -l -e Antlr4BuildTasks $files`
for i in $subset
do
	cat $i | sed -e "s%\"Antlr4BuildTasks\" Version=\".*\"%\"Antlr4BuildTasks\" Version=\"$next_version\"%" > asdfasdf
	mv asdfasdf $i
done

cat Antlr4BuildTasks/Antlr4BuildTasks.csproj | sed -e "s%<AssemblyVersion>[^<]*%<AssemblyVersion>$next_version%" > asdfasdf
mv asdfasdf Antlr4BuildTasks/Antlr4BuildTasks.csproj
cat Antlr4BuildTasks/Antlr4BuildTasks.csproj | sed -e "s%<FileVersion>[^<]*%<FileVersion>$next_version%" > asdfasdf
mv asdfasdf Antlr4BuildTasks/Antlr4BuildTasks.csproj
cat Antlr4BuildTasks/Antlr4BuildTasks.csproj | sed -e "s%<Version>[^<]*%<Version>$next_version%" > asdfasdf
mv asdfasdf Antlr4BuildTasks/Antlr4BuildTasks.csproj
cat Antlr4BuildTasks/Antlr4BuildTasks.csproj | sed -e "s%<PackageReleaseNotes>[^<]*%<PackageReleaseNotes>$next_version%" > asdfasdf
mv asdfasdf Antlr4BuildTasks/Antlr4BuildTasks.csproj
