// Controllers/SettingsController.cs
using kfrpj.Data;
using kfrpj.Models.settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace kfrpj.Controllers
{
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(ApplicationDbContext context, ILogger<SettingsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var settings = await _context
                    .settings_list.Where(s => s.record_status == "N")
                    .OrderBy(s => s.category)
                    .ThenBy(s => s.setting_name)
                    .ToListAsync();

                // ถ้าไม่มีข้อมูล ให้สร้างค่าเริ่มต้น
                if (!settings.Any())
                {
                    await CreateDefaultSettings();
                    settings = await _context
                        .settings_list.Where(s => s.record_status == "N")
                        .OrderBy(s => s.category)
                        .ThenBy(s => s.setting_name)
                        .ToListAsync();
                }

                return View(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการดึงข้อมูลการตั้งค่า");
                TempData["Error"] = "ไม่สามารถดึงข้อมูลการตั้งค่าได้ กรุณาลองใหม่อีกครั้ง";
                return View(new List<settings_list>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSetting(int settingId, string value)
        {
            try
            {
                var setting = await _context.settings_list.FindAsync(settingId);
                if (setting == null)
                {
                    return Json(new { success = false, message = "ไม่พบการตั้งค่านี้" });
                }

                setting.setting_value = value;
                setting.updated_at = DateTime.Now;
                setting.updated_by = HttpContext.Session.GetString("Username") ?? "System";

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการอัปเดตการตั้งค่า");
                return Json(new { success = false, message = "ไม่สามารถอัปเดตการตั้งค่าได้" });
            }
        }

        private async Task CreateDefaultSettings()
        {
            var defaultSettings = new List<settings_list>
            {
                // การตั้งค่าค่าไฟ
                new settings_list
                {
                    setting_name = "ค่าไฟต่อหน่วย",
                    setting_key = "ELECTRIC_RATE_PER_UNIT",
                    setting_value = "5.00",
                    data_type = "decimal",
                    unit = "บาท/หน่วย",
                    description = "ราคาค่าไฟต่อหน่วยการไฟฟ้า",
                    category = "ค่าสาธารณูปโภค",
                    created_at = DateTime.Now,
                    created_by = "System",
                    record_status = "N",
                },
                // การตั้งค่าค่าน้ำ
                new settings_list
                {
                    setting_name = "ค่าน้ำต่อหน่วย",
                    setting_key = "WATER_RATE_PER_UNIT",
                    setting_value = "15.00",
                    data_type = "decimal",
                    unit = "บาท/หน่วย",
                    description = "ราคาค่าน้ำต่อหน่วยการประปา",
                    category = "ค่าสาธารณูปโภค",
                    created_at = DateTime.Now,
                    created_by = "System",
                    record_status = "N",
                },
                // การตั้งค่าค่าบริการเพิ่มเติม
                new settings_list
                {
                    setting_name = "ค่าบริการรายเดือน",
                    setting_key = "MONTHLY_SERVICE_FEE",
                    setting_value = "200.00",
                    data_type = "decimal",
                    unit = "บาท/เดือน",
                    description = "ค่าบริการรายเดือน (ขยะ, รักษาความปลอดภัย)",
                    category = "ค่าบริการ",
                    created_at = DateTime.Now,
                    created_by = "System",
                    record_status = "N",
                },
                // การตั้งค่าอื่นๆ
                new settings_list
                {
                    setting_name = "วันครบกำหนดชำระค่าเช่า",
                    setting_key = "RENT_DUE_DATE",
                    setting_value = "5",
                    data_type = "number",
                    unit = "วันของเดือน",
                    description = "วันที่ครบกำหนดชำระค่าเช่าของทุกเดือน",
                    category = "การเงิน",
                    created_at = DateTime.Now,
                    created_by = "System",
                    record_status = "N",
                },
                new settings_list
                {
                    setting_name = "ค่าปรับชำระล่าช้า",
                    setting_key = "LATE_PAYMENT_FEE",
                    setting_value = "50.00",
                    data_type = "decimal",
                    unit = "บาท/วัน",
                    description = "ค่าปรับสำหรับการชำระเงินล่าช้า",
                    category = "การเงิน",
                    created_at = DateTime.Now,
                    created_by = "System",
                    record_status = "N",
                },
            };

            _context.settings_list.AddRange(defaultSettings);
            await _context.SaveChangesAsync();
        }

        // Method สำหรับดึงค่าตั้งค่าใช้งานในส่วนอื่นๆ
        public async Task<string?> GetSettingValue(string settingKey)
        {
            var setting = await _context.settings_list.FirstOrDefaultAsync(s =>
                s.setting_key == settingKey && s.record_status == "N"
            );
            return setting?.setting_value;
        }

        public async Task<decimal> GetDecimalSetting(string settingKey, decimal defaultValue = 0)
        {
            var value = await GetSettingValue(settingKey);
            return decimal.TryParse(value, out var result) ? result : defaultValue;
        }
    }
}
