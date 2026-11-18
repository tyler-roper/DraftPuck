#!/usr/bin/env bash
# Runs an isolated copy of DraftPuck (Web + Worker built from the working tree) for looking at changes in a browser.
# Everything is prefixed draftpuck-run and kept apart from the developer's own stack: its own SQL Server, Redis and
# Azurite containers on a private Docker network, and the app on http://localhost:17100.
#
#   run.sh up       build both images from the working tree and start everything (re-run after code changes)
#   run.sh seed     sign up test players and make them friends (prints their logins)
#   run.sh shot <email> <out-dir> <path[|selector]>...   screenshot pages in headless Chrome as that player
#   run.sh logs     tail the Web and Worker logs
#   run.sh down     stop and remove the containers, network and generated env file
set -euo pipefail
# Git Bash on Windows rewrites Unix-looking paths in arguments (/opt/... inside a container); stop it.
export MSYS_NO_PATHCONV=1

# A Windows-style path under Git Bash (pwd -W), so docker.exe understands it.
REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && (pwd -W 2>/dev/null || pwd))"
NET=draftpuck-run
PORT=17100
URL="http://localhost:$PORT"
SQL_PASSWORD='DraftPuck-Run-1'
# Generated from the developer's env.development.list (it holds their secrets), so it lives outside the repo.
ENV_FILE="${TMPDIR:-/tmp}/draftpuck-run.env"
command -v cygpath >/dev/null && ENV_FILE="$(cygpath -m "$ENV_FILE")"
# Azurite's published development account key (the same for every Azurite install).
AZURITE_KEY='Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw=='
STORAGE="DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=$AZURITE_KEY;BlobEndpoint=http://draftpuck-run-azurite:10000/devstoreaccount1;QueueEndpoint=http://draftpuck-run-azurite:10001/devstoreaccount1;TableEndpoint=http://draftpuck-run-azurite:10002/devstoreaccount1;"
# Every seeded player signs in with this.
PASSWORD='draftpuck-run-1'

log() { printf '\033[1;34m[run]\033[0m %s\n' "$*" >&2; }

write_env() {
  [ -f "$REPO/env.development.list" ] || { echo "Missing env.development.list (see docs/environment.md)." >&2; exit 1; }
  # Keep the developer's settings (JWT key, AutoMapper licence, Firebase) and point the rest at the run's containers.
  grep -viE '^(DRAFTPUCK_ConnectionStrings__|DRAFTPUCK_Redis__|DRAFTPUCK_AZURESTORAGE__|AzureWebJobsStorage|DRAFTPUCK_APPLICATION__(BASEPATH|ISTESTMODE)|DRAFTPUCK_DISCORD__BotToken)' \
    "$REPO/env.development.list" | grep -vE '^\s*(#|//)' > "$ENV_FILE"
  cat >> "$ENV_FILE" <<EOF
DRAFTPUCK_ConnectionStrings__DefaultConnection=Server=draftpuck-run-sql,1433;Database=DraftPuck;User Id=sa;Password=$SQL_PASSWORD;Encrypt=false;
DRAFTPUCK_Redis__ConnectionString=draftpuck-run-redis:6379,abortConnect=false
DRAFTPUCK_AZURESTORAGE__CONNECTIONSTRING=$STORAGE
AzureWebJobsStorage=$STORAGE
DRAFTPUCK_APPLICATION__BASEPATH=$URL/
DRAFTPUCK_APPLICATION__ISTESTMODE=false
ASPNETCORE_ENVIRONMENT=Development
EOF
}

start_infra() {
  docker network inspect "$NET" >/dev/null 2>&1 || docker network create "$NET" >/dev/null
  running() { [ "$(docker inspect -f '{{.State.Running}}' "$1" 2>/dev/null)" = true ]; }
  running draftpuck-run-sql || { docker rm -f draftpuck-run-sql >/dev/null 2>&1 || true
    docker run -d --name draftpuck-run-sql --network "$NET" -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD="$SQL_PASSWORD" \
      mcr.microsoft.com/mssql/server:2022-latest >/dev/null; }
  running draftpuck-run-redis || { docker rm -f draftpuck-run-redis >/dev/null 2>&1 || true
    docker run -d --name draftpuck-run-redis --network "$NET" redis:latest >/dev/null; }
  running draftpuck-run-azurite || { docker rm -f draftpuck-run-azurite >/dev/null 2>&1 || true
    docker run -d --name draftpuck-run-azurite --network "$NET" mcr.microsoft.com/azure-storage/azurite \
      azurite --loose --skipApiVersionCheck --blobHost 0.0.0.0 --queueHost 0.0.0.0 --tableHost 0.0.0.0 >/dev/null; }

  log "Waiting for SQL Server..."
  for _ in $(seq 60); do
    docker exec draftpuck-run-sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$SQL_PASSWORD" -Q 'SELECT 1' -b >/dev/null 2>&1 && return
    sleep 2
  done
  echo "SQL Server didn't start; see: docker logs draftpuck-run-sql" >&2; exit 1
}

