# Project Plan (Scrum)

## Vision
Deliver an ASP.NET Core MVC client that can connect to MCP servers, render MCP-UI resources, and handle interactive UI actions securely. Start with REST-only and hosted demo servers; add stdio transport and optional realtime later.

## Epics and User Stories

### Epic 1: Transport & Client Abstraction
- Story 1.1: As a developer, I can configure transport to use an HTTP streaming/SSE endpoint.
  - Acceptance Criteria:
    - Given a valid HTTP streaming or SSE endpoint is configured in settings, when I POST to `/mcp/connect` with that endpoint, then I receive 200 OK with server info and protocol version.
    - Given the connection is initialized, when I GET `/mcp/tools`, then I receive a non-empty array (or an empty array with 200 OK) within 3s and valid JSON.
    - Given an invalid endpoint, when I POST to `/mcp/connect`, then I receive a 4xx/5xx with a meaningful error message logged.
- Story 1.2: As a developer, I can switch to stdio transport (Phase 2) to spawn a local MCP server.
  - Acceptance Criteria:
    - Given valid `ExePath` and `Args`, when I start the connection, then the process starts and stderr logs are captured.
    - Given the server process is started, when I call `initialize`, then I receive a 200 OK with protocol version and server info.
    - Given the connection is active, when I disconnect, then the process exits within 5s (graceful shutdown) or is killed and reported.

### Epic 2: REST API Surface
- Story 2.1: As a user, I can initialize a connection.
  - Acceptance Criteria:
    - Given a reachable server, when I POST to `/mcp/connect`, then the response includes `protocolVersion`, `clientInfo/serverInfo`, and no error.
    - Given a bad configuration, when I POST to `/mcp/connect`, then I receive a structured error with code and message.
- Story 2.2: As a user, I can list tools/resources and paginate with cursors.
  - Acceptance Criteria:
    - Given tools exist, when I GET `/mcp/tools`, then I receive `{ tools: [...], nextCursor?: string }` and optional `nextCursor` for pagination.
    - Given resources exist, when I GET `/mcp/resources`, then I receive `{ resources: [...], nextCursor?: string }` and optional `nextCursor`.
    - Given an invalid cursor, when I GET list endpoints, then a 400 error is returned with a clear message.
- Story 2.3: As a user, I can call tools and read resources through REST.
  - Acceptance Criteria:
    - Given a valid tool name and JSON args, when I POST to `/mcp/tools/{name}/call`, then I receive either a text result or a UIResource according to spec.
    - Given a valid resource URI, when I GET `/mcp/resources/read?uri=...`, then I receive content with correct mime type and data.
    - Given a missing tool or bad args, when I POST to call, then I receive a structured error (code, message, details).

### Epic 3: UI Rendering (mcp-ui Web Component)
- Story 3.1: As a user, I can see UI resources rendered in-page.
  - Acceptance Criteria:
    - Given a `text/html` UIResource, when rendered, then the iframe renders the HTML via srcdoc without CSP violations.
    - Given a `text/uri-list` UIResource with an http/https URL, when rendered, then the iframe displays the external page (first valid URL only).
    - Given a Remote DOM UIResource, when rendered, then the widget appears using the basic component library without console errors.
- Story 3.2: As a user, I can perform interactions inside the rendered UI and have them handled by the host.
  - Acceptance Criteria:
    - Given the UI emits `onUIAction` with `{ type: 'tool', payload: { toolName, params } }`, when captured, then the backend invokes the tool and returns the result to the page.
    - Given an action fails on the backend, when the UI awaits completion, then an error message is surfaced to the user.

### Epic 4: UI Actions Handling
- Story 4.1: As a user, clicking buttons in the UI can trigger tool calls with params.
  - Acceptance Criteria:
    - Given a UI button triggers `{ type: 'tool' }`, when handled, then `/mcp/tools/{name}/call` is invoked with params and the response is displayed.
    - Given a long-running call, when invoked, then a loading state is shown and cleared on completion.
- Story 4.2: As a user, notifications from the UI appear as toasts/logs.
  - Acceptance Criteria:
    - Given `{ type: 'notify', payload: { message } }`, when received, then a toast is shown and the message is appended to the console/log pane.
