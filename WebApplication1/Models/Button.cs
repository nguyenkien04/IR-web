using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class Button
    {
        [Key]
        public int Id { get; set; }

        // Khóa ngoại liên kết với bảng Device
        public int DeviceId { get; set; }

        [ForeignKey("DeviceId")]
        public Device Device { get; set; }

        [Required(ErrorMessage = "Tên nút không được để trống")]
        [StringLength(100)]
        public string Name { get; set; } // Ví dụ: Power, Tăng nhiệt độ, Mute

        [Required]
        [StringLength(50)]
        public string Protocol { get; set; } // Ví dụ: NEC, SONY, RC5

        [Required]
        [StringLength(100)]
        public string HexCode { get; set; } // Ví dụ: 0x20DF10EF

        public int Bits { get; set; } = 32; // Số bit của mã hồng ngoại, mặc định thường là 32
    }
}