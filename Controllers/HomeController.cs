using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RECAP.Models;

namespace RECAP.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View("SystemLogin");
    }

    public IActionResult SSOLogin()
    {
        var userId = Environment.UserName;

        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("SystemLogin");
        }
        else
        {
            return RedirectToAction("Dashboard");
        }
    }
[HttpPost]
    public IActionResult RunPythonScript()
    {
        var psi = new ProcessStartInfo();
        psi.FileName = "python";
        psi.Arguments = "phython/test.py";
        psi.WorkingDirectory = Directory.GetCurrentDirectory();
        psi.RedirectStandardOutput = true;
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;

        using (var process = Process.Start(psi))
        {
            process.WaitForExit();
        }

        return Json(new { success = true });
    }
    public IActionResult Dashboard()
    {
        return View();
    }

    public IActionResult SystemLogin()
    {
        return View();
    }
    public IActionResult Data()
    {
        return View();
    }
    public IActionResult Rules()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
