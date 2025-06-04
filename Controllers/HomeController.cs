using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RECAP.Models;
using OfficeOpenXml; // Add this at the top (requires EPPlus NuGet package)

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
        HttpContext.Session.SetString("UserId", userId);


        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("SystemLogin");
        }
        else
        {
            var sourceFilesPath = Path.Combine(Directory.GetCurrentDirectory(), "SourceFiles");
            var excelPath = Path.Combine(sourceFilesPath, "userdetails.xlsx");

            // ...existing code...
            string userName = null;
            OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            using (var package = new ExcelPackage(new FileInfo(excelPath)))
            {
                var worksheet = package.Workbook.Worksheets[0]; // Assumes first worksheet
                int rowCount = worksheet.Dimension.Rows;

                for (int row = 2; row <= rowCount; row++) // Assuming first row is header
                {
                    var excelUserId = worksheet.Cells[row, 1].Text.Trim(); // Assuming UserId is in column 1
                    if (string.Equals(excelUserId, userId, StringComparison.OrdinalIgnoreCase))
                    {
                        userName = worksheet.Cells[row, 2].Text.Trim(); // Assuming UserName is in column 2
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(userName))
            {
                // User not found in Excel
                return RedirectToAction("SystemLogin");
            }
            else
            {
                HttpContext.Session.SetString("UserName", userName);
                return RedirectToAction("Dashboard");
            }
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
        SetUserInformation();
        SetCardsData();
        SetEmpiricalViewBarChartData();
        SetPieChartData();
        SetRuleChartData();

        // You can replace the above sample data with actual data fetching logic as needed.
        return View();
    }

    /// <summary>
    /// Sets user information in the current view context by retrieving values from the session.
    /// </summary>
    /// <remarks>This method retrieves the user's ID and name from the session and assigns them to the  <see
    /// cref="ViewBag"/> for use in the current view. Ensure that the session contains valid  values for "UserId" and
    /// "UserName" before calling this method.</remarks>
    private void SetUserInformation()
    {
        var userId = HttpContext.Session.GetString("UserId");
        var userName = HttpContext.Session.GetString("UserName");
        ViewBag.UserId = userId;
        ViewBag.UserName = userName;
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
        ViewBag.BarLabels = new[] { "January", "February", "March", "April", "May", "June" };
        ViewBag.MatchedBalanceRuleBasedData = new[] { 4215, 5312, 6251, 7841, 9821, 14984 };
        ViewBag.MatchedBalanceAiData = new[] { 2000, 2500, 3000, 3500, 4000, 4500 };
        ViewBag.UnmatchedBalanceData = new[] { 2215, 2812, 3251, 4341, 5821, 9484 };
    }

    /// <summary>
    /// Sets the rule chart data for the view by assigning predefined values to the ViewBag.
    /// </summary>
    /// <remarks>This method populates the ViewBag with specific values for keys "Rl01", "Rl02", and "Rl03",
    /// which can be used in the view to display rule-related chart data.</remarks>
    private void SetRuleChartData()
    {
        ViewBag.Rl01 = 39;
        ViewBag.Rl02 = 21;
        ViewBag.Rl03 = 15;
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
        psi.RedirectStandardError = true;
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;

        using (var process = Process.Start(psi))
        {
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrEmpty(error))
            {
                // Log or return the error for debugging
                return Content("Error: " + error);
            }
        }

        return Json(new { success = true });
    }

    #endregion
}
