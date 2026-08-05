#!/usr/bin/env bash
# dev.sh — local lifecycle manager for the Ecommerce-Redo shop (Vue + .NET 10)
#
# Services:
#   api       .NET API            http://127.0.0.1:5100
#   frontend  Vite dev server     http://127.0.0.1:5173  (proxies /api -> :5100)
#   gateway   YARP gateway        http://127.0.0.1:5000   (optional)
#
# Usage:
#   ./dev.sh start   [api|frontend|gateway|all]   # default target = all (= api + frontend)
#   ./dev.sh stop    [api|frontend|gateway|all]
#   ./dev.sh restart [api|frontend|gateway|all]
#   ./dev.sh status                              # show running services + ports + health
#   ./dev.sh logs     [api|frontend|gateway]     # tail a service log (Ctrl-C to exit)
#   ./dev.sh build                              # build backend + frontend (no run)
#   ./dev.sh down                               # stop everything
#
# Environment overrides (all optional):
#   EC_DB_SERVER       SQL Server host as the app should reach it.
#                      IMPORTANT: if your SQLEXPRESS has TCP+NamedPipes disabled, set
#                      EC_DB_SERVER="lpc:.\SQLEXPRESS" to use local shared memory.
#                      Default: the host already configured in appsettings.json.
#   EC_API_PORT        API port        (default 5100)
#   EC_FE_PORT         frontend port   (default 5173)
#   EC_GW_PORT         gateway port    (default 5000)
#   EC_JWT_KEY         JWT signing key (only needed if you run the API in Production;
#                      Development tolerates the appsettings.json placeholder)
#
# Runtime artefacts (pid files + logs) live in .run/ (gitignored).

set -uo pipefail

# ----------------------------------------------------------------------------
# Paths & config
# ----------------------------------------------------------------------------
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
API_DIR="$ROOT/src/backend/Ecommerce.Api"
GW_DIR="$ROOT/src/backend/Ecommerce.Gateway"
FE_DIR="$ROOT/src/frontend"
RUN_DIR="$ROOT/.run"
LOG_DIR="$RUN_DIR/logs"
mkdir -p "$LOG_DIR"

# .NET 10 is a user install on this machine; put it on PATH for child processes.
export PATH="${LOCALAPPDATA:-$HOME/.local/share}/Microsoft/dotnet:$PATH"

API_PORT="${EC_API_PORT:-5100}"
FE_PORT="${EC_FE_PORT:-5173}"
GW_PORT="${EC_GW_PORT:-5000}"

# Optional DB host override -> injected as the app's connection string at runtime.
if [[ -n "${EC_DB_SERVER:-}" ]]; then
  export ConnectionStrings__DefaultConnection="Server=${EC_DB_SERVER};Database=LegacyEcommerceDb;Trusted_Connection=True;TrustServerCertificate=True;Pooling=true;Min Pool Size=2;Max Pool Size=50;Connect Timeout=10;Application Name=Ecommerce.Api"
fi
if [[ -n "${EC_JWT_KEY:-}" ]]; then
  export Jwt__SigningKey="$EC_JWT_KEY"
fi

export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"

# ----------------------------------------------------------------------------
# Pretty print
# ----------------------------------------------------------------------------
if [[ -t 1 ]]; then
  C_GREEN=$'\033[32m'; C_RED=$'\033[31m'; C_YELLOW=$'\033[33m'
  C_CYAN=$'\033[36m';  C_BOLD=$'\033[1m'; C_OFF=$'\033[0m'
else
  C_GREEN=""; C_RED=""; C_YELLOW=""; C_CYAN=""; C_BOLD=""; C_OFF=""
fi
ok()    { printf "%s✓%s %s\n" "$C_GREEN" "$C_OFF" "$*"; }
err()   { printf "%s✗%s %s\n" "$C_RED"   "$C_OFF" "$*" >&2; }
info()  { printf "%s•%s %s\n" "$C_CYAN"   "$C_OFF" "$*"; }
warn()  { printf "%s!%s %s\n"  "$C_YELLOW" "$C_OFF" "$*"; }
header(){ printf "%s== %s ==%s\n" "$C_BOLD" "$*" "$C_OFF"; }

