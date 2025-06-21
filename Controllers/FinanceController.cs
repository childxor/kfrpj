using kfrpj.Data;
using kfrpj.Models.finance;
using kfrpj.Models.settings;
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
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;

                // คำนวณค่าน้ำรวม
                var totalWaterBill = await _context
                    .water_meters_list.Where(w =>
                        w.meter_date.Month == currentMonth
                        && w.meter_date.Year == currentYear
                        && w.record_status == "N"
                    )
                    .SumAsync(w => w.water_bill);

                // คำนวณค่าไฟรวม
                var totalElectricBill = await _context
                    .electric_meters_list.Where(e =>
                        e.meter_date.Month == currentMonth
                        && e.meter_date.Year == currentYear
                        && e.record_status == "N"
                    )
                    .SumAsync(e => e.electric_bill);

                // คำนวณค่าห้องรวม
                var totalRoomCharges = await _context
                    .room_charges_list.Where(r =>
                        r.charge_month.Month == currentMonth
                        && r.charge_month.Year == currentYear
                        && r.record_status == "N"
                    )
                    .SumAsync(r => r.room_price);

                var totalIncome = totalWaterBill + totalElectricBill + totalRoomCharges;

                // คำนวณยอดเก็บแล้ว
                var collectedWater = await _context
                    .water_meters_list.Where(w =>
                        w.meter_date.Month == currentMonth
                        && w.meter_date.Year == currentYear
                        && w.is_paid
                        && w.record_status == "N"
                    )
                    .SumAsync(w => w.water_bill);

                var collectedElectric = await _context
                    .electric_meters_list.Where(e =>
                        e.meter_date.Month == currentMonth
                        && e.meter_date.Year == currentYear
                        && e.is_paid
                        && e.record_status == "N"
                    )
                    .SumAsync(e => e.electric_bill);

                var collectedRoom = await _context
                    .room_charges_list.Where(r =>
                        r.charge_month.Month == currentMonth
                        && r.charge_month.Year == currentYear
                        && r.is_paid
                        && r.record_status == "N"
                    )
                    .SumAsync(r => r.room_price);

                var totalCollected = collectedWater + collectedElectric + collectedRoom;
                var totalPending = totalIncome - totalCollected;

                // นับจำนวนห้องที่ต้องเก็บเงิน
                var pendingRooms = await _context
                    .room_charges_list.Where(r =>
                        r.charge_month.Month == currentMonth
                        && r.charge_month.Year == currentYear
                        && !r.is_paid
                        && r.record_status == "N"
                    )
                    .CountAsync();

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

        // ====== WATER METERS ======
        [HttpGet]
        public async Task<IActionResult> GetWaterMeters()
        {
            try
            {
                var waterMeters = await _context
                    .water_meters_list.Include(w => w.Room)
                    .Where(w => w.record_status == "N")
                    .OrderByDescending(w => w.meter_date)
                    .Select(w => new
                    {
                        water_meter_id = w.water_meter_id,
                        room_name = w.Room.room_name,
                        old_meter = w.old_meter,
                        new_meter = w.new_meter,
                        water_units = w.water_units,
                        water_bill = w.water_bill,
                        is_paid = w.is_paid,
                        meter_date = w.meter_date.ToString("yyyy-MM-dd"),
                        notes = w.notes,
                    })
                    .ToListAsync();

                return Json(new { data = waterMeters });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการดึงข้อมูลค่าน้ำ");
                return Json(new { data = new object[0] });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateWaterMeter([FromForm] water_meters_list waterMeter)
        {
            try
            {
                // ดึงค่าตั้งค่าค่าน้ำ
                var waterRate = await GetWaterRate();

                waterMeter.water_units = waterMeter.new_meter - waterMeter.old_meter;
                waterMeter.water_bill = waterMeter.water_units * waterRate;
                waterMeter.created_at = DateTime.Now;
                waterMeter.created_by = HttpContext.Session.GetString("Username") ?? "System";
                waterMeter.record_status = "N";

                _context.water_meters_list.Add(waterMeter);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการสร้างข้อมูลค่าน้ำ");
                return Json(new { success = false, message = "ไม่สามารถบันทึกข้อมูลได้" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetWaterMeter(int id)
        {
            try
            {
                var waterMeter = await _context.water_meters_list.FindAsync(id);
                if (waterMeter == null)
                    return NotFound();

                return Json(waterMeter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"เกิดข้อผิดพลาดในการดึงข้อมูลค่าน้ำ ID: {id}");
                return NotFound();
            }
        }

        [HttpPut]
        public async Task<IActionResult> EditWaterMeter(
            int id,
            [FromForm] water_meters_list waterMeter
        )
        {
            try
            {
                var existingMeter = await _context.water_meters_list.FindAsync(id);
                if (existingMeter == null)
                    return NotFound();

                var waterRate = await GetWaterRate();

                existingMeter.room_id = waterMeter.room_id;
                existingMeter.meter_date = waterMeter.meter_date;
                existingMeter.old_meter = waterMeter.old_meter;
                existingMeter.new_meter = waterMeter.new_meter;
                existingMeter.water_units = waterMeter.new_meter - waterMeter.old_meter;
                existingMeter.water_bill = existingMeter.water_units * waterRate;
                existingMeter.notes = waterMeter.notes;
                existingMeter.updated_at = DateTime.Now;
                existingMeter.updated_by = HttpContext.Session.GetString("Username") ?? "System";

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"เกิดข้อผิดพลาดในการแก้ไขข้อมูลค่าน้ำ ID: {id}");
                return Json(new { success = false, message = "ไม่สามารถแก้ไขข้อมูลได้" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkWaterAsPaid(int id)
        {
            try
            {
                var waterMeter = await _context.water_meters_list.FindAsync(id);
                if (waterMeter == null)
                    return NotFound();

                waterMeter.is_paid = true;
                waterMeter.updated_at = DateTime.Now;
                waterMeter.updated_by = HttpContext.Session.GetString("Username") ?? "System";

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"เกิดข้อผิดพลาดในการอัปเดตสถานะค่าน้ำ ID: {id}");
                return Json(new { success = false, message = "ไม่สามารถอัปเดตสถานะได้" });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteWaterMeter(int id)
        {
            try
            {
                var waterMeter = await _context.water_meters_list.FindAsync(id);
                if (waterMeter == null)
                    return NotFound();

                waterMeter.record_status = "D";
                waterMeter.updated_at = DateTime.Now;
                waterMeter.updated_by = HttpContext.Session.GetString("Username") ?? "System";

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"เกิดข้อผิดพลาดในการลบข้อมูลค่าน้ำ ID: {id}");
                return Json(new { success = false, message = "ไม่สามารถลบข้อมูลได้" });
            }
        }

        // ====== ELECTRIC METERS ======
        [HttpGet]
        public async Task<IActionResult> GetElectricMeters()
        {
            try
            {
                var electricMeters = await _context
                    .electric_meters_list.Include(e => e.Room)
                    .Where(e => e.record_status == "N")
                    .OrderByDescending(e => e.meter_date)
                    .Select(e => new
                    {
                        electric_meter_id = e.electric_meter_id,
                        room_name = e.Room.room_name,
                        old_meter = e.old_meter,
                        new_meter = e.new_meter,
                        electric_units = e.electric_units,
                        electric_bill = e.electric_bill,
                        is_paid = e.is_paid,
                        meter_date = e.meter_date.ToString("yyyy-MM-dd"),
                        notes = e.notes,
                    })
                    .ToListAsync();

                return Json(new { data = electricMeters });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการดึงข้อมูลค่าไฟ");
                return Json(new { data = new object[0] });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateElectricMeter(
            [FromForm] electric_meters_list electricMeter
        )
        {
            try
            {
                var electricRate = await GetElectricRate();

                electricMeter.electric_units = electricMeter.new_meter - electricMeter.old_meter;
                electricMeter.electric_bill = electricMeter.electric_units * electricRate;
                electricMeter.created_at = DateTime.Now;
                electricMeter.created_by = HttpContext.Session.GetString("Username") ?? "System";
                electricMeter.record_status = "N";

                _context.electric_meters_list.Add(electricMeter);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการสร้างข้อมูลค่าไฟ");
                return Json(new { success = false, message = "ไม่สามารถบันทึกข้อมูลได้" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetElectricMeter(int id)
        {
            try
            {
                var electricMeter = await _context.electric_meters_list.FindAsync(id);
                if (electricMeter == null)
                    return NotFound();

                return Json(electricMeter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"เกิดข้อผิดพลาดในการดึงข้อมูลค่าไฟ ID: {id}");
                return NotFound();
            }
        }

        [HttpPut]
        public async Task<IActionResult> EditElectricMeter(
            int id,
            [FromForm] electric_meters_list electricMeter
        )
        {
            try
            {
                var existingMeter = await _context.electric_meters_list.FindAsync(id);
                if (existingMeter == null)
                    return NotFound();

                var electricRate = await GetElectricRate();

                existingMeter.room_id = electricMeter.room_id;
                existingMeter.meter_date = electricMeter.meter_date;
                existingMeter.old_meter = electricMeter.old_meter;
                existingMeter.new_meter = electricMeter.new_meter;
                existingMeter.electric_units = electricMeter.new_meter - electricMeter.old_meter;
                existingMeter.electric_bill = existingMeter.electric_units * electricRate;
                existingMeter.notes = electricMeter.notes;
                existingMeter.updated_at = DateTime.Now;
                existingMeter.updated_by = HttpContext.Session.GetString("Username") ?? "System";

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"เกิดข้อผิดพลาดในการแก้ไขข้อมูลค่าไฟ ID: {id}");
                return Json(new { success = false, message = "ไม่สามารถแก้ไขข้อมูลได้" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkElectricAsPaid(int id)
        {
            try
            {
                var electricMeter = await _context.electric_meters_list.FindAsync(id);
                if (electricMeter == null)
                    return NotFound();

                electricMeter.is_paid = true;
                electricMeter.updated_at = DateTime.Now;
                electricMeter.updated_by = HttpContext.Session.GetString("Username") ?? "System";

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"เกิดข้อผิดพลาดในการอัปเดตสถานะค่าไฟ ID: {id}");
                return Json(new { success = false, message = "ไม่สามารถอัปเดตสถานะได้" });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteElectricMeter(int id)
        {
            try
            {
                var electricMeter = await _context.electric_meters_list.FindAsync(id);
                if (electricMeter == null)
                    return NotFound();

                electricMeter.record_status = "D";
                electricMeter.updated_at = DateTime.Now;
                electricMeter.updated_by = HttpContext.Session.GetString("Username") ?? "System";

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"เกิดข้อผิดพลาดในการลบข้อมูลค่าไฟ ID: {id}");
                return Json(new { success = false, message = "ไม่สามารถลบข้อมูลได้" });
            }
        }

        // ====== ROOM CHARGES ======
        [HttpGet]
        public async Task<IActionResult> GetRoomCharges()
        {
            try
            {
                var roomCharges = await _context
                    .room_charges_list.Include(r => r.Room)
                    .Where(r => r.record_status == "N")
                    .OrderByDescending(r => r.charge_month)
                    .Select(r => new
                    {
                        room_charge_id = r.room_charge_id,
                        room_name = r.Room.room_name,
                        tenant_name = "ผู้เช่า", // จะปรับเมื่อมีตาราง tenant-room relationship
                        room_price = r.room_price,
                        charge_month = r.charge_month.ToString("yyyy-MM"),
                        is_paid = r.is_paid,
                        due_date = r.due_date.ToString("yyyy-MM-dd"),
                        paid_date = r.paid_date != null
                            ? r.paid_date.Value.ToString("yyyy-MM-dd")
                            : null,
                        notes = r.notes,
                    })
                    .ToListAsync();

                return Json(new { data = roomCharges });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการดึงข้อมูลค่าห้อง");
                return Json(new { data = new object[0] });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateRoomCharge([FromForm] room_charges_list roomCharge)
        {
            try
            {
                roomCharge.created_at = DateTime.Now;
                roomCharge.created_by = HttpContext.Session.GetString("Username") ?? "System";
                roomCharge.record_status = "N";

                _context.room_charges_list.Add(roomCharge);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการสร้างข้อมูลค่าห้อง");
                return Json(new { success = false, message = "ไม่สามารถบันทึกข้อมูลได้" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRoomCharge(int id)
        {
            try
            {
                var roomCharge = await _context.room_charges_list.FindAsync(id);
                if (roomCharge == null)
                    return NotFound();

                return Json(roomCharge);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"เกิดข้อผิดพลาดในการดึงข้อมูลค่าห้อง ID: {id}");
                return NotFound();
            }
        }

        [HttpPut]
        public async Task<IActionResult> EditRoomCharge(
            int id,
            [FromForm] room_charges_list roomCharge
        )
        {
            try
            {
                var existingCharge = await _context.room_charges_list.FindAsync(id);
                if (existingCharge == null)
                    return NotFound();

                existingCharge.room_id = roomCharge.room_id;
                existingCharge.charge_month = roomCharge.charge_month;
                existingCharge.room_price = roomCharge.room_price;
                existingCharge.due_date = roomCharge.due_date;
                existingCharge.notes = roomCharge.notes;
                existingCharge.updated_at = DateTime.Now;
                existingCharge.updated_by = HttpContext.Session.GetString("Username") ?? "System";

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"เกิดข้อผิดพลาดในการแก้ไขข้อมูลค่าห้อง ID: {id}");
                return Json(new { success = false, message = "ไม่สามารถแก้ไขข้อมูลได้" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkRoomChargeAsPaid(int id)
        {
            try
            {
                var roomCharge = await _context.room_charges_list.FindAsync(id);
                if (roomCharge == null)
                    return NotFound();

                roomCharge.is_paid = true;
                roomCharge.paid_date = DateTime.Now;
                roomCharge.updated_at = DateTime.Now;
                roomCharge.updated_by = HttpContext.Session.GetString("Username") ?? "System";

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"เกิดข้อผิดพลาดในการอัปเดตสถานะค่าห้อง ID: {id}");
                return Json(new { success = false, message = "ไม่สามารถอัปเดตสถานะได้" });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteRoomCharge(int id)
        {
            try
            {
                var roomCharge = await _context.room_charges_list.FindAsync(id);
                if (roomCharge == null)
                    return NotFound();

                roomCharge.record_status = "D";
                roomCharge.updated_at = DateTime.Now;
                roomCharge.updated_by = HttpContext.Session.GetString("Username") ?? "System";

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"เกิดข้อผิดพลาดในการลบข้อมูลค่าห้อง ID: {id}");
                return Json(new { success = false, message = "ไม่สามารถลบข้อมูลได้" });
            }
        }

        // ====== HELPER METHODS ======
        private async Task<decimal> GetWaterRate()
        {
            var setting = await _context.settings_list.FirstOrDefaultAsync(s =>
                s.setting_key == "WATER_RATE_PER_UNIT" && s.record_status == "N"
            );
            return decimal.TryParse(setting?.setting_value, out var rate) ? rate : 15.00m;
        }

        private async Task<decimal> GetElectricRate()
        {
            var setting = await _context.settings_list.FirstOrDefaultAsync(s =>
                s.setting_key == "ELECTRIC_RATE_PER_UNIT" && s.record_status == "N"
            );
            return decimal.TryParse(setting?.setting_value, out var rate) ? rate : 5.00m;
        }

        [HttpGet]
        public async Task<IActionResult> GetRates()
        {
            try
            {
                var waterRate = await GetWaterRate();
                var electricRate = await GetElectricRate();

                return Json(new { waterRate = waterRate, electricRate = electricRate });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการดึงข้อมูลอัตราค่าใช้จ่าย");
                return Json(new { waterRate = 15.00m, electricRate = 5.00m });
            }
        }
    }
}
