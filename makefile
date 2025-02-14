build:
	bash build.sh
clean:
	bash clean.sh
test-java:
	bash test-java.sh
test-no-java:
	bash test-no-java.sh
publish:
	dotnet nuget push Antlr4BuildTasks/bin/Release/Antlr4BuildTasks.12.9.0.nupkg --api-key ${trashkey} --source https://api.nuget.org/v3/index.json
