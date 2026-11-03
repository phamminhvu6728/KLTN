using System.ComponentModel.DataAnnotations;
namespace TuTuc.Models;
public partial class Companion {public DateTime EndDate {get;set;} public string TravelStyle {get;set;}="Tự túc"; public string Wishes {get;set;}=""; public string SharingScope {get;set;}="Connected";public bool ShareContact {get;set;} public DateTime CreatedAt {get;set;}=DateTime.UtcNow; [Timestamp]public byte[] Version {get;set;}=[];}
public partial class Connection {public DateTime CreatedAt {get;set;}=DateTime.UtcNow;public DateTime? RespondedAt {get;set;} [Timestamp]public byte[] Version {get;set;}=[];}
