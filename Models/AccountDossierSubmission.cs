using Microsoft.EntityFrameworkCore;
namespace TuTuc.Models;
[Index(nameof(DossierId),nameof(Submission),IsUnique=true)]
public class DossierSubmission {public int Id {get;set;}public int DossierId {get;set;}public int Submission {get;set;}public string Content {get;set;}="";public string Representative {get;set;}="";public DateTime SubmittedAt {get;set;}=DateTime.UtcNow;}
