#!/usr/bin/env bash
set -euo pipefail
exec > >(tee -a /var/log/payslip4all-bootstrap.log) 2>&1
trap 'echo "Payslip4All bootstrap failed at line $LINENO" >&2' ERR

APP_ROOT="/opt/payslip4all"
APP_USER="payslip4all"
ENV_DIR="/etc/payslip4all"
ENV_FILE="$ENV_DIR/payslip4all.env"
SERVICE_FILE="/etc/systemd/system/payslip4all.service"

ARTIFACT_SOURCE="${ARTIFACT_SOURCE:?ARTIFACT_SOURCE is required}"
ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Production}"
PERSISTENCE_PROVIDER="${PERSISTENCE_PROVIDER:-dynamodb}"
DYNAMODB_REGION="${DYNAMODB_REGION:?DYNAMODB_REGION is required}"
DYNAMODB_TABLE_PREFIX="${DYNAMODB_TABLE_PREFIX:-payslip4all}"
DYNAMODB_ENABLE_PITR="${DYNAMODB_ENABLE_PITR:-true}"
HOSTED_PAYMENTS_SECRET_ARN="${HOSTED_PAYMENTS_SECRET_ARN:-}"

dnf install -y curl tar gzip jq awscli aspnetcore-runtime-8.0

if ! id "$APP_USER" >/dev/null 2>&1; then
  useradd --system --home "$APP_ROOT" --shell /sbin/nologin "$APP_USER"
fi

mkdir -p "$APP_ROOT" "$ENV_DIR"

cat > "$ENV_FILE" <<EOF
ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT}
ASPNETCORE_URLS=http://0.0.0.0:80
PERSISTENCE_PROVIDER=${PERSISTENCE_PROVIDER}
DYNAMODB_REGION=${DYNAMODB_REGION}
DYNAMODB_TABLE_PREFIX=${DYNAMODB_TABLE_PREFIX}
DYNAMODB_ENABLE_PITR=${DYNAMODB_ENABLE_PITR}
EOF

chmod 600 "$ENV_FILE"

if [[ -n "${HOSTED_PAYMENTS_SECRET_ARN}" ]]; then
  aws secretsmanager get-secret-value \
    --secret-id "${HOSTED_PAYMENTS_SECRET_ARN}" \
    --query SecretString \
    --output text | jq -r 'to_entries[] | "\(.key)=\(.value)"' >> "$ENV_FILE"
fi

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

curl --fail --location --silent --show-error "${ARTIFACT_SOURCE}" --output /tmp/payslip4all.tgz
find "$APP_ROOT" -mindepth 1 -exec rm -rf -- {} +
tar -xzf /tmp/payslip4all.tgz -C "$APP_ROOT"

chown -R "$APP_USER:$APP_USER" "$APP_ROOT" "$ENV_DIR"

systemctl daemon-reload
systemctl enable payslip4all.service
systemctl restart payslip4all.service
