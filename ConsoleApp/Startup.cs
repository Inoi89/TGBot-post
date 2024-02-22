using Core.Logging;
using Core.Services;
using Infrastructure.Contracts;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Models.Options;

namespace ConsoleApp
{
    public class Startup
    {
        public static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            services.AddSingleton<IConfiguration>(config);

            services.AddScoped<ConsoleLogger>();
            services.AddScoped<FileLogger>();
            services.AddScoped<ILogger, ConsoleLogger>();

            services.AddScoped<BlizkiyBotService>();
            services.AddScoped<DavaySimBotService>();

            services.AddScoped<IImageService, ImageService>();

            AddOptions(services, config);

            return services.BuildServiceProvider();
        }

        private static void AddOptions(IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions();
            services.Configure<FileLoggerOptions>(configuration.GetSection("FileLoggerOptions"));
            services.Configure<ImageProcessingOptions>(configuration.GetSection("ImageProcessingOptions"));
            services.AddSingleton<IConfigureOptions<BotOptions>, ConfigureBotOptions>();
        }
    }
}
