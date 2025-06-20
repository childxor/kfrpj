using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace kfrpj.Models.rooms
{
    [Table("rooms_list")]
    public class rooms_list
    {
        [Key]
        [Display(Name = "รหัสห้อง")]
        public int room_id { get; set; }

        [Required(ErrorMessage = "กรุณากรอกชื่อห้อง")]
        [Display(Name = "ชื่อห้อง")]
        public required string room_name { get; set; }

        [Required(ErrorMessage = "กรุณากรอกประเภทห้อง")]
        [Display(Name = "ประเภทห้อง")]
        public required string room_type { get; set; }

        [Required(ErrorMessage = "กรุณากรอกราคาห้อง")]
        [Display(Name = "ราคาห้อง")]
        public decimal room_price { get; set; }

        [Display(Name = "สถานะห้อง")]
        public string? room_status { get; set; }

        [Display(Name = "รายละเอียดห้อง")]
        public string? room_description { get; set; }

        [Display(Name = "รูปภาพห้อง")]
        public string? room_image { get; set; }

        // คอลัมน์ที่จำเป็นต้องมีตามกฎ
        [Display(Name = "วันที่สร้าง")]
        public DateTime created_at { get; set; }

        [Display(Name = "ผู้สร้าง")]
        public required string created_by { get; set; }

        [Display(Name = "วันที่แก้ไข")]
        public DateTime? updated_at { get; set; }

        [Display(Name = "ผู้แก้ไข")]
        public string? updated_by { get; set; }

        [Display(Name = "สถานะการใช้งาน")]
        public string record_status { get; set; } = "N";
    }
}
