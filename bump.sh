#

next_version="12.3"

files=`find . -name '*.csproj'`
subset=`grep -l -e Antlr4BuildTasks $files`
for i in $subset
do
	cat $i | sed -e "s%\"Antlr4BuildTasks\" Version=\".*\"%\"Antlr4BuildTasks\" Version=\"$next_version\"%" > asdfasdf
	mv asdfasdf $i
done
