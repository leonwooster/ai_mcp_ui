# Test Plan for MCP-UI ASP.NET Core MVC POC

## Overview
This document provides a comprehensive test plan for manually testing the MCP-UI ASP.NET Core MVC POC implementation, starting from Epic 3 (UI Rendering).

## Prerequisites
- .NET 8 SDK installed
- Web browser (Chrome/Edge/Firefox recommended)
- Access to hosted demo MCP servers or local MCP server

## Epic 3: UI Rendering (mcp-ui Web Component)

### Story 3.1: As a user, I can see UI resources rendered in-page

#### Test Cases

**TC-3.1.1: HTML UIResource Rendering**
1. Connect to a hosted demo MCP server
2. Initialize the connection
3. List available tools
4. Find and call a tool that returns an HTML UIResource
5. Verify that:
   - The HTML content is rendered in a sandboxed iframe
   - No CSP violations occur in the browser console
   - The iframe displays the content correctly

**TC-3.1.2: URI-list UIResource Rendering**
1. Connect to a hosted demo MCP server
2. Initialize the connection
3. List available tools
4. Find and call a tool that returns a URI-list UIResource with an HTTP/HTTPS URL
5. Verify that:
   - The external page is displayed in a sandboxed iframe
   - Only the first valid URL is rendered
   - The iframe displays the content correctly

**TC-3.1.3: Remote DOM UIResource Rendering**
1. Connect to a hosted demo MCP server
2. Initialize the connection
3. List available tools
4. Find and call a tool that returns a Remote DOM UIResource
5. Verify that:
   - Remote DOM components are rendered using the basic component library
   - No console errors appear
   - Components are displayed correctly

### Story 3.2: As a user, I can perform interactions inside the rendered UI and have them handled by the host

#### Test Cases

**TC-3.2.1: Tool Action Handling**
1. Render a UIResource that contains a button triggering a tool action
2. Click the button
3. Verify that:
   - The backend invokes the specified tool
   - The tool is called with the correct parameters
   - The result is returned to the page
   - The result is displayed appropriately

**TC-3.2.2: Error Handling**
1. Render a UIResource that triggers a tool action
2. Click the button to trigger an action that will fail on the backend
3. Verify that:
   - An error message is surfaced to the user
   - The error is displayed in a clear and understandable way
   - The UI remains functional after the error

## Epic 4: UI Actions Handling

### Story 4.1: As a user, clicking buttons in the UI can trigger tool calls with params

#### Test Cases

**TC-4.1.1: Basic Tool Call**
1. Render a UIResource with a button that triggers a tool action
2. Click the button
3. Verify that:
   - `/mcp/tools/{name}/call` is invoked with the correct parameters
   - The response is displayed to the user

**TC-4.1.2: Long-running Tool Call**
1. Render a UIResource with a button that triggers a long-running tool
2. Click the button
3. Verify that:
   - A loading state is shown immediately
   - The loading state is cleared when the tool completes
   - The result is displayed after completion

### Story 4.2: As a user, notifications from the UI appear as toasts/logs

#### Test Cases

**TC-4.2.1: Notification Handling**
1. Render a UIResource that emits a notify action
2. Verify that:
   - A toast notification is shown
   - The message is appended to the console/log pane
   - Both the toast and log contain the correct message

### Story 4.3: As a user, links in UI are allowed or blocked by policy

#### Test Cases

**TC-4.3.1: Allowed Link**
1. Render a UIResource with a link to an allowed domain (e.g., github.com)
2. Click the link
3. Verify that:
   - The link opens in a new tab
   - No warnings or errors are shown

**TC-4.3.2: Blocked Link**
1. Render a UIResource with a link to a non-allowed domain
2. Click the link
3. Verify that:
   - The link is blocked
   - A warning is shown to the user
   - The warning clearly explains why the link was blocked

## Epic 5: Security & Sandbox

### Story 5.1: As an operator, I can enforce CSP and iframe sandboxing

#### Test Cases

**TC-5.1.1: CSP Headers**
1. Run the application
2. Open browser developer tools
3. Navigate to the Network tab
4. Load any page that renders UI resources
5. Verify that:
   - CSP headers are present in the response
   - CSP restricts scripts, frames, and connect sources appropriately

