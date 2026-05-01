#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
PROJECT_PATH="${REPO_ROOT}/src/Payslip4All.Web/Payslip4All.Web.csproj"

BACKEND_PORT=8080
GATEWAY_HTTP_PORT=8081
GATEWAY_HTTPS_PORT=8443
PUBLIC_HOST="payslip4all.co.za"
MODE="http"
CERT_PATH=""
CERT_PASSWORD=""
DB_PATH=""
GENERATED_DB_PATH=""
BACKEND_PID=""
GATEWAY_PID=""
BACKEND_LOG=""
GATEWAY_LOG=""

usage() {
    cat <<EOF
Usage: $(basename "$0") [options]

Starts a local backend instance and a local YARP gateway instance for testing.

Options:
  --mode http|https         Gateway mode. Default: http
  --backend-port PORT       Backend port. Default: 8080
  --gateway-port PORT       Gateway HTTP port. Default: 8081
  --https-port PORT         Gateway HTTPS port when --mode https. Default: 8443
  --public-host HOST        Host header required by YARP. Default: payslip4all.co.za
  --cert-path PATH          PFX certificate path for HTTPS mode
  --cert-password VALUE     PFX certificate password for HTTPS mode
  --db-path PATH            SQLite database path for the backend
  --help                    Show this help text
EOF
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --mode)
            MODE="$2"
            shift 2
            ;;
        --backend-port)
            BACKEND_PORT="$2"
            shift 2
            ;;
        --gateway-port)
            GATEWAY_HTTP_PORT="$2"
            shift 2
            ;;
        --https-port)
            GATEWAY_HTTPS_PORT="$2"
            shift 2
            ;;
        --public-host)
            PUBLIC_HOST="$2"
            shift 2
            ;;
        --cert-path)
            CERT_PATH="$2"
            shift 2
            ;;
        --cert-password)
            CERT_PASSWORD="$2"
            shift 2
            ;;
        --db-path)
            DB_PATH="$2"
            shift 2
            ;;
        --help)
            usage
            exit 0
            ;;
        *)
            echo "Unknown argument: $1" >&2
            usage >&2
            exit 1
            ;;
    esac
done

if [[ "${MODE}" != "http" && "${MODE}" != "https" ]]; then
    echo "--mode must be either 'http' or 'https'" >&2
    exit 1
fi

if [[ "${MODE}" == "https" ]]; then
    if [[ -z "${CERT_PATH}" || -z "${CERT_PASSWORD}" ]]; then
        echo "HTTPS mode requires --cert-path and --cert-password" >&2
        exit 1
    fi

    if [[ ! -f "${CERT_PATH}" ]]; then
        echo "Certificate file not found: ${CERT_PATH}" >&2
        exit 1
    fi
fi

if [[ -z "${DB_PATH}" ]]; then
    GENERATED_DB_PATH="$(mktemp -t p4a-yarp-local-db.XXXXXX).sqlite"
    DB_PATH="${GENERATED_DB_PATH}"
fi

BACKEND_LOG="$(mktemp -t p4a-yarp-backend.XXXXXX.log)"
GATEWAY_LOG="$(mktemp -t p4a-yarp-gateway.XXXXXX.log)"

cleanup() {
    local exit_code=$?

    if [[ -n "${BACKEND_PID}" ]] && kill -0 "${BACKEND_PID}" 2>/dev/null; then
        kill "${BACKEND_PID}" 2>/dev/null || true
        wait "${BACKEND_PID}" 2>/dev/null || true
    fi

    if [[ -n "${GATEWAY_PID}" ]] && kill -0 "${GATEWAY_PID}" 2>/dev/null; then
        kill "${GATEWAY_PID}" 2>/dev/null || true
        wait "${GATEWAY_PID}" 2>/dev/null || true
    fi

    if [[ -n "${GENERATED_DB_PATH}" && -f "${GENERATED_DB_PATH}" ]]; then
        rm -f "${GENERATED_DB_PATH}"
    fi

    exit "${exit_code}"
}

trap cleanup EXIT INT TERM

wait_for_url() {
    local url="$1"
    shift

    for _ in $(seq 1 60); do
        if curl --silent --show-error --fail "$@" "${url}" >/dev/null 2>&1; then
            return 0
        fi

        sleep 1
    done

    return 1
}

ensure_process_alive() {
    local pid="$1"
    local name="$2"
    local log_path="$3"

    if ! kill -0 "${pid}" 2>/dev/null; then
        echo "${name} exited unexpectedly. Recent log output:" >&2
        tail -n 80 "${log_path}" >&2 || true
        exit 1
    fi
}

echo "Starting backend on http://127.0.0.1:${BACKEND_PORT}"
(
    cd "${REPO_ROOT}"
    ASPNETCORE_ENVIRONMENT=Development \
    ASPNETCORE_URLS="http://127.0.0.1:${BACKEND_PORT}" \
    PERSISTENCE_PROVIDER=sqlite \
    REVERSE_PROXY_ENABLED=false \
    ConnectionStrings__DefaultConnection="Data Source=${DB_PATH}" \
    dotnet run --project "${PROJECT_PATH}" --no-launch-profile
) >"${BACKEND_LOG}" 2>&1 &
BACKEND_PID=$!

