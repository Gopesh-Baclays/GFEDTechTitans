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

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public IActionResult Index()
    {
        return View("SystemLogin");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// Displays the login view for the system.
    /// </summary>
    /// <returns>An <see cref="IActionResult"/> that renders the login view.</returns>
    public IActionResult SystemLogin()
    {
        return View();
    }

    /// <summary>
    /// Displays the data view for the system.
    /// </summary>
    /// <returns></returns>
    public IActionResult Data()
    {
        return View();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public IActionResult Rules()
    {
        return View();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public IActionResult Privacy()
    {
        return View();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    #region Dashboard Controller Methods

    /// <summary>
    /// Dashboard action method that returns the dashboard view with sample data.
    /// </summary>
    /// <returns></returns>
    public IActionResult Dashboard()
    {
        SetCardsData();
        SetEmpiricalViewBarChartData();
        SetPieChartData();

        // You can replace the above sample data with actual data fetching logic as needed.
        return View();
    }

    /// <summary>
    /// Sets card-related data to the ViewBag for use in the view.
    /// </summary>
    /// <remarks>This method populates the ViewBag with predefined financial data, including total amount, 
    /// matched balances (rule-based and AI-based), and unmatched balance. These values are intended  for display
    /// purposes and may not reflect real-time or dynamic data.</remarks>
    private void SetCardsData()
    {
        ViewBag.TotalAmount = 4000000;
        ViewBag.MatchedBalanceRuleBased = 215000;
        ViewBag.UnmatchedBalance = 12345.76;
        ViewBag.MatchedBalanceAi = 4568.00022;
    }

    /// <summary>
    /// 
    /// </summary>
    private void SetEmpiricalViewBarChartData()
    {

    }

    /// <summary>
    /// Sets the data for a pie chart visualization.
    /// </summary>
    /// <remarks>This method prepares percentage values representing different categories of data and assigns
    /// them to the <see cref="ViewBag.PieChartData"/> property for use in a pie chart. The data includes total amount
    /// percentage, matched balance (rule-based and AI-based), and unmatched balance percentage.</remarks>
    private void SetPieChartData()
    {
        // Sample data for the bar chart
        int matchedBalanceRuleBasedPercentage = 30;
        int unmatchedBalancePercentage = 25;
        int matchedBalanceAiPercentage = 5;

        ViewBag.PieChartData = new[] {
            matchedBalanceRuleBasedPercentage,
            unmatchedBalancePercentage,
            matchedBalanceAiPercentage};
    }

    #endregion

    #region Python 

    [HttpPost]
    public IActionResult RunPythonScript()
    {
        var psi = new ProcessStartInfo();
        psi.FileName = "python";
        psi.Arguments = "Python/Rule1.py";
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

    #endregion
}
