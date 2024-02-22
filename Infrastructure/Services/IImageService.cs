using Models;

namespace Infrastructure.Services
{
    public interface IImageService
    {
        FileInfo[] ProcessImages();

        string GetImagePath(string messageText);
        string FolderPath { get; }
    }
}
