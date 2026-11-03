using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
namespace TuTuc.Models;
[Index(nameof(OwnerId),IsUnique=true)]
public class UserProfile {public int Id {get;set;} public int OwnerId {get;set;} public string Residence {get;set;}="";public string Interests {get;set;}="";public string SharingScope {get;set;}="Private";public bool ShareContact {get;set;}public int? AvatarDocumentId {get;set;}public DateTime UpdatedAt {get;set;}=DateTime.UtcNow;}
[Index(nameof(OwnerId),IsUnique=true)]
public class ProviderProfile {public int Id {get;set;}public int OwnerId {get;set;}public string CompanyName {get;set;}="";public string CompanyType {get;set;}="";public string ContactName {get;set;}="";public string Address {get;set;}="";public string ContactPhone {get;set;}="";public string ContactEmail {get;set;}="";[Timestamp]public byte[] Version {get;set;}=[];}
[Index(nameof(OwnerId),nameof(SubmittedAt))]
public class VerificationDossier {public int Id {get;set;}public int OwnerId {get;set;}public string Content {get;set;}="";public string Representative {get;set;}="";public string Status {get;set;}="Chờ xác minh";public int Submission {get;set;}public DateTime SubmittedAt {get;set;}=DateTime.UtcNow;[Timestamp]public byte[] Version {get;set;}=[];}
[Index(nameof(DossierId),nameof(Sequence),IsUnique=true)]
public class VerificationDecision {public int Id {get;set;}public int DossierId {get;set;}public int Sequence {get;set;}public int AdminId {get;set;}public string Outcome {get;set;}="";public string Reason {get;set;}="";public string Comment {get;set;}="";public DateTime CreatedAt {get;set;}=DateTime.UtcNow;}
[Index(nameof(StorageKey),IsUnique=true)]
[Index(nameof(OwnerId),nameof(Scope),nameof(ObjectId))]
public class PrivateDocument {public int Id {get;set;}public int OwnerId {get;set;}public string Scope {get;set;}="";public int ObjectId {get;set;}public string OriginalName {get;set;}="";[MaxLength(100)]public string StorageKey {get;set;}="";[MaxLength(60)]public string Mime {get;set;}="";public long Size {get;set;}public int Submission {get;set;}public DateTime CreatedAt {get;set;}=DateTime.UtcNow;}
public class VerificationDetailsVm {public Account Account {get;set;}=new();public ProviderProfile? Provider {get;set;}public VerificationDossier? Dossier {get;set;}public List<VerificationDossier> History {get;set;}=[];public List<DossierSubmission> Submissions {get;set;}=[];public List<VerificationDecision> Decisions {get;set;}=[];public List<PrivateDocument> Documents {get;set;}=[];public Dictionary<int,string> AdminNames {get;set;}=[];public VerificationSubmissionInput Submission {get;set;}=new();public VerificationDecisionInput Decision {get;set;}=new();public bool IsAdmin {get;set;}}
public class VerificationQueueVm {public List<VerificationDossier> Dossiers {get;set;}=[];public Dictionary<int,string> Names {get;set;}=[];public string Status {get;set;}="Chờ xác minh";}
public class VerificationSubmissionInput {[Required,StringLength(2000)]public string Content {get;set;}="";[Required,StringLength(100)]public string Representative {get;set;}="";public string? Version {get;set;} public List<IFormFile> Files {get;set;}=[];}
public class VerificationDecisionInput {public int Id {get;set;}[Required]public string Version {get;set;}="";[Required]public string Outcome {get;set;}="";[StringLength(500)]public string? Reason {get;set;}[StringLength(500)]public string? Comment {get;set;}}