**TC-5.1.2: Iframe Sandboxing**
1. Render any UIResource that uses iframes
2. Inspect the iframe elements
3. Verify that:
   - iframes include appropriate `sandbox` attributes
   - Sandbox attributes prevent top-level navigation
   - Additional security attributes are present (referrerpolicy, loading)

**TC-5.1.3: URI-list Allowlist Enforcement**
1. Render a URI-list UIResource with a URL not on the allowlist
2. Verify that:
   - The resource is rejected
   - A clear message is displayed explaining the rejection

### Story 5.2: As an operator, payload sizes and mime types are validated

#### Test Cases

**TC-5.2.1: Oversized Payload Rejection**
1. Attempt to render a UIResource with content exceeding the maximum allowed size (10MB)
2. Verify that:
   - The payload is rejected with HTTP 413 (Payload Too Large)
   - The rejection is logged appropriately
   - A clear error message is provided

**TC-5.2.2: Unsupported MIME Type Rejection**
1. Attempt to render a UIResource with an unsupported MIME type
2. Verify that:
   - The payload is rejected with HTTP 415 (Unsupported Media Type)
   - The rejection is logged appropriately
   - A clear error message is provided

## Epic 6: Remote DOM Support

### Story 6.1: As a user, I can interact with Remote DOM widgets

#### Test Cases

**TC-6.1.1: Remote DOM Component Rendering**
1. Render a Remote DOM UIResource
2. Verify that:
   - Remote components render without console errors
   - Components are displayed using the appropriate styling
   - Component properties are displayed correctly

**TC-6.1.2: Remote DOM Action Handling**
1. Render a Remote DOM UIResource with interactive components
2. Interact with a component (e.g., click a button)
3. Verify that:
   - The backend receives the Remote DOM action
   - The action is processed successfully
   - Any UI updates are applied correctly

## Epic 7: Streaming/Realtime (Optional)

### Story 7.1: As a user, I can see live logs/progress

#### Test Cases

**TC-7.1.1: Real-time Log Streaming**
1. Enable SSE/SignalR if available
2. Perform actions that generate backend logs
3. Verify that:
   - New log entries appear in real-time in the console view
   - The scroll position is preserved appropriately
   - Log entries are appended without page refresh

**TC-7.1.2: Network Interruption Handling**
1. Enable SSE/SignalR if available
2. While streaming logs, simulate a network interruption
3. Restore network connectivity
4. Verify that:
   - The stream reconnects automatically
   - No duplicate entries appear
   - Streaming resumes from the correct point

### Story 7.2: As a user, long-running tool output is streamed

#### Test Cases

**TC-7.2.1: Streaming Tool Output**
1. Call a long-running tool that produces incremental output
2. Verify that:
   - Partial output is displayed incrementally
   - Updates occur at regular intervals
   - The stream continues until tool completion

**TC-7.2.2: Streaming Error Handling**
1. Call a long-running tool that fails mid-stream
2. Verify that:
   - The UI shows an appropriate error message
   - The stream ends properly
   - The error is clearly communicated to the user

## Test Data Requirements

### Hosted Demo Endpoints
- HTTP Streaming: `https://remote-mcp-server-authless.idosalomon.workers.dev/mcp`
- SSE: `https://remote-mcp-server-authless.idosalomon.workers.dev/sse`

### Expected Tools/Resources
- Tools that return HTML UIResources
- Tools that return URI-list UIResources
- Tools that return Remote DOM UIResources
- Long-running tools for streaming tests
- Tools that generate notifications

## Test Environment Setup

1. Clone the repository
2. Ensure .NET 8 SDK is installed
3. Run the application using `dotnet run`
4. Open the application in a browser (default: `https://localhost:xxxx`)
5. Connect to a hosted demo MCP server

## Expected Results

All test cases should pass with the following criteria:
- No console errors (except for expected security violations that are properly handled)
- All UI elements render correctly
- All user interactions produce expected results
- Security policies are enforced appropriately
- Error handling is robust and user-friendly
- Real-time features work as expected when enabled
