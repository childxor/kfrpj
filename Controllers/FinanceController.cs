using System;
using System.Globalization;
using System.Text;
using kfrpj.Data;
using kfrpj.Models.finance;
using kfrpj.Models.rooms;
using kfrpj.Models.settings;
using kfrpj.Models.tenants;
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

        public async Task<IActionResult> Index()
        {
            try
            {
                // โหลดรายการห้องสำหรับ dropdown
                ViewBag.Rooms = await _context
                    .rooms_list.Where(r => r.record_status == "N")
                    .OrderBy(r => r.room_name)
                    .Select(r => new
                    {
                        r.room_id,
                        r.room_name,
                        r.room_price,
                    })
                    .ToListAsync();

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการเข้าถึงหน้า Finance");
                TempData["Error"] = "เกิดข้อผิดพลาดในการเข้าถึงหน้า กรุณาลองใหม่อีกครั้ง";
                return View();
            }
        }

        // ====== ข้อมูลสถิติและแดชบอร์ด ======
        [HttpGet]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;

                // คำนวณรายได้รวมเดือนนี้
                var monthlyIncome = await CalculateMonthlyIncome(currentMonth, currentYear);

                // คำนวณยอดเก็บแล้ว
                var collectedAmount = await CalculateCollectedAmount(currentMonth, currentYear);

                // คำนวณยอดค้างชำระ
                var pendingAmount = monthlyIncome.total - collectedAmount.total;

                // นับจำนวนห้องที่ต้องเก็บเงิน
                var pendingRoomsCount = await GetPendingRoomsCount(currentMonth, currentYear);

                // ข้อมูลเพิ่มเติม
                var previousMonth = DateTime.Now.AddMonths(-1);
                var previousMonthIncome = await CalculateMonthlyIncome(
                    previousMonth.Month,
                    previousMonth.Year
                );
                var growthRate = CalculateGrowthRate(
                    monthlyIncome.total,
                    previousMonthIncome.total
                );

                return Json(
                    new
                    {
                        totalIncome = monthlyIncome.total,
                        totalCollected = collectedAmount.total,
                        totalPending = pendingAmount,
                        pendingRooms = pendingRoomsCount,
                        breakdown = new
                        {
                            roomCharges = new
                            {
                                income = monthlyIncome.roomCharges,
                                collected = collectedAmount.roomCharges,
                            },
                            waterBills = new
                            {
                                income = monthlyIncome.waterBills,
                                collected = collectedAmount.waterBills,
                            },
                            electricBills = new
                            {
                                income = monthlyIncome.electricBills,
                                collected = collectedAmount.electricBills,
                            },
                        },
                        analytics = new
                        {
                            collectionRate = monthlyIncome.total > 0
                                ? Math.Round((collectedAmount.total / monthlyIncome.total) * 100, 1)
                                : 0,
                            growthRate = growthRate,
                            averagePerRoom = pendingRoomsCount > 0
                                ? Math.Round(monthlyIncome.total / pendingRoomsCount, 0)
                                : 0,
                        },
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

        // ====== ข้อมูล Mission ======
        [HttpGet]
        public async Task<IActionResult> GetMissionData()
        {
            try
            {
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;

                _logger.LogInformation(
                    $"เริ่มดึงข้อมูล Mission สำหรับเดือน {currentMonth}/{currentYear}"
                );

                var rooms = await _context
                    .rooms_list.Where(r => r.record_status == "N")
                    .AsNoTracking()
                    .OrderBy(r => r.room_name)
                    .ToListAsync();

                _logger.LogInformation($"พบห้องทั้งหมด {rooms.Count} ห้อง");

                var missionData = new List<object>();

                foreach (var room in rooms.Take(100)) // จำกัดไว้ 100 ห้องแรกเพื่อทดสอบ
                {
                    try
                    {
                        var roomData = await BuildRoomMissionData(room, currentMonth, currentYear);
                        missionData.Add(roomData);
                    }
                    catch (Exception roomEx)
                    {
                        _logger.LogError(
                            roomEx,
                            $"เกิดข้อผิดพลาดในการดึงข้อมูลห้อง {room.room_name}"
                        );
                        // เพิ่มข้อมูลพื้นฐานแทน
                        missionData.Add(
                            new
                            {
                                roomId = room.room_id,
                                roomName = room.room_name,
                                tenantName = "ไม่ระบุ",
                                roomCharge = new
                                {
                                    amount = room.room_price,
                                    isPaid = false,
                                    id = (int?)null,
                                    dueDate = (DateTime?)null,
                                    daysOverdue = 0,
                                },
                                waterBill = new
                                {
                                    amount = 0m,
                                    isPaid = false,
                                    id = (int?)null,
                                    units = 0,
                                    meterDate = (DateTime?)null,
                                },
                                electricBill = new
                                {
                                    amount = 0m,
                                    isPaid = false,
                                    id = (int?)null,
                                    units = 0,
                                    meterDate = (DateTime?)null,
                                },
                                totalAmount = room.room_price,
                                paidAmount = 0m,
                                pendingAmount = room.room_price,
                                isFullyPaid = false,
                                isPartiallyPaid = false,
                                paymentProgress = 0,
                                priority = 2,
                                lastUpdateDate = DateTime.MinValue,
                            }
                        );
                    }
                }

                // เรียงลำดับตามสถานะ (ยังไม่จ่าย -> จ่ายบางส่วน -> จ่ายครบ)
                var sortedData = missionData.OrderBy(r => GetPriorityScore(r)).ToList();

                _logger.LogInformation($"ส่งข้อมูล Mission สำเร็จ จำนวน {sortedData.Count} ห้อง");

                return Json(new { success = true, rooms = sortedData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการดึงข้อมูล Mission");
                return Json(
                    new { success = false, message = "ไม่สามารถดึงข้อมูลได้: " + ex.Message }
                );
            }
        }

        // ====== การจัดการค่าน้ำ ======
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
                        room_id = w.room_id,
                        room_name = w.Room.room_name,
                        water_units = w.people_count,
                        water_bill = w.water_bill,
                        is_paid = w.is_paid,
                        meter_date = w.meter_date.ToString("yyyy-MM-dd"),
                        notes = w.notes,
                        created_at = w.created_at.ToString("dd/MM/yyyy HH:mm"),
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
        public async Task<IActionResult> CreateWaterBill([FromForm] water_meters_list waterBill)
        {
            try
            {
                // ตรวจสอบข้อมูลก่อนบันทึก
                var validation = await ValidateWaterBill(waterBill);
                if (!validation.isValid)
                {
                    return Json(new { success = false, message = validation.message });
                }

                var waterRate = await GetWaterRate();

                // คำนวณค่าน้ำตามจำนวนหน่วย
                waterBill.water_bill = waterBill.people_count * waterRate;
                waterBill.meter_date = DateTime.Now;
                waterBill.created_at = DateTime.Now;
                waterBill.created_by = HttpContext.Session.GetString("Username") ?? "System";
                waterBill.record_status = "N";

                _context.water_meters_list.Add(waterBill);
                await _context.SaveChangesAsync();

                // Log การทำงาน
                _logger.LogInformation(
                    $"สร้างข้อมูลค่าน้ำใหม่ ห้อง: {waterBill.room_id}, จำนวนคน: {waterBill.people_count}"
                );

                return Json(new { success = true, message = "บันทึกข้อมูลค่าน้ำเรียบร้อยแล้ว" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการสร้างข้อมูลค่าน้ำ");
                return Json(new { success = false, message = "ไม่สามารถบันทึกข้อมูลได้" });
            }
        }

        // ====== การจัดการค่าไฟ ======
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
                        room_id = e.room_id,
                        room_name = e.Room.room_name,
                        old_meter = e.old_meter,
                        new_meter = e.new_meter,
                        electric_units = e.electric_units,
                        electric_bill = e.electric_bill,
                        is_paid = e.is_paid,
                        meter_date = e.meter_date.ToString("yyyy-MM-dd"),
                        notes = e.notes,
                        created_at = e.created_at.ToString("dd/MM/yyyy HH:mm"),
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
                // ตรวจสอบข้อมูลก่อนบันทึก
                var validation = await ValidateElectricMeter(electricMeter);
                if (!validation.isValid)
                {
                    return Json(new { success = false, message = validation.message });
                }

                var electricRate = await GetElectricRate();

                electricMeter.electric_units = electricMeter.new_meter - electricMeter.old_meter;
                electricMeter.electric_bill = electricMeter.electric_units * electricRate;
                electricMeter.created_at = DateTime.Now;
                electricMeter.created_by = HttpContext.Session.GetString("Username") ?? "System";
                electricMeter.record_status = "N";

                _context.electric_meters_list.Add(electricMeter);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    $"สร้างข้อมูลค่าไฟใหม่ ห้อง: {electricMeter.room_id}, จำนวนหน่วย: {electricMeter.electric_units}"
                );

                return Json(new { success = true, message = "บันทึกข้อมูลค่าไฟเรียบร้อยแล้ว" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการสร้างข้อมูลค่าไฟ");
                return Json(new { success = false, message = "ไม่สามารถบันทึกข้อมูลได้" });
            }
        }

        // ====== การจัดการค่าห้อง ======
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
                        room_id = r.room_id,
                        room_name = r.Room.room_name,
                        tenant_name = GetTenantName(r.room_id), // จะสร้างฟังก์ชันนี้
                        room_price = r.room_price,
                        charge_month = r.charge_month.ToString("yyyy-MM"),
                        is_paid = r.is_paid,
                        due_date = r.due_date.ToString("yyyy-MM-dd"),
                        paid_date = r.payment_date != null
                            ? r.payment_date.Value.ToString("yyyy-MM-dd")
                            : null,
                        notes = r.notes,
                        created_at = r.created_at.ToString("dd/MM/yyyy HH:mm"),
                        days_overdue = r.is_paid
                            ? 0
                            : Math.Max(0, (DateTime.Now - r.due_date).Days),
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
                // ตรวจสอบว่า room_id มีอยู่จริงในตาราง rooms_list
                var roomExists = await _context.rooms_list.AnyAsync(r =>
                    r.room_id == roomCharge.room_id && r.record_status == "N"
                );

                if (!roomExists)
                {
                    return Json(new { success = false, message = "ไม่พบข้อมูลห้องที่ระบุ" });
                }

                // ตรวจสอบว่ามีค่าห้องสำหรับเดือนนี้แล้วหรือไม่
                var existingCharge = await _context.room_charges_list.AnyAsync(rc =>
                    rc.room_id == roomCharge.room_id
                    && rc.charge_month.Month == roomCharge.charge_month.Month
                    && rc.charge_month.Year == roomCharge.charge_month.Year
                    && rc.record_status == "N"
                );

                if (existingCharge)
                {
                    return Json(
                        new { success = false, message = "มีค่าห้องสำหรับเดือนนี้อยู่แล้ว" }
                    );
                }

                roomCharge.created_at = DateTime.Now;
                roomCharge.created_by = HttpContext.Session.GetString("Username") ?? "System";
                roomCharge.record_status = "N";

                _context.room_charges_list.Add(roomCharge);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    $"สร้างข้อมูลค่าห้องใหม่ ห้อง: {roomCharge.room_id}, เดือน: {roomCharge.charge_month:MM/yyyy}"
                );

                return Json(new { success = true, message = "บันทึกข้อมูลค่าห้องเรียบร้อยแล้ว" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการสร้างข้อมูลค่าห้อง");
                return Json(
                    new { success = false, message = "ไม่สามารถบันทึกข้อมูลได้: " + ex.Message }
                );
            }
        }

        // ====== การเก็บเงิน ======
        [HttpPost]
        public async Task<IActionResult> MarkRoomAsPaid(int roomId)
        {
            try
            {
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;
                var username = HttpContext.Session.GetString("Username") ?? "System";
                var updateCount = 0;

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
                    roomCharge.payment_date = DateTime.Now;
                    roomCharge.updated_at = DateTime.Now;
                    roomCharge.updated_by = username;
                    updateCount++;
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
                    updateCount++;
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
                    updateCount++;
                }

                if (updateCount > 0)
                {
                    await _context.SaveChangesAsync();

                    var room = await _context.rooms_list.FindAsync(roomId);
                    _logger.LogInformation(
                        $"เก็บเงินครบห้อง {room?.room_name} จำนวน {updateCount} รายการ"
                    );

                    return Json(
                        new
                        {
                            success = true,
                            message = $"เก็บเงินเรียบร้อยแล้ว จำนวน {updateCount} รายการ",
                        }
                    );
                }
                else
                {
                    return Json(new { success = false, message = "ไม่มีรายการที่ต้องเก็บเงิน" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"เกิดข้อผิดพลาดในการเก็บเงินห้อง {roomId}");
                return Json(new { success = false, message = "ไม่สามารถบันทึกการชำระเงินได้" });
            }
        }

        // ====== สร้างค่าห้องประจำเดือน ======
        [HttpPost]
        public async Task<IActionResult> GenerateMonthlyCharges()
        {
            try
            {
                var currentMonth = DateTime.Now;
                var username = HttpContext.Session.GetString("Username") ?? "System";
                var createdCount = 0;
                var skippedCount = 0;

                var rooms = await _context
                    .rooms_list.Where(r => r.record_status == "N" && r.room_status == "ไม่ว่าง")
                    .ToListAsync();

                foreach (var room in rooms)
                {
                    var existingCharge = await _context.room_charges_list.AnyAsync(rc =>
                        rc.room_id == room.room_id
                        && rc.charge_month.Month == currentMonth.Month
                        && rc.charge_month.Year == currentMonth.Year
                        && rc.record_status == "N"
                    );

                    if (!existingCharge)
                    {
                        // ดึงวันครบกำหนดจากการตั้งค่า
                        var dueDay = await GetRentDueDate();

                        var roomCharge = new room_charges_list
                        {
                            room_id = room.room_id,
                            charge_month = new DateTime(currentMonth.Year, currentMonth.Month, 1),
                            room_price = room.room_price,
                            due_date = new DateTime(currentMonth.Year, currentMonth.Month, dueDay),
                            is_paid = false,
                            created_at = DateTime.Now,
                            created_by = username,
                            record_status = "N",
                        };

                        _context.room_charges_list.Add(roomCharge);
                        createdCount++;
                    }
                    else
                    {
                        skippedCount++;
                    }
                }

                if (createdCount > 0)
                {
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation(
                    $"สร้างค่าห้องประจำเดือน: สร้างใหม่ {createdCount} ห้อง, ข้าม {skippedCount} ห้อง"
                );

                return Json(
                    new
                    {
                        success = true,
                        message = $"สร้างค่าห้องประจำเดือนสำเร็จ",
                        createdCount = createdCount,
                        skippedCount = skippedCount,
                        totalRooms = rooms.Count,
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

        // ====== ฟังก์ชันช่วยเหลือ ======
        private async Task<(
            decimal total,
            decimal roomCharges,
            decimal waterBills,
            decimal electricBills
        )> CalculateMonthlyIncome(int month, int year)
        {
            var roomCharges = await _context
                .room_charges_list.Where(rc =>
                    rc.charge_month.Month == month
                    && rc.charge_month.Year == year
                    && rc.record_status == "N"
                )
                .SumAsync(rc => rc.room_price);

            var waterBills = await _context
                .water_meters_list.Where(w =>
                    w.meter_date.Month == month
                    && w.meter_date.Year == year
                    && w.record_status == "N"
                )
                .SumAsync(w => w.water_bill);

            var electricBills = await _context
                .electric_meters_list.Where(e =>
                    e.meter_date.Month == month
                    && e.meter_date.Year == year
                    && e.record_status == "N"
                )
                .SumAsync(e => e.electric_bill);

            return (
                roomCharges + waterBills + electricBills,
                roomCharges,
                waterBills,
                electricBills
            );
        }

        private async Task<(
            decimal total,
            decimal roomCharges,
            decimal waterBills,
            decimal electricBills
        )> CalculateCollectedAmount(int month, int year)
        {
            var roomCharges = await _context
                .room_charges_list.Where(rc =>
                    rc.charge_month.Month == month
                    && rc.charge_month.Year == year
                    && rc.is_paid
                    && rc.record_status == "N"
                )
                .SumAsync(rc => rc.room_price);

            var waterBills = await _context
                .water_meters_list.Where(w =>
                    w.meter_date.Month == month
                    && w.meter_date.Year == year
                    && w.is_paid
                    && w.record_status == "N"
                )
                .SumAsync(w => w.water_bill);

            var electricBills = await _context
                .electric_meters_list.Where(e =>
                    e.meter_date.Month == month
                    && e.meter_date.Year == year
                    && e.is_paid
                    && e.record_status == "N"
                )
                .SumAsync(e => e.electric_bill);

            return (
                roomCharges + waterBills + electricBills,
                roomCharges,
                waterBills,
                electricBills
            );
        }

        private async Task<int> GetPendingRoomsCount(int month, int year)
        {
            var rooms = await _context
                .rooms_list.Where(r => r.record_status == "N")
                .Select(r => r.room_id)
                .ToListAsync();

            var pendingCount = 0;
            foreach (var roomId in rooms)
            {
                var hasUnpaidBills = await HasUnpaidBills(roomId, month, year);
                if (hasUnpaidBills)
                    pendingCount++;
            }

            return pendingCount;
        }

        private async Task<bool> HasUnpaidBills(int roomId, int month, int year)
        {
            var unpaidRoomCharge = await _context.room_charges_list.AnyAsync(rc =>
                rc.room_id == roomId
                && rc.charge_month.Month == month
                && rc.charge_month.Year == year
                && !rc.is_paid
                && rc.record_status == "N"
            );

            var unpaidWater = await _context.water_meters_list.AnyAsync(w =>
                w.room_id == roomId
                && w.meter_date.Month == month
                && w.meter_date.Year == year
                && !w.is_paid
                && w.record_status == "N"
            );

            var unpaidElectric = await _context.electric_meters_list.AnyAsync(e =>
                e.room_id == roomId
                && e.meter_date.Month == month
                && e.meter_date.Year == year
                && !e.is_paid
                && e.record_status == "N"
            );

            return unpaidRoomCharge || unpaidWater || unpaidElectric;
        }

        private async Task<object> BuildRoomMissionData(
            Models.rooms.rooms_list room,
            int month,
            int year
        )
        {
            // ดึงข้อมูลค่าห้อง
            var roomCharge = await _context
                .room_charges_list.Where(rc =>
                    rc.room_id == room.room_id
                    && rc.charge_month.Month == month
                    && rc.charge_month.Year == year
                    && rc.record_status == "N"
                )
                .OrderByDescending(rc => rc.created_at)
                .FirstOrDefaultAsync();

            // ดึงข้อมูลค่าน้ำ
            var waterBill = await _context
                .water_meters_list.Where(wm =>
                    wm.room_id == room.room_id
                    && wm.meter_date.Month == month
                    && wm.meter_date.Year == year
                    && wm.record_status == "N"
                )
                .OrderByDescending(wm => wm.created_at)
                .FirstOrDefaultAsync();

            // ดึงข้อมูลค่าไฟ
            var electricBill = await _context
                .electric_meters_list.Where(em =>
                    em.room_id == room.room_id
                    && em.meter_date.Month == month
                    && em.meter_date.Year == year
                    && em.record_status == "N"
                )
                .OrderByDescending(em => em.created_at)
                .FirstOrDefaultAsync();

            // ดึงข้อมูลผู้เช่า
            var tenantName = await GetTenantNameByRoomId(room.room_id);

            // คำนวณวันครบกำหนด
            var dueDate = roomCharge?.due_date ?? DateTime.Now;
            var daysOverdue =
                roomCharge?.is_paid != true ? Math.Max(0, (DateTime.Now - dueDate).Days) : 0;

            // คำนวณยอดรวม
            var totalAmount =
                (roomCharge?.room_price ?? 0)
                + (waterBill?.water_bill ?? 0)
                + (electricBill?.electric_bill ?? 0);

            // คำนวณยอดค้างชำระ
            var pendingAmount =
                (roomCharge?.is_paid != true ? roomCharge?.room_price ?? 0 : 0)
                + (waterBill?.is_paid != true ? waterBill?.water_bill ?? 0 : 0)
                + (electricBill?.is_paid != true ? electricBill?.electric_bill ?? 0 : 0);

            // คำนวณความคืบหน้า
            var paymentProgress =
                totalAmount > 0
                    ? Math.Round(((totalAmount - pendingAmount) / totalAmount) * 100, 0)
                    : 0;

            // สถานะการชำระ
            var isFullyPaid = pendingAmount == 0 && totalAmount > 0;
            var isPartiallyPaid = pendingAmount > 0 && pendingAmount < totalAmount;

            // จัดลำดับความสำคัญ
            var priority = GetRoomPriority(daysOverdue, isFullyPaid, isPartiallyPaid);

            return new
            {
                roomId = room.room_id,
                roomName = room.room_name,
                tenantName = tenantName,
                totalAmount = totalAmount,
                pendingAmount = pendingAmount,
                paymentProgress = paymentProgress,
                isFullyPaid = isFullyPaid,
                isPartiallyPaid = isPartiallyPaid,
                priority = priority,
                roomCharge = new
                {
                    id = roomCharge?.room_charge_id,
                    amount = roomCharge?.room_price ?? 0,
                    isPaid = roomCharge?.is_paid ?? false,
                    dueDate = roomCharge?.due_date,
                    daysOverdue = daysOverdue,
                },
                waterBill = new
                {
                    id = waterBill?.water_meter_id,
                    amount = waterBill?.water_bill ?? 0,
                    isPaid = waterBill?.is_paid ?? false,
                    units = waterBill?.people_count ?? 0,
                    meterDate = waterBill?.meter_date,
                },
                electricBill = new
                {
                    id = electricBill?.electric_meter_id,
                    amount = electricBill?.electric_bill ?? 0,
                    isPaid = electricBill?.is_paid ?? false,
                    units = electricBill?.electric_units ?? 0,
                    meterDate = electricBill?.meter_date,
                },
                lastUpdated = new[]
                {
                    roomCharge?.updated_at,
                    waterBill?.updated_at,
                    electricBill?.updated_at,
                }
                    .Where(d => d.HasValue)
                    .DefaultIfEmpty(DateTime.MinValue)
                    .Max(),
            };
        }

        private async Task<string> GetTenantNameByRoomId(int roomId)
        {
            try
            {
                var tenant = await _context
                    .room_tenant_rel.Include(rt => rt.Tenant)
                    .Where(rt =>
                        rt.room_id == roomId && rt.record_status == "N" && rt.status == "active"
                    )
                    .OrderByDescending(rt => rt.created_at)
                    .FirstOrDefaultAsync();

                return tenant?.Tenant?.name ?? "ไม่มีผู้เช่า";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"เกิดข้อผิดพลาดในการดึงข้อมูลผู้เช่าห้อง {roomId}");
                return "ไม่มีผู้เช่า";
            }
        }

        private string GetTenantName(int roomId)
        {
            try
            {
                var tenant = _context
                    .room_tenant_rel.Include(rt => rt.Tenant)
                    .Where(rt =>
                        rt.room_id == roomId && rt.record_status == "N" && rt.status == "active"
                    )
                    .OrderByDescending(rt => rt.created_at)
                    .FirstOrDefault();

                return tenant?.Tenant?.name ?? "ไม่มีผู้เช่า";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"เกิดข้อผิดพลาดในการดึงข้อมูลผู้เช่าห้อง {roomId}");
                return "ไม่มีผู้เช่า";
            }
        }

        private int GetRoomPriority(int daysOverdue, bool isFullyPaid, bool isPartiallyPaid)
        {
            if (isFullyPaid)
                return 3; // ลำดับสุดท้าย
            if (daysOverdue > 7)
                return 0; // ลำดับแรก (เร่งด่วน)
            if (daysOverdue > 0)
                return 1; // ลำดับที่สอง (ค้างชำระ)
            if (isPartiallyPaid)
                return 1; // ลำดับที่สอง (จ่ายบางส่วน)
            return 2; // ลำดับที่สาม (ยังไม่ถึงกำหนด)
        }

        private int GetPriorityScore(dynamic roomData)
        {
            return (int)roomData.priority;
        }

        private decimal CalculateGrowthRate(decimal currentValue, decimal previousValue)
        {
            if (previousValue == 0)
                return 0;
            return Math.Round(((currentValue - previousValue) / previousValue) * 100, 1);
        }

        // ====== ฟังก์ชันตรวจสอบข้อมูล ======
        private async Task<(bool isValid, string message)> ValidateWaterBill(
            water_meters_list waterBill
        )
        {
            if (waterBill.people_count <= 0)
            {
                return (false, "จำนวนคนต้องมากกว่า 0");
            }

            if (waterBill.people_count > 10)
            {
                return (false, "จำนวนคนเกินกว่าปกติ (มากกว่า 10 คน)");
            }

            // ตรวจสอบว่ามีข้อมูลซ้ำในเดือนเดียวกันหรือไม่
            var currentMonth = DateTime.Now;
            var existingRecord = await _context.water_meters_list.AnyAsync(w =>
                w.room_id == waterBill.room_id
                && w.meter_date.Month == currentMonth.Month
                && w.meter_date.Year == currentMonth.Year
                && w.record_status == "N"
            );

            if (existingRecord)
            {
                return (false, "มีข้อมูลค่าน้ำสำหรับเดือนนี้อยู่แล้ว");
            }

            return (true, "");
        }

        private async Task<(bool isValid, string message)> ValidateElectricMeter(
            electric_meters_list electricMeter
        )
        {
            if (electricMeter.new_meter <= electricMeter.old_meter)
            {
                return (false, "เลขมิเตอร์ใหม่ต้องมากกว่าเลขมิเตอร์เก่า");
            }

            if (electricMeter.new_meter - electricMeter.old_meter > 2000)
            {
                return (false, "จำนวนหน่วยใช้ไฟเกินกว่าปกติ (มากกว่า 2000 หน่วย)");
            }

            // ตรวจสอบว่ามีข้อมูลซ้ำในเดือนเดียวกันหรือไม่
            var existingRecord = await _context.electric_meters_list.AnyAsync(e =>
                e.room_id == electricMeter.room_id
                && e.meter_date.Month == electricMeter.meter_date.Month
                && e.meter_date.Year == electricMeter.meter_date.Year
                && e.record_status == "N"
            );

            if (existingRecord)
            {
                return (false, "มีข้อมูลค่าไฟสำหรับเดือนนี้อยู่แล้ว");
            }

            return (true, "");
        }

        // ====== ฟังก์ชันดึงค่าตั้งค่า ======
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

        private async Task<int> GetRentDueDate()
        {
            var setting = await _context.settings_list.FirstOrDefaultAsync(s =>
                s.setting_key == "RENT_DUE_DATE" && s.record_status == "N"
            );
            return int.TryParse(setting?.setting_value, out var day) ? day : 5;
        }

        [HttpGet]
        public async Task<IActionResult> GetRates()
        {
            try
            {
                var waterRate = await GetWaterRate();
                var electricRate = await GetElectricRate();
                var serviceFee = await GetServiceFee();

                return Json(
                    new
                    {
                        waterRate = waterRate,
                        electricRate = electricRate,
                        serviceFee = serviceFee,
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการดึงข้อมูลอัตราค่าใช้จ่าย");
                return Json(
                    new
                    {
                        waterRate = 15.00m,
                        electricRate = 5.00m,
                        serviceFee = 200.00m,
                    }
                );
            }
        }

        private async Task<decimal> GetServiceFee()
        {
            var setting = await _context.settings_list.FirstOrDefaultAsync(s =>
                s.setting_key == "MONTHLY_SERVICE_FEE" && s.record_status == "N"
            );
            return decimal.TryParse(setting?.setting_value, out var fee) ? fee : 200.00m;
        }

        // ====== API เพิ่มเติม ======
        [HttpGet]
        public async Task<IActionResult> GetPaymentHistory(int roomId, int months = 6)
        {
            try
            {
                var endDate = DateTime.Now;
                var startDate = endDate.AddMonths(-months);

                var roomCharges = await _context
                    .room_charges_list.Where(rc =>
                        rc.room_id == roomId
                        && rc.charge_month >= startDate
                        && rc.record_status == "N"
                    )
                    .OrderByDescending(rc => rc.charge_month)
                    .ToListAsync();

                var waterBills = await _context
                    .water_meters_list.Where(w =>
                        w.room_id == roomId && w.meter_date >= startDate && w.record_status == "N"
                    )
                    .OrderByDescending(w => w.meter_date)
                    .ToListAsync();

                var electricBills = await _context
                    .electric_meters_list.Where(e =>
                        e.room_id == roomId && e.meter_date >= startDate && e.record_status == "N"
                    )
                    .OrderByDescending(e => e.meter_date)
                    .ToListAsync();

                return Json(
                    new
                    {
                        success = true,
                        roomCharges = roomCharges,
                        waterBills = waterBills,
                        electricBills = electricBills,
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"เกิดข้อผิดพลาดในการดึงประวัติการชำระเงิน ห้อง {roomId}");
                return Json(new { success = false, message = "ไม่สามารถดึงข้อมูลประวัติได้" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOverdueReport()
        {
            try
            {
                var today = DateTime.Now.Date;

                var overdueRoomCharges = await _context
                    .room_charges_list.Include(rc => rc.Room)
                    .Where(rc => !rc.is_paid && rc.due_date < today && rc.record_status == "N")
                    .Select(rc => new
                    {
                        roomId = rc.room_id,
                        roomName = rc.Room.room_name,
                        amount = rc.room_price,
                        dueDate = rc.due_date,
                        daysOverdue = (today - rc.due_date).Days,
                        type = "ค่าห้อง",
                    })
                    .ToListAsync();

                var totalOverdue = overdueRoomCharges.Sum(r => r.amount);
                var avgDaysOverdue = overdueRoomCharges.Any()
                    ? Math.Round(overdueRoomCharges.Average(r => r.daysOverdue), 1)
                    : 0;

                return Json(
                    new
                    {
                        success = true,
                        overdueItems = overdueRoomCharges,
                        summary = new
                        {
                            totalAmount = totalOverdue,
                            totalItems = overdueRoomCharges.Count,
                            averageDaysOverdue = avgDaysOverdue,
                        },
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการสร้างรายงานค้างชำระ");
                return Json(new { success = false, message = "ไม่สามารถสร้างรายงานได้" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> BulkMarkAsPaid([FromBody] List<int> roomIds)
        {
            try
            {
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;
                var username = HttpContext.Session.GetString("Username") ?? "System";
                var successCount = 0;

                foreach (var roomId in roomIds)
                {
                    try
                    {
                        var result = await MarkRoomAsPaidInternal(
                            roomId,
                            currentMonth,
                            currentYear,
                            username
                        );
                        if (result > 0)
                            successCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"เกิดข้อผิดพลาดในการเก็บเงินห้อง {roomId}");
                    }
                }

                await _context.SaveChangesAsync();

                return Json(
                    new
                    {
                        success = true,
                        message = $"เก็บเงินเรียบร้อยแล้ว {successCount}/{roomIds.Count} ห้อง",
                        successCount = successCount,
                        totalCount = roomIds.Count,
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการเก็บเงินหลายห้อง");
                return Json(new { success = false, message = "ไม่สามารถเก็บเงินได้" });
            }
        }

        private async Task<int> MarkRoomAsPaidInternal(
            int roomId,
            int month,
            int year,
            string username
        )
        {
            var updateCount = 0;

            // อัพเดทค่าห้อง
            var roomCharge = await _context
                .room_charges_list.Where(rc =>
                    rc.room_id == roomId
                    && rc.charge_month.Month == month
                    && rc.charge_month.Year == year
                    && rc.record_status == "N"
                    && !rc.is_paid
                )
                .FirstOrDefaultAsync();

            if (roomCharge != null)
            {
                roomCharge.is_paid = true;
                roomCharge.payment_date = DateTime.Now;
                roomCharge.updated_at = DateTime.Now;
                roomCharge.updated_by = username;
                updateCount++;
            }

            // อัพเดทค่าน้ำ
            var waterBill = await _context
                .water_meters_list.Where(w =>
                    w.room_id == roomId
                    && w.meter_date.Month == month
                    && w.meter_date.Year == year
                    && w.record_status == "N"
                    && !w.is_paid
                )
                .FirstOrDefaultAsync();

            if (waterBill != null)
            {
                waterBill.is_paid = true;
                waterBill.updated_at = DateTime.Now;
                waterBill.updated_by = username;
                updateCount++;
            }

            // อัพเดทค่าไฟ
            var electricBill = await _context
                .electric_meters_list.Where(e =>
                    e.room_id == roomId
                    && e.meter_date.Month == month
                    && e.meter_date.Year == year
                    && e.record_status == "N"
                    && !e.is_paid
                )
                .FirstOrDefaultAsync();

            if (electricBill != null)
            {
                electricBill.is_paid = true;
                electricBill.updated_at = DateTime.Now;
                electricBill.updated_by = username;
                updateCount++;
            }

            return updateCount;
        }

        // ====== API สำหรับการลบข้อมูล ======
        [HttpDelete]
        public async Task<IActionResult> DeleteWaterMeter(int id)
        {
            try
            {
                var waterMeter = await _context.water_meters_list.FindAsync(id);
                if (waterMeter == null)
                    return Json(new { success = false, message = "ไม่พบข้อมูลที่ต้องการลบ" });

                waterMeter.record_status = "D";
                waterMeter.updated_at = DateTime.Now;
                waterMeter.updated_by = HttpContext.Session.GetString("Username") ?? "System";

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "ลบข้อมูลเรียบร้อยแล้ว" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"เกิดข้อผิดพลาดในการลบข้อมูลค่าน้ำ ID: {id}");
                return Json(new { success = false, message = "ไม่สามารถลบข้อมูลได้" });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteElectricMeter(int id)
        {
            try
            {
                var electricMeter = await _context.electric_meters_list.FindAsync(id);
                if (electricMeter == null)
                    return Json(new { success = false, message = "ไม่พบข้อมูลที่ต้องการลบ" });

                electricMeter.record_status = "D";
                electricMeter.updated_at = DateTime.Now;
                electricMeter.updated_by = HttpContext.Session.GetString("Username") ?? "System";

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "ลบข้อมูลเรียบร้อยแล้ว" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"เกิดข้อผิดพลาดในการลบข้อมูลค่าไฟ ID: {id}");
                return Json(new { success = false, message = "ไม่สามารถลบข้อมูลได้" });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteRoomCharge(int id)
        {
            try
            {
                var roomCharge = await _context.room_charges_list.FindAsync(id);
                if (roomCharge == null)
                    return Json(new { success = false, message = "ไม่พบข้อมูลที่ต้องการลบ" });

                roomCharge.record_status = "D";
                roomCharge.updated_at = DateTime.Now;
                roomCharge.updated_by = HttpContext.Session.GetString("Username") ?? "System";

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "ลบข้อมูลเรียบร้อยแล้ว" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"เกิดข้อผิดพลาดในการลบข้อมูลค่าห้อง ID: {id}");
                return Json(new { success = false, message = "ไม่สามารถลบข้อมูลได้" });
            }
        }

        // ====== API สำหรับการพิมพ์และส่งออกข้อมูล ======
        [HttpGet]
        public async Task<IActionResult> ExportMissionData()
        {
            try
            {
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;

                var rooms = await _context
                    .rooms_list.Where(r => r.record_status == "N")
                    .OrderBy(r => r.room_name)
                    .ToListAsync();

                var exportData = new List<object>();

                foreach (var room in rooms)
                {
                    var roomData = await BuildRoomMissionData(room, currentMonth, currentYear);
                    exportData.Add(roomData);
                }

                var csv = GenerateCSV(exportData);
                var bytes = Encoding.UTF8.GetBytes(csv);

                return File(
                    bytes,
                    "text/csv",
                    $"Mission_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการส่งออกข้อมูล Mission");
                return Json(new { success = false, message = "ไม่สามารถส่งออกข้อมูลได้" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> PrintMissionList()
        {
            try
            {
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;

                var rooms = await _context
                    .rooms_list.Where(r => r.record_status == "N")
                    .OrderBy(r => r.room_name)
                    .ToListAsync();

                var missionData = new List<object>();

                foreach (var room in rooms)
                {
                    var roomData = await BuildRoomMissionData(room, currentMonth, currentYear);
                    missionData.Add(roomData);
                }

                var html = GeneratePrintableHTML(missionData);

                return Content(html, "text/html", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการสร้างรายการพิมพ์");
                return BadRequest("ไม่สามารถสร้างรายการพิมพ์ได้");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GenerateRoomReceipt(int roomId)
        {
            try
            {
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;

                var room = await _context.rooms_list.FindAsync(roomId);
                if (room == null)
                    return BadRequest("ไม่พบข้อมูลห้อง");

                var roomData = await BuildRoomMissionData(room, currentMonth, currentYear);
                var html = GenerateReceiptHTML(roomData);

                return Content(html, "text/html", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"เกิดข้อผิดพลาดในการสร้างใบเสร็จห้อง {roomId}");
                return BadRequest("ไม่สามารถสร้างใบเสร็จได้");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRoomDetail(int roomId)
        {
            try
            {
                var room = await _context.rooms_list.FindAsync(roomId);
                if (room == null)
                    return Json(new { success = false, message = "ไม่พบข้อมูลห้อง" });

                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;

                var roomDetail = await BuildDetailedRoomData(room, currentMonth, currentYear);

                return Json(new { success = true, data = roomDetail });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"เกิดข้อผิดพลาดในการดึงรายละเอียดห้อง {roomId}");
                return Json(new { success = false, message = "ไม่สามารถดึงข้อมูลได้" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateWaterBill(int id)
        {
            try
            {
                var existingWaterBill = await _context.water_meters_list.FindAsync(id);
                if (existingWaterBill == null)
                    return Json(new { success = false, message = "ไม่พบข้อมูลที่ต้องการแก้ไข" });

                // ดึงข้อมูลจาก form
                var roomId = int.Parse(Request.Form["room_id"]);
                var peopleCount = int.Parse(Request.Form["people_count"]);
                var notes = Request.Form["notes"];

                // ตรวจสอบข้อมูล
                if (peopleCount <= 0)
                    return Json(new { success = false, message = "จำนวนคนต้องมากกว่า 0" });

                var waterRate = await GetWaterRate();

                existingWaterBill.room_id = roomId;
                existingWaterBill.people_count = peopleCount; // จำนวนคน
                existingWaterBill.water_bill = peopleCount * waterRate;
                existingWaterBill.notes = notes;
                existingWaterBill.updated_at = DateTime.Now;
                existingWaterBill.updated_by =
                    HttpContext.Session.GetString("Username") ?? "System";

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "แก้ไขข้อมูลค่าน้ำเรียบร้อยแล้ว" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"เกิดข้อผิดพลาดในการแก้ไขข้อมูลค่าน้ำ ID: {id}");
                return Json(new { success = false, message = "ไม่สามารถแก้ไขข้อมูลได้" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateElectricMeter(
            int id,
            [FromForm] electric_meters_list electricMeter
        )
        {
            try
            {
                var existingElectricMeter = await _context.electric_meters_list.FindAsync(id);
                if (existingElectricMeter == null)
                    return Json(new { success = false, message = "ไม่พบข้อมูลที่ต้องการแก้ไข" });

                var validation = await ValidateElectricMeter(electricMeter, id);
                if (!validation.isValid)
                    return Json(new { success = false, message = validation.message });

                var electricRate = await GetElectricRate();

                existingElectricMeter.room_id = electricMeter.room_id;
                existingElectricMeter.meter_date = electricMeter.meter_date;
                existingElectricMeter.old_meter = electricMeter.old_meter;
                existingElectricMeter.new_meter = electricMeter.new_meter;
                existingElectricMeter.electric_units =
                    existingElectricMeter.new_meter - existingElectricMeter.old_meter;
                existingElectricMeter.electric_bill =
                    existingElectricMeter.electric_units * electricRate;
                existingElectricMeter.notes = electricMeter.notes;
                existingElectricMeter.updated_at = DateTime.Now;
                existingElectricMeter.updated_by =
                    HttpContext.Session.GetString("Username") ?? "System";

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "แก้ไขข้อมูลค่าไฟเรียบร้อยแล้ว" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"เกิดข้อผิดพลาดในการแก้ไขข้อมูลค่าไฟ ID: {id}");
                return Json(new { success = false, message = "ไม่สามารถแก้ไขข้อมูลได้" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateRoomCharge(
            int id,
            [FromForm] room_charges_list roomCharge
        )
        {
            try
            {
                var existingRoomCharge = await _context.room_charges_list.FindAsync(id);
                if (existingRoomCharge == null)
                    return Json(new { success = false, message = "ไม่พบข้อมูลที่ต้องการแก้ไข" });

                // ตรวจสอบว่ามีค่าห้องสำหรับเดือนนี้แล้วหรือไม่ (ยกเว้นรายการปัจจุบัน)
                var existingCharge = await _context.room_charges_list.AnyAsync(rc =>
                    rc.room_id == roomCharge.room_id
                    && rc.charge_month.Month == roomCharge.charge_month.Month
                    && rc.charge_month.Year == roomCharge.charge_month.Year
                    && rc.record_status == "N"
                    && rc.room_charge_id != id
                );

                if (existingCharge)
                {
                    return Json(
                        new { success = false, message = "มีค่าห้องสำหรับเดือนนี้อยู่แล้ว" }
                    );
                }

                existingRoomCharge.room_id = roomCharge.room_id;
                existingRoomCharge.charge_month = roomCharge.charge_month;
                existingRoomCharge.room_price = roomCharge.room_price;
                existingRoomCharge.due_date = roomCharge.due_date;
                existingRoomCharge.notes = roomCharge.notes;
                existingRoomCharge.updated_at = DateTime.Now;
                existingRoomCharge.updated_by =
                    HttpContext.Session.GetString("Username") ?? "System";

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "แก้ไขข้อมูลค่าห้องเรียบร้อยแล้ว" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"เกิดข้อผิดพลาดในการแก้ไขข้อมูลค่าห้อง ID: {id}");
                return Json(new { success = false, message = "ไม่สามารถแก้ไขข้อมูลได้" });
            }
        }

        // ====== ฟังก์ชันช่วยเหลือสำหรับการพิมพ์และส่งออก ======
        private string GenerateCSV(List<object> data)
        {
            var csv = new StringBuilder();
            csv.AppendLine("ห้อง,ผู้เช่า,ค่าห้อง,ค่าน้ำ,ค่าไฟ,รวมทั้งหมด,สถานะ");

            foreach (dynamic room in data)
            {
                var status =
                    room.isFullyPaid ? "จ่ายครบ"
                    : room.isPartiallyPaid ? "จ่ายบางส่วน"
                    : "ยังไม่จ่าย";
                csv.AppendLine(
                    $"{room.roomName},{room.tenantName},{room.roomCharge.amount},{room.waterBill.amount},{room.electricBill.amount},{room.totalAmount},{status}"
                );
            }

            return csv.ToString();
        }

        private string GeneratePrintableHTML(List<object> data)
        {
            var html = new StringBuilder();

            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset='utf-8'>");
            html.AppendLine(
                "<title>Mission เก็บเงิน - "
                    + DateTime.Now.ToString("MMMM yyyy", new CultureInfo("th-TH"))
                    + "</title>"
            );
            html.AppendLine("<style>");
            html.AppendLine(
                @"
                body { font-family: 'THSarabunNew', sans-serif; font-size: 16px; margin: 20px; }
                h1 { text-align: center; color: #333; margin-bottom: 30px; }
                table { width: 100%; border-collapse: collapse; margin-bottom: 20px; }
                th, td { border: 1px solid #ddd; padding: 8px; text-align: center; }
                th { background-color: #f5f5f5; font-weight: bold; }
                .text-success { color: #28a745; }
                .text-danger { color: #dc3545; }
                .text-warning { color: #ffc107; }
                .footer { margin-top: 30px; text-align: center; color: #666; }
                @media print {
                    body { margin: 0; }
                    .no-print { display: none; }
                }
            "
            );
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            html.AppendLine(
                $"<h1>Mission เก็บเงิน - {DateTime.Now.ToString("MMMM yyyy", new CultureInfo("th-TH"))}</h1>"
            );
            html.AppendLine("<table>");
            html.AppendLine("<thead>");
            html.AppendLine("<tr>");
            html.AppendLine(
                "<th>ห้อง</th><th>ผู้เช่า</th><th>ค่าห้อง</th><th>ค่าน้ำ</th><th>ค่าไฟ</th><th>รวมทั้งหมด</th><th>สถานะ</th>"
            );
            html.AppendLine("</tr>");
            html.AppendLine("</thead>");
            html.AppendLine("<tbody>");

            decimal totalAmount = 0;

            foreach (dynamic room in data)
            {
                var statusClass =
                    room.isFullyPaid ? "text-success"
                    : room.isPartiallyPaid ? "text-warning"
                    : "text-danger";
                var statusText =
                    room.isFullyPaid ? "จ่ายครบ"
                    : room.isPartiallyPaid ? "จ่ายบางส่วน"
                    : "ยังไม่จ่าย";

                totalAmount += (decimal)room.pendingAmount;

                html.AppendLine("<tr>");
                html.AppendLine($"<td>{room.roomName}</td>");
                html.AppendLine($"<td>{room.tenantName}</td>");
                html.AppendLine($"<td>{((decimal)room.roomCharge.amount).ToString("N0")}</td>");
                html.AppendLine($"<td>{((decimal)room.waterBill.amount).ToString("N0")}</td>");
                html.AppendLine($"<td>{((decimal)room.electricBill.amount).ToString("N0")}</td>");
                html.AppendLine(
                    $"<td><strong>{((decimal)room.totalAmount).ToString("N0")}</strong></td>"
                );
                html.AppendLine($"<td class='{statusClass}'>{statusText}</td>");
                html.AppendLine("</tr>");
            }

            html.AppendLine("</tbody>");
            html.AppendLine("</table>");

            html.AppendLine($"<div class='footer'>");
            html.AppendLine(
                $"<p>รวมยอดค้างชำระทั้งหมด: <strong>{totalAmount.ToString("N0")} บาท</strong></p>"
            );
            html.AppendLine($"<p>พิมพ์เมื่อ: {DateTime.Now.ToString("dd/MM/yyyy HH:mm")}</p>");
            html.AppendLine("</div>");

            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }

        private string GenerateReceiptHTML(dynamic roomData)
        {
            var html = new StringBuilder();
            var currentDate = DateTime.Now.ToString("dd/MM/yyyy");
            var currentMonth = DateTime.Now.ToString("MMMM yyyy", new CultureInfo("th-TH"));
            var printTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset='utf-8'>");
            html.AppendLine($"<title>ใบเสร็จรับเงิน - ห้อง {roomData.roomName}</title>");
            html.AppendLine("<style>");
            html.AppendLine(
                @"
                body { font-family: 'THSarabunNew', sans-serif; font-size: 18px; margin: 20px; }
                .header { text-align: center; margin-bottom: 30px; }
                .receipt-details { margin-bottom: 20px; }
                table { width: 100%; border-collapse: collapse; margin-bottom: 20px; }
                th, td { border: 1px solid #ddd; padding: 10px; text-align: center; }
                th { background-color: #f5f5f5; font-weight: bold; }
                .total { font-weight: bold; font-size: 20px; }
                .text-end { text-align: right; }
                .footer { margin-top: 30px; text-align: center; }
                @media print { body { margin: 0; } }
            "
            );
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            html.AppendLine("<div class='header'>");
            html.AppendLine("<h2>ใบเสร็จรับเงิน</h2>");
            html.AppendLine($"<p>วันที่: {currentDate}</p>");
            html.AppendLine("</div>");

            html.AppendLine("<div class='receipt-details'>");
            html.AppendLine($"<p><strong>ห้อง:</strong> {roomData.roomName}</p>");
            html.AppendLine($"<p><strong>ผู้เช่า:</strong> {roomData.tenantName}</p>");
            html.AppendLine($"<p><strong>เดือน:</strong> {currentMonth}</p>");
            html.AppendLine("</div>");

            html.AppendLine("<table>");
            html.AppendLine("<thead>");
            html.AppendLine("<tr><th>รายการ</th><th>จำนวนเงิน (บาท)</th></tr>");
            html.AppendLine("</thead>");
            html.AppendLine("<tbody>");
            html.AppendLine(
                $"<tr><td>ค่าห้อง</td><td class='text-end'>{((decimal)roomData.roomCharge.amount).ToString("N0")}</td></tr>"
            );
            html.AppendLine(
                $"<tr><td>ค่าน้ำ ({roomData.waterBill.units} หน่วย)</td><td class='text-end'>{((decimal)roomData.waterBill.amount).ToString("N0")}</td></tr>"
            );
            html.AppendLine(
                $"<tr><td>ค่าไฟ ({roomData.electricBill.units} หน่วย)</td><td class='text-end'>{((decimal)roomData.electricBill.amount).ToString("N0")}</td></tr>"
            );
            html.AppendLine(
                $"<tr class='total'><td>รวมทั้งหมด</td><td class='text-end'>{((decimal)roomData.totalAmount).ToString("N0")}</td></tr>"
            );
            html.AppendLine("</tbody>");
            html.AppendLine("</table>");

            html.AppendLine("<div class='footer'>");
            html.AppendLine("<p>ขอบคุณที่ชำระเงินตรงเวลา</p>");
            html.AppendLine($"<p><small>พิมพ์เมื่อ: {printTime}</small></p>");
            html.AppendLine("</div>");

            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }

        private async Task<object> BuildDetailedRoomData(rooms_list room, int month, int year)
        {
            var roomData = await BuildRoomMissionData(room, month, year);

            // เพิ่มข้อมูลประวัติการชำระ 6 เดือนย้อนหลัง
            var paymentHistory = await GetPaymentHistoryInternal(room.room_id, 6);

            return new { roomInfo = roomData, paymentHistory = paymentHistory };
        }

        private async Task<object> GetPaymentHistoryInternal(int roomId, int months)
        {
            var endDate = DateTime.Now;
            var startDate = endDate.AddMonths(-months);

            var roomCharges = await _context
                .room_charges_list.Where(rc =>
                    rc.room_id == roomId && rc.charge_month >= startDate && rc.record_status == "N"
                )
                .OrderByDescending(rc => rc.charge_month)
                .Select(rc => new
                {
                    month = rc.charge_month.ToString("MM/yyyy"),
                    amount = rc.room_price,
                    isPaid = rc.is_paid,
                    paidDate = rc.payment_date,
                    type = "ค่าห้อง",
                })
                .ToListAsync();

            var waterBills = await _context
                .water_meters_list.Where(w =>
                    w.room_id == roomId && w.meter_date >= startDate && w.record_status == "N"
                )
                .OrderByDescending(w => w.meter_date)
                .Select(w => new
                {
                    month = w.meter_date.ToString("MM/yyyy"),
                    amount = w.water_bill,
                    units = w.people_count,
                    isPaid = w.is_paid,
                    type = "ค่าน้ำ",
                })
                .ToListAsync();

            var electricBills = await _context
                .electric_meters_list.Where(e =>
                    e.room_id == roomId && e.meter_date >= startDate && e.record_status == "N"
                )
                .OrderByDescending(e => e.meter_date)
                .Select(e => new
                {
                    month = e.meter_date.ToString("MM/yyyy"),
                    amount = e.electric_bill,
                    units = e.electric_units,
                    isPaid = e.is_paid,
                    type = "ค่าไฟ",
                })
                .ToListAsync();

            return new
            {
                roomCharges = roomCharges,
                waterBills = waterBills,
                electricBills = electricBills,
            };
        }

        // ปรับปรุงฟังก์ชัน ValidateWaterMeter และ ValidateElectricMeter ให้รองรับการแก้ไข
        private async Task<(bool isValid, string message)> ValidateWaterMeter(
            water_meters_list waterMeter,
            int? excludeId = null
        )
        {
            var query = _context.water_meters_list.Where(w =>
                w.room_id == waterMeter.room_id
                && w.meter_date.Month == waterMeter.meter_date.Month
                && w.meter_date.Year == waterMeter.meter_date.Year
                && w.record_status == "N"
            );

            if (excludeId.HasValue)
            {
                query = query.Where(w => w.water_meter_id != excludeId.Value);
            }

            var existingRecord = await query.AnyAsync();

            if (existingRecord)
            {
                return (false, "มีข้อมูลค่าน้ำสำหรับเดือนนี้อยู่แล้ว");
            }

            return (true, "");
        }

        private async Task<(bool isValid, string message)> ValidateElectricMeter(
            electric_meters_list electricMeter,
            int? excludeId = null
        )
        {
            if (electricMeter.new_meter <= electricMeter.old_meter)
            {
                return (false, "เลขมิเตอร์ใหม่ต้องมากกว่าเลขมิเตอร์เก่า");
            }

            if (electricMeter.new_meter - electricMeter.old_meter > 2000)
            {
                return (false, "จำนวนหน่วยใช้ไฟเกินกว่าปกติ (มากกว่า 2000 หน่วย)");
            }

            var query = _context.electric_meters_list.Where(e =>
                e.room_id == electricMeter.room_id
                && e.meter_date.Month == electricMeter.meter_date.Month
                && e.meter_date.Year == electricMeter.meter_date.Year
                && e.record_status == "N"
            );

            if (excludeId.HasValue)
            {
                query = query.Where(e => e.electric_meter_id != excludeId.Value);
            }

            var existingRecord = await query.AnyAsync();

            if (existingRecord)
            {
                return (false, "มีข้อมูลค่าไฟสำหรับเดือนนี้อยู่แล้ว");
            }

            return (true, "");
        }

        [HttpGet]
        public async Task<IActionResult> GetRoomChargeDetail(int id)
        {
            try
            {
                var roomCharge = await _context.room_charges_list
                    .Where(rc => rc.room_charge_id == id && rc.record_status == "N")
                    .Select(rc => new
                    {
                        rc.room_charge_id,
                        rc.room_id,
                        rc.charge_month,
                        rc.room_price,
                        rc.due_date,
                        rc.is_paid,
                        rc.notes
                    })
                    .FirstOrDefaultAsync();

                if (roomCharge == null)
                {
                    return Json(new { success = false, message = "ไม่พบข้อมูลค่าห้อง" });
                }

                return Json(new { success = true, data = roomCharge });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"เกิดข้อผิดพลาดในการดึงข้อมูลค่าห้อง ID: {id}");
                return Json(new { success = false, message = "ไม่สามารถดึงข้อมูลได้" });
            }
        }
    }
}
