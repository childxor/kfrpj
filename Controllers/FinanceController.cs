using Microsoft.AspNetCore.Mvc;
using kfrpj.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace kfrpj.Controllers
{
    public class FinanceController : Controller 
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FinanceController> _logger;

        public FinanceController(ApplicationDbContext context, ILogger<FinanceController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการเข้าถึงหน้า Finance");
                TempData["Error"] = "เกิดข้อผิดพลาดในการเข้าถึงหน้า กรุณาลองใหม่อีกครั้ง";
                return View();
            }
        }
    }
}
