using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using McpUi.Web.Models;

namespace McpUi.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }
    
    public IActionResult UIResource(string resourceId, string? sessionId)
    {
        // This is a placeholder implementation
        // In a real implementation, this would fetch the actual resource
        var uiResource = new UIResource
        {
            MimeType = "text/html",
            Text = "<h1>UI Resource Placeholder</h1><p>This is a placeholder for resource: " + resourceId + "</p>"
        };
        
        ViewBag.UIResource = uiResource;
        ViewBag.SessionId = sessionId;
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
