using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using McpUi.Web.Models;
using McpUi.Web.Services;

namespace McpUi.Web.Controllers
{
    [ApiController]
    [Route("mcp")] 
    public class McpController : ControllerBase
    {
        private readonly IMcpClient _mcp;
        private readonly ILogger<McpController> _logger;

        public McpController(IMcpClient mcp, ILogger<McpController> logger)
        {
            _mcp = mcp;
            _logger = logger;
        }

        [HttpGet("connect")]
        public async Task<IActionResult> ConnectGet([FromQuery] string? endpoint, CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Connect (GET) endpoint called");
                using var doc = await _mcp.InitializeAsync(endpoint, ct);
                _logger.LogInformation("Connect (GET) successful");
                
                // Get the session ID from the client
                var sessionId = "";
                if (_mcp is McpJsonRpcHttpClient httpClient)
                {
                    sessionId = httpClient.SessionId;
                }
                else if (_mcp is McpStdioSessionClient sessionClient)
                {
                    sessionId = sessionClient.SessionId;
                }
                else
                {
                    // Fallback for other client types
                    sessionId = (_mcp as dynamic)?.SessionId ?? "";
                }
                
                // Create a response that includes both the original result and the session ID
                var responseObject = new {
                    result = doc.RootElement.Clone(),
                    sessionId = sessionId
                };
                
                var responseJson = JsonSerializer.Serialize(responseObject, new JsonSerializerOptions { WriteIndented = true });
                return Content(responseJson, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connect (GET) failed");
                return Problem(title: "Connect failed", detail: ex.Message);
            }
        }

        [HttpPost("connect")]
        public async Task<IActionResult> Connect([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ConnectRequest? request, CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Connect endpoint called");
                using var doc = await _mcp.InitializeAsync(request?.Endpoint, ct);
                _logger.LogInformation("Connect successful");
                
                // Get the session ID from the client
                var sessionId = "";
                if (_mcp is McpJsonRpcHttpClient httpClient)
                {
                    sessionId = httpClient.SessionId;
                }
                else if (_mcp is McpStdioSessionClient sessionClient)
                {
                    sessionId = sessionClient.SessionId;
                }
                else
                {
                    // Fallback for other client types
                    sessionId = (_mcp as dynamic)?.SessionId ?? "";
                }
                
                // Create a response that includes both the original result and the session ID
                var responseObject = new {
                    result = doc.RootElement.Clone(),
                    sessionId = sessionId
                };
                
                var responseJson = JsonSerializer.Serialize(responseObject, new JsonSerializerOptions { WriteIndented = true });
                return Content(responseJson, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connect failed");
                return Problem(title: "Connect failed", detail: ex.Message);
            }
        }

        [HttpGet("tools")]
        public async Task<IActionResult> ToolsList([FromQuery] string? cursor, [FromQuery] string? endpoint, [FromQuery] string? session_id, CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Tools list endpoint called");
                using var doc = await _mcp.ToolsListAsync(session_id, cursor, endpoint, ct);
                var json = doc.RootElement.GetRawText();
                _logger.LogInformation("Tools list successful");
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "tools/list failed");
                return Problem(title: "tools/list failed", detail: ex.Message);
            }
        }

        [HttpPost("tools/{name}/call")]
        public async Task<IActionResult> ToolsCall([FromRoute] string name, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ToolCallRequest? request, CancellationToken ct)
        {
            try
            {
                using var doc = await _mcp.ToolsCallAsync(request?.session_id, name, request?.Arguments, request?.Endpoint, ct);
                var json = doc.RootElement.GetRawText();
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "tools/call failed");
                return Problem(title: "tools/call failed", detail: ex.Message);
            }
        }

        [HttpGet("resources")]
        public async Task<IActionResult> ResourcesList([FromQuery] string? cursor, [FromQuery] string? endpoint, [FromQuery] string? session_id, CancellationToken ct)
        {
            try
            {
                using var doc = await _mcp.ResourcesListAsync(session_id, cursor, endpoint, ct);
                var json = doc.RootElement.GetRawText();
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "resources/list failed");
                return Problem(title: "resources/list failed", detail: ex.Message);
            }
        }

        [HttpGet("resources/read")]
        public async Task<IActionResult> ResourcesRead([FromQuery] string uri, [FromQuery] string? endpoint, [FromQuery] string? session_id, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(uri)) return BadRequest("uri is required");
            try
            {
                using var doc = await _mcp.ResourcesReadAsync(session_id, uri, endpoint, ct);
                var json = doc.RootElement.GetRawText();
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "resources/read failed");
                return Problem(title: "resources/read failed", detail: ex.Message);
            }
        }
    }
}
