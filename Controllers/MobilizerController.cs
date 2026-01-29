using System;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace RECAP.Controllers
{
    public class MobilizerController : Controller
    {
        private readonly ILogger<MobilizerController> _logger;

        public MobilizerController(ILogger<MobilizerController> logger)
        {
            _logger = logger;
        }

		[HttpGet("/Home/MobilizerView")]
        public IActionResult Index()
        {
            // Populate values expected by Views/Home/MobilizerView.cshtml
            // Replace these with real data retrieval as needed.
            ViewBag.barLabels = new[] { "Jan", "Feb", "Mar", "Apr" };
            ViewBag.MobilizerName = "Savita Shah";
            ViewBag.NumberOfActiveCases = 12;
            ViewBag.NumberOfCompletedCases = 34;
            ViewBag.yAxisMin = 0;
            ViewBag.yAxisMax = 100;

            // The view lives under Views/Home/MobilizerView.cshtml in your workspace.
            // Return the specific view path so the existing view is reused.
            return View("~/Views/Home/MobilizerView.cshtml");
        }

        // POST: /Mobilizer/RunPythonScript
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RunPythonScript()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = "Python/Rule5.py",
                    WorkingDirectory = Directory.GetCurrentDirectory(),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(error))
                    {
                        _logger.LogError("Python script error: {Error}", error);
                        return Json(new { success = false, error });
                    }

                    _logger.LogInformation("Python script output: {Output}", output);
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run python script");
                return Json(new { success = false, error = ex.Message });
            }
        }
    }
}

