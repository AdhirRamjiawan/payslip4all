#!/usr/bin/env bash
set -euo pipefail

APP_ROOT="/opt/payslip4all"
APP_USER="payslip4all"
ENV_DIR="/etc/payslip4all"
ENV_FILE="$ENV_DIR/payslip4all.env"
SERVICE_FILE="/etc/systemd/system/payslip4all.service"
NGINX_CERT_DIR="/etc/nginx/certs"
NGINX_SITE_CONFIG="/etc/nginx/conf.d/payslip4all.conf"
PUBLIC_DOMAIN="${PUBLIC_DOMAIN:-payslip4all.co.za}"
UPSTREAM_APP_URL="${UPSTREAM_APP_URL:-http://127.0.0.1:8080}"

ARTIFACT_SOURCE="${ARTIFACT_SOURCE:?ARTIFACT_SOURCE is required}"
ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Production}"
PERSISTENCE_PROVIDER="${PERSISTENCE_PROVIDER:-dynamodb}"
DYNAMODB_REGION="${DYNAMODB_REGION:?DYNAMODB_REGION is required}"
DYNAMODB_TABLE_PREFIX="${DYNAMODB_TABLE_PREFIX:-payslip4all}"
DYNAMODB_ENABLE_PITR="${DYNAMODB_ENABLE_PITR:-true}"
HOSTED_PAYMENTS_SECRET_ARN="${HOSTED_PAYMENTS_SECRET_ARN:-}"
TLS_CERTIFICATE_SECRET_ARN="${TLS_CERTIFICATE_SECRET_ARN:?TLS_CERTIFICATE_SECRET_ARN is required}"
TLS_CERTIFICATE_FULLCHAIN_KEY="${TLS_CERTIFICATE_FULLCHAIN_KEY:-fullchainPem}"
TLS_PRIVATE_KEY_KEY="${TLS_PRIVATE_KEY_KEY:-privkeyPem}"

if ! id "$APP_USER" >/dev/null 2>&1; then
  useradd --system --home "$APP_ROOT" --shell /sbin/nologin "$APP_USER"
fi

dnf install -y curl tar gzip jq awscli aspnetcore-runtime-8.0 nginx

mkdir -p "$APP_ROOT" "$ENV_DIR" "$NGINX_CERT_DIR"
install -m 0600 /dev/null "$ENV_FILE"
curl --fail --location --silent --show-error "${ARTIFACT_SOURCE}" --output /tmp/payslip4all.tgz
find "$APP_ROOT" -mindepth 1 -exec rm -rf -- {} +
tar -xzf /tmp/payslip4all.tgz -C "$APP_ROOT"

cat > "$ENV_FILE" <<EOF
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
    --output text | jq -r 'to_entries[] | "\(.key)=\(.value)"' >> "$ENV_FILE"
fi

CERTIFICATE_JSON="$(aws secretsmanager get-secret-value \
  --secret-id "${TLS_CERTIFICATE_SECRET_ARN}" \
  --query SecretString \
  --output text)"

printf '%s' "$CERTIFICATE_JSON" | jq -r --arg key "$TLS_CERTIFICATE_FULLCHAIN_KEY" '.[$key]' > "$NGINX_CERT_DIR/fullchain.pem"
printf '%s' "$CERTIFICATE_JSON" | jq -r --arg key "$TLS_PRIVATE_KEY_KEY" '.[$key]' > "$NGINX_CERT_DIR/privkey.pem"

chmod 600 "$ENV_FILE" "$NGINX_CERT_DIR/privkey.pem"
chmod 644 "$NGINX_CERT_DIR/fullchain.pem"

cat > "$SERVICE_FILE" <<EOF
[Unit]
Description=Payslip4All web application
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
User=$APP_USER
WorkingDirectory=$APP_ROOT
EnvironmentFile=$ENV_FILE
ExecStart=/usr/bin/dotnet $APP_ROOT/Payslip4All.Web.dll
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
EOF

cat > "$NGINX_SITE_CONFIG" <<EOF
map \$http_upgrade \$connection_upgrade {
    default upgrade;
    '' close;
}

upstream payslip4all_app {
    server 127.0.0.1:8080;
    keepalive 32;
}

server {
    listen 80 default_server;
    server_name _;

    return 421;
}

server {
    listen 80;
    server_name ${PUBLIC_DOMAIN};

    return 301 https://${PUBLIC_DOMAIN}\$request_uri;
}

server {
    listen 443 ssl default_server;
    server_name _;

    ssl_certificate ${NGINX_CERT_DIR}/fullchain.pem;
    ssl_certificate_key ${NGINX_CERT_DIR}/privkey.pem;

    return 421;
}

server {
    listen 443 ssl http2;
    server_name ${PUBLIC_DOMAIN};

    ssl_certificate ${NGINX_CERT_DIR}/fullchain.pem;
    ssl_certificate_key ${NGINX_CERT_DIR}/privkey.pem;
    ssl_session_timeout 1d;
    ssl_session_cache shared:SSL:10m;
    ssl_protocols TLSv1.2 TLSv1.3;
    server_tokens off;

    proxy_intercept_errors on;
    error_page 502 503 504 =503 /503.html;

    location = /health {
        proxy_http_version 1.1;
        proxy_connect_timeout 5s;
        proxy_send_timeout 10s;
        proxy_read_timeout 10s;
        proxy_pass ${UPSTREAM_APP_URL};
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto https;
        proxy_set_header X-Forwarded-Host \$host;
    }

    location / {
        proxy_http_version 1.1;
        proxy_connect_timeout 5s;
        proxy_send_timeout 60s;
        proxy_read_timeout 300s;
        proxy_buffering off;
        proxy_pass ${UPSTREAM_APP_URL};
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto https;
        proxy_set_header X-Forwarded-Host \$host;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection \$connection_upgrade;
    }

    location = /503.html {
        internal;
        default_type text/plain;
        return 503 "Service temporarily unavailable.\n";
    }
}
EOF

chmod 644 "$NGINX_SITE_CONFIG"
chown -R "$APP_USER:$APP_USER" "$APP_ROOT" "$ENV_DIR"

systemctl daemon-reload
systemctl enable payslip4all.service
systemctl enable nginx
systemctl restart payslip4all.service
systemctl is-active --quiet payslip4all.service
nginx -t
systemctl restart nginx
