using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using RECAP.Models;

namespace RECAP.Controllers
{
    public class ProspectController : Controller
    {
        private readonly ILogger<ProspectController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public ProspectController(ILogger<ProspectController> logger, IConfiguration configuration, IWebHostEnvironment env)
        {
            _logger = logger;
            _configuration = configuration;
            _env = env;
        }

        /// <summary>
        /// Gets the full file path from configuration.
        /// </summary>
        private string GetFilePath(string configKey)
        {
            var fileName = _configuration[$"FileStoragePaths:Files:{configKey}"];
            var sourceFolder = _configuration["FileStoragePaths:SourceFilesFolder"];
            return Path.Combine(_env.ContentRootPath, sourceFolder, fileName);
        }

        // Map both /Prospect and /Home/Prospect to this action
        [HttpGet("/Prospect")]
        [HttpGet("/Home/Prospect")]
        public IActionResult Index(int? id)
        {
            var list = ReadProspectsFromExcel();

            ProspectViewModel selected = null;
            int? selectedIndex = null;

            if (id.HasValue)
            {
                var idx = id.Value - 1;
                if (idx >= 0 && idx < list.Count)
                {
                    selected = list[idx];
                    selectedIndex = id.Value;
                }
            }

            var model = new ProspectDetailsViewModel
            {
                SelectedProspect = selected,
                SelectedIndex = selectedIndex,
                Prospects = list
            };

            return View("~/Views/Home/Prospect.cshtml", model);
        }

        // POST: accept both route forms
        [HttpPost("/Prospect/Save")]
        [HttpPost("/Home/Prospect/Save")]
        [ValidateAntiForgeryToken]
        public IActionResult Save(ProspectViewModel model)
        {
            if (model == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid prospect data.");
                return RedirectToAction(nameof(Index));
            }

            if (model.DOB == default)
            {
                var dobValue = Request.Form["DOB"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(dobValue))
                {
                    if (DateOnly.TryParse(dobValue, out var parsed))
                        model.DOB = parsed;
                    else if (DateTime.TryParse(dobValue, out var dtd))
                        model.DOB = DateOnly.FromDateTime(dtd);
                }
            }

            var list = ReadProspectsFromExcel();
            list.Add(model);

            try
            {
                WriteProspectsToExcel(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write prospects to Excel");
                TempData["Error"] = "Failed to save prospect. See logs for details.";
            }

            return RedirectToAction(nameof(Index));
        }

        private List<ProspectViewModel> ReadProspectsFromExcel()
        {
            var list = new List<ProspectViewModel>();
            var prospectsFile = GetFilePath("ProspectDataFile");

            if (!System.IO.File.Exists(prospectsFile))
                return list;

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage(new FileInfo(prospectsFile)))
            {
                var ws = package.Workbook.Worksheets.FirstOrDefault();
                if (ws == null) return list;

                int row = 2;
                while (!string.IsNullOrWhiteSpace(ws.Cells[row, 1].Text))
                {
                    var name = ws.Cells[row, 1].Text;
                    var dobText = ws.Cells[row, 2].Text;
                    DateOnly dob;
                    if (!DateOnly.TryParse(dobText, out dob))
                    {
                        if (DateTime.TryParse(dobText, out var dt))
                            dob = DateOnly.FromDateTime(dt);
                        else
                            dob = DateOnly.FromDateTime(DateTime.MinValue);
                    }

                    list.Add(new ProspectViewModel
                    {
                        Name = name,
                        DOB = dob,
                        AADHAR = ws.Cells[row, 3].Text,
                        GuardianName = ws.Cells[row, 4].Text,
                        Address = ws.Cells[row, 5].Text,
                        FamilyIncome = ws.Cells[row, 6].Text,
                        Reference = bool.TryParse(ws.Cells[row, 7].Text, out var r) && r,
                        CurrentStage = ws.Cells[row, 8].Text,
                        Score = int.TryParse(ws.Cells[row, 9].Text, out var s) ? s : 0
                    });

                    row++;
                }
            }

            return list;
        }

        private void WriteProspectsToExcel(List<ProspectViewModel> list)
        {
            var prospectsFile = GetFilePath("ProspectDataFile");
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var dir = Path.GetDirectoryName(prospectsFile);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Prospects");
                ws.Cells[1, 1].Value = "Name";
                ws.Cells[1, 2].Value = "DOB";
                ws.Cells[1, 3].Value = "AADHAR";
                ws.Cells[1, 4].Value = "GuardianName";
                ws.Cells[1, 5].Value = "Address";
                ws.Cells[1, 6].Value = "FamilyIncome";
                ws.Cells[1, 7].Value = "Reference";
                ws.Cells[1, 8].Value = "CurrentStage";
                ws.Cells[1, 9].Value = "Score";

                for (int i = 0; i < list.Count; i++)
                {
                    var r = i + 2;
                    ws.Cells[r, 1].Value = list[i].Name;
                    ws.Cells[r, 2].Value = !list[i].DOB.HasValue || list[i].DOB == DateOnly.FromDateTime(DateTime.MinValue) ? "" : list[i].DOB.Value.ToString("yyyy-MM-dd");
                    ws.Cells[r, 3].Value = list[i].AADHAR;
                    ws.Cells[r, 4].Value = list[i].GuardianName;
                    ws.Cells[r, 5].Value = list[i].Address;
                    ws.Cells[r, 6].Value = list[i].FamilyIncome;
                    ws.Cells[r, 7].Value = list[i].Reference;
                    ws.Cells[r, 8].Value = list[i].CurrentStage;
                    ws.Cells[r, 9].Value = list[i].Score;
                }

                var fi = new FileInfo(prospectsFile);
                package.SaveAs(fi);
            }
        }
    }
}