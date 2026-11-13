using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace WebApplication1.Models
{
    public class Scene
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } // Ví dụ: Xem phim, Đi ngủ

        [StringLength(255)]
        public string Description { get; set; } // Mô tả kịch bản

        // Quan hệ Nhiều-Nhiều với bảng Button thông qua bảng trung gian SceneButton
        public ICollection<SceneButton> SceneButtons { get; set; } = new List<SceneButton>();
    }
}