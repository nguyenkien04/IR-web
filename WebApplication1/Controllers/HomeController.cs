using Microsoft.AspNetCore.Mvc;
using MQTTnet;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        private readonly IMqttService _mqttService;

        public HomeController(IMqttService mqttService)
        {
            _mqttService = mqttService;
        }

        // Trang chủ (Dashboard) chỉ lấy các thiết bị có IsActive = 1 từ CSDL
        public IActionResult Index()
        {
            DataTable devices = DatabaseHelper.GetActiveDevices();
            ViewBag.Devices = devices;
            return View();
        }

        [HttpGet]
        public IActionResult Provision()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetDevicesByModule(string hwId)
        {
            try
            {
                DataTable dt = DatabaseHelper.GetActiveDevices();
                var list = new System.Collections.Generic.List<object>();

                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new
                    {
                        deviceId = row["DeviceId"],
                        deviceName = row["DeviceName"],
                        description = row["Description"]
                    });
                }
                return Json(list);
            }
            catch (Exception ex)
            {
                return Json(new System.Collections.Generic.List<object>());
            }
        }

        [HttpGet]
        public IActionResult GetButtons(int deviceId)
        {
            DataTable buttons = DatabaseHelper.GetButtonsByDeviceId(deviceId);
            var list = new System.Collections.Generic.List<object>();

            foreach (DataRow row in buttons.Rows)
            {
                list.Add(new
                {
                    buttonId = row["ButtonId"],
                    buttonName = row["ButtonName"],
                    protocol = row["Protocol"],
                    hexCode = row["HexCode"],
                    bits = row["Bits"]
                });
            }
            return Json(list);
        }

        [HttpPost]
        public async Task<IActionResult> SendIRCommand([FromBody] IRCommandModel cmd)
        {
            try
            {
                if (cmd.ButtonId > 0)
                {
                    DataRow row = DatabaseHelper.GetButtonById(cmd.ButtonId);
                    if (row != null)
                    {
                        cmd.Protocol = row["Protocol"].ToString();
                        cmd.HexCode = row["HexCode"].ToString();
                        cmd.Bits = Convert.ToInt32(row["Bits"]);
                    }
                }

                int[] rawData = null;
                string rawDataString = cmd.HexCode;

                if (!string.IsNullOrEmpty(rawDataString) && (rawDataString.Contains(" ") || rawDataString.Contains(",")))
                {
                    rawData = Array.ConvertAll(rawDataString.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries), int.Parse);
                }
                else if (!string.IsNullOrEmpty(cmd.HexCode))
                {
                    rawData = ConvertHexToRawData(cmd.HexCode, cmd.Bits);
                }

                if (rawData != null && rawData.Length > 0)
                {
                    await _mqttService.SendRawIrCommandAsync(rawData);
                    return Json(new { success = true, message = $"Đã phát lệnh RAW (rawLength: {rawData.Length}) thành công qua MQTT!" });
                }

                return Json(new { success = false, message = "Không thể chuyển đổi lệnh sang mảng rawData hợp lệ!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi gửi lệnh MQTT: " + ex.Message });
            }
        }

        private int[] ConvertHexToRawData(string hexCode, int bits)
        {
            string cleanHex = hexCode.Replace("0x", "").Replace("0X", "");
            if (!long.TryParse(cleanHex, System.Globalization.NumberStyles.HexNumber, null, out long numericValue))
            {
                return null;
            }

            System.Collections.Generic.List<int> pulses = new System.Collections.Generic.List<int>();
            pulses.Add(9000);
            pulses.Add(4500);

            int totalBits = bits > 0 ? bits : 32;
            for (int i = totalBits - 1; i >= 0; i--)
            {
                pulses.Add(560);
                long bit = (numericValue >> i) & 1;
                if (bit == 1)
                {
                    pulses.Add(1690);
                }
                else
                {
                    pulses.Add(560);
                }
            }
            pulses.Add(560);

            return pulses.ToArray();
        }

        [HttpPost]
        public async Task<IActionResult> StartLearnMode()
        {
            try
            {
                var payload = new { action = "LEARN" };
                string jsonMessage = JsonConvert.SerializeObject(payload);

                var mqttFactory = new MqttClientFactory();
                using (var mqttClient = mqttFactory.CreateMqttClient())
                {
                    var mqttClientOptions = new MqttClientOptionsBuilder()
                        .WithTcpServer("broker.hivemq.com", 1883)
                        .WithClientId("Web_IR_Learn_" + Guid.NewGuid().ToString())
                        .Build();

                    await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

                    var message = new MqttApplicationMessageBuilder()
                        .WithTopic("ir_hub/control")
                        .WithPayload(Encoding.UTF8.GetBytes(jsonMessage))
                        .Build();

                    await mqttClient.PublishAsync(message, CancellationToken.None);
                    await mqttClient.DisconnectAsync();
                }

                return Json(new { success = true, message = "Đã gửi yêu cầu bật chế độ Học Lệnh tới ESP32!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi MQTT: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult SaveNewButton([FromBody] SaveButtonModel model)
        {
            if (model.DeviceId <= 0 || string.IsNullOrEmpty(model.ButtonName) || string.IsNullOrEmpty(model.HexCode))
            {
                return Json(new { success = false, message = "Vui lòng nhập đầy đủ tên nút và mã HexCode!" });
            }

            bool result = DatabaseHelper.SaveButton(model.DeviceId, model.ButtonName, model.Protocol, model.HexCode, model.Bits);
            if (result)
            {
                return Json(new { success = true, message = $"Đã lưu nút '{model.ButtonName}' vào CSDL thành công!" });
            }
            return Json(new { success = false, message = "Lỗi khi lưu vào SQL Server!" });
        }

        [HttpPost]
        public IActionResult AddNewDevice([FromBody] AddDeviceModel model)
        {
            if (model == null)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });
            }

            int deviceIdToActivate = 0;

            if (model.DeviceId > 0)
            {
                deviceIdToActivate = model.DeviceId;
                DatabaseHelper.ActivateDevice(deviceIdToActivate);
            }
            else if (!string.IsNullOrEmpty(model.Brand))
            {
                int newId = DatabaseHelper.AddDevice(model.Brand, model.Description);
                if (newId > 0)
                {
                    deviceIdToActivate = newId;
                }
            }

            if (deviceIdToActivate > 0)
            {
                return Json(new { success = true, message = "Đã thêm thiết bị ra màn hình điều khiển thành công!" });
            }

            return Json(new { success = false, message = "Vui lòng chọn thiết bị hoặc nhập đầy đủ thông tin!" });
        }

        [HttpGet]
        public IActionResult GetAvailableDevicesFromDB()
        {
            DataTable dt = DatabaseHelper.GetAvailableDevices();
            var list = new System.Collections.Generic.List<object>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new
                {
                    deviceId = row["DeviceId"],
                    deviceName = row["DeviceName"],
                    description = row["Description"]
                });
            }
            return Json(list);
        }

        [HttpGet]
        public IActionResult GetActiveDevicesForScene()
        {
            DataTable dt = DatabaseHelper.GetActiveDevices();
            var list = new System.Collections.Generic.List<object>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new
                {
                    deviceId = row["DeviceId"],
                    deviceName = row["DeviceName"],
                    description = row["Description"]
                });
            }
            return Json(list);
        }

        [HttpGet]
        public IActionResult GetScenes()
        {
            DataTable dt = DatabaseHelper.GetScenes();
            var list = new System.Collections.Generic.List<object>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new
                {
                    sceneId = row["SceneId"],
                    sceneName = row["SceneName"],
                    description = row["Description"]
                });
            }
            return Json(list);
        }

        // Action lưu kịch bản mới xuống CSDL
        [HttpPost]
        public IActionResult CreateScene([FromBody] SceneCreateModel model)
        {
            try
            {
                if (model == null || string.IsNullOrEmpty(model.SceneName))
                {
                    return Json(new { success = false, message = "Tên kịch bản không được để trống!" });
                }

                if (model.Steps == null || model.Steps.Count == 0)
                {
                    return Json(new { success = false, message = "Kịch bản phải có ít nhất 1 bước hành động!" });
                }

                bool res = DatabaseHelper.AddScene(model.SceneName, model.Description, model.Steps);

                if (res)
                {
                    return Json(new { success = true, message = "Tạo kịch bản thành công!" });
                }
                return Json(new { success = false, message = "Không thể lưu kịch bản vào CSDL!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }

        public IActionResult Devices()
        {
            DataTable activeDevices = DatabaseHelper.GetActiveDevices();
            return View(activeDevices);
        }

        public IActionResult Remote(int id = 0)
        {
            DataTable devices = DatabaseHelper.GetActiveDevices();
            ViewBag.Devices = devices;
            ViewBag.SelectedDeviceId = id;
            return View();
        }

        public IActionResult Learning()
        {
            DataTable devices = DatabaseHelper.GetActiveDevices();
            ViewBag.Devices = devices;
            return View();
        }

        public IActionResult Scenes()
        {
            return View();
        }

        public IActionResult Schedule()
        {
            DataTable dt = DatabaseHelper.GetSchedules();
            return View(dt);
        }

        public IActionResult Settings()
        {
            return View();
        }

        [HttpPost]
        public IActionResult DeleteDevice(int id)
        {
            bool res = DatabaseHelper.DeactivateDevice(id);
            if (res)
            {
                return Json(new { success = true, message = "Đã ẩn thiết bị khỏi màn hình!" });
            }
            return Json(new { success = false, message = "Không tìm thấy thiết bị cần ẩn!" });
        }

        [HttpGet]
        public IActionResult GetSchedules()
        {
            DataTable dt = DatabaseHelper.GetSchedules();
            var list = new System.Collections.Generic.List<object>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new
                {
                    scheduleId = row["ScheduleId"],
                    deviceName = row["DeviceName"],
                    buttonName = row["ButtonName"],
                    scheduleTime = row["ScheduleTime"].ToString(),
                    description = row["Description"]
                });
            }
            return Json(list);
        }

        [HttpPost]
        public IActionResult CreateSchedule([FromBody] CreateScheduleModel model)
        {
            if (model == null || model.ButtonId <= 0 || string.IsNullOrEmpty(model.TimeStr))
            {
                return Json(new { success = false, message = "Vui lòng chọn đầy đủ thông tin lệnh và thời gian thực thi!" });
            }

            if (!TimeSpan.TryParse(model.TimeStr, out TimeSpan time))
            {
                return Json(new { success = false, message = "Định dạng thời gian không hợp lệ!" });
            }

            bool result = DatabaseHelper.AddSchedule(model.ButtonId, time, model.Description);
            if (result)
            {
                return Json(new { success = true, message = "Đã lên lịch hẹn giờ thành công!" });
            }
            return Json(new { success = false, message = "Lỗi khi lưu lịch vào SQL Server!" });
        }

        [HttpPost]
        public IActionResult DeleteSchedule(int id)
        {
            bool res = DatabaseHelper.DeleteSchedule(id);
            return Json(new { success = res, message = res ? "Đã xóa lịch thành công!" : "Lỗi xóa lịch!" });
        }

        [HttpPost]
        public IActionResult DeleteScene(int id)
        {
            bool res = DatabaseHelper.DeleteScene(id);
            return Json(new { success = res, message = res ? "Đã xóa kịch bản thành công!" : "Lỗi khi xóa kịch bản trong SQL Server!" });
        }

        [HttpPost]
        public IActionResult ToggleSchedule(int id)
        {
            bool res = DatabaseHelper.ToggleSchedule(id);
            return Json(new { success = res, message = res ? "Đã cập nhật trạng thái lịch thành công!" : "Lỗi cập nhật trạng thái lịch!" });
        }

        [HttpPost]
        public async Task<IActionResult> RunSleepMacro()
        {
            try
            {
                await SendIRCommand(new IRCommandModel { Protocol = "NEC", HexCode = "0x20DF10EF", Bits = 32 });
                await Task.Delay(1000);
                await SendIRCommand(new IRCommandModel { Protocol = "NEC", HexCode = "0x11223344", Bits = 32 });

                return Json(new { success = true, message = "Đã kích hoạt kịch bản thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi kịch bản: " + ex.Message });
            }
        }
    }

    public class AddDeviceModel
    {
        public int DeviceId { get; set; }
        public string Brand { get; set; }
        public string DeviceType { get; set; }
        public string Description { get; set; }
    }

    // Model hứng dữ liệu kịch bản từ giao diện Scenes.cshtml
    public class SceneCreateModel
    {
        public string SceneName { get; set; }
        public string Description { get; set; }
        public System.Collections.Generic.List<SceneStepItem> Steps { get; set; }
    }

    public class SceneStepItem
    {
        public string HwId { get; set; }
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string ButtonName { get; set; }
        public string Protocol { get; set; }
        public string HexCode { get; set; }
        public int Bits { get; set; }
        public int Delay { get; set; }
    }
}