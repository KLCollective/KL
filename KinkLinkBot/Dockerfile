# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY KinkLinkCommon/KinkLinkCommon.csproj KinkLinkCommon/
COPY KinkLinkBot/KinkLinkBot.csproj KinkLinkBot/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore KinkLinkBot/KinkLinkBot.csproj \
    --source https://api.nuget.org/v3/index.json

COPY . .

WORKDIR /src/KinkLinkBot
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "KinkLinkBot.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "KinkLinkBot.dll"]
