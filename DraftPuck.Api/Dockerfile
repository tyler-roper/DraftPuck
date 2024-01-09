FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /opt/app

FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine3.17 AS build
LABEL BUILD_IMAGE=true

RUN apk add --update nodejs npm
WORKDIR /repo

COPY ./DraftPuck.Api/DraftPuck.Api.csproj ./DraftPuck.Api/
COPY ./DraftPuck.sln ./
COPY ./DraftPuck.Api/ClientApp/package.json ./DraftPuck.Api/ClientApp/
COPY ./DraftPuck.Api/ClientApp/package-lock.json ./DraftPuck.Api/ClientApp/

WORKDIR /repo/DraftPuck.Api/ClientApp
RUN npm install

WORKDIR /repo
COPY . .

ARG BUILD_CONFIGURATION=Release

RUN dotnet restore

RUN dotnet build --no-restore -c $BUILD_CONFIGURATION && \
	dotnet publish "DraftPuck.Api/DraftPuck.Api.csproj" --no-build --no-restore -c $BUILD_CONFIGURATION -o /app/publish

FROM base AS final
WORKDIR /opt/app
COPY --from=build /app/publish .

ARG ASPNETCORE_URLS
ENV ASPNETCORE_URLS ${ASPNETCORE_URLS:-http://+:17000/}

EXPOSE 17000 

ENTRYPOINT ["dotnet", "DraftPuck.Api.dll"]