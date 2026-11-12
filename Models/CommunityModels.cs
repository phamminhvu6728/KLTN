using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
namespace TuTuc.Models;
[Index(nameof(ConnectionId),nameof(Revision),IsUnique=true)]
public class CompanionAgreement
{
 public int Id {get;set;}public int ConnectionId {get;set;}public int Revision {get;set;}public int ProposerId {get;set;}public string Content {get;set;}="";public string Kind {get;set;}="Nội dung";public string Status {get;set;}="Chờ xác nhận";public DateTime CreatedAt {get;set;}=DateTime.UtcNow;
 public DateTime Start {get;set;}public DateTime End {get;set;}public string Destination {get;set;}="";public decimal? Budget {get;set;}public string CostSharing {get;set;}="";public string Scope {get;set;}="";public string Reason {get;set;}="";public int? RespondedBy {get;set;}public DateTime? RespondedAt {get;set;}public int? EndedBy {get;set;}public DateTime? EndedAt {get;set;}[Timestamp]public byte[] Version {get;set;}=[];
}
public class DemandInput
{
 public int Id {get;set;}[Required(ErrorMessage="Nhập điểm đến."),StringLength(200)]public string Destination {get;set;}="";[Required(ErrorMessage="Nhập nội dung nhu cầu."),StringLength(2000)]public string Description {get;set;}="";
 public DateTime Date {get;set;}=DateTime.Today.AddDays(7);public DateTime EndDate {get;set;}=DateTime.Today.AddDays(9);[Range(0,1000000000)]public decimal Budget {get;set;}[Range(1,20)]public int People {get;set;}=2;public string TravelStyle {get;set;}="Tự túc";[StringLength(1000)]public string? Wishes {get;set;}public string SharingScope {get;set;}="Connected";public bool ShareContact {get;set;}public string Version {get;set;}="";
}
public class CommunitySearch
{
 [StringLength(200)]public string? Q {get;set;}public DateTime? From {get;set;}public DateTime? To {get;set;}[Range(0,1000000000)]public decimal? MaxBudget {get;set;}public string? TravelStyle {get;set;}public int Page {get;set;}=1;
}
public class DemandCard
{
 public int Id {get;set;}public string Destination {get;set;}="";public DateTime Date {get;set;}public DateTime EndDate {get;set;}public string TravelStyle {get;set;}="";public string OwnerName {get;set;}="";public string? Description {get;set;}public decimal? Budget {get;set;}public string? Wishes {get;set;}public int? People {get;set;}public string? Bio {get;set;}public string? Residence {get;set;}public string? Interests {get;set;}public string? Email {get;set;}public string? Phone {get;set;}public bool Limited {get;set;}public bool Connected {get;set;}public bool CanConnect {get;set;}public string SharingScope {get;set;}="";
}
public class CommunityPage {public List<DemandCard> Demands {get;set;}=[];public CommunitySearch Search {get;set;}=new();public int Total {get;set;}public int Me {get;set;}public bool IsTraveler {get;set;}}
public class ConnectionItem {public Connection Connection {get;set;}=new();public Companion Demand {get;set;}=new();public string SenderName {get;set;}="";public string OtherName {get;set;}="";public string? OtherBio {get;set;}public string? OtherEmail {get;set;}public string? OtherPhone {get;set;}public string? OtherResidence {get;set;}public string? OtherInterests {get;set;}public DemandCard? Counterpart {get;set;}public List<CompanionAgreement> Agreements {get;set;}=[];}
public class CommunityDashboard {public int Me {get;set;}public List<Companion> Demands {get;set;}=[];public List<ConnectionItem> Connections {get;set;}=[];}
public class DemandPreview {public DemandInput Draft {get;set;}=new();public DemandCard RegisteredView {get;set;}=new();public DemandCard? PublicView {get;set;}public string Token {get;set;}="";}
public class AgreementInput
{
 public int ConnectionId {get;set;}public int BaseRevision {get;set;}public int? PendingId {get;set;}public string? PendingVersion {get;set;}
 public DateTime Start {get;set;}=DateTime.Today.AddDays(7);public DateTime End {get;set;}=DateTime.Today.AddDays(9);[Required(ErrorMessage="Nhập nơi đến."),StringLength(200)]public string Destination {get;set;}="";[Range(0,1000000000)]public decimal? Budget {get;set;}[Required(ErrorMessage="Nhập cách chia chi phí."),StringLength(1000)]public string CostSharing {get;set;}="";[Required(ErrorMessage="Nhập phạm vi đi cùng."),StringLength(1000)]public string Scope {get;set;}="";[StringLength(2000)]public string? Content {get;set;}
}
public class AgreementPage {public ConnectionItem Item {get;set;}=new();public AgreementInput Draft {get;set;}=new();}
public record CommunityOutcome(bool Success,string Message,int Id=0,int StatusCode=400,string Code="");

