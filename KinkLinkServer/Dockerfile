# syntax=docker/dockerfile:1
# Use the official .NET 9.0 runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5006
EXPOSE 9090

# Use the .NET 9.0 SDK as build image
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies with cache mount
COPY ["KinkLinkServer/KinkLinkServer.csproj", "KinkLinkServer/"]
COPY ["KinkLinkCommon/KinkLinkCommon.csproj", "KinkLinkCommon/"]
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "KinkLinkServer/KinkLinkServer.csproj" \
    --source https://api.nuget.org/v3/index.json

# Copy the source code
COPY . .

# Build the application with parallel compilation
WORKDIR "/src/KinkLinkServer"
RUN --mount=type=cache,target=/root/.nuget/packages dotnet build "KinkLinkServer.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN --mount=type=cache,target=/root/.nuget/packages dotnet publish "KinkLinkServer.csproj" -c Release -o /app/publish -p:UseAppHost=false

# Create the final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN mkdir -p /app/Configuration

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=https://+:5006

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f https://localhost:5006/health || exit 1

ENTRYPOINT ["dotnet", "KinkLinkServer.dll"]
