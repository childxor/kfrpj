using Microsoft.AspNetCore.Mvc;
using kfrpj.Data;
using kfrpj.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BCrypt.Net;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;

namespace kfrpj.Controllers
{
    public class AuthenController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthenController> _logger;

        public AuthenController(ApplicationDbContext context, ILogger<AuthenController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // แสดงหน้า Login
        public IActionResult Login()
        {
            return View();
        }

        // แสดงหน้า Register
        public IActionResult Register()
        {
            return View();
        }

        // จัดการการ Login
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            try
            {
                // ตรวจสอบข้อมูลที่ส่งมา
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                { 
                    TempData["LoginError"] = "กรุณากรอกชื่อผู้ใช้และรหัสผ่าน";
                    return RedirectToAction("Login");
                }

                // ค้นหาผู้ใช้จากฐานข้อมูล
                var user = await _context.sys_users
                    .Join(_context.sys_users_password, 
                        u => u.id, 
                        p => p.user_id, 
                        (u, p) => new { u, p })
                    .FirstOrDefaultAsync(up => up.u.username == username.Trim());

                // ตรวจสอบว่าพบผู้ใช้หรือไม่
                if (user == null)
                {
                    _logger.LogWarning($"พยายามเข้าสู่ระบบด้วยชื่อผู้ใช้ที่ไม่ถูกต้อง: {username}");
                    TempData["LoginError"] = "ไม่พบชื่อผู้ใช้นี้ในระบบ";
                    return RedirectToAction("Login");
                }

                // ตรวจสอบสถานะผู้ใช้
                if (user.u.record_status != "N")
                {
                    _logger.LogWarning($"พยายามเข้าสู่ระบบด้วยบัญชีที่ถูกระงับ: {username}");
                    TempData["LoginError"] = "บัญชีนี้ถูกระงับการใช้งาน กรุณาติดต่อผู้ดูแลระบบ";
                    return RedirectToAction("Login");
                }

                // ตรวจสอบรหัสผ่าน
                if (!BCrypt.Net.BCrypt.Verify(password, user.p.passwordhash))
                {
                    _logger.LogWarning($"พยายามเข้าสู่ระบบด้วยรหัสผ่านที่ไม่ถูกต้อง: {username}");
                    TempData["LoginError"] = "รหัสผ่านไม่ถูกต้อง";
                    return RedirectToAction("Login");
                }

                // อัพเดทเวลาล็อกอินล่าสุด
                user.u.last_login = DateTime.Now;
                await _context.SaveChangesAsync();

                // เก็บข้อมูลผู้ใช้ใน Session
                HttpContext.Session.SetString("UserId", user.u.id.ToString());
                HttpContext.Session.SetString("Username", user.u.username);
                HttpContext.Session.SetString("UserRole", user.u.role);
                HttpContext.Session.SetString("UserEmail", user.u.email);

                // บันทึก log การเข้าสู่ระบบสำเร็จ
                _logger.LogInformation($"ผู้ใช้ {username} เข้าสู่ระบบสำเร็จ");

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                // บันทึก error log
                _logger.LogError($"เกิดข้อผิดพลาดในการเข้าสู่ระบบ: {ex.Message}");
                TempData["LoginError"] = "เกิดข้อผิดพลาดในการเข้าสู่ระบบ กรุณาลองใหม่อีกครั้ง";
                return RedirectToAction("Login");
            }
        }

        // จัดการการ Register
        [HttpPost]
        public async Task<IActionResult> Register(string username, string password, string fullname, string email, string phone_number)
        {
            try
            {
                // ตรวจสอบว่ามีข้อมูลครบหรือไม่
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    return Json(new { success = false, message = "กรุณากรอกชื่อผู้ใช้และรหัสผ่าน" });
                }

                // ตรวจสอบว่ามีชื่อผู้ใช้นี้อยู่แล้วหรือไม่
                var existingUser = await _context.sys_users.FirstOrDefaultAsync(u => u.username == username.Trim());
                if (existingUser != null)
                {
                    return Json(new { success = false, message = "ชื่อผู้ใช้นี้มีอยู่ในระบบแล้ว" });
                }

                // เข้ารหัสรหัสผ่าน
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

                // สร้างผู้ใช้ใหม่
                var newUser = new sys_users
                {
                    username = username.Trim(),
                    password = passwordHash,
                    fullname = fullname?.Trim(),
                    email = email?.Trim(),
                    phone_number = phone_number?.Trim(),
                    role = "user", // กำหนดบทบาทเริ่มต้น
                    created_at = DateTime.Now,
                    record_status = "Y"
                };

                _context.sys_users.Add(newUser);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "ลงทะเบียนสำเร็จ" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการลงทะเบียน");
                return Json(new { success = false, message = "เกิดข้อผิดพลาดในการลงทะเบียน" });
            }
        }

        // ฟังก์ชันตรวจสอบรูปแบบอีเมล
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        // ออกจากระบบ
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // ตรวจสอบสถานะ Session
        [HttpGet]
        public IActionResult CheckSession()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            return Ok();
        }

        [HttpGet]
        public IActionResult LoginWithGoogle()
        {
            var redirectUrl = Url.Action("GoogleResponse", "Authen");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (result.Succeeded)
            {
                // เข้าสู่ระบบสำเร็จ
                // คุณสามารถบันทึกข้อมูลผู้ใช้ใน session หรือ database ได้ที่นี่
                return RedirectToAction("Index", "Home");
            }
            return RedirectToAction("Login");
        }
    }
}
