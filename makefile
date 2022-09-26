build:
	bash build.sh
clean:
	bash clean.sh
test-java:
	bash test-java.sh
test-no-java:
	bash test-no-java.sh
publish:
	dotnet nuget push Antlr4BuildTasks/bin/Debug/Antlr4BuildTasks.11.5.0.nupkg --api-key ${trashkey} --source https://api.nuget.org/v3/index.json
