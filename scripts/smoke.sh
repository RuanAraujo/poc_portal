#!/usr/bin/env bash

set -euo pipefail

api_base_url="${API_BASE_URL:-http://localhost:8080}"
agent_base_url="${AGENT_BASE_URL:-http://localhost:8090}"
version="smoke-$(date +%s)"
correlation_id="$version-ingestion"
agent_correlation_id="$version-agent"
sensitive_marker="$version-sensitive-content"
headers_file=$(mktemp)
trap 'rm -f "$headers_file"' EXIT

content=$(printf '{"openapi":"3.0.0","info":{"title":"Smoke API","version":"1.0.0","description":"%s"},"paths":{"/health":{"get":{"operationId":"getHealth","summary":"Read health","responses":{"200":{"description":"OK"}}}}}}' "$sensitive_marker")
payload=$(printf '{"apiId":"smoke-api","name":"Smoke API","version":"%s","environment":"development","format":"json","content":%s}' \
  "$version" \
  "$(printf '%s' "$content" | sed 's/\\/\\\\/g; s/"/\\"/g; s/^/"/; s/$/"/')")

echo "Creating documentation version $version"
publish_response=$(curl --fail-with-body --silent --show-error \
  -X POST "$api_base_url/api/documentations" \
  -H 'Content-Type: application/json' \
  -H "X-Correlation-ID: $correlation_id" \
  --dump-header "$headers_file" \
  --data-binary "$payload")
returned_correlation_id=$(awk 'tolower($1) == "x-correlation-id:" { gsub("\r", "", $2); value=$2 } END { print value }' "$headers_file")
if [ "$returned_correlation_id" != "$correlation_id" ]; then
  echo "Expected X-Correlation-ID '$correlation_id', got '$returned_correlation_id'" >&2
  exit 1
fi
document_id=$(printf '%s' "$publish_response" | python3 -c 'import json,sys; print(json.load(sys.stdin)["documentId"])')
version_id=$(printf '%s' "$publish_response" | python3 -c 'import json,sys; print(json.load(sys.stdin)["versionId"])')

echo "Waiting for asynchronous ingestion"
status=""
for _ in $(seq 1 60); do
  documentation=$(curl --fail-with-body --silent --show-error "$api_base_url/api/documentations/$document_id")
  status=$(printf '%s' "$documentation" | python3 -c 'import json,sys; data=json.load(sys.stdin); wanted=sys.argv[1]; print(next(version["status"] for version in data["versions"] if version["id"] == wanted))' "$version_id")
  case "$status" in
    available) break ;;
    indexingFailed|publishFailed) echo "Ingestion failed with status $status" >&2; exit 1 ;;
  esac
  sleep 2
done

if [ "$status" != "available" ]; then
  echo "Ingestion did not become available within 120 seconds (last status: $status)" >&2
  exit 1
fi

dimensions=$(docker compose exec -T postgres psql \
  -U "${POSTGRES_USER:-documentation_user}" \
  -d "${POSTGRES_DB:-documentation_portal}" \
  -Atc "SELECT min(vector_dims(embedding)) FROM ingestion.document_chunks WHERE version_id = '$version_id';")
if [ "$dimensions" != "768" ]; then
  echo "Expected 768 embedding dimensions, got '$dimensions'" >&2
  exit 1
fi

echo "Ingestion available with 768-dimensional vectors"
chat_response=$(curl --fail-with-body --silent --show-error \
  -X POST "$agent_base_url/api/agents/chat" \
  -H 'Content-Type: application/json' \
  -H "X-Correlation-ID: $agent_correlation_id" \
  --dump-header "$headers_file" \
  --data-binary '{"message":"Qual endpoint da Smoke API verifica a saúde?"}')
printf '%s\n' "$chat_response"

returned_correlation_id=$(awk 'tolower($1) == "x-correlation-id:" { gsub("\r", "", $2); value=$2 } END { print value }' "$headers_file")
if [ "$returned_correlation_id" != "$agent_correlation_id" ]; then
  echo "Expected agent X-Correlation-ID '$agent_correlation_id', got '$returned_correlation_id'" >&2
  exit 1
fi

sleep 1
for service in documentation-api documentation-ingestion; do
  service_logs=$(docker compose logs --no-color "$service")
  if ! grep -Fq "$correlation_id" <<<"$service_logs"; then
    echo "Correlation ID '$correlation_id' was not found in $service logs" >&2
    exit 1
  fi
  if grep -Fq "$sensitive_marker" <<<"$service_logs"; then
    echo "Sensitive content was found in $service logs" >&2
    exit 1
  fi
done

embedding_logs=$(docker compose logs --no-color documentation-embeddings)
for expected in "$correlation_id" "$agent_correlation_id"; do
  if ! grep -Fq "$expected" <<<"$embedding_logs"; then
    echo "Correlation ID '$expected' was not found in documentation-embeddings logs" >&2
    exit 1
  fi
done
if grep -Fq "$sensitive_marker" <<<"$embedding_logs"; then
  echo "Sensitive content was found in documentation-embeddings logs" >&2
  exit 1
fi

agent_logs=$(docker compose logs --no-color documentation-agent)
if ! grep -Fq "$agent_correlation_id" <<<"$agent_logs"; then
  echo "Correlation ID '$agent_correlation_id' was not found in documentation-agent logs" >&2
  exit 1
fi

echo "Correlated logs verified without sensitive content"
