using Microsoft.AspNetCore.Hosting;
using TodoWebApi.Application.Interfaces;

namespace TodoWebApi.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _iWebHostEnvironment;
    private readonly string _fileStorageName = "uploads";

    public LocalFileStorageService(IWebHostEnvironment iWebHostEnvironment)
    {
        _iWebHostEnvironment = iWebHostEnvironment;
    }

    public async Task<string> UploadFileAsync(Stream stream, string fileName, string contentType)
    {
        var uploadsFolderPath = Path.Combine(_iWebHostEnvironment.WebRootPath, _fileStorageName);
        if (!Directory.Exists(uploadsFolderPath))
        {
            Directory.CreateDirectory(uploadsFolderPath);
        }

        var filePath = Path.Combine(uploadsFolderPath, fileName);

        await using var fileStream = new FileStream(filePath, FileMode.Create);
        await stream.CopyToAsync(fileStream);

        return $"/uploads/{fileName}";
    }

    public Task DeleteFileAsync(string fileName)
    {
        var filePath = Path.Combine(_iWebHostEnvironment.WebRootPath, _fileStorageName, fileName);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }
}