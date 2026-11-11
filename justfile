set shell := ["powershell.exe", "-c"]

build:
    dotnet build CommanderDeckBuilder.sln

test target="all":
    if ("{{target}}" -eq "unit") { just test-unit } elseif ("{{target}}" -eq "integration") { just test-integration } elseif ("{{target}}" -eq "all") { just test-unit; if ($?) { just test-integration } } else { throw "Unknown test target '{{target}}'. Use unit, integration, or omit the target." }

test-unit:
    dotnet test tests\CommanderDeckBuilder.Tests\CommanderDeckBuilder.Tests.csproj --no-restore

test-integration:
    dotnet test tests\CommanderDeckBuilder.Tests\CommanderDeckBuilder.Tests.csproj --no-restore -p:RunScryfallIntegrationTests=true --filter FullyQualifiedName~ScryfallApiIntegrationTests

run:
    dotnet run --project src\CommanderDeckBuilder.UI\CommanderDeckBuilder.UI.csproj