# ----------------------------------------------------------------------------
# Helpers
# ----------------------------------------------------------------------------
need_cmd() { command -v "$1" >/dev/null 2>&1 || { err "command not found: $1"; return 1; }; }

# Is a Windows PID alive? (uses tasklist — reliable for native pids on Git Bash)
alive() {
  local pid="$1"
  [[ -z "$pid" ]] && return 1
  tasklist //FI "PID eq $pid" 2>/dev/null | grep -qw "$pid"
}

# PID currently LISTENING on a port (empty if none / not found).
# Matches both IPv4 (127.0.0.1:5100) and IPv6 ([::1]:5100) local addresses.
port_pid() {
  local port="$1"
  netstat -ano 2>/dev/null \
    | awk -v p=":$port" '$4 == "LISTENING" && $2 ~ p "$" {print $5; exit}'
}
port_in_use() { local p; p="$(port_pid "$1")"; [[ -n "$p" ]]; }

# HTTP health of a local port: returns 0 if it answers 2xx.
# Uses "localhost" so it works whether the service binds IPv4 (127.0.0.1,
# as the .NET API does) or IPv6 ([::1], as Vite does by default).
port_up() {
  local port="$1" path="${2:-/}"
  curl -fsS -m 3 "http://localhost:${port}${path}" >/dev/null 2>&1
}

svc_pidfile() { echo "$RUN_DIR/$1.pid"; }
svc_logfile() { echo "$LOG_DIR/$1.log"; }

# Resolve the service target list from the CLI arg.
#   "all" or "" -> api frontend   (gateway stays opt-in; few people need it locally)
resolve_targets() {
  local arg="${1:-all}"
  case "$arg" in
    all|"")   echo "api frontend" ;;
    api|frontend|gateway) echo "$arg" ;;
    *) err "unknown target '$arg' (use api|frontend|gateway|all)"; exit 2 ;;
  esac
}

# ----------------------------------------------------------------------------
# Per-service start
#
# Liveness model: dotnet/npm launchers spawn the real server as a CHILD and
# then exit, so $! (the launcher pid) is NOT a reliable liveness signal.
# We therefore treat the PORT as the source of truth: a service is "up" when
# its port answers, and "failed" only if the launcher died AND the port never
# came up. The pidfile is just a best-effort marker; stop sweeps by port too.
# ----------------------------------------------------------------------------

# wait_for_port <name> <port> <health_path> <launcher_pid> <log> <seconds>
wait_for_port() {
  local name="$1" port="$2" hpath="$3" lpid="$4" logf="$5" secs="$6"
  local i
  for i in $(seq 1 "$secs"); do
    if port_up "$port" "$hpath"; then
      ok "${name} up   → http://127.0.0.1:${port}${hpath%*/}"; return 0
    fi
    # Only declare failure when BOTH the launcher is gone AND nothing owns the port.
    if ! alive "$lpid" && ! port_in_use "$port"; then
      err "${name} failed to start — last log lines:"; tail -n 20 "$logf" >&2; return 1
    fi
    sleep 1
  done
  warn "${name} started but not answering within ${secs}s — check: ./dev.sh logs ${name}"
  return 0
}

start_api() {
  local pidf; pidf="$(svc_pidfile api)"; local logf; logf="$(svc_logfile api)"
  if alive "$(cat "$pidf" 2>/dev/null || true)" || port_in_use "$API_PORT"; then
    warn "api already running (port $API_PORT)"; return 0
  fi
  need_cmd dotnet || return 1
  header "Starting api on :$API_PORT"
  # Build first so `dotnet run --no-build` launches the server directly (fast,
  # and avoids the build-then-handoff lag that makes liveness detection flaky).
  (cd "$API_DIR" && dotnet build --nologo -v quiet >/dev/null 2>&1) || { err "api build failed — run: dotnet build Ecommerce.sln"; return 1; }
  (
    cd "$API_DIR"
    # Matches the README manual run: `dotnet run --urls http://127.0.0.1:5100`.
    # ASPNETCORE_ENVIRONMENT + any EC_* overrides are exported by the parent.
    nohup dotnet run --no-build --urls "http://127.0.0.1:${API_PORT}" >"$logf" 2>&1 &
    echo $! >"$pidf"
  )
  info "launcher pid $(cat "$pidf") — waiting for health…"
  wait_for_port api "$API_PORT" /api/health "$(cat "$pidf")" "$logf" 45
}

