param(
    [Switch]$HotReload
)

$BUILD_CONFIGURATION='Release'

If ($HotReload.IsPresent) {
    $BUILD_CONFIGURATION='Debug'
}

$REPO_DIRECTORY=(Get-Item $PSScriptRoot).Parent.Parent.FullName

docker rm -f $(docker ps -aq --filter ancestor=brewpuck)

docker build `
-f "$REPO_DIRECTORY\src\BrewPuck\Dockerfile" `
--build-arg BUILD_CONFIGURATION=$BUILD_CONFIGURATION `
--build-arg ASPNETCORE_URLS=https://+:17000/ `
-t brewpuck:latest ..\..\

docker run -dt `
--env-file "..\..\env.development.list" `
-v "$env:USERPROFILE\AppData\Roaming\ASP.NET\Https:/etc/ssl/certs/.aspnet/https:ro" `
-v "$REPO_DIRECTORY\src\BrewPuck:/app:rw" `
-p 17000:17000 `
brewpuck:latest