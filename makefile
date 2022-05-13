build:
	bash build.sh
clean:
	bash clean.sh
publish:
	dotnet nuget push bin/Debug/Antlr4BuildTasks.10.4.0.nupkg --api-key ${trashkey} --source https://api.nuget.org/v3/index.json
