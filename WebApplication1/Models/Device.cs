using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models // Thay YourProjectName bằng tên namespace thực tế của bạn
{
    public class Device
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên thiết bị không được để trống")]
        [StringLength(100)]
        public string Name { get; set; } // Ví dụ: Điều hòa phòng khách, TV Sony

        [StringLength(100)]
        public string Room { get; set; } // Ví dụ: Phòng khách, Phòng ngủ

        // Quan hệ 1-Nhiều: Một thiết bị có thể có nhiều nút bấm (Bật/Tắt, Tăng giảm âm lượng...)
        public ICollection<Button> Buttons { get; set; } = new List<Button>();
    }
}