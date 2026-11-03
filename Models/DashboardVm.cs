namespace TuTuc.Models;
public class DemoSeedMarker { public int Id {get;set;} public string Key {get;set;}=""; }
public class DashboardVm
{
    public int TripCount {get;set;}
    public int RequestCount {get;set;}
    public int PendingCount {get;set;}
    public int UnreadCount {get;set;}
    public List<Trip> Trips {get;set;}=[];
    public Trip? ActiveTrip {get;set;}
    public List<Trip> TripChoices {get;set;}=[];
    public List<Booking> Bookings {get;set;}=[];
    public List<UserNotification> Notifications {get;set;}=[];
}
