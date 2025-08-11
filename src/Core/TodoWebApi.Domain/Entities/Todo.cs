namespace TodoWebApi.Domain.Entities;

public class Todo
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool IsComplete { get; set; }

    public string? UserId { get; set; }
    public ApiUser? User { get; set; }

    public string? AttachmentUrl { get; set; }
    public string? StoredFileName { get; set; }
    public string? OriginalFileName { get; set; }
}
