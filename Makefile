.PHONY: build clean test sqlc docker-up docker-down

# Build commands for each project
build: sqlc
	dotnet build KinkLink.sln

build-client:
	dotnet build KinkLinkClient/KinkLinkClient.csproj

build-server: sqlc
	dotnet build KinkLinkServer/KinkLinkServer.csproj

build-bot: sqlc
	dotnet build KinkLinkBot/KinkLinkBot.csproj

# Clean commands
clean:
	dotnet clean KinkLink.sln

# Test commands
test:
	dotnet test KinkLinkServerTests/KinkLinkServerTests.csproj

fmt:
	dotnet format

test-coverage:
	dotnet test KinkLinkServerTests/KinkLinkServerTests.csproj --collect:"XPlat Code Coverage"

# SQLC code generation
sqlc:
	sqlc generate -f KinkLinkCommon/sqlc.yaml

# Docker commands
# Bring the current compose file up	
up:
	docker compose up -d

# Build the image and bring it up.
upb: sqlc
	docker compose up -d --build

# Bring the image down
down:
	docker compose down

# Full rebuild
rebuild: clean build
