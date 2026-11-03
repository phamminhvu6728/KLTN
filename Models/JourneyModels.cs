using System.ComponentModel.DataAnnotations;
namespace TuTuc.Models;
public class TripInput {
 public int Id {get;set;} [Required(ErrorMessage="Nhập tên chuyến đi."),StringLength(150)]public string Name {get;set;}="";[Required(ErrorMessage="Nhập điểm đến."),StringLength(200)]public string Destination {get;set;}="";
 public DateTime Start {get;set;}=DateTime.Today.AddDays(7);public DateTime End {get;set;}=DateTime.Today.AddDays(9);[Range(0,1000000000)]public decimal? Budget {get;set;}public string Version {get;set;}="";
}
public class ActivityInput {
 public int Id {get;set;}public int TripId {get;set;}[StringLength(300)]public string? Title {get;set;}[StringLength(300)]public string? Location {get;set;}[StringLength(1000)]public string? Notes {get;set;}
 public DateTime Start {get;set;}public DateTime End {get;set;}[Range(0,1000000000)]public decimal? Cost {get;set;}public string Version {get;set;}="";public bool HasTime {get;set;}=true;public int SortOrder {get;set;}=1;public string SourceType {get;set;}="Manual";public int? SourceId {get;set;}public string? SourceSelection {get;set;}
}
public class IncidentInput {
 [Range(1,int.MaxValue,ErrorMessage="Chọn một chuyến đi của bạn.")]public int TripId {get;set;}[Required,StringLength(80)]public string Category {get;set;}="Thời tiết";[Required(ErrorMessage="Nhập mô tả tình huống."),StringLength(2000)]public string Description {get;set;}="";[StringLength(300)]public string? Location {get;set;}public List<int> ActivityIds {get;set;}=[];
}
public class IncidentPlanInput {public int Id {get;set;}public string Version {get;set;}="";[Required(ErrorMessage="Nhập phương án đã chọn."),StringLength(1000)]public string Content {get;set;}="";}
public class IncidentResultInput {public int Id {get;set;}public string Version {get;set;}="";public string Outcome {get;set;}="Chưa giải quyết";[StringLength(2000)]public string? Resolution {get;set;}}
public class JourneyDetails {public int DayOffset {get;set;}public Trip Trip {get;set;}=new();public List<Incident> Incidents {get;set;}=[];public List<string> Warnings {get;set;}=[];public List<Place> Places {get;set;}=[];public List<TravelService> Services {get;set;}=[];public TripInput? DraftTrip {get;set;}public ActivityInput? DraftActivity {get;set;}}
public class JourneyIncidents {public List<Trip> Trips {get;set;}=[];public List<Incident> Incidents {get;set;}=[];public int? SelectedTripId {get;set;}public IncidentInput Draft {get;set;}=new();}
public class IncidentDetails {public Incident Incident {get;set;}=new();public Trip Trip {get;set;}=new();public List<TripActivity> Activities {get;set;}=[];public List<IncidentPlan> Plans {get;set;}=[];public IncidentPlanInput? DraftPlan {get;set;}public IncidentResultInput? DraftResult {get;set;}}
public record JourneyOutcome(bool Success,string Message,int Id=0,string Code="",int StatusCode=400);
public class ActivityEditor {public Trip Trip {get;set;}=new();public ActivityInput Input {get;set;}=new();public List<Place> Places {get;set;}=[];public List<TravelService> Services {get;set;}=[];}