start_frontend() {
  local pidf; pidf="$(svc_pidfile frontend)"; local logf; logf="$(svc_logfile frontend)"
  if alive "$(cat "$pidf" 2>/dev/null || true)" || port_in_use "$FE_PORT"; then
    warn "frontend already running (port $FE_PORT)"; return 0
  fi
  need_cmd npm || return 1
  [[ -d "$FE_DIR/node_modules" ]] || { info "installing frontend deps…"; (cd "$FE_DIR" && npm install) || return 1; }
  header "Starting frontend on :$FE_PORT"
  (
    cd "$FE_DIR"
    nohup npm run dev -- --port "$FE_PORT" --strictPort >"$logf" 2>&1 &
    echo $! >"$pidf"
  )
  info "launcher pid $(cat "$pidf") — waiting for dev server…"
  wait_for_port frontend "$FE_PORT" / "$(cat "$pidf")" "$logf" 30
}

start_gateway() {
  local pidf; pidf="$(svc_pidfile gateway)"; local logf; logf="$(svc_logfile gateway)"
  if alive "$(cat "$pidf" 2>/dev/null || true)" || port_in_use "$GW_PORT"; then
    warn "gateway already running (port $GW_PORT)"; return 0
  fi
  need_cmd dotnet || return 1
  header "Starting gateway on :$GW_PORT"
  (cd "$GW_DIR" && dotnet build --nologo -v quiet >/dev/null 2>&1) || { err "gateway build failed"; return 1; }
  (
    cd "$GW_DIR"
    nohup dotnet run --no-build --urls "http://127.0.0.1:${GW_PORT}" >"$logf" 2>&1 &
    echo $! >"$pidf"
  )
  info "launcher pid $(cat "$pidf") — waiting for gateway…"
  wait_for_port gateway "$GW_PORT" / "$(cat "$pidf")" "$logf" 30
}

# ----------------------------------------------------------------------------
# Stop (tree-kill the recorded pid, then sweep anything left on the port)
# ----------------------------------------------------------------------------
stop_port() {
  local port="$1"
  local p; p="$(port_pid "$port")"
  [[ -z "$p" ]] && return 0
  taskkill //PID "$p" //F //T >/dev/null 2>&1 || kill "$p" >/dev/null 2>&1 || true
  # confirm it's gone
  local i
  for i in 1 2 3 4 5; do [[ -z "$(port_pid "$port")" ]] && return 0; sleep 0.5; done
  return 0
}

