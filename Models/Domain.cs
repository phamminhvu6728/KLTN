using System.ComponentModel.DataAnnotations;

namespace TuTuc.Models;

public partial class Account
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "Traveler";
    public string Phone { get; set; } = "";
    public string Bio { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public bool ProviderVerified { get; set; }
    public string SessionStamp { get; set; } = Guid.NewGuid().ToString("N");
}

public partial class Place
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Region { get; set; } = "";
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public string Image { get; set; } = "";
    public decimal Price { get; set; }
}

// Kế hoạch: dữ liệu lưu tại bảng Trips.
public partial class Trip
{
    // Định danh và chủ sở hữu.
    public int Id { get; set; }
    public int OwnerId { get; set; }

    // Thông tin chung và khoảng ngày chuyến đi.
    public string Name { get; set; } = "";
    public string Destination { get; set; } = "";
    public decimal? Budget { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }

    public string Status { get; set; } = "Nháp";

    // SQL Server tự sinh rowversion; dùng kiểm soát cập nhật đồng thời.
    [Timestamp]
    public byte[] Version { get; set; } = [];

    // Quan hệ 1:N với các hoạt động của kế hoạch.
    public List<TripActivity> Activities { get; set; } = [];
}

// Hoạt động: dữ liệu lưu tại bảng Activities; TripId liên kết về kế hoạch.
public partial class TripActivity
{
    public int Id { get; set; }
    public int TripId { get; set; }

    public string Title { get; set; } = "";
    public string Location { get; set; } = "";
    public string Notes { get; set; } = "";

    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public decimal? Cost { get; set; }
}

public partial class Companion
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public string Name { get; set; } = "";
    public string Destination { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime Date { get; set; }
    public bool IsPublished { get; set; } = true;
    public decimal Budget { get; set; }
    public int People { get; set; } = 2;
}

public partial class Connection
{
    public int Id { get; set; }
    public int CompanionId { get; set; }
    public int SenderId { get; set; }
    public string Message { get; set; } = "";
    public string Status { get; set; } = "Chờ phản hồi";
}

public partial class TravelService
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public string Category { get; set; } = "Trải nghiệm";
    public string Location { get; set; } = "Đà Lạt";
    public string Image { get; set; } = "";
    public string ApprovalStatus { get; set; } = "Đã duyệt";
    public bool IsAccepting { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public partial class Booking
{
    public int Id { get; set; }
    public Guid OperationId { get; set; }
    public int OwnerId { get; set; }
    public int TravelServiceId { get; set; }
    public TravelService? TravelService { get; set; }
    public string SnapshotName { get; set; } = "";
    public string Note { get; set; } = "";
    public string Result { get; set; } = "";
    public string Feedback { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime Date { get; set; }
    public int People { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "Chờ phản hồi";

    [Timestamp]
    public byte[] Version { get; set; } = [];
}

public partial class Incident
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public int? TripId { get; set; }
    public string Category { get; set; } = "Khác";
    public string Plan { get; set; } = "";
    public string Description { get; set; } = "";
    public string Resolution { get; set; } = "";
    public string Status { get; set; } = "Đang xử lý";
}
