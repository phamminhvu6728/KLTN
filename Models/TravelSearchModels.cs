using System.ComponentModel.DataAnnotations;
namespace TuTuc.Models;
public class TravelSearchInput
{
    [StringLength(100)]public string? Keyword {get;set;}
    [StringLength(100)]public string? Region {get;set;}
    public string? Type {get;set;}
    [Range(0,1000000000)]public decimal? MinPrice {get;set;}
    [Range(0,1000000000)]public decimal? MaxPrice {get;set;}
    public string Sort {get;set;}="name";
}
public class TravelSearchItem
{
    public int Id {get;set;}
    public string SourceType {get;set;}="Place";
    public string Name {get;set;}="";
    public string Region {get;set;}="";
    public string Category {get;set;}="";
    public string Description {get;set;}="";
    public string Image {get;set;}="";
    public decimal Price {get;set;}
    public bool CanRequest {get;set;}
    public string State {get;set;}="Thông tin tham khảo";
    public string Url=>SourceType=="Place"?"/Home/Place/"+Id:"/Marketplace/Details/"+Id;
}
public class TravelSearchVm
{
    public TravelSearchInput Input {get;set;}=new();
    public List<TravelSearchItem> Items {get;set;}=[];
}
