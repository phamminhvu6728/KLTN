using System.ComponentModel.DataAnnotations;

namespace TuTuc.Models;

// Thuộc tính bổ sung của cùng lớp TripActivity, vẫn lưu trong bảng Activities.
public partial class TripActivity
{
    public int? PlaceId { get; set; }
    public int? TravelServiceId { get; set; }
    public int SortOrder { get; set; }
    public bool HasTime { get; set; } = true;
}

public partial class Incident
{
    public string Location { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Timestamp]
    public byte[] Version { get; set; } = [];
}

public class IncidentActivity
{
    public int Id { get; set; }
    public int IncidentId { get; set; }
    public Incident Incident { get; set; } = null!;
    public int ActivityId { get; set; }
    public TripActivity Activity { get; set; } = null!;
}

public class IncidentPlan
{
    public int Id { get; set; }
    public int IncidentId { get; set; }
    public Incident Incident { get; set; } = null!;
    public string Content { get; set; } = "";
    public DateTime ChosenAt { get; set; } = DateTime.UtcNow;
    public bool IsCurrent { get; set; } = true;
}
