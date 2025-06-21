using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using kfrpj.Models.rooms;

namespace kfrpj.Models.finance
{
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
        [Display(Name = "เดือนที่เรียกเก็บ")]
        public DateTime charge_month { get; set; }

        [Required]
        [Display(Name = "ค่าห้อง")]
        public decimal room_price { get; set; }

        [Required]
        [Display(Name = "วันที่ครบกำหนด")]
        public DateTime due_date { get; set; }

        [Display(Name = "สถานะการชำระ")]
        public bool is_paid { get; set; } = false;

        [Display(Name = "วันที่ชำระ")]
        public DateTime? payment_date { get; set; }

        [Display(Name = "หมายเหตุ")]
        public string? notes { get; set; }

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
    }
} 