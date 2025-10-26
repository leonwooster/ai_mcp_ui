using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        private readonly PayloadValidationOptions _payloadValidationOptions;

        public McpController(IMcpClient mcp, ILogger<McpController> logger, IOptions<PayloadValidationOptions> payloadValidationOptions)
        {
            _mcp = mcp;
            _logger = logger;
            _payloadValidationOptions = payloadValidationOptions.Value;
        }

        private IActionResult HandleError(Exception ex, string operation, int errorCode = 500)
        {
            _logger.LogError(ex, "{Operation} failed: {Message}", operation, ex.Message);
            
            var errorResponse = new McpUi.Web.Models.ErrorResponse
            {
                Code = errorCode,
                Message = ex.Message,
                Details = new { Operation = operation }
            };
            
            return StatusCode(errorCode, errorResponse);
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
                return HandleError(ex, "Connect (GET)");
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
                return HandleError(ex, "Connect");
            }
        }

        [HttpGet("tools")]
        public async Task<IActionResult> ToolsList([FromQuery] string? cursor, [FromQuery] string? endpoint, [FromQuery] string? session_id, CancellationToken ct)
        {
            // Validate cursor parameter
            if (!string.IsNullOrEmpty(cursor) && cursor.Length > 1000)
            {
                return BadRequest(new ErrorResponse 
                { 
                    Code = 400, 
                    Message = "Invalid cursor parameter: cursor is too long", 
                    Details = new { Parameter = "cursor" } 
                });
            }
            
            try
            {
                _logger.LogInformation("Tools list endpoint called");
                using var doc = await _mcp.ToolsListAsync(session_id, cursor, endpoint, ct);
                
                // Format response according to acceptance criteria: { tools: [...], nextCursor?: string }
                var result = doc.RootElement; // IMcpClient already unwraps JSON-RPC result
                var tools = result.GetProperty("tools");
                
                // Check if there's a nextCursor in the response
                string? nextCursor = null;
                if (result.TryGetProperty("nextCursor", out var nextCursorElement) && nextCursorElement.ValueKind != JsonValueKind.Null)
                {
                    nextCursor = nextCursorElement.GetString();
                }
                
                // Create a properly formatted response
                var formattedResponse = new Dictionary<string, object?>
                {
                    ["tools"] = JsonSerializer.Deserialize<object>(tools.GetRawText()),
                    ["nextCursor"] = nextCursor
                };
                
                var responseJson = JsonSerializer.Serialize(formattedResponse, new JsonSerializerOptions { WriteIndented = true });
                _logger.LogInformation("Tools list successful");
                return Content(responseJson, "application/json");
            }
            catch (Exception ex)
            {
                return HandleError(ex, "tools/list");
            }
        }

        [HttpPost("tools/{name}/call")]
        public async Task<IActionResult> ToolsCall([FromRoute] string name, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ToolCallRequest? request, CancellationToken ct)
        {
            // Validate tool name
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new ErrorResponse 
                { 
                    Code = 400, 
                    Message = "Tool name is required", 
                    Details = new { Parameter = "name" } 
                });
            }
            
            try
            {
                _logger.LogInformation("Tools call endpoint called for tool: {ToolName}", name);
                using var doc = await _mcp.ToolsCallAsync(request?.session_id, name, request?.Arguments, request?.Endpoint, ct);
                
                // Format response according to spec: either text result or UIResource
                var result = doc.RootElement; // already the result object
                
                // Check if there's content in the response
                if (result.TryGetProperty("content", out var contentElement))
                {
                    // Return the content as the response
                    var formattedResponse = new Dictionary<string, object?>
                    {
                        ["content"] = JsonSerializer.Deserialize<object>(contentElement.GetRawText())
                    };
                    
                    var responseJson = JsonSerializer.Serialize(formattedResponse, new JsonSerializerOptions { WriteIndented = true });
                    return Content(responseJson, "application/json");
                }
                
                // If no content, return the raw result
                var responseJsonRaw = JsonSerializer.Serialize(new Dictionary<string, object?>
                {
                    ["result"] = JsonSerializer.Deserialize<object>(result.GetRawText())
                }, new JsonSerializerOptions { WriteIndented = true });
                
                return Content(responseJsonRaw, "application/json");
            }
            catch (Exception ex)
            {
                return HandleError(ex, "tools/call", 400); // Use 400 for tool call errors
            }
        }

        [HttpGet("resources")]
        public async Task<IActionResult> ResourcesList([FromQuery] string? cursor, [FromQuery] string? endpoint, [FromQuery] string? session_id, CancellationToken ct)
        {
            // Validate cursor parameter
            if (!string.IsNullOrEmpty(cursor) && cursor.Length > 1000)
            {
                return BadRequest(new ErrorResponse 
                { 
                    Code = 400, 
                    Message = "Invalid cursor parameter: cursor is too long", 
                    Details = new { Parameter = "cursor" } 
                });
            }
            
            try
            {
                _logger.LogInformation("Resources list endpoint called");
                using var doc = await _mcp.ResourcesListAsync(session_id, cursor, endpoint, ct);
                
                // Format response according to acceptance criteria: { resources: [...], nextCursor?: string }
                var result = doc.RootElement; // already the result object
                var resources = result.GetProperty("resources");
                
                // Check if there's a nextCursor in the response
                string? nextCursor = null;
                if (result.TryGetProperty("nextCursor", out var nextCursorElement) && nextCursorElement.ValueKind != JsonValueKind.Null)
                {
                    nextCursor = nextCursorElement.GetString();
                }
                
                // Create a properly formatted response
                var formattedResponse = new Dictionary<string, object?>
                {
                    ["resources"] = JsonSerializer.Deserialize<object>(resources.GetRawText()),
                    ["nextCursor"] = nextCursor
                };
                
                var responseJson = JsonSerializer.Serialize(formattedResponse, new JsonSerializerOptions { WriteIndented = true });
                _logger.LogInformation("Resources list successful");
                return Content(responseJson, "application/json");
            }
            catch (Exception ex)
            {
                return HandleError(ex, "resources/list");
            }
        }

        [HttpGet("resources/read")]
        public async Task<IActionResult> ResourcesRead([FromQuery] string uri, [FromQuery] string? endpoint, [FromQuery] string? session_id, CancellationToken ct)
        {
            // Validate URI parameter
            if (string.IsNullOrWhiteSpace(uri))
            {
                return BadRequest(new ErrorResponse 
                { 
                    Code = 400, 
                    Message = "URI is required", 
                    Details = new { Parameter = "uri" } 
                });
            }
            
            try
            {
                _logger.LogInformation("Resources read endpoint called for URI: {Uri}", uri);
                using var doc = await _mcp.ResourcesReadAsync(session_id, uri, endpoint, ct);
                
                // Format response with mime type and data according to spec
                var result = doc.RootElement; // already the result object
                
                // Extract content and mimeType if available
                object? content = null;
                string? contentText = null;
                string? mimeType = null;
                
                if (result.TryGetProperty("contents", out var contentsElement))
                {
                    contentText = contentsElement.GetRawText();
                    content = JsonSerializer.Deserialize<object>(contentText);
                }
                
                if (result.TryGetProperty("mimeType", out var mimeTypeElement))
                {
                    mimeType = mimeTypeElement.GetString();
                }
                
                // Validate payload size and MIME type
                var validationError = ValidatePayload(contentText, mimeType, _payloadValidationOptions);
                if (validationError != null)
                {
                    return StatusCode(validationError.Code, validationError);
                }
                
                var formattedResponse = new Dictionary<string, object?>
                {
                    ["content"] = content,
                    ["mimeType"] = mimeType
                };
                
                var responseJson = JsonSerializer.Serialize(formattedResponse, new JsonSerializerOptions { WriteIndented = true });
                return Content(responseJson, "application/json");
            }
            catch (Exception ex)
            {
                return HandleError(ex, "resources/read", 400); // Use 400 for resource read errors
            }
        }

        [HttpGet("ui-resource/{resourceId}")]
        public async Task<IActionResult> RenderUIResource([FromRoute] string resourceId, [FromQuery] string? session_id, CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("UI Resource render endpoint called for resource: {ResourceId}", resourceId);
                
                // For now, we'll use a placeholder URI - in a real implementation, this would map to actual resources
                var uri = $"mcp://resource/{resourceId}";
                using var doc = await _mcp.ResourcesReadAsync(session_id, uri, null, ct);
                
                // Extract resource data
                var result = doc.RootElement; // already the result object
                
                string? mimeType = null;
                string? content = null;
                
                if (result.TryGetProperty("mimeType", out var mimeTypeElement))
                {
                    mimeType = mimeTypeElement.GetString();
                }
                
                if (result.TryGetProperty("contents", out var contentsElement))
                {
                    content = contentsElement.GetRawText();
                }
                
                // Validate payload size and MIME type
                var validationError = ValidatePayload(content, mimeType, _payloadValidationOptions);
                if (validationError != null)
                {
                    return StatusCode(validationError.Code, validationError);
                }
                
                // Create UI resource model
                var uiResource = new UIResource
                {
                    MimeType = mimeType,
                    Text = content
                };
                
                // Return JSON representation of the UI resource
                var responseJson = JsonSerializer.Serialize(uiResource, new JsonSerializerOptions { WriteIndented = true });
                return Content(responseJson, "application/json");
            }
            catch (Exception ex)
            {
                return HandleError(ex, "ui-resource/render", 400);
            }
        }
        
        [HttpPost("ui-action")]
        public async Task<IActionResult> HandleUIAction([FromBody] UIActionRequest request, CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("UI Action endpoint called with type: {ActionType}", request?.Type);
                
                if (request == null)
                {
                    return BadRequest(new ErrorResponse 
                    { 
                        Code = 400, 
                        Message = "Request body is required", 
                        Details = new { Parameter = "request" } 
                    });
                }
                
                switch (request.Type)
                {
                    case "tool":
                        return await HandleToolAction(request, ct);
                    case "notify":
                        return HandleNotifyAction(request);
                    case "link":
                        return HandleLinkAction(request);
                    case "remote-dom":
                        return await HandleRemoteDomAction(request, ct);
                    default:
                        return BadRequest(new ErrorResponse 
                        { 
                            Code = 400, 
                            Message = $"Unknown action type: {request.Type}", 
                            Details = new { Parameter = "type" } 
                        });
                }
            }
            catch (Exception ex)
            {
                return HandleError(ex, "ui-action", 400);
            }
        }
        
        private async Task<IActionResult> HandleToolAction(UIActionRequest request, CancellationToken ct)
        {
            if (request.Payload?.TryGetProperty("toolName", out var toolNameElement) != true)
            {
                return BadRequest(new ErrorResponse 
                { 
                    Code = 400, 
                    Message = "Tool name is required for tool actions", 
                    Details = new { Parameter = "toolName" } 
                });
            }
            
            var toolName = toolNameElement.GetString();
            if (string.IsNullOrWhiteSpace(toolName))
            {
                return BadRequest(new ErrorResponse 
                { 
                    Code = 400, 
                    Message = "Tool name cannot be empty", 
                    Details = new { Parameter = "toolName" } 
                });
            }
            
            // Extract parameters if provided
            JsonElement? arguments = null;
            if (request.Payload.HasValue && request.Payload.Value.TryGetProperty("params", out var paramsElement))
            {
                arguments = paramsElement;
            }
            
            try
            {
                using var doc = await _mcp.ToolsCallAsync(request.SessionId, toolName, arguments, null, ct);
                
                // Format response according to spec
                var result = doc.RootElement; // already the result object
                
                // Check if there's content in the response
                if (result.TryGetProperty("content", out var contentElement))
                {
                    // Return the content as the response
                    var formattedResponse = new Dictionary<string, object?>
                    {
                        ["content"] = JsonSerializer.Deserialize<object>(contentElement.GetRawText())
                    };
                    
                    var responseJson = JsonSerializer.Serialize(formattedResponse, new JsonSerializerOptions { WriteIndented = true });
                    return Content(responseJson, "application/json");
                }
                
                // If no content, return the raw result
                var responseJsonRaw = JsonSerializer.Serialize(new Dictionary<string, object?>
                {
                    ["result"] = JsonSerializer.Deserialize<object>(result.GetRawText())
                }, new JsonSerializerOptions { WriteIndented = true });
                
                return Content(responseJsonRaw, "application/json");
            }
            catch (Exception ex)
            {
                return HandleError(ex, $"tool/{toolName}", 400);
            }
        }
        
        private IActionResult HandleNotifyAction(UIActionRequest request)
        {
            if (!request.Payload.HasValue || !request.Payload.Value.TryGetProperty("message", out var messageElement))
            {
                return BadRequest(new ErrorResponse 
                { 
                    Code = 400, 
                    Message = "Message is required for notify actions", 
                    Details = new { Parameter = "message" } 
                });
            }
            
            var message = messageElement.GetString();
            if (string.IsNullOrWhiteSpace(message))
            {
                return BadRequest(new ErrorResponse 
                { 
                    Code = 400, 
                    Message = "Message cannot be empty", 
                    Details = new { Parameter = "message" } 
                });
            }
            
            // Log the notification
            _logger.LogInformation("UI Notification: {Message}", message);
            
            // Return success response
            var response = new Dictionary<string, object?>
            {
                ["status"] = "success",
                ["message"] = "Notification received"
            };
            
            var responseJson = JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
            return Content(responseJson, "application/json");
        }
        
        private IActionResult HandleLinkAction(UIActionRequest request)
        {
            if (!request.Payload.HasValue || !request.Payload.Value.TryGetProperty("url", out var urlElement))
            {
                return BadRequest(new ErrorResponse 
                { 
                    Code = 400, 
                    Message = "URL is required for link actions", 
                    Details = new { Parameter = "url" } 
                });
            }
            
            var url = urlElement.GetString();
            if (string.IsNullOrWhiteSpace(url))
            {
                return BadRequest(new ErrorResponse 
                { 
                    Code = 400, 
                    Message = "URL cannot be empty", 
                    Details = new { Parameter = "url" } 
                });
            }
            
            // Validate URL format
            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                return BadRequest(new ErrorResponse 
                { 
                    Code = 400, 
                    Message = "Invalid URL format", 
                    Details = new { Parameter = "url" } 
                });
            }
            
            // In a real implementation, you would check against an allowlist
            // For now, we'll allow all HTTP/HTTPS URLs
            var uri = new Uri(url);
            if (uri.Scheme != "http" && uri.Scheme != "https")
            {
                return BadRequest(new ErrorResponse 
                { 
                    Code = 400, 
                    Message = "Only HTTP and HTTPS URLs are allowed", 
                    Details = new { Parameter = "url" } 
                });
            }
            
            // Log the link action
            _logger.LogInformation("UI Link Action: {Url}", url);
            
            // Return success response
            var response = new Dictionary<string, object?>
            {
                ["status"] = "success",
                ["message"] = "Link action processed"
            };
            
            var responseJson = JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
            return Content(responseJson, "application/json");
        }
        
        private async Task<IActionResult> HandleRemoteDomAction(UIActionRequest request, CancellationToken ct)
        {
            if (!request.Payload.HasValue)
            {
                return BadRequest(new ErrorResponse 
                { 
                    Code = 400, 
                    Message = "Payload is required for Remote DOM actions", 
                    Details = new { Parameter = "payload" } 
                });
            }
            
            // Extract Remote DOM action parameters
            var payload = request.Payload.Value;
            
            if (!payload.TryGetProperty("resourceUri", out var resourceUriElement))
            {
                return BadRequest(new ErrorResponse 
                { 
                    Code = 400, 
                    Message = "Resource URI is required for Remote DOM actions", 
                    Details = new { Parameter = "resourceUri" } 
                });
            }
            
            if (!payload.TryGetProperty("componentId", out var componentIdElement))
            {
                return BadRequest(new ErrorResponse 
                { 
                    Code = 400, 
                    Message = "Component ID is required for Remote DOM actions", 
                    Details = new { Parameter = "componentId" } 
                });
            }
            
            if (!payload.TryGetProperty("componentType", out var componentTypeElement))
            {
                return BadRequest(new ErrorResponse 
                { 
                    Code = 400, 
                    Message = "Component type is required for Remote DOM actions", 
                    Details = new { Parameter = "componentType" } 
                });
            }
            
            if (!payload.TryGetProperty("actionType", out var actionTypeElement))
            {
                return BadRequest(new ErrorResponse 
                { 
                    Code = 400, 
                    Message = "Action type is required for Remote DOM actions", 
                    Details = new { Parameter = "actionType" } 
                });
            }
            
            var resourceUri = resourceUriElement.GetString();
            var componentId = componentIdElement.GetString();
            var componentType = componentTypeElement.GetString();
            var actionType = actionTypeElement.GetString();
            
            if (string.IsNullOrWhiteSpace(resourceUri))
            {
                return BadRequest(new ErrorResponse 
                { 
                    Code = 400, 
                    Message = "Resource URI cannot be empty", 
                    Details = new { Parameter = "resourceUri" } 
                });
            }
            
            if (string.IsNullOrWhiteSpace(componentId))
            {
                return BadRequest(new ErrorResponse 
                { 
                    Code = 400, 
                    Message = "Component ID cannot be empty", 
                    Details = new { Parameter = "componentId" } 
                });
            }
            
            if (string.IsNullOrWhiteSpace(componentType))
            {
                return BadRequest(new ErrorResponse 
                { 
                    Code = 400, 
                    Message = "Component type cannot be empty", 
                    Details = new { Parameter = "componentType" } 
                });
            }
            
            if (string.IsNullOrWhiteSpace(actionType))
            {
                return BadRequest(new ErrorResponse 
                { 
                    Code = 400, 
                    Message = "Action type cannot be empty", 
                    Details = new { Parameter = "actionType" } 
                });
            }
            
            // Log the Remote DOM action
            _logger.LogInformation("Remote DOM Action: {ActionType} on {ComponentType} ({ComponentId}) for resource {ResourceUri}", 
                actionType, componentType, componentId, resourceUri);
            
            try
            {
                // In a real implementation, this would process the Remote DOM action
                // For now, we'll simulate processing by reading the resource and returning a response
                using var doc = await _mcp.ResourcesReadAsync(request.SessionId, resourceUri, null, ct);
                
                // Create a response indicating the action was processed
                var response = new Dictionary<string, object?>
                {
                    ["status"] = "success",
                    ["message"] = $"Remote DOM action '{actionType}' processed successfully on {componentType} ({componentId})",
                    ["actionType"] = actionType,
                    ["componentId"] = componentId,
                    ["componentType"] = componentType,
                    ["resourceUri"] = resourceUri,
                    ["timestamp"] = DateTime.UtcNow
                };
                
                // Add UI updates if needed
                // In a real implementation, this would contain actual UI update instructions
                response["uiUpdates"] = new Dictionary<string, object?>
                {
                    ["componentId"] = componentId,
                    ["updates"] = new Dictionary<string, object?>
                    {
                        ["lastAction"] = actionType,
                        ["lastActionTime"] = DateTime.UtcNow
                    }
                };
                
                var responseJson = JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
                return Content(responseJson, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Remote DOM action failed: {ActionType} on {ComponentType} ({ComponentId})", 
                    actionType, componentType, componentId);
                
                return HandleError(ex, $"remote-dom/{actionType}", 500);
            }
        }
        
        /// <summary>
        /// Validates payload size and MIME type according to security policies
        /// </summary>
        /// <param name="content">The content to validate</param>
        /// <param name="mimeType">The MIME type to validate</param>
        /// <param name="options">Payload validation options</param>
        /// <returns>ErrorResponse if validation fails, null if validation passes</returns>
        private ErrorResponse? ValidatePayload(string? content, string? mimeType, PayloadValidationOptions options)
        {
            // Validate payload size
            if (!string.IsNullOrEmpty(content) && options.RejectOversizedPayloads)
            {
                var contentLength = System.Text.Encoding.UTF8.GetByteCount(content);
                if (contentLength > options.MaxPayloadSize)
                {
                    _logger.LogWarning("Payload size {ContentSize} bytes exceeds maximum allowed size {MaxSize} bytes for MIME type {MimeType}", 
                        contentLength, options.MaxPayloadSize, mimeType);
                    
                    return new ErrorResponse
                    {
                        Code = 413,
                        Message = $"Payload size {contentLength} bytes exceeds maximum allowed size {options.MaxPayloadSize} bytes",
                        Details = new { ContentType = mimeType, ContentSize = contentLength, MaxSize = options.MaxPayloadSize }
                    };
                }
            }
            
            // Validate MIME type
            if (!string.IsNullOrEmpty(mimeType) && options.RejectUnsupportedMimeTypes)
            {
                if (!options.AllowedMimeTypes.Contains(mimeType, StringComparer.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Unsupported MIME type: {MimeType}", mimeType);
                    
                    return new ErrorResponse
                    {
                        Code = 415,
                        Message = $"Unsupported MIME type: {mimeType}",
                        Details = new { ContentType = mimeType, AllowedTypes = options.AllowedMimeTypes }
                    };
                }
            }
            
            return null; // Validation passed
        }
    }
}