if ! wait_for_url "http://127.0.0.1:${BACKEND_PORT}/health"; then
    ensure_process_alive "${BACKEND_PID}" "Backend" "${BACKEND_LOG}"
    echo "Backend did not become ready in time." >&2
    exit 1
fi

if [[ "${MODE}" == "https" ]]; then
    GATEWAY_URLS="http://127.0.0.1:${GATEWAY_HTTP_PORT};https://127.0.0.1:${GATEWAY_HTTPS_PORT}"
    echo "Starting gateway on ${GATEWAY_URLS}"
    (
        cd "${REPO_ROOT}"
        ASPNETCORE_ENVIRONMENT=Development \
        ASPNETCORE_URLS="${GATEWAY_URLS}" \
        REVERSE_PROXY_ENABLED=true \
        REVERSE_PROXY_PUBLIC_HOST="${PUBLIC_HOST}" \
        REVERSE_PROXY_UPSTREAM_BASE_URL="http://127.0.0.1:${BACKEND_PORT}" \
        Kestrel__Certificates__Default__Path="${CERT_PATH}" \
        Kestrel__Certificates__Default__Password="${CERT_PASSWORD}" \
        dotnet run --project "${PROJECT_PATH}" --no-launch-profile
    ) >"${GATEWAY_LOG}" 2>&1 &
else
    GATEWAY_URLS="http://127.0.0.1:${GATEWAY_HTTP_PORT}"
    echo "Starting gateway on ${GATEWAY_URLS}"
    (
        cd "${REPO_ROOT}"
        ASPNETCORE_ENVIRONMENT=Development \
        ASPNETCORE_URLS="${GATEWAY_URLS}" \
        REVERSE_PROXY_ENABLED=true \
        REVERSE_PROXY_PUBLIC_HOST="${PUBLIC_HOST}" \
        REVERSE_PROXY_UPSTREAM_BASE_URL="http://127.0.0.1:${BACKEND_PORT}" \
        dotnet run --project "${PROJECT_PATH}" --no-launch-profile
    ) >"${GATEWAY_LOG}" 2>&1 &
fi
GATEWAY_PID=$!

if [[ "${MODE}" == "https" ]]; then
    if ! wait_for_url "https://${PUBLIC_HOST}:${GATEWAY_HTTPS_PORT}/health" --insecure --resolve "${PUBLIC_HOST}:${GATEWAY_HTTPS_PORT}:127.0.0.1"; then
        ensure_process_alive "${GATEWAY_PID}" "Gateway" "${GATEWAY_LOG}"
        echo "Gateway did not become ready in time." >&2
        exit 1
    fi
else
    if ! wait_for_url "http://127.0.0.1:${GATEWAY_HTTP_PORT}/health" -H "Host: ${PUBLIC_HOST}"; then
        ensure_process_alive "${GATEWAY_PID}" "Gateway" "${GATEWAY_LOG}"
        echo "Gateway did not become ready in time." >&2
        exit 1
    fi
fi

cat <<EOF

Local YARP test environment is running.

Backend:
  http://127.0.0.1:${BACKEND_PORT}

Gateway:
  ${GATEWAY_URLS}

Public host:
  ${PUBLIC_HOST}

Database:
  ${DB_PATH}

Logs:
  backend: ${BACKEND_LOG}
  gateway: ${GATEWAY_LOG}

Suggested checks:
EOF

if [[ "${MODE}" == "https" ]]; then
    cat <<EOF
  curl -k --resolve ${PUBLIC_HOST}:${GATEWAY_HTTPS_PORT}:127.0.0.1 https://${PUBLIC_HOST}:${GATEWAY_HTTPS_PORT}/health
  curl -i --resolve ${PUBLIC_HOST}:${GATEWAY_HTTP_PORT}:127.0.0.1 http://${PUBLIC_HOST}:${GATEWAY_HTTP_PORT}/
  curl -i --resolve unexpected.example.com:${GATEWAY_HTTP_PORT}:127.0.0.1 http://unexpected.example.com:${GATEWAY_HTTP_PORT}/
EOF
else
    cat <<EOF
  curl -i -H 'Host: ${PUBLIC_HOST}' http://127.0.0.1:${GATEWAY_HTTP_PORT}/health
  curl -i -H 'Host: ${PUBLIC_HOST}' http://127.0.0.1:${GATEWAY_HTTP_PORT}/
  curl -i -H 'Host: unexpected.example.com' http://127.0.0.1:${GATEWAY_HTTP_PORT}/
EOF
fi

echo
echo "Press Ctrl+C to stop both processes."

while true; do
    ensure_process_alive "${BACKEND_PID}" "Backend" "${BACKEND_LOG}"
    ensure_process_alive "${GATEWAY_PID}" "Gateway" "${GATEWAY_LOG}"
    sleep 1
done
