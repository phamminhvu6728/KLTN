namespace TuTuc.Models;
public class UserNotification
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Url { get; set; } = "/Dashboard";
    public string Type { get; set; } = "";
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class AuditEntry
{
    public int Id { get; set; }
    public int ActorId { get; set; }
    public string Entity { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string Action { get; set; } = "";
    public string EventCode { get; set; } = "";
    public string Before { get; set; } = "";
    public string After { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
