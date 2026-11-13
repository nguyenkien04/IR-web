using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace WebApplication1.Controllers
{
    public class SceneController : Controller
    {
        // Hiển thị danh sách các Ngữ cảnh
        public IActionResult Index()
        {
            DataTable dtScenes = DatabaseHelper.GetScenes();
            return View(dtScenes);
        }

        // Trang thêm mới Ngữ cảnh
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Devices = DatabaseHelper.GetDevices(); // Lấy danh sách thiết bị để chọn nút gán vào kịch bản
            return View();
        }

        [HttpPost]
        public IActionResult Create(string sceneName, string description, int[] selectedButtons)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                ModelState.AddModelError("", "Tên ngữ cảnh không được để trống");
                ViewBag.Devices = DatabaseHelper.GetDevices();
                return View();
            }

            // 1. Thêm Scene và lấy về Id vừa tạo
            int sceneId = DatabaseHelper.AddScene(sceneName, description);

            // 2. Thêm các nút được chọn vào SceneButtons
            if (selectedButtons != null)
            {
                foreach (int buttonId in selectedButtons)
                {
                    DatabaseHelper.AddButtonToScene(sceneId, buttonId);
                }
            }

            return RedirectToAction("Index");
        }

        // Thực thi đồng thời ngữ cảnh (Gửi lệnh MQTT hoặc xử lý hàng loạt)
        [HttpPost]
        public IActionResult Execute(int id)
        {
            DataTable dtButtons = DatabaseHelper.GetButtonsBySceneId(id);

            foreach (DataRow row in dtButtons.Rows)
            {
                string protocol = row["Protocol"].ToString();
                string hexCode = row["HexCode"].ToString();
                int bits = Convert.ToInt32(row["Bits"]);

                // TODO: Gọi dịch vụ MQTT hoặc TCP client để gửi lệnh hồng ngoại xuống phần cứng ESP32 tại đây
                // Ví dụ: MqttService.PublishInfraredCommand(protocol, hexCode, bits);
            }

            TempData["Message"] = $"Đã kích hoạt thành công kịch bản ID: {id} với {dtButtons.Rows.Count} lệnh hồng ngoại đồng thời!";
            return RedirectToAction("Index");
        }

        // Xóa ngữ cảnh
        public IActionResult Delete(int id)
        {
            DatabaseHelper.DeleteScene(id);
            return RedirectToAction("Index");
        }
    }
}