up() {
  write_env
  start_infra
  log "Building images from the working tree (the first build takes several minutes)..."
  docker build -q -f "$REPO/DraftPuck.Web/Dockerfile" -t draftpuck-run-web "$REPO" >/dev/null
  docker build -q -f "$REPO/DraftPuck.Worker/Dockerfile" -t draftpuck-run-worker "$REPO" >/dev/null

  docker rm -f draftpuck-run-web draftpuck-run-worker >/dev/null 2>&1 || true
  # The Web app creates the database and applies migrations on start; the Worker then loads today's NHL schedule.
  docker run -d --name draftpuck-run-web --network "$NET" -p "$PORT:17000" --env-file "$ENV_FILE" draftpuck-run-web >/dev/null
  log "Waiting for the Web app..."
  for _ in $(seq 90); do
    curl -sf "$URL/" >/dev/null 2>&1 && break
    sleep 2
  done
  curl -sf "$URL/" >/dev/null || { docker logs --tail 50 draftpuck-run-web >&2; exit 1; }
  docker run -d --name draftpuck-run-worker --network "$NET" --env-file "$ENV_FILE" draftpuck-run-worker >/dev/null
  log "Running at $URL"
}

# POST/GET helpers for the seed: JSON in, JSON out, with an optional bearer token.
api() {
  local method=$1 path=$2 token=${3:-} body=${4:-}
  local args=(-sS -f -X "$method" "$URL/api/$path" -H 'Content-Type: application/json')
  [ -n "$token" ] && args+=(-H "Authorization: Bearer $token")
  [ -n "$body" ] && args+=(-d "$body")
  curl "${args[@]}"
}

json() { node -e 'let s="";process.stdin.on("data",d=>s+=d).on("end",()=>{const v=new Function("o","return o"+process.argv[1])(JSON.parse(s));console.log(typeof v==="string"?v:JSON.stringify(v))})' "$1"; }

# Signs up a player (a guest first, as the app does) and prints "<id> <token>".
signup() {
  local nickname=$1 auth token id
  auth=$(api POST users)
  token=$(json .jwtToken <<<"$auth")
  api POST users/signup "$token" "{\"email\":\"$nickname@draftpuck.test\",\"nickname\":\"$nickname\",\"password\":\"$PASSWORD\"}" >/dev/null
  auth=$(api POST auth/login "$token" "{\"email\":\"$nickname@draftpuck.test\",\"password\":\"$PASSWORD\"}")
  id=$(json .user.id <<<"$auth")
  token=$(json .jwtToken <<<"$auth")
  echo "$id $token"
}

seed() {
  local suffix sam alex jo sam_id sam_t alex_id alex_t jo_id jo_t
  suffix=$(date +%H%M%S)
  sam="Sam$suffix"; alex="Alex$suffix"; jo="Jo$suffix"
  read -r sam_id sam_t <<<"$(signup "$sam")"
  read -r alex_id alex_t <<<"$(signup "$alex")"
  read -r jo_id jo_t <<<"$(signup "$jo")"
  # Sam and Alex are friends; Jo has asked Sam (so Sam has a request waiting).
  api POST "friends/$alex_id" "$sam_t" >/dev/null
  api POST "friends/$sam_id" "$alex_t" >/dev/null
  api POST "friends/$sam_id" "$jo_t" >/dev/null

  # Lobbies need tonight's games, which the Worker loads from the NHL a few seconds after it starts.
  local games="" sam_code alex_code
  for _ in $(seq 30); do
    games=$(api GET games/summaries "$sam_t" | json '.map(g => g.id)')
    [ "$games" != "[]" ] && break
    sleep 2
  done
  if [ "$games" = "[]" ]; then
    log "No NHL games today, so no lobbies were seeded."
  else
    lobby() { api POST lobbies "$1" "{\"name\":\"$2\",\"picksPerTeam\":1,\"isBotAutoPickingEnabled\":true,\"bots\":[],\"gameIds\":$games}" | json .joinCode; }
    sam_code=$(lobby "$sam_t" "$sam")
    alex_code=$(lobby "$alex_t" "$alex")
    # Alex invites Sam, so Sam has an invite (and a request) waiting.
    api POST "friends/invites/$alex_code" "$alex_t" "{\"friendIds\":[\"$sam_id\"]}" >/dev/null
  fi

  cat <<EOF
Seeded (password for all: $PASSWORD):
  $sam@draftpuck.test   friends with $alex; a request from $jo and an invite to lobby ${alex_code:-none} waiting; hosts lobby ${sam_code:-none}
  $alex@draftpuck.test  friends with $sam; hosts lobby ${alex_code:-none}
  $jo@draftpuck.test    asked $sam to be friends
EOF
}

case "${1:-}" in
  up) up ;;
  seed) seed ;;
  shot)
    # Headless Chrome via playwright-core, installed once into a temp folder (it uses the installed Chrome).
    shift
    tools="${TMPDIR:-/tmp}/draftpuck-run-tools"
    command -v cygpath >/dev/null && tools="$(cygpath -m "$tools")"
    mkdir -p "$tools"
    [ -d "$tools/node_modules/playwright-core" ] || npm i --prefix "$tools" --silent --no-audit --no-fund playwright-core@1.56.1 >/dev/null
    cp "$(dirname "${BASH_SOURCE[0]}")/shoot.mjs" "$tools/shoot.mjs"
    node "$tools/shoot.mjs" "$@" ;;
  logs) docker logs -f --tail 50 draftpuck-run-web & docker logs -f --tail 50 draftpuck-run-worker ;;
  down)
    docker rm -f draftpuck-run-web draftpuck-run-worker draftpuck-run-sql draftpuck-run-redis draftpuck-run-azurite >/dev/null 2>&1 || true
    docker network rm "$NET" >/dev/null 2>&1 || true
    rm -f "$ENV_FILE"
    log "Stopped." ;;
  *) sed -n '2,11p' "$0"; exit 1 ;;
esac
