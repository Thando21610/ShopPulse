#!/bin/sh
set -eu

if [ -n "${ApiBaseUrl:-}" ]; then
    envsubst '${ApiBaseUrl}' < /usr/share/nginx/html/appsettings.json.template > /usr/share/nginx/html/appsettings.json
fi
