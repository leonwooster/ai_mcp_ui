using System.Text.Json;

namespace McpUi.Web.Models
{
    public class ConnectRequest
    {
        public string? Endpoint { get; set; }
    }

    public class ListRequest
    {
        public string? Cursor { get; set; }
        public string? Endpoint { get; set; }
        public string? session_id { get; set; }
    }

    public class ToolCallRequest
    {
        public JsonElement? Arguments { get; set; }
        public string? Endpoint { get; set; }
        public string? session_id { get; set; }
    }

    public class ResourceReadRequest
    {
        public string? Uri { get; set; }
        public string? Endpoint { get; set; }
        public string? session_id { get; set; }
    }
}
