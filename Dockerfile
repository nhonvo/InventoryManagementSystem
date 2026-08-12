# === Stage 1: Build ===
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy solution and project files for layer-cached restore
COPY ["src/InventoryManagementSystem.sln", "./"]
COPY ["src/Directory.Packages.props", "./"]
COPY ["src/InventoryAlert.Domain/InventoryAlert.Domain.csproj", "InventoryAlert.Domain/"]
COPY ["src/InventoryAlert.Infrastructure/InventoryAlert.Infrastructure.csproj", "InventoryAlert.Infrastructure/"]
COPY ["src/InventoryAlert.Api/InventoryAlert.Api.csproj", "InventoryAlert.Api/"]
COPY ["src/InventoryAlert.Worker/InventoryAlert.Worker.csproj", "InventoryAlert.Worker/"]

# Restore dependencies
RUN dotnet restore "InventoryAlert.Api/InventoryAlert.Api.csproj"
RUN dotnet restore "InventoryAlert.Worker/InventoryAlert.Worker.csproj"

# Copy full source and publish API and Worker
COPY src/ .
RUN dotnet publish "InventoryAlert.Api/InventoryAlert.Api.csproj" \
    -c Release \
    --no-restore \
    -o /app/publish/api

RUN dotnet publish "InventoryAlert.Worker/InventoryAlert.Worker.csproj" \
    -c Release \
    --no-restore \
    -o /app/publish/worker

# === Stage 2: Runtime ===
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

# Install security, diagnostic, and Python Moto server dependencies for Render deployment
RUN apk add --no-cache icu-libs curl gcompat libgdiplus krb5-libs python3 py3-pip && \
    pip install --no-cache-dir --break-system-packages "moto[server]"

# Configure runtime for Alpine (Globalization & File Watcher)
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish/api ./api
COPY --from=build /app/publish/worker ./worker

# Startup script: Starts Moto server + Worker in background, then executes .NET Web API
RUN echo '#!/bin/sh' > /app/entrypoint.sh && \
    echo 'moto_server -p 5000 -H 127.0.0.1 &' >> /app/entrypoint.sh && \
    echo 'sleep 2' >> /app/entrypoint.sh && \
    echo 'dotnet /app/worker/InventoryAlert.Worker.dll &' >> /app/entrypoint.sh && \
    echo 'sleep 1' >> /app/entrypoint.sh && \
    echo 'exec dotnet /app/api/InventoryAlert.Api.dll' >> /app/entrypoint.sh && \
    chmod +x /app/entrypoint.sh

EXPOSE 8080
ENTRYPOINT ["/app/entrypoint.sh"]

LABEL maintainer="InventoryAlert Team"
LABEL version="1.0"
LABEL description="Consolidated API & Worker for Inventory Management System with built-in Moto emulator"
