namespace WebApplication1.Models
{
    public class SceneButton
    {
        public int SceneId { get; set; }
        public Scene Scene { get; set; }

        public int ButtonId { get; set; }
        public Button Button { get; set; }
    }
}