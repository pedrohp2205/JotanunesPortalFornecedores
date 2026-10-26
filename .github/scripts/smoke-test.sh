#!/usr/bin/env bash
set -euo pipefail

app="$1"
fqdn=$(az containerapp show -g "$RESOURCE_GROUP" -n "$app" --query properties.configuration.ingress.fqdn -o tsv --only-show-errors)
url="https://$fqdn/swagger/v1/swagger.json"

for attempt in $(seq 1 12); do
  code=$(curl -s -o /dev/null -w "%{http_code}" --max-time 60 "$url" || true)
  echo "Tentativa $attempt: $url -> $code"
  if [ "$code" = "200" ]; then
    exit 0
  fi
  sleep 10
done

echo "::error::$app não respondeu 200 após o deploy"
exit 1
