# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS base
WORKDIR /home/site/wwwroot
EXPOSE 8080


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj files and restore (cached unless dependencies change)
COPY ["DraftPuck.Worker/DraftPuck.Worker.csproj", "DraftPuck.Worker/"]
COPY ["DraftPuck.Shared/DraftPuck.Shared.csproj", "DraftPuck.Shared/"]
COPY ["DraftPuck.Application/DraftPuck.Application.csproj", "DraftPuck.Application/"]
COPY ["DraftPuck.Infrastructure/DraftPuck.Infrastructure.csproj", "DraftPuck.Infrastructure/"]
RUN dotnet restore "./DraftPuck.Worker/DraftPuck.Worker.csproj"

# Copy source code and build
COPY . .
WORKDIR "/src/DraftPuck.Worker"
RUN dotnet build "./DraftPuck.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./DraftPuck.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .

ARG GIT_SHA
ENV DRAFTPUCK_SYSTEM__GitSha=$GIT_SHA

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true