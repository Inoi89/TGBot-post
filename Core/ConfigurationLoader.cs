using Microsoft.Extensions.Configuration;
using Models.BotConfiguration;

namespace Core
{
    public class ConfigurationLoader
    {
        public static RootConfiguration LoadConfiguration(string fileName)
        {
            var builder = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile(fileName, optional: false, reloadOnChange: true);

            IConfigurationRoot configurationRoot = builder.Build();

            var rootConfig = new RootConfiguration();
            configurationRoot.GetSection("BotSettings").Bind(rootConfig.BotSettings);
            configurationRoot.GetSection("Logging").Bind(rootConfig.Logging);
            configurationRoot.GetSection("ImageProcessing").Bind(rootConfig.ImageProcessing);

            return rootConfig;
        }
    }
}
