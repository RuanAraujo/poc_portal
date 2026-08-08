#!/usr/bin/env bash

set -euo pipefail

api_base_url="${API_BASE_URL:-http://localhost:8080}"
version="smoke-$(date +%s)"

content='{"openapi":"3.0.0","info":{"title":"Smoke API","version":"1.0.0"},"paths":{"/health":{"get":{"operationId":"getHealth","summary":"Read health","responses":{"200":{"description":"OK"}}}}}}'
payload=$(printf '{"apiId":"smoke-api","name":"Smoke API","version":"%s","environment":"development","format":"json","content":%s}' \
  "$version" \
  "$(printf '%s' "$content" | sed 's/\\/\\\\/g; s/"/\\"/g; s/^/"/; s/$/"/')")

echo "Creating documentation version $version"
curl --fail-with-body --silent --show-error \
  -X POST "$api_base_url/api/documentations" \
  -H 'Content-Type: application/json' \
  --data-binary "$payload"

echo
echo "Waiting briefly for asynchronous ingestion"
sleep 2

curl --fail-with-body --silent --show-error \
  "$api_base_url/api/documentations"
echo
