#!/usr/bin/env bash
set -euo pipefail

APP_ROOT="/opt/payslip4all"
APP_USER="payslip4all"
ENV_DIR="/etc/payslip4all"
APP_ENV_FILE="$ENV_DIR/payslip4all.env"
GATEWAY_ENV_FILE="$ENV_DIR/payslip4all-gateway.env"
APP_CONFIG_SECRETS_FILE="${APP_CONFIG_SECRETS_FILE:-/etc/payslip4all/app-config.secrets.json}"
APP_SERVICE_FILE="/etc/systemd/system/payslip4all.service"
GATEWAY_SERVICE_FILE="/etc/systemd/system/payslip4all-gateway.service"
TLS_CERT_DIR="/etc/payslip4all/certs"
TLS_FULLCHAIN_PATH="/etc/payslip4all/certs/fullchain.pem"
TLS_PRIVATE_KEY_PATH="/etc/payslip4all/certs/privkey.pem"
TLS_PFX_PATH="/etc/payslip4all/certs/payslip4all.pfx"
PUBLIC_DOMAIN="${PUBLIC_DOMAIN:-payslip4all.co.za}"
UPSTREAM_APP_URL="http://127.0.0.1:8080"
PUBLIC_EDGE_URLS="${PUBLIC_EDGE_URLS:-http://0.0.0.0:80;https://0.0.0.0:443}"
REVERSE_PROXY_ACTIVITY_TIMEOUT_SECONDS="${REVERSE_PROXY_ACTIVITY_TIMEOUT_SECONDS:-10}"
CERTIFICATE_ACTIVATION_ERROR="HTTPS activation failed for payslip4all.co.za: certificate material is missing or invalid; public traffic remains disabled."

ARTIFACT_SOURCE="${ARTIFACT_SOURCE:?ARTIFACT_SOURCE is required}"
ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Production}"
PERSISTENCE_PROVIDER="${PERSISTENCE_PROVIDER:-dynamodb}"
DYNAMODB_REGION="${DYNAMODB_REGION:?DYNAMODB_REGION is required}"
DYNAMODB_TABLE_PREFIX="${DYNAMODB_TABLE_PREFIX:-payslip4all}"
DYNAMODB_ENABLE_PITR="${DYNAMODB_ENABLE_PITR:-true}"
APP_CONFIG_SECRET_ARN="${APP_CONFIG_SECRET_ARN:-}"
HOSTED_PAYMENTS_SECRET_ARN="${HOSTED_PAYMENTS_SECRET_ARN:-}"
TLS_CERTIFICATE_SECRET_ARN="${TLS_CERTIFICATE_SECRET_ARN:?TLS_CERTIFICATE_SECRET_ARN is required}"
TLS_CERTIFICATE_FULLCHAIN_KEY="${TLS_CERTIFICATE_FULLCHAIN_KEY:-fullchainPem}"
TLS_PRIVATE_KEY_KEY="${TLS_PRIVATE_KEY_KEY:-privkeyPem}"

if ! id "$APP_USER" >/dev/null 2>&1; then
  useradd --system --home "$APP_ROOT" --shell /sbin/nologin "$APP_USER"
fi

dnf install -y curl tar gzip jq awscli aspnetcore-runtime-8.0 openssl

mkdir -p "$APP_ROOT" "$ENV_DIR" "$TLS_CERT_DIR"
install -m 0600 /dev/null "$APP_ENV_FILE"
install -m 0600 /dev/null "$GATEWAY_ENV_FILE"

curl --fail --location --silent --show-error "${ARTIFACT_SOURCE}" --output /tmp/payslip4all.tgz
find "$APP_ROOT" -mindepth 1 -exec rm -rf -- {} +
tar -xzf /tmp/payslip4all.tgz -C "$APP_ROOT"

cat > "$APP_ENV_FILE" <<EOF
ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT}
ASPNETCORE_URLS=http://127.0.0.1:8080
PERSISTENCE_PROVIDER=${PERSISTENCE_PROVIDER}
DYNAMODB_REGION=${DYNAMODB_REGION}
DYNAMODB_TABLE_PREFIX=${DYNAMODB_TABLE_PREFIX}
DYNAMODB_ENABLE_PITR=${DYNAMODB_ENABLE_PITR}
EOF

if [[ -n "${HOSTED_PAYMENTS_SECRET_ARN}" ]]; then
  aws secretsmanager get-secret-value \
    --secret-id "${HOSTED_PAYMENTS_SECRET_ARN}" \
    --query SecretString \
    --output text | jq -r 'to_entries[] | "\(.key)=\(.value)"' >> "$APP_ENV_FILE"