stop_api()      { header "Stopping api";      local pidf; pidf="$(svc_pidfile api)";      alive "$(cat "$pidf" 2>/dev/null||true)" && taskkill //PID "$(cat "$pidf")" //F //T >/dev/null 2>&1 || true; rm -f "$pidf"; stop_port "$API_PORT"; ok "api stopped"; }
stop_frontend() { header "Stopping frontend"; local pidf; pidf="$(svc_pidfile frontend)"; alive "$(cat "$pidf" 2>/dev/null||true)" && taskkill //PID "$(cat "$pidf")" //F //T >/dev/null 2>&1 || true; rm -f "$pidf"; stop_port "$FE_PORT";  ok "frontend stopped"; }
stop_gateway()  { header "Stopping gateway";  local pidf; pidf="$(svc_pidfile gateway)";  alive "$(cat "$pidf" 2>/dev/null||true)" && taskkill //PID "$(cat "$pidf")" //F //T >/dev/null 2>&1 || true; rm -f "$pidf"; stop_port "$GW_PORT";  ok "gateway stopped"; }

# ----------------------------------------------------------------------------
# Status
# ----------------------------------------------------------------------------
svc_state() {
  local svc="$1" port="$2" health_path="${3:-/}"
  local pidf; pidf="$(svc_pidfile "$svc")"
  local owned; owned="$(cat "$pidf" 2>/dev/null || true)"   # pid dev.sh launched (may be a dead launcher)
  local actual; actual="$(port_pid "$port")"                # pid actually holding the port (the real server)
  local mark="$C_RED down $C_OFF" note=""
  if port_up "$port" "$health_path"; then
    mark="$C_GREEN up   $C_OFF"; note="(pid ${actual:-?}, healthy)"
  elif port_in_use "$port"; then
    # Port held but health path not answering.
    if [[ -n "$owned" || "$owned" == "$actual" || -f "$pidf" ]]; then
      mark="$C_YELLOW up?  $C_OFF"; note="(pid ${actual:-?}, starting…)"
    else
      mark="$C_YELLOW ghost$C_OFF"; note="(port $port held by pid ${actual:-?}, not started by dev.sh)"
    fi
  fi
  printf "  %-9s :%-5s  %s %s\n" "$svc" "$port" "$mark" "$note"
}

cmd_status() {
  header "Ecommerce-Redo status"
  { command -v dotnet >/dev/null && ok "dotnet $(dotnet --version 2>/dev/null)"; } || err "dotnet not found on PATH"
  { command -v node   >/dev/null && ok "node $(node --version 2>/dev/null)"; }     || err "node not found"
  echo
  svc_state api      "$API_PORT" /api/health
  svc_state frontend "$FE_PORT" /
  svc_state gateway  "$GW_PORT" /
  echo
  info "logs in $LOG_DIR/"
}

# ----------------------------------------------------------------------------
# Build
# ----------------------------------------------------------------------------
cmd_build() {
  header "Building backend"
  need_cmd dotnet || return 1
  (cd "$ROOT" && dotnet build Ecommerce.sln) || { err "backend build failed"; return 1; }
  header "Building frontend (typecheck + vite)"
  need_cmd npm || return 1
  (cd "$FE_DIR" && [[ -d node_modules ]] || npm install) && (cd "$FE_DIR" && npm run build) \
    || { err "frontend build failed"; return 1; }
  ok "build complete"
}

# ----------------------------------------------------------------------------
# Logs
# ----------------------------------------------------------------------------
cmd_logs() {
  local svc="${1:-}"
  case "$svc" in
    api|frontend|gateway) ;;
    "") err "usage: ./dev.sh logs <api|frontend|gateway>"; exit 2 ;;
    *)   err "unknown service '$svc'"; exit 2 ;;
  esac
  local logf; logf="$(svc_logfile "$svc")"
  [[ -f "$logf" ]] || { warn "no log yet for $svc ($logf)"; exit 0; }
  info "tailing $logf (Ctrl-C to exit)"
  tail -n 50 -f "$logf"
}

# ----------------------------------------------------------------------------
# Dispatch
# ----------------------------------------------------------------------------
usage() {
  sed -n '2,26p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'
}

main() {
  local cmd="${1:-}"; shift || true
  local targets; targets="$(resolve_targets "${1:-all}")"
  case "$cmd" in
    start)
      rc=0
      for t in $targets; do "start_$t" || rc=$?; done
      return $rc ;;
    stop)
      for t in $targets; do "stop_$t"; done ;;
    restart)
      for t in $targets; do "stop_$t"; done
      sleep 1
      rc=0
      for t in $targets; do "start_$t" || rc=$?; done
      return $rc ;;
    status)  cmd_status ;;
    logs)    cmd_logs "${1:-}" ;;
    build)   cmd_build ;;
    down)    for t in api frontend gateway; do "stop_$t"; done ;;
    help|-h|--help) usage ;;
    "") usage; exit 0 ;;
    *) err "unknown command '$cmd'"; usage; exit 2 ;;
  esac
}

main "$@"
