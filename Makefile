.PHONY: build test lint format clean validate-packs docs-dev docs-build

build:
	dotnet build src/DINOForge.sln

test:
	dotnet test src/DINOForge.sln --verbosity normal

lint:
	dotnet format src/DINOForge.sln --verify-no-changes

format:
	dotnet format src/DINOForge.sln

clean:
	dotnet clean src/DINOForge.sln

validate-packs:
	dotnet run --project src/Tools/PackCompiler -- validate packs/example-balance

docs-dev:
	npm run dev

docs-build:
	npm run build

ci-build:
	dotnet build src/DINOForge.CI.sln

ci-test:
	dotnet test src/DINOForge.CI.sln --verbosity normal
