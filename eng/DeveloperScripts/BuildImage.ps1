docker build `
-f "..\..\src\BrewPuck\Dockerfile" `
--build-arg BUILD_CONFIGURATION='Release' `
--build-arg ASPNETCORE_URLS=https://+:17000/ `
-t brewpuck:latest ..\..\