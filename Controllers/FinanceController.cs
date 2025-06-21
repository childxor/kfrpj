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

        // 21
        // เพิ่ม methods เหล่านี้ใน FinanceController.cs

        [HttpGet]
        public async Task<IActionResult> GetMissionData()
        {
            try
            {
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;

                // ดึงข้อมูลห้องทั้งหมด
                var rooms = await _context
                    .rooms_list.Where(r => r.record_status == "N")
                    .OrderBy(r => r.room_name)
                    .ToListAsync();

                var missionData = new List<object>();

                foreach (var room in rooms)
                {
                    // ดึงข้อมูลค่าห้อง
                    var roomCharge = await _context
                        .room_charges_list.Where(rc =>
                            rc.room_id == room.room_id
                            && rc.charge_month.Month == currentMonth
                            && rc.charge_month.Year == currentYear
                            && rc.record_status == "N"
                        )
                        .FirstOrDefaultAsync();

                    // ดึงข้อมูลค่าน้ำ
                    var waterBill = await _context
                        .water_meters_list.Where(w =>
                            w.room_id == room.room_id
                            && w.meter_date.Month == currentMonth
                            && w.meter_date.Year == currentYear
                            && w.record_status == "N"
                        )
                        .OrderByDescending(w => w.meter_date)
                        .FirstOrDefaultAsync();

                    // ดึงข้อมูลค่าไฟ
                    var electricBill = await _context
                        .electric_meters_list.Where(e =>
                            e.room_id == room.room_id
                            && e.meter_date.Month == currentMonth
                            && e.meter_date.Year == currentYear
                            && e.record_status == "N"
                        )
                        .OrderByDescending(e => e.meter_date)
                        .FirstOrDefaultAsync();

                    // ดึงข้อมูลผู้เช่า (สำหรับในอนาคต)
                    var tenantName = "ผู้เช่า"; // จะปรับเมื่อมีตาราง tenant-room relationship

                    // คำนวณยอดเงิน
                    var roomChargeAmount = roomCharge?.room_price ?? room.room_price;
                    var waterAmount = waterBill?.water_bill ?? 0;
                    var electricAmount = electricBill?.electric_bill ?? 0;
                    var totalAmount = roomChargeAmount + waterAmount + electricAmount;

                    // คำนวณยอดที่จ่ายแล้ว
                    var roomChargePaid = roomCharge?.is_paid ?? false;
                    var waterPaid = waterBill?.is_paid ?? false;
                    var electricPaid = electricBill?.is_paid ?? false;

                    var paidAmount = 0m;
                    if (roomChargePaid)
                        paidAmount += roomChargeAmount;
                    if (waterPaid)
                        paidAmount += waterAmount;
                    if (electricPaid)
                        paidAmount += electricAmount;

                    // สถานะการจ่าย
                    var isFullyPaid = roomChargePaid && waterPaid && electricPaid;
                    var isPartiallyPaid =
                        (roomChargePaid || waterPaid || electricPaid) && !isFullyPaid;

                    missionData.Add(
                        new
                        {
                            roomId = room.room_id,
                            roomName = room.room_name,
                            tenantName = tenantName,
                            roomCharge = new
                            {
                                amount = roomChargeAmount,
                                isPaid = roomChargePaid,
                                id = roomCharge?.room_charge_id,
                            },
                            waterBill = new
                            {
                                amount = waterAmount,
                                isPaid = waterPaid,
                                id = waterBill?.water_meter_id,
                                units = waterBill?.water_units ?? 0,
                            },
                            electricBill = new
                            {
                                amount = electricAmount,
                                isPaid = electricPaid,
                                id = electricBill?.electric_meter_id,
                                units = electricBill?.electric_units ?? 0,
                            },
                            totalAmount = totalAmount,
                            paidAmount = paidAmount,
                            pendingAmount = totalAmount - paidAmount,
                            isFullyPaid = isFullyPaid,
                            isPartiallyPaid = isPartiallyPaid,
                            paymentProgress = totalAmount > 0
                                ? (int)((paidAmount / totalAmount) * 100)
                                : 0,
                        }
                    );
                }

                return Json(new { success = true, rooms = missionData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการดึงข้อมูล Mission");
                return Json(new { success = false, message = "ไม่สามารถดึงข้อมูลได้" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkRoomAsPaid(int roomId)
        {
            try
            {
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;
                var username = HttpContext.Session.GetString("Username") ?? "System";

                // อัพเดทค่าห้อง
                var roomCharge = await _context
                    .room_charges_list.Where(rc =>
                        rc.room_id == roomId
                        && rc.charge_month.Month == currentMonth
                        && rc.charge_month.Year == currentYear
                        && rc.record_status == "N"
                    )
                    .FirstOrDefaultAsync();

                if (roomCharge != null && !roomCharge.is_paid)
                {
                    roomCharge.is_paid = true;
                    roomCharge.paid_date = DateTime.Now;
                    roomCharge.updated_at = DateTime.Now;
                    roomCharge.updated_by = username;
                }

                // อัพเดทค่าน้ำ
                var waterBill = await _context
                    .water_meters_list.Where(w =>
                        w.room_id == roomId
                        && w.meter_date.Month == currentMonth
                        && w.meter_date.Year == currentYear
                        && w.record_status == "N"
                    )
                    .OrderByDescending(w => w.meter_date)
                    .FirstOrDefaultAsync();

                if (waterBill != null && !waterBill.is_paid)
                {
                    waterBill.is_paid = true;
                    waterBill.updated_at = DateTime.Now;
                    waterBill.updated_by = username;
                }

                // อัพเดทค่าไฟ
                var electricBill = await _context
                    .electric_meters_list.Where(e =>
                        e.room_id == roomId
                        && e.meter_date.Month == currentMonth
                        && e.meter_date.Year == currentYear
                        && e.record_status == "N"
                    )
                    .OrderByDescending(e => e.meter_date)
                    .FirstOrDefaultAsync();

                if (electricBill != null && !electricBill.is_paid)
                {
                    electricBill.is_paid = true;
                    electricBill.updated_at = DateTime.Now;
                    electricBill.updated_by = username;
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "บันทึกการชำระเงินเรียบร้อยแล้ว" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"เกิดข้อผิดพลาดในการบันทึกการชำระเงิน Room ID: {roomId}");
                return Json(new { success = false, message = "ไม่สามารถบันทึกการชำระเงินได้" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkSpecificBillAsPaid(
            int roomId,
            string billType,
            int? billId = null
        )
        {
            try
            {
                var username = HttpContext.Session.GetString("Username") ?? "System";
                var success = false;

                switch (billType.ToLower())
                {
                    case "room":
                        if (billId.HasValue)
                        {
                            var roomCharge = await _context.room_charges_list.FindAsync(
                                billId.Value
                            );
                            if (roomCharge != null && !roomCharge.is_paid)
                            {
                                roomCharge.is_paid = true;
                                roomCharge.paid_date = DateTime.Now;
                                roomCharge.updated_at = DateTime.Now;
                                roomCharge.updated_by = username;
                                success = true;
                            }
                        }
                        break;

                    case "water":
                        if (billId.HasValue)
                        {
                            var waterBill = await _context.water_meters_list.FindAsync(
                                billId.Value
                            );
                            if (waterBill != null && !waterBill.is_paid)
                            {
                                waterBill.is_paid = true;
                                waterBill.updated_at = DateTime.Now;
                                waterBill.updated_by = username;
                                success = true;
                            }
                        }
                        break;

                    case "electric":
                        if (billId.HasValue)
                        {
                            var electricBill = await _context.electric_meters_list.FindAsync(
                                billId.Value
                            );
                            if (electricBill != null && !electricBill.is_paid)
                            {
                                electricBill.is_paid = true;
                                electricBill.updated_at = DateTime.Now;
                                electricBill.updated_by = username;
                                success = true;
                            }
                        }
                        break;
                }

                if (success)
                {
                    await _context.SaveChangesAsync();
                    return Json(
                        new
                        {
                            success = true,
                            message = $"บันทึกการชำระ{GetBillTypeName(billType)}เรียบร้อยแล้ว",
                        }
                    );
                }
                else
                {
                    return Json(new { success = false, message = "ไม่พบข้อมูลที่ต้องการอัพเดท" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"เกิดข้อผิดพลาดในการบันทึกการชำระเงิน {billType} Room ID: {roomId}"
                );
                return Json(new { success = false, message = "ไม่สามารถบันทึกการชำระเงินได้" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMissionSummary()
        {
            try
            {
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;

                // สถิติโดยรวม
                var totalRooms = await _context
                    .rooms_list.Where(r => r.record_status == "N")
                    .CountAsync();

                // ห้องที่จ่ายครบแล้ว
                var paidRoomsQuery =
                    from room in _context.rooms_list
                    where room.record_status == "N"
                    let roomCharge = _context
                        .room_charges_list.Where(rc =>
                            rc.room_id == room.room_id
                            && rc.charge_month.Month == currentMonth
                            && rc.charge_month.Year == currentYear
                            && rc.record_status == "N"
                        )
                        .FirstOrDefault()
                    let waterBill = _context
                        .water_meters_list.Where(w =>
                            w.room_id == room.room_id
                            && w.meter_date.Month == currentMonth
                            && w.meter_date.Year == currentYear
                            && w.record_status == "N"
                        )
                        .OrderByDescending(w => w.meter_date)
                        .FirstOrDefault()
                    let electricBill = _context
                        .electric_meters_list.Where(e =>
                            e.room_id == room.room_id
                            && e.meter_date.Month == currentMonth
                            && e.meter_date.Year == currentYear
                            && e.record_status == "N"
                        )
                        .OrderByDescending(e => e.meter_date)
                        .FirstOrDefault()
                    where
                        (roomCharge == null || roomCharge.is_paid)
                        && (waterBill == null || waterBill.is_paid)
                        && (electricBill == null || electricBill.is_paid)
                    select room;

                var fullyPaidRooms = await paidRoomsQuery.CountAsync();

                // ห้องที่จ่ายบางส่วน
                var partialPaidRoomsQuery =
                    from room in _context.rooms_list
                    where room.record_status == "N"
                    let roomCharge = _context
                        .room_charges_list.Where(rc =>
                            rc.room_id == room.room_id
                            && rc.charge_month.Month == currentMonth
                            && rc.charge_month.Year == currentYear
                            && rc.record_status == "N"
                        )
                        .FirstOrDefault()
                    let waterBill = _context
                        .water_meters_list.Where(w =>
                            w.room_id == room.room_id
                            && w.meter_date.Month == currentMonth
                            && w.meter_date.Year == currentYear
                            && w.record_status == "N"
                        )
                        .OrderByDescending(w => w.meter_date)
                        .FirstOrDefault()
                    let electricBill = _context
                        .electric_meters_list.Where(e =>
                            e.room_id == room.room_id
                            && e.meter_date.Month == currentMonth
                            && e.meter_date.Year == currentYear
                            && e.record_status == "N"
                        )
                        .OrderByDescending(e => e.meter_date)
                        .FirstOrDefault()
                    let paidCount = ((roomCharge != null && roomCharge.is_paid) ? 1 : 0)
                        + ((waterBill != null && waterBill.is_paid) ? 1 : 0)
                        + ((electricBill != null && electricBill.is_paid) ? 1 : 0)
                    let totalBills = ((roomCharge != null) ? 1 : 0)
                        + ((waterBill != null) ? 1 : 0)
                        + ((electricBill != null) ? 1 : 0)
                    where paidCount > 0 && paidCount < totalBills
                    select room;

                var partiallyPaidRooms = await partialPaidRoomsQuery.CountAsync();

                var unpaidRooms = totalRooms - fullyPaidRooms - partiallyPaidRooms;

                // คำนวณยอดเงิน
                var totalIncome = 0m;
                var totalCollected = 0m;

                var roomChargesIncome = await _context
                    .room_charges_list.Where(rc =>
                        rc.charge_month.Month == currentMonth
                        && rc.charge_month.Year == currentYear
                        && rc.record_status == "N"
                    )
                    .SumAsync(rc => rc.room_price);

                var roomChargesCollected = await _context
                    .room_charges_list.Where(rc =>
                        rc.charge_month.Month == currentMonth
                        && rc.charge_month.Year == currentYear
                        && rc.is_paid
                        && rc.record_status == "N"
                    )
                    .SumAsync(rc => rc.room_price);

                var waterIncome = await _context
                    .water_meters_list.Where(w =>
                        w.meter_date.Month == currentMonth
                        && w.meter_date.Year == currentYear
                        && w.record_status == "N"
                    )
                    .SumAsync(w => w.water_bill);

                var waterCollected = await _context
                    .water_meters_list.Where(w =>
                        w.meter_date.Month == currentMonth
                        && w.meter_date.Year == currentYear
                        && w.is_paid
                        && w.record_status == "N"
                    )
                    .SumAsync(w => w.water_bill);

                var electricIncome = await _context
                    .electric_meters_list.Where(e =>
                        e.meter_date.Month == currentMonth
                        && e.meter_date.Year == currentYear
                        && e.record_status == "N"
                    )
                    .SumAsync(e => e.electric_bill);

                var electricCollected = await _context
                    .electric_meters_list.Where(e =>
                        e.meter_date.Month == currentMonth
                        && e.meter_date.Year == currentYear
                        && e.is_paid
                        && e.record_status == "N"
                    )
                    .SumAsync(e => e.electric_bill);

                totalIncome = roomChargesIncome + waterIncome + electricIncome;
                totalCollected = roomChargesCollected + waterCollected + electricCollected;

                return Json(
                    new
                    {
                        success = true,
                        summary = new
                        {
                            totalRooms = totalRooms,
                            fullyPaidRooms = fullyPaidRooms,
                            partiallyPaidRooms = partiallyPaidRooms,
                            unpaidRooms = unpaidRooms,
                            totalIncome = totalIncome,
                            totalCollected = totalCollected,
                            totalPending = totalIncome - totalCollected,
                            collectionRate = totalIncome > 0
                                ? Math.Round((totalCollected / totalIncome) * 100, 1)
                                : 0,
                            monthYear = $"{currentMonth:D2}/{currentYear}",
                        },
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการดึงข้อมูลสรุป Mission");
                return Json(new { success = false, message = "ไม่สามารถดึงข้อมูลสรุปได้" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GenerateMonthlyCharges()
        {
            try
            {
                var currentMonth = DateTime.Now;
                var username = HttpContext.Session.GetString("Username") ?? "System";
                var createdCount = 0;

                // ดึงห้องทั้งหมดที่ใช้งาน
                var rooms = await _context
                    .rooms_list.Where(r => r.record_status == "N")
                    .ToListAsync();

                foreach (var room in rooms)
                {
                    // ตรวจสอบว่ามีค่าห้องสำหรับเดือนนี้แล้วหรือไม่
                    var existingCharge = await _context.room_charges_list.AnyAsync(rc =>
                        rc.room_id == room.room_id
                        && rc.charge_month.Month == currentMonth.Month
                        && rc.charge_month.Year == currentMonth.Year
                        && rc.record_status == "N"
                    );

                    if (!existingCharge)
                    {
                        // สร้างค่าห้องใหม่
                        var roomCharge = new room_charges_list
                        {
                            room_id = room.room_id,
                            charge_month = new DateTime(currentMonth.Year, currentMonth.Month, 1),
                            room_price = room.room_price,
                            due_date = new DateTime(currentMonth.Year, currentMonth.Month, 5), // วันที่ 5 ของเดือน
                            is_paid = false,
                            created_at = DateTime.Now,
                            created_by = username,
                            record_status = "N",
                        };

                        _context.room_charges_list.Add(roomCharge);
                        createdCount++;
                    }
                }

                await _context.SaveChangesAsync();

                return Json(
                    new
                    {
                        success = true,
                        message = $"สร้างค่าห้องประจำเดือนสำเร็จ จำนวน {createdCount} ห้อง",
                        createdCount = createdCount,
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการสร้างค่าห้องประจำเดือน");
                return Json(
                    new { success = false, message = "ไม่สามารถสร้างค่าห้องประจำเดือนได้" }
                );
            }
        }

        private string GetBillTypeName(string billType)
        {
            return billType.ToLower() switch
            {
                "room" => "ค่าห้อง",
                "water" => "ค่าน้ำ",
                "electric" => "ค่าไฟ",
                _ => "ค่าใช้จ่าย",
            };
        }
    }
}
