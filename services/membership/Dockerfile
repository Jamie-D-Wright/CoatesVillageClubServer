# Build stage - restore and build application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["CoatesVillageClubServer.sln", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["services/membership/src/VillageClub.Membership/VillageClub.Membership.csproj", "services/membership/src/VillageClub.Membership/"]
COPY ["libs/VillageClub.Contracts/VillageClub.Contracts.csproj", "libs/VillageClub.Contracts/"]
COPY ["libs/VillageClub.Auth/src/VillageClub.Auth/VillageClub.Auth.csproj", "libs/VillageClub.Auth/src/VillageClub.Auth/"]

# Restore dependencies
RUN dotnet restore "services/membership/src/VillageClub.Membership/VillageClub.Membership.csproj"

# Copy source code
COPY ["services/membership/src/VillageClub.Membership/", "services/membership/src/VillageClub.Membership/"]
COPY ["libs/VillageClub.Contracts/", "libs/VillageClub.Contracts/"]
COPY ["libs/VillageClub.Auth/src/VillageClub.Auth/", "libs/VillageClub.Auth/src/VillageClub.Auth/"]

# Build and publish
WORKDIR "/src/services/membership/src/VillageClub.Membership"
RUN dotnet build "VillageClub.Membership.csproj" -c Release -o /app/build
RUN dotnet publish "VillageClub.Membership.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage - minimal image with Azure Functions runtime
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS runtime
WORKDIR /home/site/wwwroot

# Copy published application
COPY --from=build /app/publish .

# Environment configuration
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    ASPNETCORE_ENVIRONMENT=Production

# Health check endpoint
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost/api/v1/health || exit 1
