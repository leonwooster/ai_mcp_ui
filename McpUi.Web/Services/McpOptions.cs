using System;

namespace McpUi.Web.Services
{
    public class McpOptions
    {
        public string Transport { get; set; } = "HttpStreaming"; // HttpStreaming | Sse | Stdio (future)
        public McpHttpOptions Http { get; set; } = new();
        public McpSseOptions Sse { get; set; } = new();
        public McpStdioOptions Stdio { get; set; } = new();
    }

    public class McpHttpOptions
    {
        public string? Endpoint { get; set; }
    }

    public class McpSseOptions
    {
        public string? Endpoint { get; set; }
    }

    public class McpStdioOptions
    {
        public string? ExePath { get; set; }
        public string? Args { get; set; }
    }
}
