using Models.Options;

namespace Infrastructure.Services
{
    public interface IBotService
    {
        Task ExecuteAsync();
        Task StartRecieving();

        void Configure(BotSettings botSettings);
    }
}
