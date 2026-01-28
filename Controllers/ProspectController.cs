using Microsoft.AspNetCore.Mvc;
using RECAP.Models;

namespace RECAP.Controllers
{
    public class ProspectController : Controller
    {
        public IActionResult Index()
        {
            // Replace with real data fetching logic
            var model = new ProspectViewModel
            {
                //TotalAmount = 40000,
                //MatchedBalanceRuleBased = 215000,
                //UnmatchedBalance = 18,
                //MatchedBalanceAiPercent = 50
            };

            return View(model);
        }
    }
}