using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Models;

namespace TodoApp.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        // Redirect to Todo controller if user is authenticated
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Todo");
        }
        return View();
    }

    [Authorize] // Only accessible to authenticated users
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [Route("/error/{statusCode}")]
    public IActionResult HandleError(int statusCode)
    {
        if (statusCode == 404)
        {
            return View("NotFound");
        }
        
        return View("Error", new ErrorViewModel { 
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier 
        });
    }
}
