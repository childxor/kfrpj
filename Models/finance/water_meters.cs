// Models/finance/water_meters.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace kfrpj.Models.finance
{
    [Table("water_meters_list")]
    public class water_meters_list
    {
        [Key]
        [Display(Name = "รหัสมิเตอร์น้ำ")]
        public int water_meter_id { get; set; }

        [Required]
        [Display(Name = "รหัสห้อง")]
        public int room_id { get; set; }

        [Required]
        [Display(Name = "วันที่บันทึก")]
        public DateTime meter_date { get; set; }

        [Required]
        [Display(Name = "เลขมิเตอร์เก่า")]
        public int old_meter { get; set; }

        [Required]
        [Display(Name = "เลขมิเตอร์ใหม่")]
        public int new_meter { get; set; }

        [Display(Name = "จำนวนหน่วย")]
        public int water_units { get; set; }

        [Display(Name = "ค่าน้ำ")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal water_bill { get; set; }

        [Display(Name = "สถานะการชำระ")]
        public bool is_paid { get; set; } = false;

        [Display(Name = "หมายเหตุ")]
        public string? notes { get; set; }

        // คอลัมน์มาตรฐาน
        [Display(Name = "วันที่สร้าง")]
        public DateTime created_at { get; set; }

        [Display(Name = "ผู้สร้าง")]
        public string? created_by { get; set; }

        [Display(Name = "วันที่แก้ไข")]
        public DateTime? updated_at { get; set; }

        [Display(Name = "ผู้แก้ไข")]
        public string? updated_by { get; set; }

        [Display(Name = "สถานะการใช้งาน")]
        public string record_status { get; set; } = "N";

        // Navigation properties
        [ForeignKey("room_id")]
        public virtual rooms.rooms_list? Room { get; set; }
    }

    [Table("electric_meters_list")]
    public class electric_meters_list
    {
        [Key]
        [Display(Name = "รหัสมิเตอร์ไฟ")]
        public int electric_meter_id { get; set; }

        [Required]
        [Display(Name = "รหัสห้อง")]
        public int room_id { get; set; }

        [Required]
        [Display(Name = "วันที่บันทึก")]
        public DateTime meter_date { get; set; }

        [Required]
        [Display(Name = "เลขมิเตอร์เก่า")]
        public int old_meter { get; set; }

        [Required]
        [Display(Name = "เลขมิเตอร์ใหม่")]
        public int new_meter { get; set; }

        [Display(Name = "จำนวนหน่วย")]
        public int electric_units { get; set; }

        [Display(Name = "ค่าไฟ")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal electric_bill { get; set; }

        [Display(Name = "สถานะการชำระ")]
        public bool is_paid { get; set; } = false;

        [Display(Name = "หมายเหตุ")]
        public string? notes { get; set; }

        // คอลัมน์มาตรฐาน
        [Display(Name = "วันที่สร้าง")]
        public DateTime created_at { get; set; }

        [Display(Name = "ผู้สร้าง")]
        public string? created_by { get; set; }

        [Display(Name = "วันที่แก้ไข")]
        public DateTime? updated_at { get; set; }

        [Display(Name = "ผู้แก้ไข")]
        public string? updated_by { get; set; }

        [Display(Name = "สถานะการใช้งาน")]
        public string record_status { get; set; } = "N";

        // Navigation properties
        [ForeignKey("room_id")]
        public virtual rooms.rooms_list? Room { get; set; }
    }

    [Table("room_charges_list")]
    public class room_charges_list
    {
        [Key]
        [Display(Name = "รหัสค่าห้อง")]
        public int room_charge_id { get; set; }

        [Required]
        [Display(Name = "รหัสห้อง")]
        public int room_id { get; set; }

        [Required]
        [Display(Name = "เดือน")]
        public DateTime charge_month { get; set; }

        [Required]
        [Display(Name = "ค่าห้อง")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal room_price { get; set; }

        [Required]
        [Display(Name = "วันที่ครบกำหนด")]
        public DateTime due_date { get; set; }

        [Display(Name = "สถานะการชำระ")]
        public bool is_paid { get; set; } = false;

        [Display(Name = "วันที่ชำระ")]
        public DateTime? paid_date { get; set; }

        [Display(Name = "หมายเหตุ")]
        public string? notes { get; set; }

        // คอลัมน์มาตรฐาน
        [Display(Name = "วันที่สร้าง")]
        public DateTime created_at { get; set; }

        [Display(Name = "ผู้สร้าง")]
        public string? created_by { get; set; }

        [Display(Name = "วันที่แก้ไข")]
        public DateTime? updated_at { get; set; }

        [Display(Name = "ผู้แก้ไข")]
        public string? updated_by { get; set; }

        [Display(Name = "สถานะการใช้งาน")]
        public string record_status { get; set; } = "N";

        // Navigation properties
        [ForeignKey("room_id")]
        public virtual rooms.rooms_list? Room { get; set; }
    }
}
