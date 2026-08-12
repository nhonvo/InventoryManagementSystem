# === Stage 1: Build ===
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy solution and project files for layer-cached restore
COPY ["InventoryManagementSystem.sln", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["InventoryAlert.Domain/InventoryAlert.Domain.csproj", "InventoryAlert.Domain/"]
COPY ["InventoryAlert.Infrastructure/InventoryAlert.Infrastructure.csproj", "InventoryAlert.Infrastructure/"]
COPY ["InventoryAlert.Api/InventoryAlert.Api.csproj", "InventoryAlert.Api/"]

# Restore dependencies
RUN dotnet restore "InventoryAlert.Api/InventoryAlert.Api.csproj"

# Copy full source and publish API
COPY . .
RUN dotnet publish "InventoryAlert.Api/InventoryAlert.Api.csproj" \
    -c Release \
    --no-restore \
    -o /app/publish

# === Stage 2: Runtime ===
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

# Install security and diagnostic dependencies
RUN apk add --no-cache icu-libs curl gcompat libgdiplus krb5-libs

# Configure runtime for Alpine (Globalization & File Watcher)
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "InventoryAlert.Api.dll"]

LABEL maintainer="InventoryAlert Team"
LABEL version="1.0"
LABEL description="REST API Service for Inventory Management System"

