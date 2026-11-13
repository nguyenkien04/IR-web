using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Device> Devices { get; set; }
        public DbSet<Button> Buttons { get; set; }
        public DbSet<Scene> Scenes { get; set; }
        public DbSet<SceneButton> SceneButtons { get; set; }
        public DbSet<Schedule> Schedules { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình quan hệ 1 - Nhiều giữa Device và Button (đảm bảo đồng bộ với ICollection<Button> Buttons trong model Device)
            modelBuilder.Entity<Button>()
                .HasOne(b => b.Device)
                .WithMany(d => d.Buttons)
                .HasForeignKey(b => b.DeviceId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa thiết bị sẽ tự động xóa các nút lệnh thuộc về thiết bị đó

            // Cấu hình khóa chính hỗn hợp cho bảng trung gian SceneButton
            modelBuilder.Entity<SceneButton>()
                .HasKey(sb => new { sb.SceneId, sb.ButtonId });

            modelBuilder.Entity<SceneButton>()
                .HasOne(sb => sb.Scene)
                .WithMany(s => s.SceneButtons)
                .HasForeignKey(sb => sb.SceneId);

            modelBuilder.Entity<SceneButton>()
                .HasOne(sb => sb.Button)
                .WithMany()
                .HasForeignKey(sb => sb.ButtonId);
        }
    }
}