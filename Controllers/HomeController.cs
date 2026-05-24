using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Laundry.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public IActionResult Dashboard()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Privacy()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Subscribe(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                TempData["NewsletterError"] = "Please enter a valid email address.";
                return RedirectToAction("Index");
            }

            TempData["NewsletterSuccess"] = "Thanks for subscribing! Your 10% discount code: FRESH10";
            return RedirectToAction("Index");
        }
    }
}