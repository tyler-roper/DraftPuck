FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /opt/app

COPY ./DraftPuck.Web/entrypoint.sh /opt/app/entrypoint.sh
RUN chmod +x /opt/app/entrypoint.sh

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine3.21 AS build
LABEL BUILD_IMAGE=true
ARG BUILD_CONFIGURATION=Release

RUN apk add --update nodejs npm
WORKDIR /repo

# Copy csproj files and restore (cached unless dependencies change)
COPY ./DraftPuck.sln ./
COPY ./DraftPuck.Web/DraftPuck.Web.csproj ./DraftPuck.Web/
COPY ./DraftPuck.Shared/DraftPuck.Shared.csproj ./DraftPuck.Shared/
COPY ./DraftPuck.Application/DraftPuck.Application.csproj ./DraftPuck.Application/
COPY ./DraftPuck.Infrastructure/DraftPuck.Infrastructure.csproj ./DraftPuck.Infrastructure/

RUN dotnet restore

# Copy package.json and install npm dependencies (cached unless package.json changes)
COPY ./DraftPuck.Web/ClientApp/package.json ./DraftPuck.Web/ClientApp/
COPY ./DraftPuck.Web/ClientApp/package-lock.json ./DraftPuck.Web/ClientApp/

WORKDIR /repo/DraftPuck.Web/ClientApp
RUN npm ci

# Copy source code and build
WORKDIR /repo
COPY . .

RUN dotnet build --no-restore -c $BUILD_CONFIGURATION "DraftPuck.Web/DraftPuck.Web.csproj"
RUN dotnet publish "DraftPuck.Web/DraftPuck.Web.csproj" --no-build --no-restore -c $BUILD_CONFIGURATION -o /app/publish

FROM base AS final
WORKDIR /opt/app
COPY --from=build /app/publish .

ARG GIT_SHA="Local"
ENV DRAFTPUCK_SYSTEM__GitSha=$GIT_SHA

ENV ASPNETCORE_URLS=http://+:17000/
EXPOSE 17000

ENTRYPOINT ["/opt/app/entrypoint.sh", "dotnet", "DraftPuck.Web.dll"]
