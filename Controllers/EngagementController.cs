using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RECAP.Models;

namespace RECAP.Controllers
{
    public class EngagementController : Controller
    {
        private readonly ILogger<EngagementController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public EngagementController(ILogger<EngagementController> logger, IConfiguration configuration, IWebHostEnvironment env)
        {
            _logger = logger;
            _configuration = configuration;
            _env = env;
        }

        /// <summary>
        /// Displays the Engagement Engine page
        /// </summary>
        public IActionResult Index(int? prospectId = 1)
        {
            // Get sample prospect data (in production, fetch from database)
            var model = GetProspectEngagementData(prospectId ?? 1);
            return View("~/Views/Home/Engagement.cshtml", model);
        }

        /// <summary>
        /// Gets prospect engagement data with related guardian and referee information
        /// </summary>
        private EngagementViewModel GetProspectEngagementData(int prospectId)
        {
            // Sample data - Replace with actual database queries in production
            var model = new EngagementViewModel
            {
                ProspectId = prospectId,
                SelectedStage = "Identification",
                
                // Prospect Information
                ProspectName = "Rajesh Kumar",
                ProspectPhoneNumber = "+91-9876543210",
                PredictabilityScore = 78.5m,

                // Guardian Information
                GuardianName = "Ramesh Kumar (Father)",
                GuardianPhoneNumber = "+91-9876543211",

                // Referee Information
                RefereeName = "Vikram Singh",
                RefereePhoneNumber = "+91-9876543212"
            };

            return model;
        }

        /// <summary>
        /// Gets the list of stages
        /// </summary>
        public List<string> GetStages()
        {
            return new List<string>
            {
                "Identification",
                "Screening",
                "Counseling",
                "Document Processing",
                "Waiting for Batch",
                "On Job"
            };
        }

        /// <summary>
        /// Handles engagement via WhatsApp
        /// </summary>
        [HttpPost]
        public IActionResult EngageViaWhatsApp(EngagementRequest request)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid request data" });
            }

            try
            {
                // Log the engagement action
                _logger.LogInformation(
                    "WhatsApp engagement initiated - Prospect: {ProspectId}, Party: {PartyType}, Stage: {Stage}, Phone: {Phone}",
                    request.ProspectId, request.PartyType, request.Stage, request.PhoneNumber);

                // In production, integrate with WhatsApp Business API
                // For now, we'll just return a success message
                var message = $"WhatsApp message sent to {request.Name} ({request.PartyType}) at {request.PhoneNumber} for stage: {request.Stage}";

                return Json(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WhatsApp engagement");
                return Json(new { success = false, message = "Error sending WhatsApp message" });
            }
        }

        /// <summary>
        /// Handles engagement via Social Media
        /// </summary>
        [HttpPost]
        public IActionResult EngageViaSocialMedia(EngagementRequest request)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid request data" });
            }

            try
            {
                _logger.LogInformation(
                    "Social media engagement initiated - Prospect: {ProspectId}, Party: {PartyType}, Stage: {Stage}",
                    request.ProspectId, request.PartyType, request.Stage);

                // In production, integrate with social media APIs (Facebook, Instagram, etc.)
                var message = $"Social media message sent to {request.Name} ({request.PartyType}) for stage: {request.Stage}";

                return Json(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending social media engagement");
                return Json(new { success = false, message = "Error sending social media message" });
            }
        }

        /// <summary>
        /// Handles engagement via SMS/Message
        /// </summary>
        [HttpPost]
        public IActionResult EngageByMessage(EngagementRequest request)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid request data" });
            }

            try
            {
                _logger.LogInformation(
                    "SMS engagement initiated - Prospect: {ProspectId}, Party: {PartyType}, Stage: {Stage}, Phone: {Phone}",
                    request.ProspectId, request.PartyType, request.Stage, request.PhoneNumber);

                // In production, integrate with SMS service (Twilio, AWS SNS, etc.)
                var message = $"SMS message sent to {request.Name} ({request.PartyType}) at {request.PhoneNumber} for stage: {request.Stage}";

                return Json(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS engagement");
                return Json(new { success = false, message = "Error sending message" });
            }
        }

        /// <summary>
        /// Gets stages as JSON for AJAX
        /// </summary>
        [HttpGet]
        public IActionResult GetStagesJson()
        {
            var stages = GetStages();
            return Json(stages);
        }
    }
}