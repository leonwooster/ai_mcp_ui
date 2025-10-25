using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using McpUi.Web.Models;

namespace McpUi.Web.Services
{
    public class McpStdioClient : IMcpClient
    {
        private readonly IOptions<McpOptions> _options;
        private readonly ILogger<McpStdioClient> _logger;
        private Process? _process;
        private StreamWriter? _stdin;
        private StreamReader? _stdout;
        private int _requestId = 1;
        private string _sessionId = Guid.Empty.ToString();
        private bool _disposed = false;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            Converters = { new McpUi.Web.Models.FlexibleIdConverter() }
        };

        public McpStdioClient(IOptions<McpOptions> options, ILogger<McpStdioClient> logger)
        {
            _options = options;
            _logger = logger;
        }

        public async Task<JsonDocument> InitializeAsync(string? endpointOverride = null, CancellationToken ct = default)
        {
            var exePath = _options.Value.Stdio?.ExePath;
            var args = _options.Value.Stdio?.Args ?? string.Empty;

            if (string.IsNullOrWhiteSpace(exePath))
                throw new InvalidOperationException("Stdio transport requires ExePath to be configured.");

            // Start the process
            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            _process = new Process { StartInfo = startInfo };
            
            // Capture stderr logs
            _process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    _logger.LogInformation("Stdio stderr: {Stderr}", e.Data);
                }
            };

            _process.Start();
            _process.BeginErrorReadLine();

            _stdin = _process.StandardInput;
            _stdout = _process.StandardOutput;
            // Don't access StandardError directly when using async operations
            // _stderr = _process.StandardError;

            // Send initialize request
            var payload = new
            {
                protocolVersion = "0.1.0",
                clientInfo = new { name = "aspnet-mvc-mcp-ui-poc", version = "0.1.0" },
                capabilities = new { }
            };

            var response = await SendAsync("initialize", payload, ct);
            
            // For stdio, we don't get a session ID from headers, so we'll generate one
            _sessionId = Guid.NewGuid().ToString("N");
            _logger.LogInformation("Stdio session initialized with ID: {SessionId}", _sessionId);
            
            return response;
        }

        public Task<JsonDocument> ToolsListAsync(string? sessionId, string? cursor = null, string? endpointOverride = null, CancellationToken ct = default)
        {
            if (!string.IsNullOrEmpty(sessionId))
            {
                _sessionId = sessionId;
            }
            return SendAsync("tools/list", new { cursor }, ct);
        }

        public Task<JsonDocument> ToolsCallAsync(string? sessionId, string name, JsonElement? arguments = null, string? endpointOverride = null, CancellationToken ct = default)
        {
            if (!string.IsNullOrEmpty(sessionId))
            {
                _sessionId = sessionId;
            }
            return SendAsync("tools/call", new { name, arguments = arguments ?? default(JsonElement) }, ct);
        }

        public Task<JsonDocument> ResourcesListAsync(string? sessionId, string? cursor = null, string? endpointOverride = null, CancellationToken ct = default)
        {
            if (!string.IsNullOrEmpty(sessionId))
            {
                _sessionId = sessionId;
            }
            return SendAsync("resources/list", new { cursor }, ct);
        }

        public Task<JsonDocument> ResourcesReadAsync(string? sessionId, string uri, string? endpointOverride = null, CancellationToken ct = default)
        {
            if (!string.IsNullOrEmpty(sessionId))
            {
                _sessionId = sessionId;
            }
            return SendAsync("resources/read", new { uri }, ct);
        }

        private async Task<JsonDocument> SendAsync(string method, object? @params, CancellationToken ct)
        {
            if (_process == null || _stdin == null || _stdout == null)
                throw new InvalidOperationException("Process not started. Call InitializeAsync first.");

            var requestId = _requestId++;
            var req = new
            {
                jsonrpc = "2.0",
                id = (object)requestId,
                method = method,
                @params = @params
            };

            var json = JsonSerializer.Serialize(req, JsonOptions);
            _logger.LogInformation("Sending stdio request: {Request}", json);

            await _stdin.WriteLineAsync(json);
            await _stdin.FlushAsync();

            // Read response - handle multiple lines until we get valid JSON
            string? responseLine;
            int attempts = 0;
            const int maxAttempts = 10; // Prevent infinite loop
            
            while (attempts < maxAttempts)
            {
                responseLine = await _stdout.ReadLineAsync();
                attempts++;
                
                if (string.IsNullOrEmpty(responseLine))
                {
                    if (attempts == 1)
                        throw new InvalidOperationException("Empty response from stdio process");
                    else
                        continue; // Try again
                }

                // Skip non-JSON lines (like startup messages)
                if (!responseLine.TrimStart().StartsWith("{"))
                {
                    _logger.LogInformation("Skipping non-JSON line: {Line}", responseLine);
                    continue;
                }

                _logger.LogInformation("Received stdio response: {Response}", responseLine);

                try
                {
                    var rpc = JsonSerializer.Deserialize<JsonRpcResponse>(responseLine, JsonOptions);
                    if (rpc == null)
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
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize JSON-RPC response (attempt {Attempt}): {Body}", attempts, responseLine);
                    if (attempts >= maxAttempts)
                        throw;
                    // Continue to next attempt
                }
            }
            
            throw new InvalidOperationException($"Failed to receive valid JSON response after {maxAttempts} attempts");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_process != null)
                    {
                        try
                        {
                            // Try graceful shutdown first
                            _process.CloseMainWindow();
                            if (!_process.WaitForExit(5000)) // Wait up to 5 seconds
                            {
                                _process.Kill(); // Force kill if not exited
                                _logger.LogWarning("Stdio process killed after timeout");
                            }
                            else
                            {
                                _logger.LogInformation("Stdio process exited gracefully");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error shutting down stdio process");
                        }
                        finally
                        {
                            _process.Dispose();
                            _process = null;
                        }
                    }
                }
                _disposed = true;
            }
        }
    }
}