- Story 4.3: As a user, links in UI are allowed or blocked by policy.
  - Acceptance Criteria:
    - Given a link action to an allowlisted domain, when clicked, then it opens in a new tab.
    - Given a link action to a non-allowlisted domain, when clicked, then it is blocked and a warning is shown.

### Epic 5: Security & Sandbox
- Story 5.1: As an operator, I can enforce CSP and iframe sandboxing.
  - Acceptance Criteria:
    - Given the app runs, when I inspect response headers, then CSP is present restricting scripts, frames, and connect sources.
    - Given embedded content, when rendered, then iframes include `sandbox` attributes preventing top-level navigation.
    - Given a `text/uri-list` URL not on the allowlist, when rendered, then it is rejected with a clear message.
- Story 5.2: As an operator, payload sizes and mime types are validated.
  - Acceptance Criteria:
    - Given an oversized UIResource payload, when received, then it is rejected with 413 and logged.
    - Given an unsupported `mimeType`, when received, then it is rejected with 415 and logged.

### Epic 6: Remote DOM Support
- Story 6.1: As a user, I can interact with Remote DOM widgets.
  - Acceptance Criteria:
    - Given a Remote DOM resource, when loaded, then remote components render and respond to interactions without console errors.
    - Given a Remote DOM action is dispatched, when handled, then the backend receives and processes it (e.g., tool call) successfully.

### Epic 7: Streaming/Realtime (Optional)
- Story 7.1: As a user, I can see live logs/progress.
  - Acceptance Criteria:
    - Given SSE/SignalR is enabled, when the backend receives new logs, then the console view appends entries in real time and preserves scroll position.
    - Given a network interruption, when connectivity resumes, then the stream reconnects and resumes without duplicating entries.
- Story 7.2: As a user, long-running tool output is streamed.
  - Acceptance Criteria:
    - Given a long-running tool, when invoked, then partial output is displayed incrementally (at least one update every N seconds) until completion.
    - Given the tool fails mid-stream, when it errors, then the UI shows an error and the stream ends.

### Epic 8: Server Process Lifecycle (Stdio) (Optional)
- Story 8.1: As an operator, the app can spawn and supervise a local MCP server.
  - Acceptance Criteria:
    - Given the server crashes unexpectedly, when supervision is enabled, then it restarts with exponential backoff and logs the incident.
    - Given the user clicks disconnect, when supervision is active, then the server stops gracefully and no orphaned processes remain.

### Epic 9: Observability
- Story 9.1: As an operator, I can view structured logs and request timing.
  - Acceptance Criteria:
    - Given requests are executed, when I view logs, then each entry includes transport, method, duration, request/response sizes, and status.
    - Given an error occurs, when I inspect logs, then a correlation ID is present and surfaced to clients.

### Epic 10: Distribution & Docs
- Story 10.1: As a developer, I can read architecture, project plan, and user guide to get started.
  - Acceptance Criteria:
    - Given the repo is cloned, when I open `docs/architecture.md`, `docs/project-plan.md`, and `docs/user-guide.md`, then they load without broken links and reflect the current feature set.
    - Given new features ship, when docs are reviewed, then they are updated within the milestone before release.

## Backlog — Future Improvements
- Schema-driven forms for tool arguments using JSON Schema.
- UI state persistence, multi-session management, and transcript export.
- Authentication/authorization for multi-user deployments.
- Configurable adapters (e.g., Apps SDK) and host integrations.
- WebSocket bridge for browser-only deployments without server backend.
- Packaging: Dockerfile, CI/CD pipeline, environment matrices.

## Possible Use Cases
- Interactive data dashboards returned as UI resources (filters, charts, drill-downs).
- Workflow wizards that chain tool calls via UI actions.
- File explorers/editors emitted by filesystem MCP servers.
- Prompt builders that emit intents or prompts through UI.
- CRUD admin panels backed by domain-specific MCP servers.

## Releases (Milestones)
- M1: REST-only with hosted demo endpoints; HTML/URL UIResource rendering; basic actions (tool, notify, link).
- M2: Remote DOM examples; CSP hardening; domain allowlist.
- M3: SSE/SignalR console; long-running tool streaming.
- M4: Stdio transport and local server process management.
- M5: JSON Schema forms; transcripts; packaging.
