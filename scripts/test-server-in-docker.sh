#!/bin/sh
# Runs the server tests inside a Linux .NET SDK container, the same environment CI uses.
# Useful when the host can't run them directly, e.g. Windows Smart App Control blocking freshly built test DLLs.
# The repo is copied into the container (without bin/obj/node_modules), so host build output is left alone.
# Testcontainers starts SQL Server and Redis next to it through the mounted Docker socket.
#
# Usage (from the repo root, Git Bash or any POSIX shell): scripts/test-server-in-docker.sh [extra dotnet test args]
set -e

# pwd -W gives a Windows path (needed by Docker Desktop) under Git Bash; elsewhere fall back to plain pwd.
repo_root="$(cd "$(dirname "$0")/.." && { pwd -W 2>/dev/null || pwd; })"

MSYS_NO_PATHCONV=1 docker run --rm \
  -v //var/run/docker.sock:/var/run/docker.sock \
  -v "$repo_root:/src:ro" \
  -e TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  sh -c 'mkdir /work \
    && tar --exclude=./**/bin --exclude=./**/obj --exclude=./**/node_modules --exclude=./.git -C /src -cf - . | tar -C /work -xf - \
    && cd /work \
    && dotnet test DraftPuck.Tests/DraftPuck.Tests.csproj --nologo "$@"' sh "$@"
