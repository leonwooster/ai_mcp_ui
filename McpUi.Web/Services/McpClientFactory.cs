using System;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace McpUi.Web.Services
{
    public class McpClientFactory
    {
        private readonly IOptions<McpOptions> _options;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<McpJsonRpcHttpClient> _httpLogger;
        private readonly ILogger<McpStdioClient> _stdioLogger;
        private readonly McpStdioSessionManager _sessionManager;

        public McpClientFactory(
            IOptions<McpOptions> options,
            IHttpClientFactory httpClientFactory,
            ILogger<McpJsonRpcHttpClient> httpLogger,
            ILogger<McpStdioClient> stdioLogger,
            McpStdioSessionManager sessionManager)
        {
            _options = options;
            _httpClientFactory = httpClientFactory;
            _httpLogger = httpLogger;
            _stdioLogger = stdioLogger;
            _sessionManager = sessionManager;
        }

        public IMcpClient CreateClient()
        {
            return _options.Value.Transport?.ToLowerInvariant() switch
            {
                "httpstreaming" or "sse" => new McpJsonRpcHttpClient(_httpClientFactory, _options, _httpLogger),
                "stdio" => new McpStdioSessionClient(_options, _stdioLogger, _sessionManager),
                _ => throw new NotSupportedException($"Transport type '{_options.Value.Transport}' is not supported.")
            };
        }
    }
}
