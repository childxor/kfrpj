// Models/settings/settings.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace kfrpj.Models.settings
{
    [Table("settings_list")]
    public class settings_list
    {
        [Key]
        [Display(Name = "รหัสการตั้งค่า")]
        public int setting_id { get; set; }

        [Required]
        [Display(Name = "ชื่อการตั้งค่า")]
        public required string setting_name { get; set; }

        [Required]
        [Display(Name = "รหัสการตั้งค่า")]
        public required string setting_key { get; set; }

        [Required]
        [Display(Name = "ค่าที่ตั้งไว้")]
        public required string setting_value { get; set; }

        [Display(Name = "ประเภทข้อมูล")]
        public string? data_type { get; set; } = "string"; // string, number, decimal

        [Display(Name = "หน่วย")]
        public string? unit { get; set; }

        [Display(Name = "คำอธิบาย")]
        public string? description { get; set; }

        [Display(Name = "หมวดหมู่")]
        public string? category { get; set; }

        // คอลัมน์มาตรฐานตามกฎ
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
    }
}