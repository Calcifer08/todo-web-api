namespace TodoWebApi.Application.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadFileAsync(Stream stream, string fileName, string contentType);
    Task DeleteFileAsync(string fileName);
}