using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Models.Options
{
    public class ConfigureBotOptions : IConfigureOptions<BotOptions>
    {
        private readonly IConfiguration _configuration;

        public ConfigureBotOptions(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Configure(BotOptions options)
        {
            options.BotSettings = _configuration.GetSection("BotSettings")
                .GetChildren()
                .Select(section => new BotSettings
                {
                    Name = section["Name"],
                    Token = section["Token"],
                    ChatId = section["ChatId"],
                    TempChatId = section["TempChatId"]
                })
                .ToList();
        }
    }
}
