using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplication1.Controllers;

namespace WebApplication1
{
    public class DatabaseHelper
    {
        private static string connectionString = @"Server=ADMIN-PC\MSSQLSERVER01;Database=IRHubDB;Trusted_Connection=True;TrustServerCertificate=True;";

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }

        public static bool TestConnection()
        {
            try
            {
                using (SqlConnection conn = GetConnection())
                {
                    conn.Open();
                    return true;
                }
            }
            catch { return false; }
        }

        // --- CÁC HÀM CẬP NHẬT TRẠNG THÁI HIỂN THỊ (IsActive) ---

        // 1. Chỉ lấy các thiết bị đang được kích hoạt hiển thị ngoài màn hình (IsActive = 1)
        public static DataTable GetActiveDevices()
        {
            using (SqlConnection conn = GetConnection())
            {
                string query = "SELECT Id AS DeviceId, Name AS DeviceName, Room AS Description FROM Devices WHERE IsActive = 1";
                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                return dt;
            }
        }

        // 2. Chỉ lấy các thiết bị chưa hiển thị (IsActive = 0) để đổ vào Modal chọn
        public static DataTable GetAvailableDevices()
        {
            using (SqlConnection conn = GetConnection())
            {
                string query = "SELECT Id AS DeviceId, Name AS DeviceName, Room AS Description FROM Devices WHERE IsActive = 0";
                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                return dt;
            }
        }

        // Giữ lại hàm cũ để tương thích nếu nơi khác gọi
        public static DataTable GetDevices()
        {
            return GetActiveDevices();
        }

