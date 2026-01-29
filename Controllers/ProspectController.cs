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
                    DateOnly? dob = null;
                    if (!string.IsNullOrWhiteSpace(dobText))
                    {
                        if (DateOnly.TryParse(dobText, out var parsedDob))
                            dob = parsedDob;
                        else if (DateTime.TryParse(dobText, out var dt))
                            dob = DateOnly.FromDateTime(dt);
                    }

                    list.Add(new ProspectViewModel
                    {
                        CandidateName = ws.Cells[row, 1].Text,
                        MobileNumber = ws.Cells[row, 2].Text,
                        Age = int.TryParse(ws.Cells[row, 3].Text, out var age) ? age : null,
                        DOB = dob,
                        AADHAR = ws.Cells[row, 5].Text,
                        GuardianName = ws.Cells[row, 6].Text,
                        Address = ws.Cells[row, 7].Text,
                        EmailId = ws.Cells[row, 8].Text,
                        FamilyIncome = ws.Cells[row, 9].Text,
                        EducationLevel = ws.Cells[row, 10].Text,
                        CurrentStage = ws.Cells[row, 11].Text,
                        SmartDevAvailable = ws.Cells[row, 12].Text,
                        InternetAccess = ws.Cells[row, 13].Text,
                        PastWorkExperience = bool.TryParse(ws.Cells[row, 14].Text, out var pwe) ? pwe : null,
                        PastWorkDetails = ws.Cells[row, 15].Text,
                        PreferredLanguage = ws.Cells[row, 16].Text,
                        PreferredContactMode = ws.Cells[row, 17].Text,
                        PreferredTimeForContact = ws.Cells[row, 18].Text,
                        HealthIssues = ws.Cells[row, 19].Text,
                        FamilySupport = ws.Cells[row, 20].Text,
                        FamilyMembers = int.TryParse(ws.Cells[row, 21].Text, out var fm) ? fm : null,
                        SmokingHabits = bool.TryParse(ws.Cells[row, 22].Text, out var sh) ? sh : null,
                        DrinkingHabits = bool.TryParse(ws.Cells[row, 23].Text, out var dh) ? dh : null,
                        Reference = bool.TryParse(ws.Cells[row, 24].Text, out var r) && r,
                        ReferenceSourceOf = ws.Cells[row, 25].Text,
                        InstallmentDetails = ws.Cells[row, 26].Text,
                        PreviousProgram = ws.Cells[row, 27].Text,
                        ReasonForPlanning = ws.Cells[row, 28].Text,
                        Score = int.TryParse(ws.Cells[row, 29].Text, out var s) ? s : null
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
                
                // Header row with all columns
                ws.Cells[1, 1].Value = "CandidateName";
                ws.Cells[1, 2].Value = "MobileNumber";
                ws.Cells[1, 3].Value = "Age";
                ws.Cells[1, 4].Value = "DOB";
                ws.Cells[1, 5].Value = "AADHAR";
                ws.Cells[1, 6].Value = "GuardianName";
                ws.Cells[1, 7].Value = "Address";
                ws.Cells[1, 8].Value = "EmailId";
                ws.Cells[1, 9].Value = "FamilyIncome";
                ws.Cells[1, 10].Value = "EducationLevel";
                ws.Cells[1, 11].Value = "CurrentStage";
                ws.Cells[1, 12].Value = "SmartDevAvailable";
                ws.Cells[1, 13].Value = "InternetAccess";
                ws.Cells[1, 14].Value = "PastWorkExperience";
                ws.Cells[1, 15].Value = "PastWorkDetails";
                ws.Cells[1, 16].Value = "PreferredLanguage";
                ws.Cells[1, 17].Value = "PreferredContactMode";
                ws.Cells[1, 18].Value = "PreferredTimeForContact";
                ws.Cells[1, 19].Value = "HealthIssues";
                ws.Cells[1, 20].Value = "FamilySupport";
                ws.Cells[1, 21].Value = "FamilyMembers";
                ws.Cells[1, 22].Value = "SmokingHabits";
                ws.Cells[1, 23].Value = "DrinkingHabits";
                ws.Cells[1, 24].Value = "Reference";
                ws.Cells[1, 25].Value = "ReferenceSourceOf";
                ws.Cells[1, 26].Value = "InstallmentDetails";
                ws.Cells[1, 27].Value = "PreviousProgram";
                ws.Cells[1, 28].Value = "ReasonForPlanning";
                ws.Cells[1, 29].Value = "Score";

                // Data rows
                for (int i = 0; i < list.Count; i++)
                {
                    var r = i + 2;
                    ws.Cells[r, 1].Value = list[i].CandidateName;
                    ws.Cells[r, 2].Value = list[i].MobileNumber;
                    ws.Cells[r, 3].Value = list[i].Age;
                    ws.Cells[r, 4].Value = list[i].DOB.HasValue ? list[i].DOB.Value.ToString("yyyy-MM-dd") : "";
                    ws.Cells[r, 5].Value = list[i].AADHAR;
                    ws.Cells[r, 6].Value = list[i].GuardianName;
                    ws.Cells[r, 7].Value = list[i].Address;
                    ws.Cells[r, 8].Value = list[i].EmailId;
                    ws.Cells[r, 9].Value = list[i].FamilyIncome;
                    ws.Cells[r, 10].Value = list[i].EducationLevel;
                    ws.Cells[r, 11].Value = list[i].CurrentStage;
                    ws.Cells[r, 12].Value = list[i].SmartDevAvailable;
                    ws.Cells[r, 13].Value = list[i].InternetAccess;
                    ws.Cells[r, 14].Value = list[i].PastWorkExperience;
                    ws.Cells[r, 15].Value = list[i].PastWorkDetails;
                    ws.Cells[r, 16].Value = list[i].PreferredLanguage;
                    ws.Cells[r, 17].Value = list[i].PreferredContactMode;
                    ws.Cells[r, 18].Value = list[i].PreferredTimeForContact;
                    ws.Cells[r, 19].Value = list[i].HealthIssues;
                    ws.Cells[r, 20].Value = list[i].FamilySupport;
                    ws.Cells[r, 21].Value = list[i].FamilyMembers;
                    ws.Cells[r, 22].Value = list[i].SmokingHabits;
                    ws.Cells[r, 23].Value = list[i].DrinkingHabits;
                    ws.Cells[r, 24].Value = list[i].Reference;
                    ws.Cells[r, 25].Value = list[i].ReferenceSourceOf;
                    ws.Cells[r, 26].Value = list[i].InstallmentDetails;
                    ws.Cells[r, 27].Value = list[i].PreviousProgram;
                    ws.Cells[r, 28].Value = list[i].ReasonForPlanning;
                    ws.Cells[r, 29].Value = list[i].Score;
                }

                var fi = new FileInfo(prospectsFile);
                package.SaveAs(fi);
            }
        }

        /// <summary>
        /// Send behavior survey questionnaire to prospect via SMS and email
        /// </summary>
        [HttpPost]
        public IActionResult SendBehaviorSurvey([FromBody] dynamic surveyData)
        {
            try
            {
                string name = surveyData?.name;
                string aadhar = surveyData?.aadhar;

                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(aadhar))
                {
                    return Json(new { success = false, message = "Name and AADHAR are required" });
                }

                // TODO: Implement actual SMS/Email sending logic
                // This would integrate with SMS gateway (Twilio, AWS SNS, etc.) and Email service
                // For now, we're simulating successful send

                return Json(new { success = true, message = "Survey sent successfully to " + name });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}