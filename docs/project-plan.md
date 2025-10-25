# Project Plan (Scrum)

## Vision
Deliver an ASP.NET Core MVC client that can connect to MCP servers, render MCP-UI resources, and handle interactive UI actions securely. Start with REST-only and hosted demo servers; add stdio transport and optional realtime later.

## Epics and User Stories

### Epic 1: Transport & Client Abstraction
- Story 1.1: As a developer, I can configure transport to use an HTTP streaming/SSE endpoint.
  - Acceptance: App reads endpoint from settings; `/mcp/connect` succeeds; `/mcp/tools` returns data.
- Story 1.2: As a developer, I can switch to stdio transport (Phase 2) to spawn a local MCP server.
  - Acceptance: Given exe+args, process starts; `initialize` succeeds; graceful shutdown.

### Epic 2: REST API Surface
- Story 2.1: As a user, I can initialize a connection.
  - Acceptance: POST `/mcp/connect` returns server info/protocol version.
- Story 2.2: As a user, I can list tools/resources and paginate with cursors.
  - Acceptance: GET `/mcp/tools`, GET `/mcp/resources` accept optional cursor; return consistent shape.
- Story 2.3: As a user, I can call tools and read resources through REST.
  - Acceptance: POST `/mcp/tools/{name}/call` and GET `/mcp/resources/read?uri=...` return results or errors.

### Epic 3: UI Rendering (mcp-ui Web Component)
- Story 3.1: As a user, I can see UI resources rendered in-page.
  - Acceptance: `<ui-resource-renderer>` renders `text/html`, `text/uri-list`, and Remote DOM examples.
- Story 3.2: As a user, I can perform interactions inside the rendered UI and have them handled by the host.
  - Acceptance: `onUIAction` events are captured and routed to the backend; success/failure surfaced in UI.

### Epic 4: UI Actions Handling
- Story 4.1: As a user, clicking buttons in the UI can trigger tool calls with params.
  - Acceptance: action `{ type: 'tool' }` results in `/mcp/tools/{name}/call`; response displayed.
- Story 4.2: As a user, notifications from the UI appear as toasts/logs.
  - Acceptance: `{ type: 'notify' }` renders a toast and logs the message.
- Story 4.3: As a user, links in UI are allowed or blocked by policy.
  - Acceptance: `{ type: 'link' }` opens if domain is allowlisted; otherwise blocked with message.

### Epic 5: Security & Sandbox
- Story 5.1: As an operator, I can enforce CSP and iframe sandboxing.
  - Acceptance: CSP headers configured; iframe has `sandbox` attributes; external URLs limited by allowlist.
- Story 5.2: As an operator, payload sizes and mime types are validated.
  - Acceptance: invalid or oversized payloads rejected gracefully, logged.

### Epic 6: Remote DOM Support
- Story 6.1: As a user, I can interact with Remote DOM widgets.
  - Acceptance: A Remote DOM example renders and can send actions to host.

### Epic 7: Streaming/Realtime (Optional)
- Story 7.1: As a user, I can see live logs/progress.
  - Acceptance: SSE or SignalR streams append to a console view.
- Story 7.2: As a user, long-running tool output is streamed.
  - Acceptance: incremental output visible without refreshing.

### Epic 8: Server Process Lifecycle (Stdio) (Optional)
- Story 8.1: As an operator, the app can spawn and supervise a local MCP server.
  - Acceptance: restart on crash with backoff; graceful stop on disconnect.

### Epic 9: Observability
- Story 9.1: As an operator, I can view structured logs and request timing.
  - Acceptance: logs show transport, method, duration, sizes; errors have correlation IDs.

### Epic 10: Distribution & Docs
- Story 10.1: As a developer, I can read architecture, project plan, and user guide to get started.
  - Acceptance: `docs/architecture.md`, `docs/project-plan.md`, `docs/user-guide.md` published and maintained.

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
