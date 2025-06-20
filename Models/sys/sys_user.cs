using System.ComponentModel.DataAnnotations;

namespace kfrpj.Models
{
    public class sys_users
    {
        [Key]
        public int id { get; set; }

        [StringLength(50, ErrorMessage = "ชื่อผู้ใช้ต้องไม่เกิน 50 ตัวอักษร")]
        [Display(Name = "ชื่อผู้ใช้")]
        public string? username { get; set; }

        [StringLength(100, ErrorMessage = "รหัสผ่านต้องไม่เกิน 100 ตัวอักษร")]
        [Display(Name = "รหัสผ่าน")]
        public string? password { get; set; }

        [StringLength(100, ErrorMessage = "ชื่อ-นามสกุลต้องไม่เกิน 100 ตัวอักษร")]
        [Display(Name = "ชื่อ-นามสกุล")]
        public string? fullname { get; set; }

        [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
        [StringLength(100, ErrorMessage = "อีเมลต้องไม่เกิน 100 ตัวอักษร")]
        [Display(Name = "อีเมล")]
        public string? email { get; set; }

        [StringLength(20, ErrorMessage = "เบอร์โทรศัพท์ต้องไม่เกิน 20 ตัวอักษร")]
        [Display(Name = "เบอร์โทรศัพท์")]
        public string? phone_number { get; set; }

        [Display(Name = "บทบาท")]
        public string? role { get; set; }

        [Display(Name = "วันที่สร้าง")]
        public DateTime? created_at { get; set; }

        [Display(Name = "วันที่แก้ไขล่าสุด")]
        public DateTime? updated_at { get; set; }

        [Display(Name = "ผู้สร้าง")]
        public string? created_by { get; set; }

        [Display(Name = "ผู้แก้ไขล่าสุด")]
        public string? updated_by { get; set; }

        [Display(Name = "สถานะ Record status")]
        public string? record_status { get; set; } = "N";

        [Display(Name = "วันที่ล็อกอินล่าสุด")]
        public DateTime? last_login { get; set; }
    }

    public class sys_users_password
    {
        [Key]
        public int user_id { get; set; }

        public string? passwordhash { get; set; }
    }
}