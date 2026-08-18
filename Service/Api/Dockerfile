# Basis-Image für die Laufzeit
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 80

# Build-Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Kopiere alle Projektdateien für bessere Layer-Caching
COPY Api/AppOrchestrator.Api.csproj Api/
COPY Infrastructure/AppOrchestrator.Infrastructure.csproj Infrastructure/
COPY Domain/AppOrchestrator.Domain.csproj Domain/

# Restore Abhängigkeiten für alle Projekte
RUN dotnet restore Api/AppOrchestrator.Api.csproj

# Kopiere den restlichen Source Code
COPY Api/ Api/
COPY Infrastructure/ Infrastructure/
COPY Domain/ Domain/

# Build das API-Projekt
WORKDIR /src/Api
RUN dotnet build AppOrchestrator.Api.csproj -c Release -o /app/build

# Publish-Stage
FROM build AS publish
RUN dotnet publish AppOrchestrator.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Final-Stage für das Produktions-Image
FROM base AS final
WORKDIR /app

# Install Docker CLI + Compose plugin so the API can run "docker compose" commands
# against the host Docker daemon mounted via /var/run/docker.sock
RUN apt-get update && \
    apt-get install -y --no-install-recommends ca-certificates curl gnupg && \
    install -m 0755 -d /etc/apt/keyrings && \
    curl -fsSL https://download.docker.com/linux/debian/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg && \
    chmod a+r /etc/apt/keyrings/docker.gpg && \
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] \
        https://download.docker.com/linux/debian $(. /etc/os-release && echo "$VERSION_CODENAME") stable" \
        > /etc/apt/sources.list.d/docker.list && \
    apt-get update && \
    apt-get install -y --no-install-recommends docker-ce-cli docker-compose-plugin && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# Kopiere den publish-Ordner aus der vorherigen Stage
COPY --from=publish /app/publish .

# Startet die Anwendung
ENTRYPOINT ["dotnet", "AppOrchestrator.Api.dll"]
