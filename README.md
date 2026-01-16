# DOTNET Experiments

## Summary of Workflow for every new topic:

Every time you want to learn a new section (e.g., "Middlewares"):

1. Open the terminal in dotnet_experiments.
2. Run:

```
dotnet new webapi -o Middlewares
```

3. This creates the folder (Middlewares) and puts the project inside. 3. Run:

```
dotnet sln add Middlewares/Middlewares.csproj
```

"register" this new project in a solution file (dotnet_experiments.sln) so you can open everything at once in VS Code.
