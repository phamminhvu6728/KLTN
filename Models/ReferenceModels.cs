using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
namespace TuTuc.Models;
[Index(nameof(OperationId),IsUnique=true)]
public class SavedReferenceList {
 public int Id {get;set;} public int OwnerId {get;set;}
 [MaxLength(150)]public string Name {get;set;}="";
 public Guid OperationId {get;set;}
 public DateTime CreatedAt {get;set;}=DateTime.UtcNow;
 public List<SavedReferenceItem> Items {get;set;}=[];
}
public class SavedReferenceItem {
 public int Id {get;set;} public int SavedReferenceListId {get;set;}
 public int PlaceId {get;set;} public Place? Place {get;set;}
}
public class ReferenceLibraryViewModel {public List<Place> Temporary {get;set;}=[];public List<SavedReferenceList> Saved {get;set;}=[];}
