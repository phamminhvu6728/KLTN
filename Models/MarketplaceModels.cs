using System.ComponentModel.DataAnnotations;
namespace TuTuc.Models;
public class ServiceRevision
{
    public int Id { get; set; }
    public int TravelServiceId { get; set; }
    public TravelService? TravelService { get; set; }
    public int RevisionNumber { get; set; } = 1;
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public string Location { get; set; } = "";
    public string Image { get; set; } = "";
    public decimal Price { get; set; }
    public string ServingHours { get; set; } = "";
    public string Conditions { get; set; } = "";
    public DateTime? ClosureStart { get; set; }
    public DateTime? ClosureEnd { get; set; }
    public string ClosureReason { get; set; } = "";
    public string ClosureScope { get; set; } = "";
    public string Status { get; set; } = "Chờ duyệt";
    public string ReviewNote { get; set; } = "";
    public int? ReviewedById { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Timestamp] public byte[] Version { get; set; } = [];
}
public class BookingConditionVersion
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public Booking? Booking { get; set; }
    public int VersionNumber { get; set; }
    public string Snapshot { get; set; } = "";
    public int ActorId { get; set; }
    public string Reason { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class BookingChange
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public Booking? Booking { get; set; }
    public int RequesterId { get; set; }
    public string Kind { get; set; } = "Điều chỉnh";
    public int BaseConditionsVersion { get; set; } = 1;
    public string OldConditions { get; set; } = "";
    public DateTime Date { get; set; }
    public DateTime? EndDate { get; set; }
    public int Guests { get; set; } = 1;
    public int People { get; set; }
    public string ArrivalDeparture { get; set; } = "";
    public string Options { get; set; } = "";
    public decimal? Total { get; set; }
    public string Reason { get; set; } = "";
    public string DecisionReason { get; set; } = "";
    public string ProviderOfferReason { get; set; } = "";
    public int LogicalVersion { get; set; } = 1;
    public string Status { get; set; } = "Chờ phản hồi";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }
    [Timestamp] public byte[] Version { get; set; } = [];
}
public class BookingItemResult
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public Booking? Booking { get; set; }
    public string ItemKey { get; set; } = "";
    public string ItemTitle { get; set; } = "";
    public string Outcome { get; set; } = "Chưa xác định";
    public string Note { get; set; } = "";
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    public string VerificationStatus { get; set; } = "Chờ kiểm tra";
    public string Feedback { get; set; } = "";
    public DateTime? FeedbackAt { get; set; }
    public DateTime? CheckedAt { get; set; }
    [Timestamp] public byte[] Version { get; set; } = [];
}
public class BookingResultHistory
{
    public int Id { get; set; }
    public int BookingItemResultId { get; set; }
    public BookingItemResult? BookingItemResult { get; set; }
    public string PreviousOutcome { get; set; } = "";
    public string PreviousNote { get; set; } = "";
    public string NewOutcome { get; set; } = "";
    public string Evidence { get; set; } = "";
    public int ActorId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class ServiceEditVm
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string Name { get; set; } = "";
    [MaxLength(4000)] public string? Description { get; set; }
    [Required, MaxLength(100)] public string Category { get; set; } = "Trải nghiệm";
    [Required, MaxLength(200)] public string Location { get; set; } = "";
    public string? Image { get; set; }
    [Range(0, 1000000000)] public decimal Price { get; set; }
    [MaxLength(200)] public string? ServingHours { get; set; }
    [MaxLength(2000)] public string? Conditions { get; set; }
    public DateTime? ClosureStart { get; set; }
    public DateTime? ClosureEnd { get; set; }
    [MaxLength(500)] public string? ClosureReason { get; set; }
    [MaxLength(500)] public string? ClosureScope { get; set; }
    public string? Version { get; set; }
    public string Operation { get; set; } = "Submit";
}
public class BookingTermsInput
{
    public string? PreviewToken { get; set; }
    public int Service { get; set; }
    public int Id { get; set; }
    public Guid Operation { get; set; }
    [Required] public DateTime Date { get; set; }
    public DateTime? EndDate { get; set; }
    [Range(1,100)] public int People { get; set; } = 1;
    [Range(1,100)] public int Guests { get; set; } = 1;
    [MaxLength(100)] public string? ArrivalDeparture { get; set; }
    [MaxLength(1000)] public string? Options { get; set; }
    [MaxLength(1000)] public string? Note { get; set; }
    [Range(0,1000000000)] public decimal? Total { get; set; }
    public int ConditionsVersion { get; set; }
    public string? Version { get; set; }
    public string? Reason { get; set; }
}
public class BookingPreviewVm { public TravelService Service { get; set; } = null!; public BookingTermsInput Terms { get; set; } = new(); public List<string> Warnings { get; set; } = []; }
public class BookingResultInput { public string ItemKey { get; set; } = ""; public string Outcome { get; set; } = ""; public string? Note { get; set; } }
public class MarketCatalogVm { public List<TravelService> Services { get; set; } = []; public string Query { get; set; } = ""; public string Category { get; set; } = ""; public decimal? MinPrice { get; set; } public decimal? MaxPrice { get; set; } public string Error { get; set; } = ""; }
public class RequestListVm { public Dictionary<int,string> CustomerContacts {get;set;}=[]; public int Page {get;set;}=1; public int TotalCount {get;set;} public string? StatusFilter {get;set;} public int FeedbackWindowDays {get;set;}=30; public int? DraftBookingId {get;set;} public BookingTermsInput? DraftTerms {get;set;} public string? DraftReason {get;set;} public string? DraftDecision {get;set;} public List<Booking> Bookings { get; set; } = []; public List<BookingChange> Changes { get; set; } = []; public List<BookingConditionVersion> History { get; set; } = []; public List<BookingItemResult> Results { get; set; } = []; public List<BookingResultHistory> ResultHistory { get; set; } = []; public Dictionary<int,string> PeopleNames { get; set; } = []; public bool IsProvider { get; set; } }
public class ProviderDashboardVm { public List<TravelService> Services { get; set; } = []; public List<Booking> Bookings { get; set; } = []; public List<ServiceRevision> Revisions { get; set; } = []; public bool Verified { get; set; } public string Status { get; set; } = ""; }
public class AdminDashboardVm { public List<Account> Accounts { get; set; } = []; public List<TravelService> Services { get; set; } = []; public List<ServiceRevision> Revisions { get; set; } = []; public int BookingCount { get; set; } }
