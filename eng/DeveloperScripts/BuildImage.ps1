docker build `
-f "..\..\DraftPuck.Api\Dockerfile" `
--build-arg BUILD_CONFIGURATION='Release' `
--build-arg ASPNETCORE_URLS=https://+:17000/ `
-t draftpuck:latest ..\..\