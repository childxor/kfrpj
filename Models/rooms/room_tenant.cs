using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using kfrpj.Models.tenants;

namespace kfrpj.Models.rooms
{
    [Table("room_tenant_rel")]
    public class room_tenant_rel
    {
        [Key]
        [Display(Name = "รหัสความสัมพันธ์")]
        public int rel_id { get; set; }

        [Required]
        [Display(Name = "รหัสห้อง")]
        public int room_id { get; set; }

        [Required]
        [Display(Name = "รหัสผู้เช่า")]
        public int tenant_id { get; set; }

        [Display(Name = "วันที่เริ่มสัญญา")]
        public DateTime? start_date { get; set; }

        [Display(Name = "วันที่สิ้นสุดสัญญา")]
        public DateTime? end_date { get; set; }

        [Display(Name = "สถานะความสัมพันธ์")]
        public string status { get; set; } = "active"; // active, inactive

        // คอลัมน์มาตรฐาน
        [Required]
        [Display(Name = "วันที่สร้าง")]
        public DateTime created_at { get; set; }

        [Display(Name = "ผู้สร้าง")]
        public string? created_by { get; set; }

        [Display(Name = "วันที่แก้ไข")]
        public DateTime? updated_at { get; set; }

        [Display(Name = "ผู้แก้ไข")]
        public string? updated_by { get; set; }

        [Required]
        [Display(Name = "สถานะการใช้งาน")]
        public string record_status { get; set; } = "N";

        // Navigation properties
        [ForeignKey("room_id")]
        public virtual rooms_list? Room { get; set; }

        [ForeignKey("tenant_id")]
        public virtual tenants_list? Tenant { get; set; }
    }
} 