        // 3. Kích hoạt thiết bị hiển thị ra màn hình (Đặt IsActive = 1)
        public static bool ActivateDevice(int deviceId)
        {
            using (SqlConnection conn = GetConnection())
            {
                string query = "UPDATE Devices SET IsActive = 1 WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", deviceId);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        // 4. Ẩn thiết bị khỏi màn hình (Đặt IsActive = 0 khi bấm nút Delete)
        public static bool DeactivateDevice(int deviceId)
        {
            using (SqlConnection conn = GetConnection())
            {
                string query = "UPDATE Devices SET IsActive = 0 WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", deviceId);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public static DataTable GetButtonsByDeviceId(int deviceId)
        {
            using (SqlConnection conn = GetConnection())
            {
                string query = "SELECT Id AS ButtonId, Name AS ButtonName, Protocol, HexCode, Bits FROM Buttons WHERE DeviceId = @DeviceId";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@DeviceId", deviceId);
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                return dt;
            }
        }

        public static DataRow GetButtonById(int buttonId)
        {
            using (SqlConnection conn = GetConnection())
            {
                string query = "SELECT Name AS ButtonName, Protocol, HexCode, Bits FROM Buttons WHERE Id = @ButtonId";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ButtonId", buttonId);
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                if (dt.Rows.Count > 0) return dt.Rows[0];
                return null;
            }
        }

        // --- CÁC HÀM BỔ SUNG CHO TUẦN 3 ---

        // 1. Hàm thêm Thiết bị mới vào CSDL (Mặc định IsActive = 1 khi tạo mới từ giao diện)
        public static int AddDevice(string deviceName, string description)
        {
            using (SqlConnection conn = GetConnection())
            {
                string query = @"INSERT INTO Devices (Name, Room, IsActive) 
                                OUTPUT INSERTED.Id 
                                VALUES (@DeviceName, @Description, 1)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@DeviceName", deviceName);
                cmd.Parameters.AddWithValue("@Description", description ?? "");

                conn.Open();
                return (int)cmd.ExecuteScalar();
            }
        }

        // 2. Hàm lưu Nút bấm mới đã học được vào CSDL
        public static bool SaveButton(int deviceId, string buttonName, string protocol, string hexCode, int bits)
        {
            using (SqlConnection conn = GetConnection())
            {
                string query = @"INSERT INTO Buttons (DeviceId, Name, Protocol, HexCode, Bits) 
                                VALUES (@DeviceId, @ButtonName, @Protocol, @HexCode, @Bits)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@DeviceId", deviceId);
                cmd.Parameters.AddWithValue("@ButtonName", buttonName);
                cmd.Parameters.AddWithValue("@Protocol", protocol);
                cmd.Parameters.AddWithValue("@HexCode", hexCode);
                cmd.Parameters.AddWithValue("@Bits", bits);

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        // --- BỔ SUNG CHO TUẦN 4 (TỰ ĐỘNG HÓA & HẸN GIỜ) ---

        // 1. Thêm lịch hẹn giờ mới vào bảng Schedules
        public static bool AddSchedule(int targetId, TimeSpan scheduleTime, string description)
        {
            using (SqlConnection conn = GetConnection())
            {
                string query = @"INSERT INTO Schedules (Name, Time, IsActive) 
                                VALUES (@Description, @ScheduleTime, 1)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ScheduleTime", scheduleTime);
                cmd.Parameters.AddWithValue("@Description", description ?? "");

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        // 2. Lấy danh sách tất cả lịch hẹn giờ kết hợp thông tin Nút & Thiết bị
        public static DataTable GetSchedules()
        {
            using (SqlConnection conn = GetConnection())
            {
                string query = @"SELECT s.Id AS ScheduleId, s.Time AS ScheduleTime, s.IsActive, s.Name AS Description, 
                                    b.Name AS ButtonName, b.Protocol, b.HexCode, b.Bits, d.Name AS DeviceName
                                FROM Schedules s
                                LEFT JOIN SceneButtons sb ON s.Id = sb.SceneId
                                LEFT JOIN Buttons b ON sb.ButtonId = b.Id
                                LEFT JOIN Devices d ON b.DeviceId = d.Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                return dt;
            }
        }

        // 3. Xóa lịch hẹn giờ theo ScheduleId
        public static bool DeleteSchedule(int scheduleId)
        {
            using (SqlConnection conn = GetConnection())
            {
                string query = "DELETE FROM Schedules WHERE Id = @ScheduleId";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ScheduleId", scheduleId);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        // 4. Bật/Tắt trạng thái lịch trình (Toggle IsActive)
        public static bool ToggleSchedule(int scheduleId)
        {
            using (SqlConnection conn = GetConnection())
            {
                string query = "UPDATE Schedules SET IsActive = CASE WHEN IsActive = 1 THEN 0 ELSE 1 END WHERE Id = @ScheduleId";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ScheduleId", scheduleId);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        // --- BỔ SUNG CHO TRANG DEVICES ---

        // 5. Hàm xóa thiết bị và xóa luôn các nút bấm liên quan trong CSDL SQL Server
        public static bool DeleteDevice(int deviceId)
        {
            using (SqlConnection conn = GetConnection())
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    string deleteButtonsQuery = "DELETE FROM Buttons WHERE DeviceId = @DeviceId";
                    SqlCommand cmd1 = new SqlCommand(deleteButtonsQuery, conn, transaction);
                    cmd1.Parameters.AddWithValue("@DeviceId", deviceId);
                    cmd1.ExecuteNonQuery();

                    string deleteDeviceQuery = "DELETE FROM Devices WHERE Id = @DeviceId";
                    SqlCommand cmd2 = new SqlCommand(deleteDeviceQuery, conn, transaction);
                    cmd2.Parameters.AddWithValue("@DeviceId", deviceId);
                    cmd2.ExecuteNonQuery();

                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    return false;
                }
            }
        }

        // --- BỔ SUNG CHO TÍNH NĂNG QUẢN LÝ NGỮ CẢNH (SCENES) ---

        // 1. Lấy danh sách tất cả các Ngữ cảnh (Scenes)
        public static DataTable GetScenes()
        {
            using (SqlConnection conn = GetConnection())
            {
                string query = "SELECT Id AS SceneId, Name AS SceneName, Description FROM Scenes";
                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                return dt;
            }
        }

        // 2. Thêm Ngữ cảnh cơ bản (Trả về ID)
        public static int AddScene(string sceneName, string description)
        {
            using (SqlConnection conn = GetConnection())
            {
                string query = @"INSERT INTO Scenes (Name, Description) 
                                OUTPUT INSERTED.Id 
                                VALUES (@SceneName, @Description)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@SceneName", sceneName);
                cmd.Parameters.AddWithValue("@Description", description ?? "");

                conn.Open();
                return (int)cmd.ExecuteScalar();
            }
        }

        // 2.1. Thêm Ngữ cảnh mới kèm theo danh sách các bước tuần tự (Khớp với HomeController)
        public static bool AddScene(string sceneName, string description, List<SceneStepItem> steps)
        {
            using (SqlConnection conn = GetConnection())
            {
                conn.Open();
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string insertSceneQuery = @"INSERT INTO Scenes (Name, Description) 
                                                    OUTPUT INSERTED.Id 
                                                    VALUES (@SceneName, @Description)";
                        SqlCommand cmdScene = new SqlCommand(insertSceneQuery, conn, transaction);
                        cmdScene.Parameters.AddWithValue("@SceneName", sceneName);
                        cmdScene.Parameters.AddWithValue("@Description", description ?? "");

                        int sceneId = (int)cmdScene.ExecuteScalar();

                        if (steps != null)
                        {
                            foreach (var step in steps)
                            {
                                // Sửa lại để lấy DeviceId/ButtonId đúng từ SceneStepItem
                                int parsedButtonId = 0;
                                int.TryParse(step.DeviceId, out parsedButtonId);

                                string insertStepQuery = @"INSERT INTO SceneButtons (SceneId, ButtonId) VALUES (@SceneId, @ButtonId)";
                                SqlCommand cmdStep = new SqlCommand(insertStepQuery, conn, transaction);
                                cmdStep.Parameters.AddWithValue("@SceneId", sceneId);
                                cmdStep.Parameters.AddWithValue("@ButtonId", parsedButtonId > 0 ? parsedButtonId : 1);
                                cmdStep.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }

        // 3. Thêm Nút bấm vào Ngữ cảnh (Bảng SceneButtons)
        public static bool AddButtonToScene(int sceneId, int buttonId)
        {
            using (SqlConnection conn = GetConnection())
            {
                string query = @"INSERT INTO SceneButtons (SceneId, ButtonId) VALUES (@SceneId, @ButtonId)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@SceneId", sceneId);
                cmd.Parameters.AddWithValue("@ButtonId", buttonId);

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        // 4. Lấy danh sách các nút thuộc một Ngữ cảnh cụ thể
        public static DataTable GetButtonsBySceneId(int sceneId)
        {
            using (SqlConnection conn = GetConnection())
            {
                string query = @"SELECT sb.Id AS SceneButtonId, b.Id AS ButtonId, b.Name AS ButtonName, 
                                    b.Protocol, b.HexCode, b.Bits, d.Name AS DeviceName
                                FROM SceneButtons sb
                                JOIN Buttons b ON sb.ButtonId = b.Id
                                JOIN Devices d ON b.DeviceId = d.Id
                                WHERE sb.SceneId = @SceneId";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@SceneId", sceneId);
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                return dt;
            }
        }

        // 5. Xóa Ngữ cảnh và các liên kết nút của nó
        public static bool DeleteScene(int sceneId)
        {
            using (SqlConnection conn = GetConnection())
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    string deleteSbQuery = "DELETE FROM SceneButtons WHERE SceneId = @SceneId";
                    SqlCommand cmd1 = new SqlCommand(deleteSbQuery, conn, transaction);
                    cmd1.Parameters.AddWithValue("@SceneId", sceneId);
                    cmd1.ExecuteNonQuery();

                    string deleteSceneQuery = "DELETE FROM Scenes WHERE Id = @SceneId";
                    SqlCommand cmd2 = new SqlCommand(deleteSceneQuery, conn, transaction);
                    cmd2.Parameters.AddWithValue("@SceneId", sceneId);
                    cmd2.ExecuteNonQuery();

                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    return false;
                }
            }
        }
    }
}