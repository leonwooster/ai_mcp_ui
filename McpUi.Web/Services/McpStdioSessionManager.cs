using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpUi.Web.Services
{
    public class McpStdioSessionManager
    {
        private readonly IOptions<McpOptions> _options;
        private readonly ILogger<McpStdioClient> _logger;
        private readonly ConcurrentDictionary<string, McpStdioClient> _activeSessions = new();
        
        public McpStdioSessionManager(IOptions<McpOptions> options, ILogger<McpStdioClient> logger)
        {
            _options = options;
            _logger = logger;
        }
        
        public McpStdioClient GetOrCreateSession(string sessionId)
        {
            return _activeSessions.GetOrAdd(sessionId, id =>
            {
                var client = new McpStdioClient(_options, _logger);
                return client;
            });
        }
        
        public bool TryGetSession(string sessionId, out McpStdioClient? client)
        {
            return _activeSessions.TryGetValue(sessionId, out client);
        }
        
        public void RemoveSession(string sessionId)
        {
            if (_activeSessions.TryRemove(sessionId, out var client))
            {
                client?.Dispose();
            }
        }
        
        public void DisposeAllSessions()
        {
            foreach (var kvp in _activeSessions)
            {
                kvp.Value?.Dispose();
            }
            _activeSessions.Clear();
        }
    }
}
