using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
namespace TuTuc.Models;
[Index(nameof(OperationId),IsUnique=true)]
public class GovernanceDecision
{
    public int Id {get;set;}
    public Guid OperationId {get;set;}
    public int OwnerId {get;set;}
    public int AdminId {get;set;}
    public string Action {get;set;}="";
    public string Before {get;set;}="";
    public string After {get;set;}="";
    public string Reason {get;set;}="";
    public string Evidence {get;set;}="";
    public DateTime? Until {get;set;}
    public DateTime CreatedAt {get;set;}=DateTime.UtcNow;
}
[Index(nameof(DecisionId),IsUnique=true)]
public class GovernanceReminder {public int Id {get;set;}public int DecisionId {get;set;}public DateTime CreatedAt {get;set;}=DateTime.UtcNow;}
public class ProviderExplanation
{
    public int Id {get;set;}
    public int OwnerId {get;set;}
    public int DecisionId {get;set;}
    public string Content {get;set;}="";
    public string Status {get;set;}="Chờ xem xét";
    public int? ReviewerId {get;set;}
    public string ReviewReason {get;set;}="";
    public string ReviewEvidence {get;set;}="";
    public DateTime CreatedAt {get;set;}=DateTime.UtcNow;
    public DateTime? ReviewedAt {get;set;}
    [Timestamp]public byte[] Version {get;set;}=[];
}
public class GovernanceInput
{
    public int ProviderId {get;set;}
    [Required]public string Action {get;set;}="Suspend";
    [Required,StringLength(2000)]public string Reason {get;set;}="";
    [Required,StringLength(3000)]public string Evidence {get;set;}="";
    public DateTime? Until {get;set;}
    [Required]public string Version {get;set;}="";
    public int? ExplanationId {get;set;}
}
public class GovernancePreview
{
    public Guid OperationId {get;set;}
    public GovernanceInput Input {get;set;}=new();
    public string ProviderName {get;set;}="";
    public string Before {get;set;}="";
    public string After {get;set;}="";
    public int ServiceCount {get;set;}
    public int PendingCount {get;set;}
    public int ConfirmedCount {get;set;}
    public DateTime ExpiresAt {get;set;}=DateTime.UtcNow.AddMinutes(5);
}
public class GovernanceList
{
    public List<Account> Providers {get;set;}=[];
    public List<ProviderExplanation> Explanations {get;set;}=[];
    public List<GovernanceDecision> Decisions {get;set;}=[];
    public string Filter {get;set;}="";
}
public class GovernanceDetail
{
    public Account Provider {get;set;}=new();
    public List<GovernanceDecision> Decisions {get;set;}=[];
    public List<ProviderExplanation> Explanations {get;set;}=[];
    public List<TravelService> Services {get;set;}=[];
    public List<Booking> Bookings {get;set;}=[];
    public List<PrivateDocument> Documents {get;set;}=[];
    public GovernanceInput Input {get;set;}=new();
    public bool IsAdmin {get;set;}
}
