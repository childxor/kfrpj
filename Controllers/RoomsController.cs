using Microsoft.AspNetCore.Mvc;
using kfrpj.Data;
using kfrpj.Models.rooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace kfrpj.Controllers
{
    public class RoomsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RoomsController> _logger; 

        public RoomsController(ApplicationDbContext context, ILogger<RoomsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // แสดงรายการห้องทั้งหมด
        public async Task<IActionResult> Index()
        {
            try
            {
                var rooms = await _context.rooms_list
                    .Where(r => r.record_status == "N")
                    .OrderBy(r => r.room_name) 
                    .ToListAsync();
                return View(rooms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการดึงข้อมูลห้อง");
                TempData["Error"] = "ไม่สามารถดึงข้อมูลห้องได้ กรุณาลองใหม่อีกครั้ง";
                return View(new List<rooms_list>());
            }
        }

        // ดึงข้อมูลห้องจากฐานข้อมูล
        [HttpPost]
        public async Task<IActionResult> GetRooms()
        {
            try
            {
                var rooms = await _context.rooms_list
                    .Where(r => r.record_status == "N")
                    .OrderBy(r => r.room_name)
                    .ToListAsync();

                _logger.LogInformation($"ดึงข้อมูลห้องสำเร็จ จำนวน {rooms.Count} รายการ");
                return Json(new { data = rooms });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการดึงข้อมูลห้อง");
                return StatusCode(500, new { error = "ไม่สามารถดึงข้อมูลห้องได้ กรุณาลองใหม่อีกครั้ง" });
            }
        }

        // ดึงข้อมูลห้องตาม ID
        [HttpGet]
        public async Task<IActionResult> GetRoom(int id)
        {
            try
            {
                var room = await _context.rooms_list.FindAsync(id);
                if (room == null)
                {
                    return NotFound();
                }
                return Json(room);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"เกิดข้อผิดพลาดในการดึงข้อมูลห้อง ID: {id}");
                return StatusCode(500, new { error = "ไม่สามารถดึงข้อมูลห้องได้ กรุณาลองใหม่อีกครั้ง" });
            }
        }

        // เพิ่มห้องใหม่
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] rooms_list room)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    room.created_at = DateTime.Now;
                    room.created_by = HttpContext.Session.GetString("Username") ?? "System";
                    room.record_status = "N";
                    _context.Add(room);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true });
                }
                return BadRequest(ModelState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการเพิ่มห้องใหม่");
                return StatusCode(500, new { error = "ไม่สามารถเพิ่มห้องใหม่ได้ กรุณาลองใหม่อีกครั้ง" });
            }
        }

        // แก้ไขข้อมูลห้อง
        [HttpPut]
        public async Task<IActionResult> Edit(int id, [FromForm] rooms_list room)
        {
            try
            {
                if (id != room.room_id)
                {
                    return BadRequest();
                }

                if (ModelState.IsValid)
                {
                    var existingRoom = await _context.rooms_list.FindAsync(id);
                    if (existingRoom == null)
                    {
                        return NotFound();
                    }

                    // อัปเดตเฉพาะฟิลด์ที่แก้ไข
                    existingRoom.room_name = room.room_name;
                    existingRoom.room_type = room.room_type;
                    existingRoom.room_price = room.room_price;
                    existingRoom.room_status = room.room_status;
                    existingRoom.room_description = room.room_description;
                    existingRoom.updated_at = DateTime.Now;
                    existingRoom.updated_by = HttpContext.Session.GetString("Username") ?? "System";

                    await _context.SaveChangesAsync();
                    return Json(new { success = true });
                }
                return BadRequest(ModelState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"เกิดข้อผิดพลาดในการแก้ไขข้อมูลห้อง ID: {id}");
                return StatusCode(500, new { error = "ไม่สามารถแก้ไขข้อมูลห้องได้ กรุณาลองใหม่อีกครั้ง" });
            }
        }

        // ลบห้อง
        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var room = await _context.rooms_list.FindAsync(id);
                if (room == null)
                {
                    return NotFound();
                }

                room.record_status = "N";
                room.updated_at = DateTime.Now;
                room.updated_by = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"เกิดข้อผิดพลาดในการลบห้อง ID: {id}");
                return StatusCode(500, new { error = "ไม่สามารถลบห้องได้ กรุณาลองใหม่อีกครั้ง" });
            }
        }

        // ตรวจสอบว่าห้องมีอยู่จริงหรือไม่
        private bool RoomExists(int id)
        {
            return _context.rooms_list.Any(e => e.room_id == id && e.record_status == "Y");
        }

        [HttpPost]
        public async Task<IActionResult> Duplicate(int id)
        {
            var room = await _context.rooms_list.FindAsync(id);
            if (room == null)
                return Json(new { success = false, error = "ไม่พบข้อมูลห้อง" });

            var newRoom = new rooms_list
            {
                room_name = room.room_name + " (คัดลอก)",
                room_type = room.room_type,
                room_price = room.room_price,
                room_status = room.room_status,
                room_description = room.room_description,
                created_at = DateTime.Now,
                created_by = HttpContext.Session.GetString("Username") ?? "System",
                record_status = "N"
            };
            _context.rooms_list.Add(newRoom);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPut]
        public async Task<IActionResult> UpdateRoomStatus(int id, [FromForm] string room_status)
        {
            var room = await _context.rooms_list.FindAsync(id);
            if (room == null)
                return NotFound();

            room.room_status = room_status;
            room.updated_at = DateTime.Now;
            room.updated_by = HttpContext.Session.GetString("Username") ?? "System";
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetRoomStatistics() 
        {
            var statistics = await _context.rooms_list
                .GroupBy(r => r.room_status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var totalRooms = statistics.Sum(s => s.Count);
            var availableRooms = statistics.FirstOrDefault(s => s.Status == "ว่าง")?.Count ?? 0;
            var occupiedRooms = statistics.FirstOrDefault(s => s.Status == "ไม่ว่าง")?.Count ?? 0;
            var totalRevenue = await _context.rooms_list
                .Where(r => r.room_status == "ไม่ว่าง")
                .SumAsync(r => r.room_price);

            return Json(new
            {
                totalRooms = totalRooms,
                availableRooms = availableRooms,
                occupiedRooms = occupiedRooms,
                totalRevenue = totalRevenue
            });
        }
    
    }
}
