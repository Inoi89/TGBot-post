using Models.Options;

namespace Models.BotConfiguration
{
    public class RootConfiguration
    {
        public List<BotSettings> BotSettings { get; set; } = new List<BotSettings>();
        public Logging Logging { get; set; } = new Logging();
        public ImageProcessingOptions ImageProcessing { get; set; } = new ImageProcessingOptions();
    }
}
