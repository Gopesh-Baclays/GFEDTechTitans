using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RECAP.Models;

namespace RECAP.Controllers;
public class DashboardController : Controller
{
    
    public IActionResult Dashboard()
    {
        return View();
    }
    
}
