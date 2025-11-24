# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ./DraftPuck.Shared/DraftPuck.Shared.csproj ./DraftPuck.Shared/
COPY ./DraftPuck.Application/DraftPuck.Application.csproj ./DraftPuck.Application/
COPY ./DraftPuck.Infrastructure/DraftPuck.Infrastructure.csproj ./DraftPuck.Infrastructure/
COPY ./DraftPuck.DiscordBot/DraftPuck.DiscordBot.csproj ./DraftPuck.DiscordBot/

WORKDIR /src/DraftPuck.DiscordBot
RUN dotnet restore "./DraftPuck.DiscordBot.csproj"

WORKDIR /src
COPY . .

WORKDIR /src/DraftPuck.DiscordBot
RUN dotnet build "./DraftPuck.DiscordBot.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./DraftPuck.DiscordBot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ARG GIT_SHA
ENV DRAFTPUCK_SYSTEM__GitSha=$GIT_SHA

ENTRYPOINT ["dotnet", "DraftPuck.DiscordBot.dll"]