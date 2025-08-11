namespace TodoWebApi.Application.DTOs;

public class TodoViewDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool IsComplete { get; set; }
    public string? AttachmentUrl { get; set; }
}