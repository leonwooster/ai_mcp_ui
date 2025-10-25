using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using McpUi.Web.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;

namespace McpUi.Web.Services
{
    public class McpJsonRpcHttpClient : IMcpClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptions<McpOptions> _options;
        private readonly ILogger<McpJsonRpcHttpClient> _logger;
        private string _sessionId = Guid.Empty.ToString();

        // Expose session ID for controller to return to client
        public string SessionId => _sessionId;

        // Allow setting session ID from outside (e.g., from request body)
        public void SetSessionId(string sessionId)
        {
            if (!string.IsNullOrEmpty(sessionId))
            {
                _sessionId = sessionId;
                _logger.LogInformation("Session ID set to: {SessionId}", _sessionId);
            }
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public McpJsonRpcHttpClient(IHttpClientFactory httpClientFactory, IOptions<McpOptions> options, ILogger<McpJsonRpcHttpClient> logger)
        {
            _httpClientFactory = httpClientFactory;
            _options = options;
            _logger = logger;
        }

        public async Task<JsonDocument> InitializeAsync(string? endpointOverride = null, CancellationToken ct = default)
        {
            // For now, we'll keep generating our own session ID
            // In a real implementation, we might want to use a session ID from the server response
            _sessionId = Guid.NewGuid().ToString("N");
            _logger.LogInformation("Generating new session ID: {SessionId}", _sessionId);
            
            var payload = new
            {
                protocolVersion = "0.1.0",
                clientInfo = new { name = "aspnet-mvc-mcp-ui-poc", version = "0.1.0" },
                capabilities = new { }
            };
            var doc = await SendAsync("initialize", payload, endpointOverride, ct);
            // Log the actual response to see if it contains a session ID
            _logger.LogInformation("Initialize response: {Response}", doc.RootElement.GetRawText());
            
            // Check if the server response contains a session ID we should use
            // For now, we'll continue using our generated session ID
            _logger.LogInformation("Session initialized with ID: {SessionId}", _sessionId);
            return doc;
        }

        public Task<JsonDocument> ToolsListAsync(string? sessionId, string? cursor = null, string? endpointOverride = null, CancellationToken ct = default)
        {
            if (!string.IsNullOrEmpty(sessionId))
            {
                SetSessionId(sessionId);
            }
            return SendAsync("tools/list", new { cursor }, endpointOverride, ct);
        }

        public Task<JsonDocument> ToolsCallAsync(string? sessionId, string name, JsonElement? arguments = null, string? endpointOverride = null, CancellationToken ct = default)
        {
            if (!string.IsNullOrEmpty(sessionId))
            {
                SetSessionId(sessionId);
            }
            return SendAsync("tools/call", new { name, arguments = arguments ?? default(JsonElement) }, endpointOverride, ct);
        }

        public Task<JsonDocument> ResourcesListAsync(string? sessionId, string? cursor = null, string? endpointOverride = null, CancellationToken ct = default)
        {
            if (!string.IsNullOrEmpty(sessionId))
            {
                SetSessionId(sessionId);
            }
            return SendAsync("resources/list", new { cursor }, endpointOverride, ct);
        }

        public Task<JsonDocument> ResourcesReadAsync(string? sessionId, string uri, string? endpointOverride = null, CancellationToken ct = default)
        {
            if (!string.IsNullOrEmpty(sessionId))
            {
                SetSessionId(sessionId);
            }
            return SendAsync("resources/read", new { uri }, endpointOverride, ct);
        }

        private async Task<JsonDocument> SendAsync(string method, object? @params, string? endpointOverride, CancellationToken ct)
        {
            var endpoint = endpointOverride ?? _options.Value.Http?.Endpoint;
            if (string.IsNullOrWhiteSpace(endpoint))
                throw new InvalidOperationException("MCP HTTP endpoint is not configured.");

            var client = _httpClientFactory.CreateClient("Mcp");

            var req = new JsonRpcRequest
            {
                Method = method,
                Params = @params is null
                    ? (JsonElement?)null
                    : JsonDocument.Parse(JsonSerializer.SerializeToUtf8Bytes(@params, JsonOptions)).RootElement
            };

            var json = JsonSerializer.Serialize(req, JsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var httpReq = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = content
            };
            // Per upstream requirements: Mcp-Session-Id must NOT be sent on initialize,
            // but should be present for subsequent requests in the same session.
            if (!string.Equals(method, "initialize", StringComparison.OrdinalIgnoreCase))
            {
                httpReq.Headers.TryAddWithoutValidation("Mcp-Session-Id", _sessionId);
            }

            using var httpResp = await client.SendAsync(httpReq, ct).ConfigureAwait(false);
            var respText = await httpResp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            
            // Log response headers to see if there's a session ID
            foreach (var header in httpResp.Headers)
            {
                _logger.LogInformation("Response header: {HeaderName}: {HeaderValue}", header.Key, string.Join(", ", header.Value));
            }
            
            // Check if there's a session ID in the response headers
            if (httpResp.Headers.TryGetValues("Mcp-Session-Id", out var sessionHeaders))
            {
                var serverSessionId = sessionHeaders.FirstOrDefault();
                if (!string.IsNullOrEmpty(serverSessionId))
                {
                    _logger.LogInformation("Server provided session ID: {SessionId}", serverSessionId);
                    _sessionId = serverSessionId;
                }
            }

            if (!httpResp.IsSuccessStatusCode)
            {
                _logger.LogWarning("MCP HTTP non-success status {Status}: {Body}", (int)httpResp.StatusCode, respText);
                throw new HttpRequestException($"MCP HTTP {httpResp.StatusCode}: {respText}");
            }

            JsonRpcResponse? rpc;
            try
            {
                // Some endpoints return Server-Sent Events (SSE) with lines like:
                //   event: message\n
                //   data: {json}\n\n
                // If so, extract the first data JSON payload.
                var mediaType = httpResp.Content.Headers.ContentType?.MediaType;
                string payload = respText;
                if ((mediaType?.Contains("text/event-stream", StringComparison.OrdinalIgnoreCase) ?? false) || respText.StartsWith("event:", StringComparison.OrdinalIgnoreCase))
                {
                    var extracted = ExtractFirstSseDataJson(respText);
                    if (!string.IsNullOrWhiteSpace(extracted))
                    {
                        payload = extracted!;
                    }
                }
                rpc = JsonSerializer.Deserialize<JsonRpcResponse>(payload, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize JSON-RPC response: {Body}", respText);
                throw;
            }

            if (rpc is null)
                throw new InvalidOperationException("Empty JSON-RPC response.");

            if (rpc.Error != null)
            {
                _logger.LogWarning("MCP JSON-RPC error {Code}: {Message}", rpc.Error.Code, rpc.Error.Message);
                throw new InvalidOperationException($"MCP error {rpc.Error.Code}: {rpc.Error.Message}");
            }

            if (rpc.Result is null)
                return JsonDocument.Parse("null");

            return JsonDocument.Parse(rpc.Result.Value.GetRawText());
        }

        private static string? ExtractFirstSseDataJson(string s)
        {
            using var reader = new StringReader(s);
            string? line;
            string? eventType = null;
            var sb = new StringBuilder();
            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith(":"))
                {
                    // comment line, ignore
                    continue;
                }
                if (line.StartsWith("event:", StringComparison.OrdinalIgnoreCase))
                {
                    eventType = line.Substring(6).Trim();
                }
                else if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    var data = line.Substring(5).TrimStart();
                    if (sb.Length > 0)
                        sb.Append('\n');
                    sb.Append(data);
                }
                else if (string.IsNullOrWhiteSpace(line))
                {
                    // end of event
                    if (!string.IsNullOrEmpty(eventType) && sb.Length > 0)
                    {
                        return sb.ToString();
                    }
                    // reset for next event
                    eventType = null;
                    sb.Clear();
                }
            }
            // If stream ended without trailing blank line, return accumulated
            return sb.Length > 0 ? sb.ToString() : null;
        }
    }
}
