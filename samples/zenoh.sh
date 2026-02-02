#!/usr/bin/env bash
set -euo pipefail

NAME=zenoh
PORT=7447

cleanup() {
  echo ""
  echo "Stopping zenoh container..."
  docker rm -f "$NAME" >/dev/null 2>&1 || true
}
trap cleanup EXIT INT TERM

echo "Starting zenoh router (zenohd)..."

docker run --rm \
  --name "$NAME" \
  -p ${PORT}:7447 \
  eclipse/zenoh:latest \
  --listen tcp/0.0.0.0:${PORT} \
  --no-multicast-scouting &

wait
