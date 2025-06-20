using System.Diagnostics;
using kfrpj.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using kfrpj.Data;

namespace kfrpj.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // ทดสอบการเชื่อมต่อฐานข้อมูล
                await _context.Database.CanConnectAsync();
                TempData["DbConnectionStatus"] = "success";
            }
            catch (Exception ex)
            {
                TempData["DbConnectionStatus"] = "error";
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการเชื่อมต่อฐานข้อมูล");
            }

            return View();
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
    }
}
