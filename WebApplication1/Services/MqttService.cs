using MQTTnet;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WebApplication1.Services
{
    public interface IMqttService
    {
        // Cập nhật hàm interface để nhận trực tiếp mảng rawData
        Task SendRawIrCommandAsync(int[] rawData);
    }

    public class MqttService : IMqttService
    {
        private readonly string _brokerHost = "broker.hivemq.com";
        private readonly int _brokerPort = 1883;
        private readonly string _topic = "ir_hub/control";

        public async Task SendRawIrCommandAsync(int[] rawData)
        {
            // Đóng gói payload chứa action SEND_RAW, rawLength và mảng rawData
            var payload = new
            {
                action = "SEND_RAW",
                rawLength = rawData.Length,
                rawData = rawData
            };
            string jsonMessage = JsonSerializer.Serialize(payload);

            var mqttFactory = new MqttClientFactory();
            using var mqttClient = mqttFactory.CreateMqttClient();

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(_brokerHost, _brokerPort)
                .WithClientId("Web_IR_Sender_" + Guid.NewGuid().ToString())
                .Build();

            await mqttClient.ConnectAsync(options, CancellationToken.None);

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(_topic)
                .WithPayload(Encoding.UTF8.GetBytes(jsonMessage))
                .Build();

            await mqttClient.PublishAsync(message, CancellationToken.None);
            await mqttClient.DisconnectAsync();
        }
    }
}