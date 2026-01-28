using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RECAP.Models;
using OfficeOpenXml;

namespace RECAP.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;

    public HomeController(ILogger<HomeController> logger, IConfiguration configuration, IWebHostEnvironment env)
    {
        _logger = logger;
        _configuration = configuration;
        _env = env;
    }

    /// <summary>
    /// Gets the full file path from configuration using the specified key.
    /// </summary>
    private string GetFilePath(string configKey)
    {
        var fileName = _configuration[$"FileStoragePaths:Files:{configKey}"];
        var sourceFolder = _configuration["FileStoragePaths:SourceFilesFolder"];
        return Path.Combine(_env.ContentRootPath, sourceFolder, fileName);
    }

    /// <summary>
    /// Gets the output file path from configuration.
    /// </summary>
    private string GetOutputFilePath(string configKey)
    {
        var fileName = _configuration[$"FileStoragePaths:Files:{configKey}"];
        var outputFolder = _configuration["FileStoragePaths:OutputFilesFolder"];
        return Path.Combine(_env.ContentRootPath, outputFolder, fileName);
    }

    public IActionResult Index()
    {
        return View("SystemLogin");
    }

    public IActionResult SSOLogin()
    {
        var userId = Environment.UserName;
        HttpContext.Session.SetString("UserId", userId);

        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("SystemLogin");
        }

        var excelPath = GetFilePath("UserDetailsFile");

        string userName = null;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using (var package = new ExcelPackage(new FileInfo(excelPath)))
        {
            var worksheet = package.Workbook.Worksheets[0];
            int rowCount = worksheet.Dimension.Rows;

            for (int row = 2; row <= rowCount; row++)
            {
                var excelUserId = worksheet.Cells[row, 1].Text.Trim();
                if (string.Equals(excelUserId, userId, StringComparison.OrdinalIgnoreCase))
                {
                    userName = worksheet.Cells[row, 2].Text.Trim();
                    break;
                }
            }
        }

        if (string.IsNullOrEmpty(userName))
        {
            return RedirectToAction("SystemLogin");
        }

        HttpContext.Session.SetString("UserName", userName);
        return RedirectToAction("Dashboard");
    }

    public IActionResult SystemLogin()
    {
        return View();
    }

    public IActionResult DataFetch()
    {
        var outputPath = GetOutputFilePath("FinalOutputFile");
        var matched = new List<Dictionary<string, object>>();
        var unmatched = new List<Dictionary<string, object>>();

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
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

    public IActionResult Data()
    {
        SetUserInformation();
        return View();
    }

    #region Rules

    public IActionResult Rules()
    {
        var rules = ReadRulesFromExcel();
        return View(rules);
    }

    [HttpPost]
    public IActionResult AddOrEditRule(RuleModel rule, int? rowIndex)
    {
        var rules = ReadRulesFromExcel();
        if (rowIndex.HasValue && rowIndex.Value >= 0 && rowIndex.Value < rules.Count)
        {
            rules[rowIndex.Value] = rule;
        }
        else
        {
            rules.Add(rule);
        }
        WriteRulesToExcel(rules);
        return RedirectToAction("Rules");
    }

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

    private List<RuleModel> ReadRulesFromExcel()
    {
        var rules = new List<RuleModel>();
        var rulesFile = GetFilePath("RulesFile");

        if (!System.IO.File.Exists(rulesFile))
            return rules;

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using (var package = new ExcelPackage(new FileInfo(rulesFile)))
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

    private void WriteRulesToExcel(List<RuleModel> rules)
    {
        var rulesFile = GetFilePath("RulesFile");
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
            package.SaveAs(new FileInfo(rulesFile));
        }
    }

    #endregion

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    #region Dashboard Controller Methods

    public IActionResult Dashboard()
    {
        GetDatafromExcel();
        SetUserInformation();
        SetCardsData();
        SetEmpiricalViewBarChartData();
        SetPieChartData();
        SetRuleChartData();
        return View();
    }

    private List<DashboardRow> _dashboardRows;

    private void GetDatafromExcel()
    {
        var excelPath = GetFilePath("DashboardDataFile");

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        _dashboardRows = new List<DashboardRow>();

        using (var package = new ExcelPackage(new FileInfo(excelPath)))
        {
            var worksheet = package.Workbook.Worksheets[0];
            int rowCount = worksheet.Dimension.Rows;

            for (int row = 2; row <= rowCount; row++)
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

    private class DashboardRow
    {
        public string Month { get; set; }
        public decimal Total { get; set; }
        public decimal MatchedRule { get; set; }
        public decimal MatchedAI { get; set; }
        public decimal Unmatched { get; set; }
    }

    private void SetUserInformation()
    {
        var userId = HttpContext.Session.GetString("UserId");
        var userName = HttpContext.Session.GetString("UserName");
        ViewBag.UserId = userId;
        ViewBag.UserName = userName;
    }

    private void SetCardsData()
    {
        if (_dashboardRows == null || !_dashboardRows.Any()) return;
        var latest = _dashboardRows.Last();
        ViewBag.TotalAmount = latest.Total;
        ViewBag.MatchedBalanceRuleBased = latest.MatchedRule;
        ViewBag.UnmatchedBalance = latest.Unmatched;
        ViewBag.MatchedBalanceAi = latest.MatchedAI;
    }

    private void SetEmpiricalViewBarChartData()
    {
        if (_dashboardRows == null || !_dashboardRows.Any()) return;
        ViewBag.BarLabels = _dashboardRows.Select(r => r.Month).ToArray();
        ViewBag.MatchedBalanceRuleBasedData = _dashboardRows.Select(r => r.MatchedRule).ToArray();
        ViewBag.MatchedBalanceAiData = _dashboardRows.Select(r => r.MatchedAI).ToArray();
        ViewBag.UnmatchedBalanceData = _dashboardRows.Select(r => r.Unmatched).ToArray();
        ViewBag.YAxisMax = 380000;
    }

    private void SetRuleChartData()
    {
        var excelPath = GetFilePath("RuleDataFile");

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        int rl01Pct = 0, rl02Pct = 0, rl03Pct = 0;

        if (System.IO.File.Exists(excelPath))
        {
            using (var package = new ExcelPackage(new FileInfo(excelPath)))
            {
                var worksheet = package.Workbook.Worksheets[0];
                int rowCount = worksheet.Dimension.Rows;

                for (int row = 2; row <= rowCount; row++)
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

    private void SetPieChartData()
    {
        if (_dashboardRows == null || !_dashboardRows.Any()) return;
        var latest = _dashboardRows.Last();
        decimal total = latest.Total;
        decimal matchedRule = latest.MatchedRule;
        decimal matchedAI = latest.MatchedAI;
        decimal unmatched = latest.Unmatched;

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
        psi.WorkingDirectory = _env.ContentRootPath;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;

        using (var process = System.Diagnostics.Process.Start(psi))
        {
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrEmpty(error))
            {
                _logger.LogError("Python script error: {Error}", error);
                return Content("Error: " + error);
            }
        }

        return Json(new { success = true });
    }

    #endregion
}