using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpUi.Web.Services
{
    /// <summary>
    /// Wrapper client for stdio transport that manages sessions through a session manager.
    /// This allows maintaining stdio processes across multiple HTTP requests.
    /// </summary>
    public class McpStdioSessionClient : IMcpClient
    {
        private readonly IOptions<McpOptions> _options;
        private readonly ILogger<McpStdioClient> _logger;
        private readonly McpStdioSessionManager _sessionManager;
        private string _currentSessionId = Guid.Empty.ToString();
        private bool _initialized = false;
        private bool _disposed = false;

        public McpStdioSessionClient(
            IOptions<McpOptions> options, 
            ILogger<McpStdioClient> logger,
            McpStdioSessionManager sessionManager)
        {
            _options = options;
            _logger = logger;
            _sessionManager = sessionManager;
        }

        public string SessionId => _currentSessionId;

        public async Task<JsonDocument> InitializeAsync(string? endpointOverride = null, CancellationToken ct = default)
        {
            // For stdio transport, we generate a session ID and create a session
            _currentSessionId = Guid.NewGuid().ToString("N");
            var client = _sessionManager.GetOrCreateSession(_currentSessionId);
            
            // Initialize the underlying client
            var result = await client.InitializeAsync(endpointOverride, ct);
            _initialized = true;
            
            _logger.LogInformation("Stdio session client initialized with session ID: {SessionId}", _currentSessionId);
            return result;
        }

        public Task<JsonDocument> ToolsListAsync(string? sessionId, string? cursor = null, string? endpointOverride = null, CancellationToken ct = default)
        {
            if (!_initialized && string.IsNullOrEmpty(sessionId))
                throw new InvalidOperationException("Process not started. Call InitializeAsync first or provide a valid session ID.");
                
            var targetSessionId = !string.IsNullOrEmpty(sessionId) ? sessionId : _currentSessionId;
            var client = _sessionManager.GetOrCreateSession(targetSessionId);
            
            // If this is a new session, we need to initialize it
            if (!_initialized && !string.IsNullOrEmpty(sessionId))
            {
                _currentSessionId = sessionId!;
                _initialized = true;
            }
            
            return client.ToolsListAsync(sessionId, cursor, endpointOverride, ct);
        }

        public Task<JsonDocument> ToolsCallAsync(string? sessionId, string name, JsonElement? arguments = null, string? endpointOverride = null, CancellationToken ct = default)
        {
            if (!_initialized && string.IsNullOrEmpty(sessionId))
                throw new InvalidOperationException("Process not started. Call InitializeAsync first or provide a valid session ID.");
                
            var targetSessionId = !string.IsNullOrEmpty(sessionId) ? sessionId : _currentSessionId;
            var client = _sessionManager.GetOrCreateSession(targetSessionId);
            
            // If this is a new session, we need to initialize it
            if (!_initialized && !string.IsNullOrEmpty(sessionId))
            {
                _currentSessionId = sessionId!;
                _initialized = true;
            }
            
            return client.ToolsCallAsync(sessionId, name, arguments, endpointOverride, ct);
        }

        public Task<JsonDocument> ResourcesListAsync(string? sessionId, string? cursor = null, string? endpointOverride = null, CancellationToken ct = default)
        {
            if (!_initialized && string.IsNullOrEmpty(sessionId))
                throw new InvalidOperationException("Process not started. Call InitializeAsync first or provide a valid session ID.");
                
            var targetSessionId = !string.IsNullOrEmpty(sessionId) ? sessionId : _currentSessionId;
            var client = _sessionManager.GetOrCreateSession(targetSessionId);
            
            // If this is a new session, we need to initialize it
            if (!_initialized && !string.IsNullOrEmpty(sessionId))
            {
                _currentSessionId = sessionId!;
                _initialized = true;
            }
            
            return client.ResourcesListAsync(sessionId, cursor, endpointOverride, ct);
        }

        public Task<JsonDocument> ResourcesReadAsync(string? sessionId, string uri, string? endpointOverride = null, CancellationToken ct = default)
        {
            if (!_initialized && string.IsNullOrEmpty(sessionId))
                throw new InvalidOperationException("Process not started. Call InitializeAsync first or provide a valid session ID.");
                
            var targetSessionId = !string.IsNullOrEmpty(sessionId) ? sessionId : _currentSessionId;
            var client = _sessionManager.GetOrCreateSession(targetSessionId);
            
            // If this is a new session, we need to initialize it
            if (!_initialized && !string.IsNullOrEmpty(sessionId))
            {
                _currentSessionId = sessionId!;
                _initialized = true;
            }
            
            return client.ResourcesReadAsync(sessionId, uri, endpointOverride, ct);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // Don't dispose the actual client here as it might be used by other requests
                // The session manager will handle cleanup
                _disposed = true;
            }
        }
    }
}
