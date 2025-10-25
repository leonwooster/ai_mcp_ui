# MCP-UI ASP.NET Core MVC POC — Architecture

## Overview
This document describes the architecture of an ASP.NET Core MVC proof-of-concept (POC) that acts as an MCP client and renders MCP-UI resources returned by MCP servers. The POC focuses on a REST-first approach and uses the mcp-ui Web Component on the front end to render server-driven UI. Real-time features (SSE/SignalR) are optional.

## Goals
- Provide a browser UI to connect to an MCP server and exercise its capabilities.
- Support UI resources (HTML, external URL, Remote DOM) returned from tool calls.
- Map UI actions (tool, prompt, intent, notify, link) from the rendered UI back to backend operations.
- Enforce security via CSP and iframe sandboxing.

## Non-goals (POC)
- Full agent orchestration.
- Advanced authentication/authorization.
- Production-hardening of process isolation beyond basic sandboxing/CSP.

## High-level Architecture
- `Browser (Razor Views + JS)`
  - Renders lists of tools/resources.
  - Embeds `mcp-ui` Web Component `<ui-resource-renderer>` to display returned UI resources.
  - Captures `onUIAction` events and sends them to the backend.
- `ASP.NET Core MVC Backend`
  - `McpClientService`: abstraction over MCP transports.
    - Phase 1: HTTP Streaming or SSE endpoints (hosted demo server from mcp-ui repo).
    - Phase 2: Stdio transport that spawns a local MCP server process.
  - `Controllers` (`McpController`): REST endpoints to initialize, list tools/resources, call tools, read resources.
  - `ActionRouter`: routes UI actions (tool/prompt/intent/notify/link) to the appropriate backend operations.
  - Optional `SignalR` Hub (Phase 2+): push notifications/streaming updates to the browser.
  - Optional `McpProcessManager`: spawn and manage a local MCP server process (stdio).

## Data Flow
1. User connects (REST) → `McpClientService.initialize()` with selected transport and server URL/exe.
2. User lists tools/resources (REST) → backend calls server → returns JSON.
3. User calls a tool (REST) → server may return text or a `UIResource` payload.
4. Browser renders `UIResource` via `<ui-resource-renderer>`.
5. User interacts with the UI → component emits `onUIAction` events.
6. Browser forwards actions to `ActionRouter` (REST or SignalR) → backend executes (e.g., call another tool) → returns result → UI updates.

## UIResource Support
- `text/html` → inline HTML rendered via iframe (srcdoc).
- `text/uri-list` → external URL rendered in iframe (first valid http/https URL only).
- `application/vnd.mcp-ui.remote-dom(+javascript)` → Remote DOM rendered within a sandboxed iframe and host-side component library.

## UI Actions
- `tool`: `{ toolName, params }` → backend POST `/mcp/tools/{name}/call`.
- `prompt`: `{ prompt }` → backend may route to a generic handler/tool.
- `intent`: `{ intent, params }` → map to a tool/prompt.
- `notify`: `{ message }` → show toast/log.
- `link`: `{ url }` → validate against allowlist; open in new tab or reject.

## Security
- Content Security Policy (CSP):
  - Restrict script sources; allow only the mcp-ui component bundle and sandboxed iframe content.
  - Restrict `frame-src` to self and explicit allowlist for external URLs used by `text/uri-list`.
- Iframe sandboxing:
  - Use `sandbox` attributes to prevent top-level navigation, limit APIs, and isolate storage.
- Validate `mimeType`, `uri`, and maximum payload sizes.
- Optional domain allowlist for `text/uri-list` resources.

## Configuration
- `appsettings.json` keys:
  - `Mcp:Transport`: `HttpStreaming`, `Sse`, or `Stdio`.
  - `Mcp:Http:Endpoint`: hosted demo URL (e.g., `https://remote-mcp-server-authless.idosalomon.workers.dev/mcp`).
  - `Mcp:Sse:Endpoint`: `https://remote-mcp-server-authless.idosalomon.workers.dev/sse`.
  - `Mcp:Stdio:ExePath` and `Mcp:Stdio:Args` (Phase 2+).
  - `Security:AllowedIframeDomains` (list).
  - `Limits:MaxPayloadBytes`.

## Error Handling & Observability
- Normalize JSON-RPC errors into consistent API responses.
- Log request/response timing and sizes.
- Capture MCP server stderr/stdout logs (for stdio).
- Health endpoints: `/health/live` and `/health/ready`.

## Extensibility
- Pluggable transport strategy (`HttpStreaming`, `Sse`, `Stdio`).
- Schema-driven forms for tools using JSON Schema (future).
- Optional SignalR for bidirectional streaming and async action replies.

## Dependencies
- Backend: ASP.NET Core MVC, System.Text.Json.
- Optional: SignalR (server + JS), Serilog.
- Frontend: `@mcp-ui/client` Web Component (`ui-resource-renderer.wc.js`).

## Environments
- Dev: use hosted demo MCP server endpoints to validate UI rendering quickly.
- Local: later, switch to stdio and a local MCP server (Node/Python/Ruby examples in the mcp-ui repo).
