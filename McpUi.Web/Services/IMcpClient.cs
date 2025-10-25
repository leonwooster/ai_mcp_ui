using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace McpUi.Web.Services
{
    public interface IMcpClient : IDisposable
    {
        Task<JsonDocument> InitializeAsync(string? endpointOverride = null, CancellationToken ct = default);
        Task<JsonDocument> ToolsListAsync(string? sessionId, string? cursor = null, string? endpointOverride = null, CancellationToken ct = default);
        Task<JsonDocument> ToolsCallAsync(string? sessionId, string name, JsonElement? arguments = null, string? endpointOverride = null, CancellationToken ct = default);
        Task<JsonDocument> ResourcesListAsync(string? sessionId, string? cursor = null, string? endpointOverride = null, CancellationToken ct = default);
        Task<JsonDocument> ResourcesReadAsync(string? sessionId, string uri, string? endpointOverride = null, CancellationToken ct = default);
    }
}
