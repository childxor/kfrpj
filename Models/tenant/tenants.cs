using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace kfrpj.Models.tenants
{
    [Table("tenants_list")]
    public class tenants_list 
    {
        [Key]
        [Display(Name = "รหัสประจำตัวผู้เช่า")]
        public int id { get; set; }

        [Display(Name = "ชื่อของผู้เช่า")]
        public string? name { get; set; }

        [EmailAddress]
        [Display(Name = "อีเมลของผู้เช่า")]
        public string? email { get; set; }

        [Phone]
        [Display(Name = "หมายเลขโทรศัพท์ของผู้เช่า")]
        public string? phone_number { get; set; }

        // ฟิลด์อื่น ๆ ตามความต้องการของระบบ
        [Display(Name = "ที่อยู่ของผู้เช่า")]
        public string? address { get; set; }

        [Display(Name = "ข้อมูลเพิ่มเติมเกี่ยวกับสัญญาเช่า")]
        public string? rental_info { get; set; }
        [Display(Name = "วันที่สร้างข้อมูล")]
        public DateTime created_at { get; set; }

        [Display(Name = "ผู้สร้างข้อมูล")]
        public string? created_by { get; set; }

        [Display(Name = "วันที่ปรับปรุงข้อมูล")]
        public DateTime? updated_at { get; set; }

        [Display(Name = "ผู้ปรับปรุงข้อมูล")]
        public string? updated_by { get; set; }

        [Display(Name = "สถานะของข้อมูล")]
        public string record_status { get; set; } = "N";
    }
}
