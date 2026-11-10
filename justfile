set shell := ["powershell.exe", "-c"]

build:
    dotnet build CommanderDeckBuilder.sln

test:
    dotnet test CommanderDeckBuilder.sln

run:
    dotnet run --project src\CommanderDeckBuilder.UI\CommanderDeckBuilder.UI.csproj
