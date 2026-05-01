#!/usr/bin/env bash

set -euo pipefail

APP_ROOT="/opt/payslip4all"
APP_DLL=""
BACKEND_ENV_FILE="/etc/payslip4all/payslip4all.env"
GATEWAY_ENV_FILE="/etc/payslip4all/payslip4all-gateway.env"
BACKEND_LOG="/var/log/payslip4all/backend.log"
GATEWAY_LOG="/var/log/payslip4all/gateway.log"
STARTUP_TIMEOUT_SECONDS=60
DRY_RUN=false

BACKEND_PID=""
GATEWAY_PID=""
PUBLIC_HOST=""
GATEWAY_URLS=""

usage() {
    cat <<EOF
Usage: $(basename "$0") [options]

Starts the published Payslip4All backend and YARP gateway directly on an EC2 host.
This is intended for manual production-like verification on the instance itself.

Options:
  --app-root PATH            Published app directory. Default: /opt/payslip4all
  --app-dll PATH             Explicit path to Payslip4All.Web.dll
  --backend-env-file PATH    Backend env file. Default: /etc/payslip4all/payslip4all.env
  --gateway-env-file PATH    Gateway env file. Default: /etc/payslip4all/payslip4all-gateway.env
  --backend-log PATH         Backend log path. Default: /var/log/payslip4all/backend.log
  --gateway-log PATH         Gateway log path. Default: /var/log/payslip4all/gateway.log
  --startup-timeout SECONDS  Health wait timeout. Default: 60
  --dry-run                  Print resolved commands and exit
  --help                     Show this help text
EOF
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --app-root)
            APP_ROOT="$2"
            shift 2
            ;;
        --app-dll)
            APP_DLL="$2"
            shift 2
            ;;
        --backend-env-file)
            BACKEND_ENV_FILE="$2"
            shift 2
            ;;
        --gateway-env-file)
            GATEWAY_ENV_FILE="$2"
            shift 2
            ;;
        --backend-log)
            BACKEND_LOG="$2"
            shift 2
            ;;
        --gateway-log)
            GATEWAY_LOG="$2"
            shift 2
            ;;
        --startup-timeout)
            STARTUP_TIMEOUT_SECONDS="$2"
            shift 2
            ;;
        --dry-run)
            DRY_RUN=true
            shift
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

if [[ -z "${APP_DLL}" ]]; then
    APP_DLL="${APP_ROOT}/Payslip4All.Web.dll"
fi

required_file() {
    local path="$1"
    local label="$2"

    if [[ ! -f "${path}" ]]; then
        echo "${label} not found: ${path}" >&2
        exit 1
    fi
}

