FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /opt/app

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine3.21 AS build
LABEL BUILD_IMAGE=true

RUN apk add --update nodejs npm
WORKDIR /repo

COPY ./DraftPuck.Web/DraftPuck.Web.csproj ./DraftPuck.Web/
COPY ./DraftPuck.sln ./
COPY ./DraftPuck.Web/ClientApp/package.json ./DraftPuck.Web/ClientApp/
COPY ./DraftPuck.Web/ClientApp/package-lock.json ./DraftPuck.Web/ClientApp/

WORKDIR /repo/DraftPuck.Web/ClientApp
RUN npm install

WORKDIR /repo
COPY . .

ARG BUILD_CONFIGURATION=Release

RUN dotnet restore

RUN dotnet build --no-restore -c $BUILD_CONFIGURATION && \
	dotnet publish "DraftPuck.Web/DraftPuck.Web.csproj" --no-build --no-restore -c $BUILD_CONFIGURATION -o /app/publish

FROM base AS final
WORKDIR /opt/app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:17000/
EXPOSE 17000

ENTRYPOINT ["dotnet", "DraftPuck.Web.dll"]