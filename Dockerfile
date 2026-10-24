# syntax=docker/dockerfile:1

# ---- Build -----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the project files first so the restore layer is cached until a dependency changes.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/Domain/*.csproj src/Domain/
COPY src/Application/*.csproj src/Application/
COPY src/Infrastructure/*.csproj src/Infrastructure/
COPY src/API/*.csproj src/API/
RUN dotnet restore src/API/ProductManagement.Api.csproj

COPY src/ src/
RUN dotnet publish src/API/ProductManagement.Api.csproj \
    --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

# ---- Runtime ---------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Run as the non-root user that ships with the .NET 8 images.
USER app

# 8080: HTTP (redirects to HTTPS when ASPNETCORE_HTTPS_PORT is set), 8081: HTTPS (needs a certificate, see README).
EXPOSE 8080 8081
ENTRYPOINT ["dotnet", "ProductManagement.Api.dll"]
