ARG IMAGE_VERSION=7.0-alpine3.16

FROM mcr.microsoft.com/dotnet/sdk:$IMAGE_VERSION AS base
WORKDIR /opt/app

FROM mcr.microsoft.com/dotnet/sdk:$IMAGE_VERSION AS build
LABEL BUILD_IMAGE=true

RUN apk add --update nodejs npm
WORKDIR /repo

COPY ./src/DraftPuck/DraftPuck.csproj ./src/DraftPuck/
COPY ./DraftPuck.sln ./
COPY ./src/DraftPuck/ClientApp/package.json ./src/DraftPuck/ClientApp/
COPY ./src/DraftPuck/ClientApp/package-lock.json ./src/DraftPuck/ClientApp/

WORKDIR /repo/src/DraftPuck/ClientApp
RUN npm install

WORKDIR /repo
COPY . .

ARG BUILD_CONFIGURATION=Release

RUN dotnet restore

RUN dotnet build --no-restore -c $BUILD_CONFIGURATION && \
	dotnet publish "src/DraftPuck/DraftPuck.csproj" --no-build --no-restore -c $BUILD_CONFIGURATION -o /app/publish

FROM base AS final
WORKDIR /opt/app
COPY --from=build /app/publish .

ARG ASPNETCORE_URLS
ENV ASPNETCORE_URLS ${ASPNETCORE_URLS:-http://+:17000/}

EXPOSE 17000 

ENTRYPOINT ["dotnet", "DraftPuck.dll"]