using System.IO;
using System.Security.Claims;
using System.Text;
using iText.Html2pdf;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using kfrpj.Data;
using kfrpj.Models.tenants;
using kfrpj.Models.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using IOPath = System.IO.Path;
using iText.Kernel.Geom;
using iText.Kernel.Font;
using iText.IO.Font;

namespace kfrpj.Controllers
{
    public class TenantsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TenantsController> _logger;
        private readonly IWebHostEnvironment _hostEnvironment;

        public TenantsController(
            ApplicationDbContext context,
            ILogger<TenantsController> logger,
            IWebHostEnvironment hostEnvironment
        )
        {
            _context = context;
            _logger = logger;
            _hostEnvironment = hostEnvironment;
        }

        // แสดงรายการผู้เช่าทั้งหมด
        public async Task<IActionResult> Index()
        {
            try
            {
                var tenants = await _context
                    .tenants_list.Where(t => t.record_status == "N")
                    .OrderBy(t => t.name)
                    .ToListAsync();
                return View(tenants);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการดึงข้อมูลผู้เช่า");
                TempData["Error"] = "ไม่สามารถดึงข้อมูลผู้เช่าได้ กรุณาลองใหม่อีกครั้ง";
                return View(new List<tenants_list>());
            }
        }

        // เพิ่มผู้เช่าใหม่
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] tenants_list tenant)
        {
            if (tenant == null)
            {
                _logger.LogError("tenant is null");
                return Json(new { success = false, message = "ข้อมูลไม่ถูกต้อง" });
            }
            // log ค่า property
            _logger.LogInformation("tenant: {@tenant}", tenant);

            tenant.created_at = DateTime.Now;
            tenant.record_status = "N";

            _context.tenants_list.Add(tenant);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // แก้ไขข้อมูลผู้เช่า
        [HttpPut]
        public async Task<IActionResult> Edit(int id, [FromBody] tenants_list tenant)
        {
            if (id != tenant.id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                _context.Entry(tenant).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "ข้อมูลไม่ถูกต้อง" });
        }

        // ดึงข้อมูลผู้เช่าตาม ID
        [HttpGet]
        public async Task<IActionResult> GetTenant(int id)
        {
            var tenant = await _context.tenants_list.FindAsync(id);
            if (tenant == null)
            {
                return NotFound();
            }
            return Json(tenant);
        }

        // ลบผู้เช่า
        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var tenant = await _context.tenants_list.FindAsync(id);
            if (tenant == null)
            {
                return NotFound();
            }

            tenant.record_status = "D"; // เปลี่ยนสถานะเป็น 'D' แทนการลบจริง
            _context.tenants_list.Update(tenant);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GeneratePdfReport()
        {
            try
            {
                var tenants = await _context
                    .tenants_list.Where(t => t.record_status == "N")
                    .Take(200)
                    .ToListAsync();

                // อ่านไฟล์ template HTML
                string templatePath = System.IO.Path.Combine(
                    _hostEnvironment.WebRootPath,
                    "report",
                    "test.cshtml"
                );
                string htmlTemplate = await System.IO.File.ReadAllTextAsync(
                    templatePath,
                    System.Text.Encoding.UTF8
                );

                // แทนที่ placeholders
                string htmlContent = htmlTemplate
                    .Replace("{{TotalCount}}", tenants.Count.ToString())
                    .Replace("{{GeneratedDate}}", DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
                    .Replace(
                        "{{GeneratedBy}}",
                        HttpContext.Session.GetString("Username") ?? "System"
                    )
                    .Replace("{{Year}}", DateTime.Now.Year.ToString());

                // สร้างตาราง
                string tableRows = "";
                int index = 1;
                foreach (var tenant in tenants)
                {
                    string statusClass =
                        tenant.record_status == "N" ? "status-active" : "status-inactive";
                    string statusText = tenant.record_status == "N" ? "ใช้งาน" : "ไม่ใช้งาน";

                    tableRows +=
                        $@"<tr>
                        <td class='index-cell'>{index++}</td>
                        <td><strong>{tenant.id}</strong></td>
                        <td>{tenant.name ?? ""}</td>
                        <td class='email-cell'>{tenant.email ?? ""}</td>
                        <td class='phone-cell'>{tenant.phone_number ?? ""}</td>
                        <td><span class='status-badge {statusClass}'>{statusText}</span></td>
                    </tr>";
                }
                htmlContent = htmlContent.Replace("{{TableRows}}", tableRows);

                // แปลง HTML เป็น PDF ด้วย iText
                using (var stream = new MemoryStream())
                {
                    // สร้าง PDF Writer
                    var writer = new PdfWriter(stream);
                    
                    // สร้าง PDF Document และกำหนดขนาด A4
                    var pdf = new PdfDocument(writer);
                    var pageSize = PageSize.A4;
                    var document = new Document(pdf, pageSize);
                    
                    // กำหนดขอบกระดาษ (margins) หน่วยเป็น point (1 inch = 72 points)
                    document.SetMargins(36, 36, 36, 36); // top, right, bottom, left (1/2 inch margins)

                    // กำหนดฟอนต์ภาษาไทย
                    var fontPath = System.IO.Path.Combine(
                        _hostEnvironment.WebRootPath,
                        "fonts",
                        "THSarabunNew.ttf"
                    );
                    var font = PdfFontFactory.CreateFont(fontPath, PdfEncodings.IDENTITY_H);
                    document.SetFont(font);

                    // แปลง HTML เป็น PDF
                    ConverterProperties properties = new ConverterProperties();
                    HtmlConverter.ConvertToPdf(htmlContent, pdf, properties);

                    document.Close();

                    return File(
                        stream.ToArray(),
                        "application/pdf",
                        $"tenants-report-{DateTime.Now:yyyyMMdd}.pdf"
                    );
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generating PDF: {ex.Message}");
            }
        }
    }
}
