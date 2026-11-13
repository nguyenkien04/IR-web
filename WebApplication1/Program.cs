using Microsoft.EntityFrameworkCore;
using WebApplication1;
using WebApplication1.Data; // Thêm namespace chứa AppDbContext
using WebApplication1.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Đăng ký AppDbContext để kết nối SQL Server sử dụng Entity Framework Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Đăng ký dịch vụ MQTT Client để Controller có thể gọi khi người dùng bấm nút trên web
builder.Services.AddSingleton<IMqttService, MqttService>();

// Đăng ký dịch vụ tự động hóa chạy ngầm Tuần 4
builder.Services.AddHostedService<ScheduleBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();