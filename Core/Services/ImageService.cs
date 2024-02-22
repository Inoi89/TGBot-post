using Core.Logging;
using Infrastructure.Contracts;
using Infrastructure.Services;
using Microsoft.Extensions.Options;
using Models;
using Models.Options;

namespace Core.Services
{
    public class ImageService : IImageService
    {
        public string FolderPath { get; private set; }
        private readonly Dictionary<string, List<string>> _responses = new Dictionary<string, List<string>>();
        private readonly ILogger logger;
        // private readonly string _folderPath;

        // это лучше законфигурировать где-то и сделать ридом из файла в инит методе, хз.
        public ImageService(ILogger logger, IOptions<ImageProcessingOptions> imageProcessingOptions)
        {
            this.logger = logger;

            FolderPath = imageProcessingOptions.Value.FolderPath;

            _responses.Add(@"img/1.gif", new List<string> { "близк" });
            _responses.Add(@"img/2.gif", new List<string> { "сириус" });
            _responses.Add(@"img/3.gif", new List<string> { "навальн", "опозици", "закон", "америк" });
            _responses.Add(@"img/4.gif", new List<string> { "легко", "лучший", "брат", "молодцы", "молодец" });
            _responses.Add(@"img/5.gif", new List<string> { "хуета" });
            _responses.Add(@"img/6.gif", new List<string> { "красиво" });
        }

        public string GetImagePath(string messageText)
        {
            foreach (KeyValuePair<string, List<string>> response in _responses)
            {
                foreach (string keyword in response.Value)
                {
                    if (messageText.ToLower().Contains(keyword))
                    {
                        return response.Key; // Возвращает путь к картинке
                    }
                }
            }
            return null;
        }

        public FileInfo[] ProcessImages()
        {



            logger.Log(LogMessageType.Trace, $"Запустился поиск файлов в папочке {FolderPath}");

            if (!Directory.Exists(FolderPath))
            {
                logger.Log(LogMessageType.Error, "Указанная папка не существует.");
                return new FileInfo[0];
            }

            DirectoryInfo directory = new DirectoryInfo(FolderPath);

            FileInfo[] imageFiles = directory.GetFiles()
                .Where(file => file.Extension.Equals(".png", StringComparison.OrdinalIgnoreCase) || file.Extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (imageFiles.Length == 0)
            {
                // В папке нет файлов, удовлетворяющих критериям
                logger.Log(LogMessageType.Error, "В папке нет изображений для обработки.");
                return new FileInfo[0];
            }

            return imageFiles;
        }
    }
}