fi

rm -f "$APP_CONFIG_SECRETS_FILE"
if [[ -n "${APP_CONFIG_SECRET_ARN}" ]]; then
  APP_CONFIG_JSON="$(aws secretsmanager get-secret-value \
    --secret-id "${APP_CONFIG_SECRET_ARN}" \
    --query SecretString \
    --output text)"

  printf '%s' "$APP_CONFIG_JSON" \
    | jq -e 'if type == "object" then . else error("App config secret must be a JSON object.") end' \
    > "$APP_CONFIG_SECRETS_FILE"

  chmod 600 "$APP_CONFIG_SECRETS_FILE"
  chown "$APP_USER:$APP_USER" "$APP_CONFIG_SECRETS_FILE"
fi

CERTIFICATE_JSON="$(aws secretsmanager get-secret-value \
  --secret-id "${TLS_CERTIFICATE_SECRET_ARN}" \
  --query SecretString \
  --output text)"

printf '%s' "$CERTIFICATE_JSON" | jq -r --arg key "$TLS_CERTIFICATE_FULLCHAIN_KEY" '.[$key]' > "$TLS_FULLCHAIN_PATH"
printf '%s' "$CERTIFICATE_JSON" | jq -r --arg key "$TLS_PRIVATE_KEY_KEY" '.[$key]' > "$TLS_PRIVATE_KEY_PATH"

if [[ ! -s "$TLS_FULLCHAIN_PATH" || ! -s "$TLS_PRIVATE_KEY_PATH" || "$(cat "$TLS_FULLCHAIN_PATH")" == "null" || "$(cat "$TLS_PRIVATE_KEY_PATH")" == "null" ]]; then
  echo "$CERTIFICATE_ACTIVATION_ERROR" >&2
  exit 1
fi

TLS_PFX_PASSWORD="$(openssl rand -hex 24)"
if ! openssl pkcs12 -export \
  -out "$TLS_PFX_PATH" \
  -inkey "$TLS_PRIVATE_KEY_PATH" \
  -in "$TLS_FULLCHAIN_PATH" \
  -password "pass:${TLS_PFX_PASSWORD}"; then
  echo "$CERTIFICATE_ACTIVATION_ERROR" >&2
  exit 1
fi

cat > "$GATEWAY_ENV_FILE" <<EOF
ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT}
ASPNETCORE_URLS=http://0.0.0.0:80;https://0.0.0.0:443
REVERSE_PROXY_ENABLED=true
REVERSE_PROXY_PUBLIC_HOST=${PUBLIC_DOMAIN}
REVERSE_PROXY_UPSTREAM_BASE_URL=http://127.0.0.1:8080
REVERSE_PROXY_ACTIVITY_TIMEOUT_SECONDS=${REVERSE_PROXY_ACTIVITY_TIMEOUT_SECONDS}
Kestrel__Certificates__Default__Path=/etc/payslip4all/certs/payslip4all.pfx
Kestrel__Certificates__Default__Password=${TLS_PFX_PASSWORD}
EOF

chmod 600 "$APP_ENV_FILE" "$GATEWAY_ENV_FILE" "$TLS_PRIVATE_KEY_PATH" "$TLS_PFX_PATH"
chmod 644 "$TLS_FULLCHAIN_PATH"

cat > "$APP_SERVICE_FILE" <<EOF
[Unit]
Description=Payslip4All web application
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
User=$APP_USER
WorkingDirectory=$APP_ROOT
EnvironmentFile=$APP_ENV_FILE
ExecStart=/usr/bin/dotnet $APP_ROOT/Payslip4All.Web.dll
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
EOF

cat > "$GATEWAY_SERVICE_FILE" <<EOF
[Unit]
Description=Payslip4All YARP gateway
After=network-online.target payslip4all.service
Wants=network-online.target

[Service]
Type=simple
User=$APP_USER
WorkingDirectory=$APP_ROOT
EnvironmentFile=$GATEWAY_ENV_FILE
ExecStart=/usr/bin/dotnet $APP_ROOT/Payslip4All.Web.dll
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
EOF

chown -R "$APP_USER:$APP_USER" "$APP_ROOT" "$ENV_DIR" "$TLS_CERT_DIR"

systemctl daemon-reload
systemctl enable payslip4all.service
systemctl enable payslip4all-gateway.service
systemctl restart payslip4all.service
systemctl is-active --quiet payslip4all.service
systemctl restart payslip4all-gateway.service
systemctl is-active --quiet payslip4all-gateway.service
