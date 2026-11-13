using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class Schedule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } // Ví dụ: Tự động tắt điều hòa lúc 23:00

        [Required]
        public TimeSpan Time { get; set; } // Thời gian thực thi trong ngày (ví dụ: 23:00:00)

        public bool IsActive { get; set; } = true; // Trạng thái bật/tắt lịch trình

        // Khóa ngoại trỏ tới Kịch bản được thực thi khi đến giờ
        public int? SceneId { get; set; }

        [ForeignKey("SceneId")]
        public Scene Scene { get; set; }
    }
}