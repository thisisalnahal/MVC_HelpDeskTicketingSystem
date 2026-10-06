using HelpDesk.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace HelpDesk.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Tickets");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult StatusCode(int code)
        {
            ViewBag.Code = code;
            ViewBag.Message = code switch
            {
                403 => "You don't have permission to do that.",
                404 => "That page or ticket doesn't exist.",
                400 => "That action isn't valid right now — the ticket may have changed status.",
                _ => "Something went wrong."
            };
            return View();
        }
    }
}
