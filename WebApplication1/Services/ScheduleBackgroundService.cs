using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebApplication1.Services
{
    public class ScheduleBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ScheduleBackgroundService> _logger;
        private TimeSpan _lastExecutedTime = TimeSpan.MinValue;

        public ScheduleBackgroundService(IServiceProvider serviceProvider, ILogger<ScheduleBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Schedule Background Service đang chạy...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndTriggerSchedulesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError("Lỗi dịch vụ hẹn giờ ngầm: " + ex.Message);
                }

                // Kiểm tra lịch mỗi 10 giây
                await Task.Delay(10000, stoppingToken);
            }
        }

        private async Task CheckAndTriggerSchedulesAsync()
        {
            TimeSpan now = DateTime.Now.TimeOfDay;

            // Bỏ qua nếu vẫn trong cùng một phút vừa chạy
            if (now.Hours == _lastExecutedTime.Hours && now.Minutes == _lastExecutedTime.Minutes)
            {
                return;
            }

            // Sử dụng Scope để đảm bảo an toàn luồng khi truy vấn cơ sở dữ liệu ngầm
            using (var scope = _serviceProvider.CreateScope())
            {
                DataTable dt = DatabaseHelper.GetSchedules();
                foreach (DataRow row in dt.Rows)
                {
                    bool isActive = Convert.ToBoolean(row["IsActive"]);
                    if (!isActive) continue;

                    TimeSpan schedTime = (TimeSpan)row["ScheduleTime"];

                    // Nếu trùng giờ và phút hiện tại
                    if (schedTime.Hours == now.Hours && schedTime.Minutes == now.Minutes)
                    {
                        _lastExecutedTime = now;

                        string protocol = row["Protocol"] != DBNull.Value ? row["Protocol"].ToString() : "";
                        string hexCode = row["HexCode"] != DBNull.Value ? row["HexCode"].ToString() : "";
                        int bits = row["Bits"] != DBNull.Value ? Convert.ToInt32(row["Bits"]) : 0;
                        string description = row["Description"] != DBNull.Value ? row["Description"].ToString() : "Không mô tả";

                        _logger.LogInformation($"[ĐÃ KÍCH HOẠT LỊCH HẸN]: {description} lúc {schedTime}");

                        // Tự động phát lệnh MQTT tới ESP32
                        await SendIRViaMQTT(protocol, hexCode, bits);
                    }
                }
            }
        }

        private async Task SendIRViaMQTT(string protocol, string hexCode, int bits)
        {
            var payload = new { action = "SEND", protocol = protocol, hex = hexCode, bits = bits };
            string json = JsonConvert.SerializeObject(payload);

            var mqttFactory = new MqttClientFactory();
            using (var mqttClient = mqttFactory.CreateMqttClient())
            {
                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer("broker.hivemq.com", 1883)
                    .WithClientId("AutoSchedule_" + Guid.NewGuid().ToString())
                    .Build();

                await mqttClient.ConnectAsync(options, CancellationToken.None);
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic("ir_hub/control")
                    .WithPayload(Encoding.UTF8.GetBytes(json))
                    .Build();

                await mqttClient.PublishAsync(message, CancellationToken.None);
                await mqttClient.DisconnectAsync();
            }
        }
    }
}