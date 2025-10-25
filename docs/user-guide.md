# User Guide — MCP-UI ASP.NET Core MVC POC

## What is this?
A web app that connects to an MCP server, lists tools/resources, and renders server-provided UI (MCP-UI resources) directly in your browser.

## Prerequisites
- .NET 8 SDK
- Browser (Chrome/Edge/Firefox)
- For quick testing, no local MCP server is required — use the hosted demo endpoints listed below.

## Quick Start
1. Run the app (from your solution root):
   - `dotnet run`
2. Open the app in your browser (default `https://localhost:xxxx`).
3. Connect to a hosted demo MCP server:
   - HTTP Streaming: `https://remote-mcp-server-authless.idosalomon.workers.dev/mcp`
   - SSE: `https://remote-mcp-server-authless.idosalomon.workers.dev/sse`
4. Click “Initialize”. You should see server information returned.
5. Go to the Tools page:
   - Click “List Tools”, select a tool, and “Call Tool”.
   - If the tool returns a `UIResource`, it will render below via `<ui-resource-renderer>`.
6. Interact with the rendered UI:
   - When you click a button or perform an action, the UI emits `onUIAction` events.
   - The app routes these to the backend (e.g., to call another tool) and displays results.

## Rendering Modes
- `text/html` — inline HTML, shown in a sandboxed iframe.
- `text/uri-list` — external URL, shown in a sandboxed iframe (first http/https URL only).
- `application/vnd.mcp-ui.remote-dom(+javascript)` — Remote DOM UI rendered within a sandboxed iframe and host-side component library.

## UI Actions
- `tool` — triggers a tool call on the backend.
- `prompt` — sends a freeform prompt to a handler.
- `intent` — semantic intent routed by the host (often mapped to tools/prompts).
- `notify` — shows a toast/log message.
- `link` — attempts to open a URL (allowed only if in the allowlist).

## Security
- Strong CSP headers and iframe sandboxing are applied.
- External URLs must be allowlisted.
- Large or unexpected payloads are rejected.

## Troubleshooting
- “Initialize failed”
  - Check the endpoint URL and your network connectivity.
- “UI didn’t render”
  - Ensure the returned payload contains a valid `UIResource` with `mimeType` and content.
  - Check the browser console for CSP/sandbox violations.
- “Link blocked”
  - The domain may not be in the allowlist. Ask your admin to add it.

## Advanced
- Realtime (optional): If enabled, the Console shows live logs via SSE/SignalR.
- Local server (optional): Switch transport to `Stdio` in settings and provide `ExePath` and `Args` to spawn a local MCP server.

## FAQ
- Do I need the `idosal/mcp-ui` repo?
  - Not strictly, but we use its Web Component to render UI resources consistently, especially for Remote DOM.
- Can I use REST only?
  - Yes. The POC is REST-first. Realtime is optional.

## Glossary
- MCP — Model Context Protocol.
- UIResource — A payload describing UI to render (HTML/URL/Remote DOM).
- Remote DOM — Server-driven UI where updates are sent as JSON and rendered by a host component library.
