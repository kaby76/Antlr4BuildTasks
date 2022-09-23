build:
	bash build.sh
clean:
	bash clean.sh
test:
	bash test.sh
publish:
	dotnet nuget push Antlr4BuildTasks/bin/Debug/Antlr4BuildTasks.11.3.0.nupkg --api-key ${trashkey} --source https://api.nuget.org/v3/index.json
