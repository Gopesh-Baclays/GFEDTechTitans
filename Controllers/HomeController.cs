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

    public IActionResult DataFetch()
    {
        var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "OutputFiles", "Final_Output.xlsx");
        var matched = new List<Dictionary<string, object>>();
        var unmatched = new List<Dictionary<string, object>>();

        OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
        using (var package = new ExcelPackage(new FileInfo(outputPath)))
        {
            var ws = package.Workbook.Worksheets[0];
            int rowCount = ws.Dimension.Rows;

        for (int row = 2; row <= rowCount; row++)
        {
            var dict = new Dictionary<string, object>
            {
                ["Business_date"] = ws.Cells[row, 1].Text,
                ["ISIN"] = ws.Cells[row, 2].Text,
                ["Reporting_id_src1"] = ws.Cells[row, 3].Text,
                ["GL_src1"] = ws.Cells[row, 4].Text,
                ["Reporting_id_src2"] = ws.Cells[row, 5].Text,
                ["GL_src2"] = ws.Cells[row, 6].Text,
                ["balancegbp_src1"] = ws.Cells[row, 7].Text,
                ["balancegbp_src2"] = ws.Cells[row, 8].Text,
                ["Balance_Difference"] = ws.Cells[row, 9].Text,
                ["Comments"] = ws.Cells[row, 10].Text,
                ["Rule_Applied"] = ws.Cells[row, 11].Text
            };

                if (dict["Comments"].ToString() == "Match")
                    matched.Add(dict);
                else
                    unmatched.Add(dict);
            }
        }

        ViewBag.Matched = matched;
        ViewBag.Unmatched = unmatched;
        return View("Data");
    }


    /// <summary>
    /// Displays the data view for the system.
    /// </summary>
    /// <returns></returns>
    public IActionResult Data()
    {
        SetUserInformation();
        return View();
    }

    #region Rules

    private readonly string _rulesFile = Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), "SourceFiles"), "Rules.xlsx");

    /// <summary>
    /// Displays a view containing a list of rules retrieved from an Excel file.
    /// </summary>
    /// <remarks>The rules are read from an external Excel file and passed to the view for rendering.  Ensure
    /// the Excel file is properly formatted and accessible to avoid errors.</remarks>
    /// <returns>An <see cref="IActionResult"/> that renders the view with the list of rules.</returns>
    public IActionResult Rules()
    {
        var rules = ReadRulesFromExcel();
        return View(rules);
    }

    /// <summary>
    /// Adds a new rule or edits an existing rule in the collection of rules stored in the Excel file.
    /// </summary>
    /// <remarks>If <paramref name="rowIndex"/> is provided, the rule at the specified index is replaced with
    /// the given <paramref name="rule"/>. If <paramref name="rowIndex"/> is null, the rule is appended to the end of
    /// the collection. The updated collection is saved to the Excel file.</remarks>
    /// <param name="rule">The rule to add or edit. Cannot be null.</param>
    /// <param name="rowIndex">The zero-based index of the rule to edit. If null, a new rule is added to the collection.</param>
    /// <returns>A redirect to the "Rules" action after the operation is completed.</returns>
    [HttpPost]
    public IActionResult AddOrEditRule(RuleModel rule, int? rowIndex)
    {
        var rules = ReadRulesFromExcel();
        if (rowIndex.HasValue && rowIndex.Value >= 0 && rowIndex.Value < rules.Count)
        {
            // Edit
            rules[rowIndex.Value] = rule;
        }
        else
        {
            // Add
            rules.Add(rule);
        }
        WriteRulesToExcel(rules);
        return RedirectToAction("Rules");
    }

    /// <summary>
    /// Deletes a rule at the specified index from the list of rules and updates the data source.
    /// </summary>
    /// <remarks>If the specified <paramref name="rowIndex"/> is out of range, no rule is deleted, and the
    /// data source remains unchanged.</remarks>
    /// <param name="rowIndex">The zero-based index of the rule to delete. Must be within the valid range of the rules list.</param>
    /// <returns>An <see cref="IActionResult"/> that redirects to the "Rules" view after the operation is completed.</returns>
    [HttpPost]
    public IActionResult DeleteRule(int rowIndex)
    {
        var rules = ReadRulesFromExcel();
        if (rowIndex >= 0 && rowIndex < rules.Count)
        {
            rules.RemoveAt(rowIndex);
            WriteRulesToExcel(rules);
        }
        return RedirectToAction("Rules");
    }

    /// <summary>
    /// Reads rules from an Excel file and returns a list of <see cref="RuleModel"/> objects.
    /// </summary>
    /// <remarks>This method reads data from the first worksheet of the specified Excel file. Each row in the
    /// worksheet represents a rule, with columns corresponding to the properties of <see cref="RuleModel"/>. If the
    /// file does not exist or the worksheet is empty, an empty list is returned.</remarks>
    /// <returns>A list of <see cref="RuleModel"/> objects populated with data from the Excel file. If the file does not exist or
    /// the worksheet is empty, the returned list will be empty.</returns>
    private List<RuleModel> ReadRulesFromExcel()
    {
        var rules = new List<RuleModel>();

        if (!System.IO.File.Exists(_rulesFile))
            return rules;

        using (var package = new ExcelPackage(new FileInfo(_rulesFile)))
        {
            var ws = package.Workbook.Worksheets.FirstOrDefault();
            if (ws == null) return rules;
            int row = 2;
            while (ws.Cells[row, 1].Value != null)
            {
                rules.Add(new RuleModel
                {
                    RuleName = ws.Cells[row, 1].Text,
                    Sheet1Attribute = ws.Cells[row, 2].Text,
                    Sheet2Attribute = ws.Cells[row, 3].Text,
                    MatchType = ws.Cells[row, 4].Text
                });
                row++;
            }
        }
        return rules;
    }

    /// <summary>
    /// Writes a collection of rules to an Excel file.
    /// </summary>
    /// <remarks>Each rule is written to a new row in the Excel file, with columns for the rule name,
    /// attributes from two sheets, and the match type. The Excel file is saved to the location specified by the
    /// internal <c>_rulesFile</c> field.</remarks>
    /// <param name="rules">A list of <see cref="RuleModel"/> objects representing the rules to be written to the Excel file.</param>
    private void WriteRulesToExcel(List<RuleModel> rules)
    {
        using (var package = new ExcelPackage())
        {
            var ws = package.Workbook.Worksheets.Add("Rules");
            ws.Cells[1, 1].Value = "RuleName";
            ws.Cells[1, 2].Value = "Sheet1Attribute";
            ws.Cells[1, 3].Value = "Sheet2Attribute";
            ws.Cells[1, 4].Value = "MatchType";
            for (int i = 0; i < rules.Count; i++)
            {
                ws.Cells[i + 2, 1].Value = rules[i].RuleName;
                ws.Cells[i + 2, 2].Value = rules[i].Sheet1Attribute;
                ws.Cells[i + 2, 3].Value = rules[i].Sheet2Attribute;
                ws.Cells[i + 2, 4].Value = rules[i].MatchType;
            }
            package.SaveAs(new FileInfo(_rulesFile));
        }
    }

    #endregion

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
        GetDatafromExcel();
        // Set user information and various data for the dashboard view.
        SetUserInformation();
        SetCardsData();
        SetEmpiricalViewBarChartData();
        SetPieChartData();
        SetRuleChartData();

        // You can replace the above sample data with actual data fetching logic as needed.
        return View();
    }

    private List<DashboardRow> _dashboardRows;

    private void GetDatafromExcel()
    {
        var sourceFilesPath = Path.Combine(Directory.GetCurrentDirectory(), "SourceFiles");
        var excelPath = Path.Combine(sourceFilesPath, "dashboarddata.xlsx");

        OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
        _dashboardRows = new List<DashboardRow>();

        using (var package = new ExcelPackage(new FileInfo(excelPath)))
        {
            var worksheet = package.Workbook.Worksheets[0];
            int rowCount = worksheet.Dimension.Rows;

            for (int row = 2; row <= rowCount; row++) // Assuming first row is header
            {
                var data = new DashboardRow
                {
                    Month = worksheet.Cells[row, 1].Text,
                    Total = decimal.Parse(worksheet.Cells[row, 2].Text),
                    MatchedRule = decimal.Parse(worksheet.Cells[row, 3].Text),
                    MatchedAI = decimal.Parse(worksheet.Cells[row, 4].Text),
                    Unmatched = decimal.Parse(worksheet.Cells[row, 5].Text)
                };
                _dashboardRows.Add(data);
            }
        }
    }

    // Helper class for dashboard data
    private class DashboardRow
    {
        public string Month { get; set; }

        public decimal Total { get; set; }

        public decimal MatchedRule { get; set; }

        public decimal MatchedAI { get; set; }

        public decimal Unmatched { get; set; }
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
        if (_dashboardRows == null || !_dashboardRows.Any()) return;
        // Use the latest month (last row)
        var latest = _dashboardRows.Last();
        ViewBag.TotalAmount = latest.Total;
        ViewBag.MatchedBalanceRuleBased = latest.MatchedRule;
        ViewBag.UnmatchedBalance = latest.Unmatched;
        ViewBag.MatchedBalanceAi = latest.MatchedAI;
    }

    /// <summary>
    /// 
    /// </summary>
    private void SetEmpiricalViewBarChartData()
    {
        if (_dashboardRows == null || !_dashboardRows.Any()) return;
        ViewBag.BarLabels = _dashboardRows.Select(r => r.Month).ToArray();
        ViewBag.MatchedBalanceRuleBasedData = _dashboardRows.Select(r => r.MatchedRule).ToArray();
        ViewBag.MatchedBalanceAiData = _dashboardRows.Select(r => r.MatchedAI).ToArray();
        ViewBag.UnmatchedBalanceData = _dashboardRows.Select(r => r.Unmatched).ToArray();

        ViewBag.YAxisMax = 380000;
    }

        /// <summary>
    /// Sets the rule chart data for the view by assigning predefined values to the ViewBag.
    /// </summary>
    /// <remarks>This method populates the ViewBag with specific values for keys "Rl01", "Rl02", and "Rl03",
    /// which can be used in the view to display rule-related chart data.</remarks>
    private void SetRuleChartData()
    {
        var sourceFilesPath = Path.Combine(Directory.GetCurrentDirectory(), "SourceFiles");
        var excelPath = Path.Combine(sourceFilesPath, "RuleData.xlsx");

        OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

        int rl01Pct = 0, rl02Pct = 0, rl03Pct = 0;

        if (System.IO.File.Exists(excelPath))
        {
            using (var package = new ExcelPackage(new FileInfo(excelPath)))
            {
                var worksheet = package.Workbook.Worksheets[0];
                int rowCount = worksheet.Dimension.Rows;

                for (int row = 2; row <= rowCount; row++) // Assuming first row is header
                {
                    var ruleName = worksheet.Cells[row, 1].Text.Trim();
                    int total = int.TryParse(worksheet.Cells[row, 2].Text, out var t) ? t : 0;
                    int match = int.TryParse(worksheet.Cells[row, 3].Text, out var m) ? m : 0;

                    int percent = (total > 0) ? (int)Math.Round((double)match / total * 100) : 0;

                if (ruleName.Equals("Rule1", StringComparison.OrdinalIgnoreCase))
                    rl01Pct = percent;
                else if (ruleName.Equals("Rule2", StringComparison.OrdinalIgnoreCase))
                    rl02Pct = percent;
                else if (ruleName.Equals("Rule3", StringComparison.OrdinalIgnoreCase))
                    rl03Pct = percent;
            }
        }
    }

        ViewBag.Rl01 = rl01Pct;
        ViewBag.Rl02 = rl02Pct;
        ViewBag.Rl03 = rl03Pct;
    }

    /// <summary>
    /// Sets the data for a pie chart visualization.
    /// </summary>
    /// <remarks>This method prepares percentage values representing different categories of data and assigns
    /// them to the <see cref="ViewBag.PieChartData"/> property for use in a pie chart. The data includes total amount
    /// percentage, matched balance (rule-based and AI-based), and unmatched balance percentage.</remarks>
    private void SetPieChartData()
    {
        if (_dashboardRows == null || !_dashboardRows.Any()) return;
        var latest = _dashboardRows.Last();
        decimal total = latest.Total;
        decimal matchedRule = latest.MatchedRule;
        decimal matchedAI = latest.MatchedAI;
        decimal unmatched = latest.Unmatched;

        // Calculate percentages
        int matchedRulePct = (int)Math.Round((decimal)matchedRule / total * 100);
        int matchedAIPct = (int)Math.Round((decimal)matchedAI / total * 100);
        int unmatchedPct = (int)Math.Round((decimal)unmatched / total * 100);

        ViewBag.PieChartData = new[] { matchedRulePct, unmatchedPct, matchedAIPct };
    }
    #endregion

    #region Python 

    [HttpPost]
    public IActionResult RunPythonScript()
    {
        var psi = new ProcessStartInfo();
        psi.FileName = "python";
        psi.Arguments = "Python/Rule5.py";
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
