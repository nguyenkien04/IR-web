namespace WebApplication1.Models
{
    public class SaveButtonModel
    {
        public int DeviceId { get; set; }
        public string ButtonName { get; set; }
        public string Protocol { get; set; }
        public string HexCode { get; set; }
        public int Bits { get; set; }
    }

    public class AddDeviceModel
    {
        public string DeviceName { get; set; }
        public string Description { get; set; }
    }
    public class CreateScheduleModel
    {
        public int ButtonId { get; set; }
        public string TimeStr { get; set; }
        public string Description { get; set; }
    }
}