read_env_var() {
    local file="$1"
    local key="$2"
    local line

    while IFS= read -r line || [[ -n "${line}" ]]; do
        [[ -z "${line}" || "${line}" =~ ^[[:space:]]*# ]] && continue
        if [[ "${line}" == "${key}="* ]]; then
            printf '%s' "${line#*=}"
            return 0
        fi
    done < "${file}"
}

extract_port() {
    local urls="$1"
    local scheme="$2"
    local url

    while IFS= read -r url; do
        if [[ "${url}" =~ ^${scheme}://[^:/]+:([0-9]+)$ ]]; then
            printf '%s' "${BASH_REMATCH[1]}"
            return 0
        fi
    done < <(printf '%s\n' "${urls}" | tr ';' '\n')

    return 1
}

wait_for_url() {
    local url="$1"
    shift

    for _ in $(seq 1 "${STARTUP_TIMEOUT_SECONDS}"); do
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

run_with_env_file() {
    local env_file="$1"
    local app_dll="$2"
    local line

    while IFS= read -r line || [[ -n "${line}" ]]; do
        [[ -z "${line}" || "${line}" =~ ^[[:space:]]*# ]] && continue
        export "${line}"
    done < "${env_file}"

    exec dotnet "${app_dll}"
}

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

    exit "${exit_code}"
}

required_file "${APP_DLL}" "Application DLL"
required_file "${BACKEND_ENV_FILE}" "Backend env file"
required_file "${GATEWAY_ENV_FILE}" "Gateway env file"

PUBLIC_HOST="$(read_env_var "${GATEWAY_ENV_FILE}" "REVERSE_PROXY_PUBLIC_HOST")"
GATEWAY_URLS="$(read_env_var "${GATEWAY_ENV_FILE}" "ASPNETCORE_URLS")"
BACKEND_URLS="$(read_env_var "${BACKEND_ENV_FILE}" "ASPNETCORE_URLS")"

if [[ -z "${PUBLIC_HOST}" ]]; then
    echo "REVERSE_PROXY_PUBLIC_HOST is required in ${GATEWAY_ENV_FILE}" >&2
    exit 1
fi

if [[ -z "${GATEWAY_URLS}" ]]; then
    echo "ASPNETCORE_URLS is required in ${GATEWAY_ENV_FILE}" >&2
    exit 1
fi

if [[ -z "${BACKEND_URLS}" ]]; then
    echo "ASPNETCORE_URLS is required in ${BACKEND_ENV_FILE}" >&2
    exit 1
fi

BACKEND_PORT="$(extract_port "${BACKEND_URLS}" "http" || true)"
if [[ -z "${BACKEND_PORT}" ]]; then
    echo "Could not determine backend HTTP port from ${BACKEND_ENV_FILE}" >&2
    exit 1
fi

GATEWAY_HTTP_PORT="$(extract_port "${GATEWAY_URLS}" "http" || true)"
GATEWAY_HTTPS_PORT="$(extract_port "${GATEWAY_URLS}" "https" || true)"

if [[ -z "${GATEWAY_HTTP_PORT}" && -z "${GATEWAY_HTTPS_PORT}" ]]; then
    echo "Could not determine gateway listener ports from ${GATEWAY_ENV_FILE}" >&2
    exit 1
fi

mkdir -p "$(dirname "${BACKEND_LOG}")" "$(dirname "${GATEWAY_LOG}")"

if [[ "${DRY_RUN}" == true ]]; then
    cat <<EOF
Dry run only. Resolved configuration:

  app dll: ${APP_DLL}
  backend env: ${BACKEND_ENV_FILE}
  gateway env: ${GATEWAY_ENV_FILE}
  backend urls: ${BACKEND_URLS}
  gateway urls: ${GATEWAY_URLS}
  public host: ${PUBLIC_HOST}
  backend log: ${BACKEND_LOG}
  gateway log: ${GATEWAY_LOG}

Commands:
  ( load "${BACKEND_ENV_FILE}" literally and run dotnet "${APP_DLL}" ) >>"${BACKEND_LOG}" 2>&1
  ( load "${GATEWAY_ENV_FILE}" literally and run dotnet "${APP_DLL}" ) >>"${GATEWAY_LOG}" 2>&1
EOF
    exit 0
fi

trap cleanup EXIT INT TERM

echo "Starting backend from ${APP_DLL}"
(
    run_with_env_file "${BACKEND_ENV_FILE}" "${APP_DLL}"
) >>"${BACKEND_LOG}" 2>&1 &
BACKEND_PID=$!

if ! wait_for_url "http://127.0.0.1:${BACKEND_PORT}/health"; then
    ensure_process_alive "${BACKEND_PID}" "Backend" "${BACKEND_LOG}"
    echo "Backend did not become ready in time." >&2
    exit 1
fi

echo "Starting gateway from ${APP_DLL}"
(
    run_with_env_file "${GATEWAY_ENV_FILE}" "${APP_DLL}"
) >>"${GATEWAY_LOG}" 2>&1 &
GATEWAY_PID=$!

if [[ -n "${GATEWAY_HTTPS_PORT}" ]]; then
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

EC2 YARP environment is running.

Backend:
  ${BACKEND_URLS}

Gateway:
  ${GATEWAY_URLS}

Public host:
  ${PUBLIC_HOST}

Logs:
  backend: ${BACKEND_LOG}
  gateway: ${GATEWAY_LOG}

Suggested checks:
EOF

if [[ -n "${GATEWAY_HTTPS_PORT}" ]]; then
    cat <<EOF
  curl -k --resolve ${PUBLIC_HOST}:${GATEWAY_HTTPS_PORT}:127.0.0.1 https://${PUBLIC_HOST}:${GATEWAY_HTTPS_PORT}/health
  curl -i --resolve unexpected.example.com:${GATEWAY_HTTP_PORT}:127.0.0.1 http://unexpected.example.com:${GATEWAY_HTTP_PORT}/
EOF
else
    cat <<EOF
  curl -i -H 'Host: ${PUBLIC_HOST}' http://127.0.0.1:${GATEWAY_HTTP_PORT}/health
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
