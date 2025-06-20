using System.Security.Claims;
using kfrpj.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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

        // ดึงข้อมูลสถิติสำหรับแดชบอร์ด
        [HttpGet]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                // คำนวณข้อมูลสถิติ (ตัวอย่าง - ปรับตามข้อมูลจริง)
                var totalIncome = 45000; // ยอดรวมรายได้เดือนนี้
                var totalCollected = 35000; // ยอดที่เก็บแล้ว
                var totalPending = 10000; // ยอดค้างชำระ
                var pendingRooms = 3; // จำนวนห้องที่ต้องเก็บเงิน

                return Json(
                    new
                    {
                        totalIncome = totalIncome,
                        totalCollected = totalCollected,
                        totalPending = totalPending,
                        pendingRooms = pendingRooms,
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการดึงข้อมูลสถิติ");
                return Json(
                    new
                    {
                        totalIncome = 0,
                        totalCollected = 0,
                        totalPending = 0,
                        pendingRooms = 0,
                    }
                );
            }
        }

        // ดึงข้อมูลค่าน้ำ
        [HttpGet]
        public async Task<IActionResult> GetWaterMeters()
        {
            try
            {
                // ตัวอย่างข้อมูล - ปรับตามโครงสร้างฐานข้อมูลจริง
                var waterMeters = new[]
                {
                    new
                    {
                        water_meter_id = 1,
                        room_name = "ห้อง 101",
                        old_meter = 1000,
                        new_meter = 1050,
                        water_units = 50,
                        water_bill = 750,
                        is_paid = false,
                        meter_date = DateTime.Now.AddDays(-5).ToString("yyyy-MM-dd"),
                    },
                    new
                    {
                        water_meter_id = 2,
                        room_name = "ห้อง 102",
                        old_meter = 2000,
                        new_meter = 2030,
                        water_units = 30,
                        water_bill = 450,
                        is_paid = true,
                        meter_date = DateTime.Now.AddDays(-3).ToString("yyyy-MM-dd"),
                    },
                };

                return Json(new { data = waterMeters });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการดึงข้อมูลค่าน้ำ");
                return Json(new { data = new object[0] });
            }
        }

        // ดึงข้อมูลค่าไฟ
        [HttpGet]
        public async Task<IActionResult> GetElectricMeters()
        {
            try
            {
                // ตัวอย่างข้อมูล - ปรับตามโครงสร้างฐานข้อมูลจริง
                var electricMeters = new[]
                {
                    new
                    {
                        electric_meter_id = 1,
                        room_name = "ห้อง 101",
                        old_meter = 5000,
                        new_meter = 5150,
                        electric_units = 150,
                        electric_bill = 750,
                        is_paid = false,
                        meter_date = DateTime.Now.AddDays(-5).ToString("yyyy-MM-dd"),
                    },
                    new
                    {
                        electric_meter_id = 2,
                        room_name = "ห้อง 102",
                        old_meter = 3000,
                        new_meter = 3120,
                        electric_units = 120,
                        electric_bill = 600,
                        is_paid = true,
                        meter_date = DateTime.Now.AddDays(-3).ToString("yyyy-MM-dd"),
                    },
                };

                return Json(new { data = electricMeters });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการดึงข้อมูลค่าไฟ");
                return Json(new { data = new object[0] });
            }
        }

        // ดึงข้อมูลค่าห้อง
        [HttpGet]
        public async Task<IActionResult> GetRoomCharges()
        {
            try
            {
                // ตัวอย่างข้อมูล - ปรับตามโครงสร้างฐานข้อมูลจริง
                var roomCharges = new[]
                {
                    new
                    {
                        room_charge_id = 1,
                        room_name = "ห้อง 101",
                        tenant_name = "คุณสมชาย",
                        room_price = 3500,
                        charge_month = DateTime.Now.ToString("yyyy-MM"),
                        is_paid = false,
                        due_date = DateTime.Now.AddDays(5).ToString("yyyy-MM-dd"),
                    },
                    new
                    {
                        room_charge_id = 2,
                        room_name = "ห้อง 102",
                        tenant_name = "คุณสมหญิง",
                        room_price = 3500,
                        charge_month = DateTime.Now.ToString("yyyy-MM"),
                        is_paid = true,
                        due_date = DateTime.Now.AddDays(-2).ToString("yyyy-MM-dd"),
                    },
                };

                return Json(new { data = roomCharges });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการดึงข้อมูลค่าห้อง");
                return Json(new { data = new object[0] });
            }
        }

        // Methods สำหรับการจัดการข้อมูล (ตัวอย่าง)
        [HttpPost]
        public async Task<IActionResult> CreateWaterMeter([FromForm] object waterMeter)
        {
            // Implementation สำหรับเพิ่มข้อมูลค่าน้ำ
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> CreateElectricMeter([FromForm] object electricMeter)
        {
            // Implementation สำหรับเพิ่มข้อมูลค่าไฟ
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> CreateRoomCharge([FromForm] object roomCharge)
        {
            // Implementation สำหรับเพิ่มข้อมูลค่าห้อง
            return Json(new { success = true });
        }

        // อื่นๆ ตามความต้องการ...
    }
}
